﻿using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellDifficulty : AbstractDBC
    {
        private MainWindow main;
        private DBAdapter adapter;

        public List<SpellDifficultyLookup> Lookups = new List<SpellDifficultyLookup>();

        public SpellDifficulty(MainWindow window, DBAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile<SpellDifficulty_DBC_Record>("DBC/SpellDifficulty.dbc");

                int boxIndex = 1;
                main.Difficulty.Items.Add(0);
                SpellDifficultyLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];

                    uint[] difficulties = (uint[]) record["Difficulties"];
                    uint id = (uint) record["ID"];

                    SpellDifficultyLookup temp;
                    temp.ID = id;
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    Label label = new Label();
                    label.Content = id + ": " + string.Join(", ", difficulties);

                    string tooltip = "";
                    for (int diffIndex = 0; diffIndex < difficulties.Length; ++diffIndex)
                    {
                        tooltip += "[" + difficulties[diffIndex] + "] ";
                        var rows = adapter.query(string.Format("SELECT * FROM `{0}` WHERE `ID` = '{1}' LIMIT 1", adapter.Table, difficulties[diffIndex])).Rows;
                        if (rows.Count > 0)
                        {
                            var row = rows[0];
                            string selectedLocale = "";
                            for (int locale = 0; locale < 8; ++i)
                            {
                                var name = row["SpellName" + locale].ToString();
                                if (name.Length > 0)
                                {
                                    selectedLocale = name;
                                    break;
                                }
                            }
                            tooltip += selectedLocale;
                        }
                        tooltip += "\n";
                    }
                    label.ToolTip = tooltip;

                    main.Difficulty.Items.Add(label);

                    boxIndex++;
                }
                reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                reader = null;
                Body = null;
            }
            catch (Exception ex)
            {
                window.HandleErrorMessage(ex.Message);
                return;
            }
        }

        public void UpdateDifficultySelection()
        {
            uint ID = uint.Parse(adapter.query(string.Format("SELECT `SpellDifficultyID` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());
            if (ID == 0)
            {
                main.Difficulty.threadSafeIndex = 0;
                return;
            }
            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.Difficulty.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }
    }

    public struct SpellDifficultyLookup
    {
        public uint ID;
        public int comboBoxIndex;
    };
 
    /*
     * Seems to point to other spells, for example:
     Id: 6
     Normal10Men: 50864 = Omar's Seal of Approval, You have Omar's 10 Man Normal Seal of Approval!
     Normal25Men: 69848 = Omar's Seal of Approval, You have Omar's 25 Man Normal Seal of Approval!
     Heroic10Men: 69849 = Omar's Seal of Approval, You have Omar's 10 Man Heroic Seal of Approval!
     Heroic25Men: 69850 = Omar's Seal of Approval, You have Omar's 25 Man Heroic Seal of Approval!
    */
    public struct SpellDifficulty_DBC_Record
    {
        public uint ID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Difficulties;
    };
}
