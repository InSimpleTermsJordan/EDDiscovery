﻿/*
 * Copyright © 2016 - 2017 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */
using EDDiscovery.Controls;
using EliteDangerousCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EDDiscovery.UserControls
{
    public partial class UserControlShipYards : UserControlCommonBase
    {
        private string dbYardSave = "Select";

        #region Init

        public UserControlShipYards()
        {
            InitializeComponent();
        }

        public override void Init()
        {
            DBBaseName = "ShipYardGrid";

            dataGridViewShips.MakeDoubleBuffered();
            dataGridViewShips.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            discoveryform.OnHistoryChange += Discoveryform_OnHistoryChange; ;
            discoveryform.OnNewEntry += Discoveryform_OnNewEntry;

            var enumlist = new Enum[] { EDTx.UserControlShipYards_labelYardSel, EDTx.UserControlShipYards_labelYard };
            var enumlisttt = new Enum[] { EDTx.UserControlShipYards_comboBoxYards_ToolTip };

            BaseUtils.Translator.Instance.TranslateControls(this, enumlist);
            BaseUtils.Translator.Instance.TranslateTooltip(toolTip, enumlisttt, this);
        }

        public override void ChangeCursorType(IHistoryCursor thc)
        {
            uctg.OnTravelSelectionChanged -= Display;
            uctg = thc;
            uctg.OnTravelSelectionChanged += Display;
        }

        public override void LoadLayout()
        {
            dataGridViewShips.RowTemplate.MinimumHeight = Font.ScalePixels(26);
            uctg.OnTravelSelectionChanged += Display;
            DGVLoadColumnLayout(dataGridViewShips);
        }

        public override void Closing()
        {
            DGVSaveColumnLayout(dataGridViewShips);
            uctg.OnTravelSelectionChanged -= Display;
            discoveryform.OnNewEntry -= Discoveryform_OnNewEntry;
            discoveryform.OnHistoryChange -= Discoveryform_OnHistoryChange;
        }


        #endregion

        #region Display

        private void Discoveryform_OnNewEntry(HistoryEntry he, HistoryList hl)
        {
            Discoveryform_OnHistoryChange(hl);
        }

        private void Discoveryform_OnHistoryChange(HistoryList hl)
        {
            UpdateComboBox(hl);
        }

        private void UpdateComboBox(HistoryList hl)
        {
            ShipYardList shm = hl.Shipyards;
            string cursel = comboBoxYards.Text;

            string the = "Travel History Entry".T(EDTx.UserControlShipYards_TravelHistoryEntry);

            comboBoxYards.Items.Clear();
            comboBoxYards.Items.Add(the);

            comboBoxYards.Items.AddRange(shm.ShipList());

            var list = (from ShipYard x in shm.GetFilteredList() select x.Ident()).ToList();        // yard list repeats allowed within timescale
            comboBoxYards.Items.AddRange(list);

            if (cursel == "")
                cursel = GetSetting(dbYardSave, "");

            if (cursel == "" || !comboBoxYards.Items.Contains(cursel))
                cursel = the;

            comboBoxYards.Enabled = false;
            comboBoxYards.SelectedItem = cursel;
            comboBoxYards.Enabled = true;
        }

        public override void InitialDisplay()
        {
            Display(uctg.GetCurrentHistoryEntry, discoveryform.history);
        }

        HistoryEntry last_he = null;

        private void Display(HistoryEntry he, HistoryList hl) =>
            Display(he, hl, true);

        private void Display(HistoryEntry he, HistoryList hl, bool selectedEntry)
        {
            if ( comboBoxYards.Items.Count == 0 )
                UpdateComboBox(hl);

            last_he = he;
            Display();
        }

        private void Display()
        {
            DataGridViewColumn sortcol = dataGridViewShips.SortedColumn != null ? dataGridViewShips.SortedColumn : dataGridViewShips.Columns[0];
            SortOrder sortorder = dataGridViewShips.SortOrder;

            dataGridViewShips.Rows.Clear();
            dataViewScrollerPanel1.SuspendLayout();

            labelYard.Visible = false;

            ShipYard yard = null;

            if (comboBoxYards.SelectedIndex == 0 || comboBoxYards.Text.Length == 0)  // second is due to the order History gets called vs this on start
            {
                HistoryEntry lastshipyard = discoveryform.history.GetLastHistoryEntry(x => x.EntryType == JournalTypeEnum.Shipyard, last_he);
                if (lastshipyard != null)
                    yard = (lastshipyard.journalEntry as EliteDangerousCore.JournalEvents.JournalShipyard).Yard;
            }
            else
            {
                yard = discoveryform.history.Shipyards.GetFilteredList().Find(x => x.Ident().Equals(comboBoxYards.Text));
            }

            if (yard?.Ships != null)
            {
                DisplayYard(yard);
            }
            else
            {
                List<Tuple<ShipYard, ShipYard.ShipyardItem>> shiplist = discoveryform.history.Shipyards.GetShipLocations(comboBoxYards.Text,nolocrepeats:true);
                if ( shiplist.Count > 0 )
                    DisplayShips(shiplist, comboBoxYards.Text);
            }

            dataViewScrollerPanel1.ResumeLayout();
            dataGridViewShips.Sort(sortcol, (sortorder == SortOrder.Descending) ? ListSortDirection.Descending : ListSortDirection.Ascending);
            dataGridViewShips.Columns[sortcol.Index].HeaderCell.SortGlyphDirection = sortorder;
        }

        private void DisplayShips(List<Tuple<ShipYard, ShipYard.ShipyardItem>> shiplist,string ship)
        {
            ISystem cursys = discoveryform.history.CurrentSystem();

            foreach (Tuple<ShipYard, ShipYard.ShipyardItem> i in shiplist)
            {
                double distance = discoveryform.history.DistanceCurrentTo(i.Item1.StarSystem);
                string dte = EDDConfig.Instance.ConvertTimeToSelectedFromUTC(i.Item1.Datetime).ToString();
                object[] rowobj = { dte, i.Item1.Location, (distance > -1) ? (distance.ToString("N1") + "ly") : "Unknown".T(EDTx.Unknown), i.Item2.ShipPrice.ToString("N0") + "cr" };
                dataGridViewShips.Rows.Add(rowobj);
            }

            labelYard.Text = ship;
            labelYard.Visible = true;
            Col1.HeaderText = "Date".T(EDTx.UserControlShipYards_Date);
            Col1.Tag = "DT";
            Col2.HeaderText = "Yard".T(EDTx.UserControlShipYards_Yard);
            Col2.Tag = null;
            Col3.HeaderText = "Distance".T(EDTx.UserControlShipYards_Distance);
            Col3.Tag = "ly";
            ColPrice.HeaderText = "Price".T(EDTx.UserControlShipYards_Price);
            ColPrice.Tag = "cr";
        }

        private void DisplayYard(ShipYard yard)    
        {
            foreach (ShipYard.ShipyardItem i in yard.Ships)
            {
                string col2 = "Unknown".T(EDTx.Unknown), col3 = "";
                Dictionary<ItemData.ShipPropID, ItemData.IModuleInfo> shipprops = ItemData.Instance.GetShipProperties(i.ShipType);
                if ( shipprops != null)
                {
                    col2 = (shipprops[ItemData.ShipPropID.Manu] as ItemData.ShipInfoString).Value;
                    col3 = (shipprops[ItemData.ShipPropID.HullMass] as ItemData.ShipInfoDouble).Value.ToString("0.#t") +
                        ", " + (shipprops[ItemData.ShipPropID.Speed] as ItemData.ShipInfoInt).Value.ToString() +
                        "/" + (shipprops[ItemData.ShipPropID.Boost] as ItemData.ShipInfoInt).Value.ToString();
                }

                object[] rowobj = { i.ShipType_Localised, col2, col3,i.ShipPrice.ToString("N0") + "cr" };

                dataGridViewShips.Rows.Add(rowobj);
            }

            double distance = discoveryform.history.DistanceCurrentTo(yard.StarSystem);

            labelYard.Text = yard.Ident() + (distance>-1 ? (" @ " + distance.ToString("N1") + "ly") : "");
            labelYard.Visible = true;
            Col1.HeaderText = "Ship".T(EDTx.UserControlShipYards_Ship);
            Col1.Tag = null;
            Col2.HeaderText = "Manufacturer".T(EDTx.UserControlShipYards_Manufacturer);
            Col2.Tag = null;
            Col3.HeaderText = "Mass/Speed".T(EDTx.UserControlShipYards_MS);
            Col3.Tag = null;
            ColPrice.HeaderText = "Price".T(EDTx.UserControlShipYards_Price);
            ColPrice.Tag = "cr";
        }

        #endregion


        private void comboBoxHistoryWindow_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxYards.Enabled)
            {
                PutSetting(dbYardSave, comboBoxYards.Text);
                Display();
            }
        }

        private void dataGridViewShips_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (e.Column.Tag != null)
            {
                string t = (string)e.Column.Tag;

                if (t == "DT")
                    e.SortDataGridViewColumnDate();
                else
                    e.SortDataGridViewColumnNumeric(t);
            }
        }

    }
}
