//
// (C) Copyright 2003-2019 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE. AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is subject to
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.
// 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Mechanical;

namespace Revit.SDK.Samples.AddSpaceAndZone.CS
{
    /// <summary>
    /// The DataManager Class is used to obtain, create or edit the Space elements and Zone elements.
    /// </summary>
    public class DataManager
    {
        ExternalCommandData m_commandData;
        List<Level>  m_levels;
        SpaceManager m_spaceManager;
        ZoneManager m_zoneManager;
        Level m_currentLevel;
        Phase m_defaultPhase;

        /// <summary>
        /// The constructor of DataManager class.
        /// </summary>
        /// <param name="commandData">The ExternalCommandData</param>
        public DataManager(ExternalCommandData commandData)
        {
            m_commandData = commandData;
            m_levels = new List<Level>();
            Initialize();
            m_currentLevel = m_levels[0];
            var para = commandData.Application.ActiveUIDocument.Document.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE);
            var phaseId = para.AsElementId();
            m_defaultPhase = commandData.Application.ActiveUIDocument.Document.GetElement(phaseId) as Phase;
        }

        /// <summary>
        /// Initialize the data member, obtain the Space and Zone elements.
        /// </summary>
        private void Initialize()
        {
            var spaceDictionary = new Dictionary<ElementId, List<Space>>();
            var zoneDictionary = new Dictionary<ElementId, List<Zone>>();

            var activeDoc = m_commandData.Application.ActiveUIDocument.Document;

            var levelsIterator = (new FilteredElementCollector(activeDoc)).OfClass(typeof(Level)).GetElementIterator();
            var spacesIterator =(new FilteredElementCollector(activeDoc)).WherePasses(new SpaceFilter()).GetElementIterator();
            var zonesIterator = (new FilteredElementCollector(activeDoc)).OfClass(typeof(Zone)).GetElementIterator();
          
            levelsIterator.Reset();
            while (levelsIterator.MoveNext())
            {
                var level = levelsIterator.Current as Level;
                if (level != null)
                {
                    m_levels.Add(level);
                    spaceDictionary.Add(level.Id, new List<Space>());
                    zoneDictionary.Add(level.Id, new List<Zone>());
                }
            }

            spacesIterator.Reset();
            while (spacesIterator.MoveNext())
            {
                var space = spacesIterator.Current as Space;
                if (space != null)
                {
                    spaceDictionary[space.LevelId].Add(space);
                }
            }

            zonesIterator.Reset();
            while (zonesIterator.MoveNext())
            {
                var zone = zonesIterator.Current as Zone;
                if (zone != null && activeDoc.GetElement(zone.LevelId) != null)
                {
                    zoneDictionary[zone.LevelId].Add(zone);
                }
            }

            m_spaceManager = new SpaceManager(m_commandData, spaceDictionary);
            m_zoneManager = new ZoneManager(m_commandData, zoneDictionary);
        }

        /// <summary>
        /// Get the Level elements.
        /// </summary>
        public ReadOnlyCollection<Level> Levels => new(m_levels);

        /// <summary>
        /// Create a Zone element.
        /// </summary>
        public void CreateZone()
        {
            if (m_defaultPhase == null)
            {
                TaskDialog.Show("Revit", "The phase of the active view is null, you can't create zone in a null phase");
                return;
            }
            try
            {
                m_zoneManager.CreateZone(m_currentLevel, m_defaultPhase);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Revit", ex.Message);
            }         
        }

        /// <summary>
        /// Create some spaces.
        /// </summary>
        public void CreateSpaces()
        {           
            if (m_defaultPhase == null)
            {
                TaskDialog.Show("Revit", "The phase of the active view is null, you can't create spaces in a null phase");
                return;
            }

            try
            {
                if (m_commandData.Application.ActiveUIDocument.Document.ActiveView.ViewType == ViewType.FloorPlan)
                {
                    m_spaceManager.CreateSpaces(m_currentLevel, m_defaultPhase);
                }
                else
                {
                    TaskDialog.Show("Revit", "You can not create spaces in this plan view");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Revit", ex.Message);
            }               
        }

        /// <summary>
        /// Get the Space elements.
        /// </summary>
        /// <returns>A space list in current level.</returns>
        public List<Space> GetSpaces()
        {
            return m_spaceManager.GetSpaces(m_currentLevel);
        }

        /// <summary>
        /// Get the Zone elements.
        /// </summary>
        /// <returns>A Zone list in current level.</returns>
        public List<Zone> GetZones()
        {
            return m_zoneManager.GetZones(m_currentLevel);
        }

        /// <summary>
        /// Update the current level.
        /// </summary>
        /// <param name="level"></param>
        public void Update(Level level)
        {
            m_currentLevel = level;
        }
    }
}
