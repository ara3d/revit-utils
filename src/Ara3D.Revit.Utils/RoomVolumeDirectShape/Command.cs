//#define USE_FACE_TRIANGULATION
#define CREATE_NEW_SOLID_USING_TESSELATION

#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.IO;
#endregion

namespace RoomVolumeDirectShape
{
  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
    // Cannot use OST_Rooms; DirectShape.CreateElement 
    // throws ArgumentExceptionL: Element id categoryId 
    // may not be used as a DirectShape category.

    /// <summary>
    /// Category assigned to the room volume direct shape
    /// </summary>
    ElementId _id_category_for_direct_shape
      = new ElementId( BuiltInCategory.OST_GenericModel );

    /// <summary>
    /// DirectShape parameter to populate with JSON
    /// dictionary containing all room properies
    /// </summary>
    BuiltInParameter _bip_properties
      = BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS;

    /// <summary>
    /// Return a JSON string representing a dictionary
    /// mapping string key to string value.
    /// </summary>
    static string FormatDictAsJson(
      Dictionary<string, string> d )
    {
      var keys = new List<string>( d.Keys );
      keys.Sort();

      var key_vals = new List<string>(
        keys.Count );

      foreach( var key in keys )
      {
        key_vals.Add(
          string.Format( "\"{0}\" : \"{1}\"",
            key, d[key] ) );
      }
      return "{" + string.Join( ", ", key_vals ) + "}";
    }

    /// <summary>
    /// Return parameter storage type abbreviation
    /// </summary>
    static char ParameterStorageTypeChar(
      Parameter p )
    {
      if( null == p )
      {
        throw new ArgumentNullException(
          "p", "expected non-null parameter" );
      }

      var abbreviation = '?';

      switch( p.StorageType )
      {
        case StorageType.Double:
          abbreviation = 'r'; // real number
          break;
        case StorageType.Integer:
          abbreviation = 'n'; // integer number
          break;
        case StorageType.String:
          abbreviation = 's'; // string
          break;
        case StorageType.ElementId:
          abbreviation = 'e'; // element id
          break;
        case StorageType.None:
          throw new ArgumentOutOfRangeException(
            "p", "expected valid parameter "
            + "storage type, not 'None'" );
      }
      return abbreviation;
    }

    /// <summary>
    /// Return parameter value formatted as string
    /// </summary>
    static string ParameterToString( Parameter p )
    {
      var s = "null";

      if( p != null )
      {
        switch( p.StorageType )
        {
          case StorageType.Double:
            s = p.AsDouble().ToString( "0.##" );
            break;
          case StorageType.Integer:
            s = p.AsInteger().ToString();
            break;
          case StorageType.String:
            s = p.AsString();
            break;
          case StorageType.ElementId:
            s = p.AsElementId().IntegerValue.ToString();
            break;
          case StorageType.None:
            s = "none";
            break;
        }
      }
      return s;
    }

    /// <summary>
    /// Return all the element parameter values in a
    /// dictionary mapping parameter names to values
    /// </summary>
    static Dictionary<string, string> GetParamValues(
      Element e )
    {
      // Two choices: 
      // Element.Parameters property -- Retrieves 
      // a set containing all the parameters.
      // GetOrderedParameters method -- Gets the 
      // visible parameters in order.

      //IList<Parameter> ps = e.GetOrderedParameters();

      var pset = e.Parameters;

      var d
        = new Dictionary<string, string>( pset.Size );

      foreach( Parameter p in pset )
      {
        // AsValueString displays the value as the 
        // user sees it. In some cases, the underlying
        // database value returned by AsInteger, AsDouble,
        // etc., may be more relevant, as done by 
        // ParameterToString

        var key = string.Format( "{0}({1})",
          p.Definition.Name,
          ParameterStorageTypeChar( p ) );

        var val = ParameterToString( p );

        if( d.ContainsKey( key ) )
        {
          if( d[key] != val )
          {
            d[key] += " | " + val;
          }
        }
        else
        {
          d.Add( key, val );
        }
      }
      return d;
    }

    static string GetRoomPropertiesJson( Room r )
    {
      var param_values
        = GetParamValues( r );

      // These room properties are all stored in 
      // parameters and therefore already captured

      //double baseOffset = r.BaseOffset;
      //double limitOffset = r.LimitOffset;
      //double unboundedHeight = r.UnboundedHeight;
      //string upperLimit = r.UpperLimit.Name;
      //double volume = r.Volume;

      return FormatDictAsJson( param_values );
    }

    /// <summary>
    /// XYZ equality comparer to eliminate 
    /// slightly differing vertices
    /// </summary>
    class XyzEqualityComparer : IEqualityComparer<XYZ>
    {
      /// <summary>
      /// Tolerance. 
      /// 0.003 imperial feet is ca. 0.9 mm
      /// </summary>
      double _tol = 0.003;

      public bool Equals( XYZ a, XYZ b )
      {
        return _tol > a.DistanceTo( b );
      }

      public int GetHashCode( XYZ a )
      {
        var format = "0.####";
        var s = a.X.ToString( format )
                + "," + a.Y.ToString( format )
                + "," + a.Z.ToString( format );
        return s.GetHashCode();
      }
    }

    /// <summary>
    /// Create a new list of geometry objects from the 
    /// given input. As input, we supply the result of 
    /// Room.GetClosedShell. The output is the exact 
    /// same solid lacking whatever flaws are present 
    /// in the input solid.
    /// </summary>
    static IList<GeometryObject> CopyGeometry(
      GeometryElement geo,
      ElementId materialId,
      List<IntPoint3d> coords,
      List<TriangleIndices> indices )
    {
      TessellatedShapeBuilderResult result = null;

      var builder
        = new TessellatedShapeBuilder();

      // Need to include the key in the value, otherwise
      // no way to access it later, cf.
      // https://stackoverflow.com/questions/1619090/getting-a-keyvaluepair-directly-from-a-dictionary

      var pts
        = new Dictionary<XYZ, KeyValuePair<XYZ, int>>(
          new XyzEqualityComparer() );

      var nSolids = 0;
      //int nFaces = 0;
      var nTriangles = 0;
      //int nVertices = 0;
      var vertices = new List<XYZ>( 3 );

      foreach( var obj in geo )
      {
        var solid = obj as Solid;

        if( null != solid )
        {
          if( 0 < solid.Volume )
          {
            ++nSolids;

            builder.OpenConnectedFaceSet( false );

            #region Create a new solid based on tessellation of the invalid room closed shell solid
#if CREATE_NEW_SOLID_USING_TESSELATION

            Debug.Assert(
              SolidUtils.IsValidForTessellation( solid ),
              "expected a valid solid for room closed shell" );

            var controls
              = new SolidOrShellTessellationControls()
              {
                //
                // Summary:
                //     A positive real number specifying how accurately a triangulation should approximate
                //     a solid or shell.
                //
                // Exceptions:
                //   T:Autodesk.Revit.Exceptions.ArgumentOutOfRangeException:
                //     When setting this property: The given value for accuracy must be greater than
                //     0 and no more than 30000 feet.
                // This statement is not true. I set Accuracy = 0.003 and an exception was thrown.
                // Setting it to 0.006 was acceptable. 0.03 is a bit over 9 mm.
                //
                // Remarks:
                //     The maximum distance from a point on the triangulation to the nearest point on
                //     the solid or shell should be no greater than the specified accuracy. This constraint
                //     may be approximately enforced.
                Accuracy = 0.03,
                //
                // Summary:
                //     An number between 0 and 1 (inclusive) specifying the level of detail for the
                //     triangulation of a solid or shell.
                //
                // Exceptions:
                //   T:Autodesk.Revit.Exceptions.ArgumentOutOfRangeException:
                //     When setting this property: The given value for levelOfDetail must lie between
                //     0 and 1 (inclusive).
                //
                // Remarks:
                //     Smaller values yield coarser triangulations (fewer triangles), while larger values
                //     yield finer triangulations (more triangles).
                LevelOfDetail = 0.1,
                //
                // Summary:
                //     A non-negative real number specifying the minimum allowed angle for any triangle
                //     in the triangulation, in radians.
                //
                // Exceptions:
                //   T:Autodesk.Revit.Exceptions.ArgumentOutOfRangeException:
                //     When setting this property: The given value for minAngleInTriangle must be at
                //     least 0 and less than 60 degrees, expressed in radians. The value 0 means to
                //     ignore the minimum angle constraint.
                //
                // Remarks:
                //     A small value can be useful when triangulating long, thin objects, in order to
                //     keep the number of triangles small, but it can result in long, thin triangles,
                //     which are not acceptable for all applications. If the value is too large, this
                //     constraint may not be satisfiable, causing the triangulation to fail. This constraint
                //     may be approximately enforced. A value of 0 means to ignore the minimum angle
                //     constraint.
                MinAngleInTriangle = 3 * Math.PI / 180.0,
                //
                // Summary:
                //     A positive real number specifying the minimum allowed value for the external
                //     angle between two adjacent triangles, in radians.
                //
                // Exceptions:
                //   T:Autodesk.Revit.Exceptions.ArgumentOutOfRangeException:
                //     When setting this property: The given value for minExternalAngleBetweenTriangles
                //     must be greater than 0 and no more than 30000 feet.
                //
                // Remarks:
                //     A small value yields more smoothly curved triangulated surfaces, usually at the
                //     expense of an increase in the number of triangles. Note that this setting has
                //     no effect for planar surfaces. This constraint may be approximately enforced.
                MinExternalAngleBetweenTriangles = 0.2 * Math.PI
              };

            var shell
              = SolidUtils.TessellateSolidOrShell( solid, controls );

            var n = shell.ShellComponentCount;

            Debug.Assert( 1 == n,
              "expected just one shell component in room closed shell" );

            var component
              = shell.GetShellComponent( 0 );

            var coordsBase = coords.Count;
            var indicesBase = indices.Count;

            n = component.VertexCount;

            for( var i = 0; i < n; ++i )
            {
              var v = component.GetVertex( i );
              coords.Add( new IntPoint3d( v ) );
            }

            n = component.TriangleCount;

            for( var i = 0; i < n; ++i )
            {
              var t
                = component.GetTriangle( i );

              vertices.Clear();

              vertices.Add( component.GetVertex( t.VertexIndex0 ) );
              vertices.Add( component.GetVertex( t.VertexIndex1 ) );
              vertices.Add( component.GetVertex( t.VertexIndex2 ) );

              indices.Add( new TriangleIndices(
                coordsBase + t.VertexIndex0,
                coordsBase + t.VertexIndex1,
                coordsBase + t.VertexIndex2 ) );

              var tf = new TessellatedFace(
                vertices, materialId );

              if( builder.DoesFaceHaveEnoughLoopsAndVertices( tf ) )
              {
                builder.AddFace( tf );
                ++nTriangles;
              }
            }
#else
            // Iterate over the individual solid faces

            foreach( Face f in solid.Faces )
            {
              vertices.Clear();

            #region Use face triangulation
#if USE_FACE_TRIANGULATION

              Mesh mesh = f.Triangulate();
              int n = mesh.NumTriangles;

              for( int i = 0; i < n; ++i )
              {
                MeshTriangle triangle = mesh.get_Triangle( i );

                XYZ p1 = triangle.get_Vertex( 0 );
                XYZ p2 = triangle.get_Vertex( 1 );
                XYZ p3 = triangle.get_Vertex( 2 );

                vertices.Clear();
                vertices.Add( p1 );
                vertices.Add( p2 );
                vertices.Add( p3 );

                TessellatedFace tf
                  = new TessellatedFace(
                    vertices, materialId );

                if( builder.DoesFaceHaveEnoughLoopsAndVertices( tf ) )
                {
                  builder.AddFace( tf );
                  ++nTriangles;
                }
              }
#endif // USE_FACE_TRIANGULATION
            #endregion // Use face triangulation

            #region Use original solid and its EdgeLoops
#if USE_EDGELOOPS
              // This returns arbitrarily ordered and 
              // oriented edges, so no solid can be 
              // generated.

              foreach( EdgeArray loop in f.EdgeLoops )
              {
                foreach( Edge e in loop )
                {
                  XYZ p = e.AsCurve().GetEndPoint( 0 );
                  XYZ q = p;

                  if( pts.ContainsKey( p ) )
                  {
                    KeyValuePair<XYZ, int> kv = pts[p];
                    q = kv.Key;
                    int n = kv.Value;
                    pts[p] = new KeyValuePair<XYZ, int>(
                      q, ++n );

                    Debug.Print( "Ignoring vertex at {0} "
                      + "with distance {1} to existing "
                      + "vertex {2}",
                      p, p.DistanceTo( q ), q );
                  }
                  else
                  {
                    pts[p] = new KeyValuePair<XYZ, int>(
                      p, 1 );
                  }

                  vertices.Add( q );
                  ++nVertices;
                }
              }
#endif // USE_EDGELOOPS
            #endregion // Use original solid and its EdgeLoops

            #region Use original solid and GetEdgesAsCurveLoops
#if USE_AS_CURVE_LOOPS

              // The solids generated by this have some weird 
              // normals, so they do not render correctly in 
              // the Forge viewer. Revert to triangles again.

              IList<CurveLoop> loops 
                = f.GetEdgesAsCurveLoops();

              foreach( CurveLoop loop in loops )
              {
                foreach( Curve c in loop )
                {
                  XYZ p = c.GetEndPoint( 0 );
                  XYZ q = p;

                  if( pts.ContainsKey( p ) )
                  {
                    KeyValuePair<XYZ, int> kv = pts[p];
                    q = kv.Key;
                    int n = kv.Value;
                    pts[p] = new KeyValuePair<XYZ, int>(
                      q, ++n );

                    Debug.Print( "Ignoring vertex at {0} "
                      + "with distance {1} to existing "
                      + "vertex {2}",
                      p, p.DistanceTo( q ), q );
                  }
                  else
                  {
                    pts[p] = new KeyValuePair<XYZ, int>(
                      p, 1 );
                  }

                  vertices.Add( q );
                  ++nVertices;
                }
              }
#endif // USE_AS_CURVE_LOOPS
            #endregion // Use original solid and GetEdgesAsCurveLoops

              builder.AddFace( new TessellatedFace(
                vertices, materialId ) );

              ++nFaces;
            }

#endif // CREATE_NEW_SOLID_USING_TESSELATION
            #endregion // Create a new solid based on tessellation of the invalid room closed shell solid

            builder.CloseConnectedFaceSet();
            builder.Target = TessellatedShapeBuilderTarget.AnyGeometry; // Solid failed
            builder.Fallback = TessellatedShapeBuilderFallback.Mesh; // use Abort if target is Solid
            builder.Build();
            result = builder.GetBuildResult();

            // Debug printout log of current solid's glTF facet data

            n = coords.Count - coordsBase;            

            Debug.Print( "{0} glTF vertex coordinates "
              + "in millimetres:", n );

            Debug.Print( string.Join( " ", coords
              .TakeWhile( ( p, i ) => coordsBase <= i )
              .Select( p => p.ToString() ) ) );

            n = indices.Count - indicesBase;

            Debug.Print( "{0} glTF triangles:", n );

            Debug.Print( string.Join( " ", indices
              .TakeWhile( ( ti, i ) => indicesBase <= i )
              .Select( ti => ti.ToString() ) ) );
          }
        }
      }
      return result.GetGeometricalObjects();
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      var uiapp = commandData.Application;
      var uidoc = uiapp.ActiveUIDocument;
      var app = uiapp.Application;
      var doc = uidoc.Document;

      var id_addin = uiapp.ActiveAddInId.GetGUID()
        .ToString();

      var rooms
        = new FilteredElementCollector( doc )
        .WhereElementIsNotElementType()
        .OfClass( typeof( SpatialElement ) )
        .Where( e => e.GetType() == typeof( Room ) )
        .Cast<Room>();

      // Collect room data for glTF export

      var room_data = new List<GltfNodeData>(
        rooms.Count() );

      // Collect geometry data for glTF: a list of 
      // vertex coordinates in millimetres, and a list 
      // of triangle vertex indices into the coord list.

      var gltf_coords = new List<IntPoint3d>();
      var gltf_indices = new List<TriangleIndices>();

      using( var tx = new Transaction( doc ) )
      {
        tx.Start( "Generate Direct Shape Elements "
          + "Representing Room Volumes" );

        foreach( var r in rooms )
        {
          Debug.Print( "Processing "
            + r.Name + "..." );

          // Collect data for current room

          var rd = new GltfNodeData( r );

          var geo = r.ClosedShell;

          Debug.Assert(
            geo.First() is Solid,
            "expected a solid for room closed shell" );

          var solid = geo.First() as Solid;

          #region Fix the shape
#if FIX_THE_SHAPE_SOMEHOW
          // Create IList step by step

          Solid solid = geo.First<GeometryObject>() 
            as Solid;

          // The room closed shell solid faces have a 
          // non-null graphics style id: 
          // Interior Fill 106074
          // The sphere faces' graphics style id is null. 
          // Maybe this graphics style does something 
          // weird in the Forge viewer?
          // Let's create a copy of the room solid and 
          // see whether that resets the graphics style.

          solid = SolidUtils.CreateTransformed( 
            solid, Transform.Identity );

          shape = new GeometryObject[] { solid };

          // Create a sphere

          var center = XYZ.Zero;
          double radius = 2.0;

          var p = center + radius * XYZ.BasisY;
          var q = center - radius * XYZ.BasisY;

          var profile = new List<Curve>();
          profile.Add( Line.CreateBound( p, q ) );
          profile.Add( Arc.Create( q, p, 
            center + radius * XYZ.BasisX ) );

          var curveLoop = CurveLoop.Create( profile );

          var options = new SolidOptions( 
            ElementId.InvalidElementId, // material
            ElementId.InvalidElementId ); // graphics style

          var frame = new Frame( center, 
            XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY );

          var sphere = GeometryCreationUtilities
            .CreateRevolvedGeometry( frame, 
              new CurveLoop[] { curveLoop }, 
              0, 2 * Math.PI, options );

          shape = new GeometryObject[] { solid, sphere };
#endif // #if FIX_THE_SHAPE_SOMEHOW
          #endregion // Fix the shape

          IList<GeometryObject> shape
            = geo.ToList();

          // Previous counts define offsets 
          // to new binary data

          rd.CoordinatesBegin = gltf_coords.Count;
          rd.TriangleVertexIndicesBegin
            = gltf_indices.Count;

          // Create a new solid to use for the direct 
          // shape from the flawed solid returned by
          // GetClosedShell and gather glTF facet data

          shape = CopyGeometry( geo,
            ElementId.InvalidElementId,
            gltf_coords, gltf_indices );

          rd.CoordinatesCount = gltf_coords.Count 
            - rd.CoordinatesBegin;

          rd.TriangleVertexIndexCount 
            = gltf_indices.Count 
              - rd.TriangleVertexIndicesBegin;

          var pts
            = gltf_coords.Skip( 
              rd.CoordinatesBegin );

          rd.Min = new IntPoint3d(
            pts.Min<IntPoint3d, int>( p => p.X ),
            pts.Min<IntPoint3d, int>( p => p.Y ),
            pts.Min<IntPoint3d, int>( p => p.Z ) );
          rd.Max = new IntPoint3d(
            pts.Max<IntPoint3d, int>( p => p.X ),
            pts.Max<IntPoint3d, int>( p => p.Y ),
            pts.Max<IntPoint3d, int>( p => p.Z ) );

          var param_values
            = GetParamValues( r );

          var json = FormatDictAsJson( param_values );

          var ds = DirectShape.CreateElement(
            doc, _id_category_for_direct_shape );

          ds.ApplicationId = id_addin;
          ds.ApplicationDataId = r.UniqueId;
          ds.SetShape( shape );
          ds.get_Parameter( _bip_properties ).Set( json );
          ds.Name = "Room volume for " + r.Name;
          room_data.Add( rd );
        }
        tx.Commit();
      }

      // Save glTF text and binary data to two files; 
      // metadata, min, max, buffer information; 
      // vertex coordinates and triangle indices

      // Path.GetTempPath() returns a weird subdirectory 
      // created by Revit, so we will not use that here, e.g.,
      // C:\Users\tammikj\AppData\Local\Temp\bfd59506-2dff-4b0f-bbe4-31587fcaf508

      //string path = Path.GetTempPath();

      var path = "C:/tmp";

      path = Path.Combine( path, doc.Title + "_gltf" );

      using( var s = new StreamWriter( 
        path + ".txt" ) )
      {
        var n = room_data.Count;

        s.WriteLine( "{0} room{1}", n, ( ( 1 == n ) ? "" : "s" ) );
        s.WriteLine( "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
          "id", // "ElementId",
          "uid", // "UniqueId",
          "name", // "RoomName",
          "min",
          "max",
          "coord begin", // "CoordinatesBegin",
          "count", // "CoordinatesCount",
          "indices begin", // "TriangleVertexIndicesBegin",
          "count" ); // "TriangleVertexIndexCount"

        foreach( var rd in room_data )
        {
          s.WriteLine( rd.ToString() );
        }
      }

      using( var f = File.Create( path + ".bin" ) )
      {
        using( var writer = new BinaryWriter( f ) )
        {
          foreach( var p in gltf_coords )
          {
            writer.Write( (float) p.X );
            writer.Write( (float) p.Y );
            writer.Write( (float) p.Z );
          }
          foreach( var ti in gltf_indices )
          {
            foreach( var i in ti.Indices )
            {
              Debug.Assert( ushort.MaxValue > i,
                "expected vertex index to fit into unsigned short" );

              writer.Write( (ushort) i );
            }
          }
        }
      }
      return Result.Succeeded;
    }
  }
}
