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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace Revit.SDK.Samples.AutoTagRooms.CS
{
    /// <summary>
    /// The graphic user interface of auto tag rooms
    /// </summary>
    public partial class AutoTagRoomsForm : System.Windows.Forms.Form
    {
        /// <summary>
        /// Default constructor of AutoTagRoomsForm
        /// </summary>
        private AutoTagRoomsForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor of AutoTagRoomsForm
        /// </summary>
        /// <param name="roomsData">The data source of AutoTagRoomsForm</param>
        public AutoTagRoomsForm(RoomsData roomsData) : this()
        {
            m_roomsData = roomsData;
            InitRoomListView();
        }

        /// <summary>
        /// When the AutoTagRoomsForm is loading, initialize the levelsComboBox and tagTypesComboBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoTagRoomsForm_Load(object sender, EventArgs e)
        {
            // levelsComboBox
            levelsComboBox.DataSource = m_roomsData.Levels;
            levelsComboBox.DisplayMember = "Name";
            levelsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            levelsComboBox.Sorted = true;
            levelsComboBox.DropDown += levelsComboBox_DropDown;

            // tagTypesComboBox
            tagTypesComboBox.DataSource = m_roomsData.RoomTagTypes;
            tagTypesComboBox.DisplayMember = "Name";
            tagTypesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            tagTypesComboBox.DropDown += tagTypesComboBox_DropDown;
        }

        /// <summary>
        /// When the tagTypesComboBox drop down, adjust its width
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void tagTypesComboBox_DropDown(object sender, EventArgs e)
        {
            AdjustWidthComboBox_DropDown(sender, e);
        }

        /// <summary>
        /// When the levelsComboBox drop down, adjust its width
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void levelsComboBox_DropDown(object sender, EventArgs e)
        {
            AdjustWidthComboBox_DropDown(sender, e);
        }

        /// <summary>
        /// Initialize the roomsListView 
        /// </summary>
        private void InitRoomListView()
        {
            roomsListView.Columns.Clear();

            // Create the columns of the roomsListView
            roomsListView.Columns.Add("Room Name");
            foreach (var type in m_roomsData.RoomTagTypes)
            {
                roomsListView.Columns.Add(type.Name);
            }

            roomsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            roomsListView.FullRowSelect = true;
        }

        /// <summary>
        /// Update the rooms information in the specified level
        /// </summary>
        private void UpdateRoomsList()
        {
            // when update the RoomsListView, clear all the items first
            roomsListView.Items.Clear();

            foreach (var tmpRoom in m_roomsData.Rooms)
            {
                var level = levelsComboBox.SelectedItem as Level;
                
                if (tmpRoom.LevelId == level.Id)
                {
                    var item = new ListViewItem(tmpRoom.Name);

                    // Shows the number of each type of RoomTags that the room has
                    foreach (var type in m_roomsData.RoomTagTypes)
                    {
                        var count = m_roomsData.GetTagNumber(tmpRoom, type);
                        var str = count.ToString();
                        item.SubItems.Add(str);
                    }

                    roomsListView.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// When clicked the autoTag button, then tag all rooms in the specified level. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autoTagButton_Click(object sender, EventArgs e)
        {
            var level = levelsComboBox.SelectedItem as Level;
            var tagType = tagTypesComboBox.SelectedItem as RoomTagType;
            if (level != null && tagType != null)
            {
                m_roomsData.AutoTagRooms(level, tagType);
            }

            UpdateRoomsList();
        }

        /// <summary>
        /// When selected different level, then update the roomsListView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void levelsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateRoomsList();
        }


        /// <summary>
        /// Adjust combo box drop down list width to longest string width
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AdjustWidthComboBox_DropDown(object sender, EventArgs e)
        {
            var senderComboBox = (ComboBox)sender;
            var width = senderComboBox.DropDownWidth;
            var g = senderComboBox.CreateGraphics();
            var font = senderComboBox.Font;
            var vertScrollBarWidth =
                (senderComboBox.Items.Count > senderComboBox.MaxDropDownItems)
                ? SystemInformation.VerticalScrollBarWidth : 0;

            int newWidth;
            foreach (Element element in ((ComboBox)sender).Items)
            {
                var s = element.Name;
                newWidth = (int)g.MeasureString(s, font).Width
                    + vertScrollBarWidth;
                if (width < newWidth)
                {
                    width = newWidth;
                }
            }
            senderComboBox.DropDownWidth = width;
        }
    }
}