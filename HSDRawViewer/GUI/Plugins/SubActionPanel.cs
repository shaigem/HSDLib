﻿using HSDRaw;
using System.Windows.Forms;
using System.Collections.Generic;
using HSDRawViewer.Tools;
using HSDRaw.Tools.Melee;
using System.Globalization;

namespace HSDRawViewer.GUI.Plugins
{
    public partial class SubActionPanel : Form
    {
        public byte[] Data { get; internal set; }

        public HSDStruct Reference = null;

        private ComboBox PointerBox;

        private List<SubactionEditor.Action> AllActions;

        public SubActionPanel(List<SubactionEditor.Action> AllActions)
        {
            this.AllActions = AllActions;

            InitializeComponent();

            foreach(var v in SubactionManager.Subactions)
            {
                comboBox1.Items.Add(v.Name);
            }
            
            PointerBox = new ComboBox();
            PointerBox.Dock = DockStyle.Fill;
            PointerBox.DropDownStyle = ComboBoxStyle.DropDownList;

            foreach (var s in AllActions)
                PointerBox.Items.Add(s);

            PointerBox.SelectedIndexChanged += (sender, args) =>
            {
                Reference = (PointerBox.SelectedItem as SubactionEditor.Action)._struct;
            };
        }

        public void LoadData(byte[] b, HSDStruct reference)
        {
            Data = b;
            Reference = reference;

            PointerBox.SelectedItem = AllActions.Find(e => e._struct == Reference);

            Bitreader r = new Bitreader(Data);

            var sa = SubactionManager.GetSubaction((byte)r.Read(6));

            comboBox1.SelectedItem = sa.Name;

            for (int i = 0; i < sa.Parameters.Length; i++)
            {
                var p = sa.Parameters[i];

                if (p.Name.Contains("None"))
                    continue;

                var value = r.Read(p.BitCount);

                if (p.IsPointer)
                    continue;

                (panel1.Controls[sa.Parameters.Length - 1 - i].Controls[0] as SubactionValueEditor).SetValue(value);
            }
            
            CenterToScreen();
        }

        private void ReadjustHeight()
        {
            Height = panel1.Controls.Count * 24 + 120;
        }

        private void CreateParamEditor(Subaction action)
        {
            if (action == null)
                return;
            
            panel1.Controls.Clear();
            
            for(int i = action.Parameters.Length - 1; i >= 0; i--)
            {
                var p = action.Parameters[i];

                if (p.Name == "None")
                    continue;

                Panel group = new Panel();
                group.Dock = DockStyle.Top;
                group.Height = 24;

                if (p.IsPointer)
                {
                    if (Reference == null)
                        PointerBox.SelectedIndex = 0;
                    group.Controls.Add(PointerBox);
                }
                else
                if(p.Hex)
                {
                    SAHexEditor editor = new SAHexEditor();
                    editor.SetBitSize(p.BitCount);
                    group.Controls.Add(editor);

                    group.Controls.Add(new Label() { Text = "0x", Dock = DockStyle.Left });
                }
                else
                if (p.HasEnums)
                {
                    SAEnumEditor editor = new SAEnumEditor();
                    editor.SetEnums(p.Enums);
                    group.Controls.Add(editor);
                }
                else
                {
                    SAIntEditor editor = new SAIntEditor();
                    editor.SetBitSize(p.BitCount);
                    group.Controls.Add(editor);
                }

                group.Controls.Add(new Label() { Text = p.Name + ":", Dock = DockStyle.Left, Width = 200 });

                panel1.Controls.Add(group);
            }

            ReadjustHeight();
        }

        public byte[] CompileAction()
        {
            BitWriter w = new BitWriter();

            var sa = SubactionManager.Subactions[comboBox1.SelectedIndex];

            w.Write(sa.Code, 6);
            for(int i = 0; i < sa.Parameters.Length; i++)
            {
                var bm = sa.Parameters[i];

                if (bm.Name.Contains("None") || bm.IsPointer)
                {
                    w.Write(0, bm.BitCount);
                    continue;
                }
                
                var value = (int)(panel1.Controls[sa.Parameters.Length - 1 - i].Controls[0] as SubactionValueEditor).GetValue();

                w.Write(value, bm.BitCount);
            }

            // they should all theoretically be aligned to 32 bits
            if (sa.Parameters.Length == 0)
                w.Write(0, 26);

            return w.Bytes.ToArray();
        }

        private void comboBox1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            CreateParamEditor(SubactionManager.GetSubaction(comboBox1.SelectedItem as string));
        }

        private void buttonSave_Click(object sender, System.EventArgs e)
        {
            Data = CompileAction();
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    public interface SubactionValueEditor
    {
        void SetBitSize(int bitCount);

        void SetValue(int value);

        long GetValue();
    }

    // Int Editor
    public class SAIntEditor : NumericUpDown, SubactionValueEditor
    {
        public SAIntEditor()
        {
            Dock = DockStyle.Fill;
        }

        public void SetBitSize(int bitCount)
        {
            Maximum = ((1L << bitCount) - 1L);
            Minimum = 0;
        }
        
        public void SetValue(int value)
        {
            Value = value;
        }

        public long GetValue()
        {
            return (long)Value;
        }
    }

    // Enum Editor
    public class SAEnumEditor : ComboBox, SubactionValueEditor
    {
        public SAEnumEditor()
        {
            Dock = DockStyle.Fill;

            DropDownStyle = ComboBoxStyle.DropDownList;
        }

        public long GetValue()
        {
            return SelectedIndex;
        }

        public void SetBitSize(int bitCount)
        {
            // not needed
        }

        public void SetEnums(string[] enums)
        {
            Items.AddRange(enums);
        }

        public void SetValue(int value)
        {
            SelectedIndex = value;
        }
    }

    // Float Editor

    // Hex Editor
    public class SAHexEditor : TextBox, SubactionValueEditor
    {
        private uint IntValue = 0;
        private long MaxValue = 0;

        public SAHexEditor()
        {
            Dock = DockStyle.Fill;
            TextChanged += (sender, args) =>
            {
                // Filter text
                int val;
                if(int.TryParse(Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val) && val <= MaxValue)
                {
                    IntValue = (uint)val;
                }
                else
                {
                    Text = IntValue.ToString("X");
                }
            };
        }

        public long GetValue()
        {
            return IntValue;
        }

        public void SetBitSize(int bitCount)
        {
            MaxValue = ((1L << bitCount) - 1L);
        }

        public void SetValue(int value)
        {
            Text = value.ToString("X");
        }
    }

}