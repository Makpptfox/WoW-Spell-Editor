﻿using SpellEditor.Sources.BLP;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace SpellEditor.Sources.DBC
{
    class SpellIconDBC : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        private static bool loadedAllIcons = false;
        private double? iconSize = null;
        private Thickness? iconMargin = null;

        public List<Icon_DBC_Lookup> Lookups = new List<Icon_DBC_Lookup>();

        public SpellIconDBC(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/SpellIcon.dbc");
                var paths = new List<string>((int)Header.RecordCount);
                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    uint offset = (uint)record["Name"];
                    if (offset == 0)
                        continue;
                    string name = Reader.LookupStringOffset(offset);
                    paths.Add(name + ".blp");
                    uint id = (uint)record["ID"];

                    Icon_DBC_Lookup lookup;
                    lookup.ID = id;
                    lookup.Offset = offset;
                    lookup.Name = name;
                    Lookups.Add(lookup);
                }
                Reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                Reader = null;
                Body = null;
                BlpManager.GetInstance().LoadIcons(paths);
            }
            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);
                return;
            }
        }

        public void LoadImages(double margin)
        {
            UpdateMainWindowIcons(margin);
        }

        public void updateIconSize(double newSize, Thickness margin)
        {
            iconSize = newSize;
            iconMargin = margin;
        }

        private class Worker : BackgroundWorker
        {
            public IDatabaseAdapter _adapter;

            public Worker(IDatabaseAdapter _adapter)
            {
                this._adapter = _adapter;
            }
        }

        public void UpdateMainWindowIcons(double margin)
        {
            // adapter.query below caused unhandled exception with main.selectedID as 0.
            if (adapter == null || main.selectedID == 0)
                return;
            
            // Convert to background worker here

            DataRow res = adapter.Query(string.Format("SELECT `SpellIconID`,`ActiveIconID` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0];
            uint iconInt = uint.Parse(res[0].ToString());
            uint iconActiveInt = uint.Parse(res[1].ToString());
            var path = GetIconPath(iconInt) + ".blp";
            // Update currently selected icon, we don't currently use ActiveIconID
            System.Windows.Controls.Image temp = new System.Windows.Controls.Image();

            temp.Width = iconSize == null ? 32 : iconSize.Value;
            temp.Height = iconSize == null ? 32 : iconSize.Value;
            temp.Margin = iconMargin == null ? new Thickness(margin, 0, 0, 0) : iconMargin.Value;
            temp.VerticalAlignment = VerticalAlignment.Top;
            temp.HorizontalAlignment = HorizontalAlignment.Left;
            temp.Source = BlpManager.GetInstance().GetSourceForImagePath(path);
            temp.Name = "CurrentSpellIcon";

            // Code smells here on hacky positioning and updating the icon
            temp.Margin = new Thickness(103, 38, 0, 0);
            main.CurrentIconGrid.Children.Clear();
            main.CurrentIconGrid.Children.Add(temp);
            
            // Load all icons available if have not already
            if (!loadedAllIcons)
            {
                var watch = new Stopwatch();
                watch.Start();
                LoadAllIcons(margin);
                watch.Stop();
                Console.WriteLine($"Loaded all icons as UI elements in {watch.ElapsedMilliseconds}ms");
            }
        }

        public void LoadAllIcons(double margin)
        {
            loadedAllIcons = true;
            var blpManager = BlpManager.GetInstance();
            foreach (var entry in Lookups)
            {
                var path = entry.Name + ".blp";
                var source = blpManager.GetSourceForImagePath(path);
                if (source == null)
                    continue;
                System.Windows.Controls.Image temp = new System.Windows.Controls.Image();
                temp.Width = iconSize == null ? 32 : iconSize.Value;
                temp.Height = iconSize == null ? 32 : iconSize.Value;
                temp.Margin = iconMargin == null ? new Thickness(margin, 0, 0, 0) : iconMargin.Value;
                temp.VerticalAlignment = VerticalAlignment.Top;
                temp.HorizontalAlignment = HorizontalAlignment.Left;
                temp.Source = source;
                temp.Name = "Index_" + entry.Offset;
                temp.ToolTip = path;
                temp.MouseDown += ImageDown;

                main.IconGrid.Children.Add(temp);
            }
        }

        public void ImageDown(object sender, EventArgs e)
        {
            var image = (System.Windows.Controls.Image)sender;
            System.Windows.Controls.Image temp = new System.Windows.Controls.Image();

            temp.Width = iconSize == null ? 32 : iconSize.Value;
            temp.Height = iconSize == null ? 32 : iconSize.Value;
            temp.Margin = iconMargin == null ? new Thickness(16, 0, 0, 0) : iconMargin.Value;
            temp.VerticalAlignment = VerticalAlignment.Top;
            temp.HorizontalAlignment = HorizontalAlignment.Left;
            temp.Source = image.Source;
            temp.Name = "NewSpellIcon";

            // Code smells here on hacky positioning and updating the icon
            temp.Margin = new Thickness(285, 38, 0, 0);
            main.NewIconGrid.Children.Clear();
            main.NewIconGrid.Children.Add(temp);

            uint offset = uint.Parse(image.Name.Substring(6));
            uint ID = 0;

            for (int i = 0; i < Header.RecordCount; ++i)
            {
                if (Lookups[i].Offset == offset)
                {
                    ID = Lookups[i].ID;
                    break;
                }
            }
            main.newIconID = ID;
        }

        public string GetIconPath(uint iconId)
        {
            Icon_DBC_Lookup selectedRecord;
            selectedRecord.ID = int.MaxValue;
            selectedRecord.Name = "";
            for (int i = 0; i < Header.RecordCount; ++i)
            {
                if (Lookups[i].ID == iconId)
                {
                    selectedRecord = Lookups[i];
                    break;
                }
            }      
            if (selectedRecord.ID == int.MaxValue) {
                // Raising a exception is causing lag in the UI when a lot of spells do not exist, so just return nothing
                return "";
            }
            return selectedRecord.Name;
        }

        public struct Icon_DBC_Lookup
        {
            public uint ID;
            public uint Offset;
            public string Name;
        }
    };
}
