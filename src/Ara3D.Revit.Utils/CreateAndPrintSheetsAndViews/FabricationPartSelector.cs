using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreateAndPrintSheetsAndViews
{
  /// <summary>
  /// Retrieve pre-selected or prompt user 
  /// to select single fabrication parts
  /// </summary>
  class FabricationPartSelector
  {
    List<ElementId> _ids;

    public FabricationPartSelector(UIDocument uidoc)
    {
      var doc = uidoc.Document;
      var sel = uidoc.Selection;

      _ids = new List<ElementId>(
        sel.GetElementIds().Where(
          id => doc.GetElement(id) is FabricationPart));

      var n = _ids.Count;

      while (0 == n)
      {
        try
        {
          var refs = sel.PickObjects(
            ObjectType.Element,
            new FabricationPartSelectionFilter(),
            "Please select fabrication part duct elements");

          _ids = new List<ElementId>(
            refs.Select(
              r => r.ElementId));

          n = _ids.Count;
        }
        catch (OperationCanceledException)
        {
          _ids.Clear();
          break;
        }
      }
    }

    public List<ElementId> Ids => _ids;
  }
}
