﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using HSDRaw.Tools;
using HSDRaw.Common.Animation;
using HSDRawViewer.Rendering;

namespace HSDRawViewer.GUI
{
    public partial class KeyEditor : UserControl
    {
        private class Key
        {
            public float Value { get; set; }
            public float Slope { get; set; }
            public GXInterpolationType InterpolationType { get; set; }
        }

        private BindingList<Key> KeyFrames = new BindingList<Key>();

        public KeyEditor()
        {
            InitializeComponent();

            DataGridViewColumn column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "Value";
            column.Name = "Value";
            dataGridView1.Columns.Add(column);

            DataGridViewColumn column2 = new DataGridViewTextBoxColumn();
            column2.DataPropertyName = "Slope";
            column2.Name = "Slope";
            dataGridView1.Columns.Add(column2);

            DataGridViewComboBoxColumn column3 = new DataGridViewComboBoxColumn();
            column3.DataSource = Enum.GetValues(typeof(GXInterpolationType));
            column3.DataPropertyName = "InterpolationType";
            column3.Name = "Interpolation";
            dataGridView1.Columns.Add(column3);
            
            dataGridView1.AutoSize = true;
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.CurrentCellDirtyStateChanged += new EventHandler(dataGridView1_CurrentCellDirtyStateChanged);
            dataGridView1.DataSource = KeyFrames;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        public void SetKeys(List<FOBJKey> keys)
        {
            KeyFrames.Clear();

            if (keys == null || keys.Count == 0)
                return;

            var fCount = keys[keys.Count - 1].Frame + 1;

            for(int i = 0; i < fCount; i++)
            {
                KeyFrames.Add(new Key());
            }

            foreach(var k in keys)
            {
                KeyFrames[(int)k.Frame].Value = k.Value;
                KeyFrames[(int)k.Frame].Slope = k.Tan;
                KeyFrames[(int)k.Frame].InterpolationType = k.InterpolationType;
            }

            panel1.Invalidate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<FOBJKey> GetFOBJKeys()
        {
            List<FOBJKey> keys = new List<FOBJKey>();

            int frame = 0;
            foreach(var v in KeyFrames)
            {
                if(v.InterpolationType != GXInterpolationType.HSD_A_OP_NONE)
                {
                    keys.Add(new FOBJKey()
                    {
                        Frame = frame,
                        Value = v.Value,
                        Tan = v.Slope,
                        InterpolationType = v.InterpolationType
                    });
                }
                frame++;
            }

            return keys;
        }

        private static Brush brush = new SolidBrush(Color.Red);
        private static Brush grayBrush = new SolidBrush(Color.FromArgb(127, Color.Gray));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            var rowIdx = (e.RowIndex + 1).ToString();

            var centerFormat = new StringFormat()
            {
                // right alignment might actually make more sense for numbers
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            var valueBounds = grid.GetCellDisplayRectangle(0, e.RowIndex, true);
            var slopeBounds = grid.GetCellDisplayRectangle(1, e.RowIndex, true);

            if (e.RowIndex < KeyFrames.Count)
            {
                if(KeyFrames[e.RowIndex].InterpolationType == GXInterpolationType.HSD_A_OP_NONE)
                {
                    e.Graphics.FillRectangle(brush, headerBounds);
                    e.Graphics.FillRectangle(grayBrush, slopeBounds);
                    e.Graphics.FillRectangle(grayBrush, valueBounds);
                }
                if (KeyFrames[e.RowIndex].InterpolationType == GXInterpolationType.HSD_A_OP_SLP)
                {
                    e.Graphics.FillRectangle(grayBrush, valueBounds);
                }
                if (KeyFrames[e.RowIndex].InterpolationType == GXInterpolationType.HSD_A_OP_SPL ||
                    KeyFrames[e.RowIndex].InterpolationType == GXInterpolationType.HSD_A_OP_LIN ||
                    KeyFrames[e.RowIndex].InterpolationType == GXInterpolationType.HSD_A_OP_CON ||
                    KeyFrames[e.RowIndex].InterpolationType == GXInterpolationType.HSD_A_OP_KEY)
                {
                    e.Graphics.FillRectangle(grayBrush, slopeBounds);
                }
            }

            e.Graphics.DrawString(rowIdx, this.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.InvalidateRow(e.RowIndex);

            panel1.Invalidate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            bool validClick = (e.RowIndex != -1 && e.ColumnIndex != -1); //Make sure the clicked row/column is valid.
            var datagridview = sender as DataGridView;

            // Check to make sure the cell clicked is the cell containing the combobox 
            if (datagridview.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn && validClick)
            {
                datagridview.BeginEdit(true);
                ((ComboBox)datagridview.EditingControl).DroppedDown = true;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                // This fires the cell value changed handler below
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Insert && dataGridView1.SelectedRows.Count > 0)
            {
                var i = dataGridView1.SelectedRows[0].Index + 1;
                if(i != -1)
                {
                    KeyFrames.Insert(i, new Key());
                }
            }
        }

        private static Brush backBrush = new SolidBrush(Color.DarkSlateGray);
        private static Brush numBrush = new SolidBrush(Color.AntiqueWhite);
        private static Brush pointBrush = new SolidBrush(Color.Yellow);
        private static Pen linePen = new Pen(Color.AntiqueWhite);
        private static Pen faintLinePen = new Pen(Color.Gray);
        private static Font numFont = new Font(FontFamily.GenericMonospace, 8);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(backBrush, e.ClipRectangle);

            var graphOffsetX = 50;
            var graphOffsetY = 12;
            var graphWidth = panel1.Width - graphOffsetX;
            var graphHeight = panel1.Height - graphOffsetY;

            float ampHi = float.MinValue;
            float ampLw = float.MaxValue;

            var keyCount = (float)KeyFrames.Count;

            var spaces = (int)(keyCount / (graphWidth / TextRenderer.MeasureText(keyCount.ToString(), numFont).Width));
            if (spaces < 1)
                spaces = 1;
            if (spaces % 5 != 0)
                spaces += 5 - (spaces % 5);

            AnimTrack a = new AnimTrack();
            a.Keys = GetFOBJKeys();

            for (int i = 0; i < KeyFrames.Count; i++)
            {
                var v = a.GetValue(i);

                ampHi = Math.Max(ampHi, v);
                ampLw = Math.Min(ampLw, v);
            }

            var dis = ampHi - ampLw;
            var off = dis * 0.15f;

            if (dis == 0)
                return;

            dis = (ampHi - ampLw) + off * 2;

            e.Graphics.DrawString(ampHi.ToString("0.0000"), numFont, numBrush, new PointF(0, (off / dis) * graphHeight - 4 + graphOffsetY));
            e.Graphics.DrawString(ampLw.ToString("0.0000"), numFont, numBrush, new PointF(0, ((dis - off) / dis) * graphHeight - 4 + graphOffsetY));
            
            e.Graphics.DrawLine(faintLinePen, new PointF(graphOffsetX, (off / dis) * graphHeight + graphOffsetY), new PointF(panel1.Width, (off / dis) * graphHeight + graphOffsetY));
            e.Graphics.DrawLine(faintLinePen, new PointF(graphOffsetX, ((dis - off) / dis) * graphHeight + graphOffsetY), new PointF(panel1.Width, ((dis - off) / dis) * graphHeight + graphOffsetY));


            for (int i = 1; i <= KeyFrames.Count; i++)
            {
                var x1 = graphOffsetX + (int)(((i - 1) / keyCount) * graphWidth);
                var h1 = (int)((a.GetValue(i - 1) + Math.Abs(ampLw) + off) / dis * graphHeight);
                var h2 = (int)((a.GetValue(i) + Math.Abs(ampLw) + off) / dis * graphHeight);
                
                if(i - 1 < KeyFrames.Count)
                {
                    if (KeyFrames[i - 1].InterpolationType == GXInterpolationType.HSD_A_OP_CON)
                        h2 = h1;
                }

                if (((i - 1) % spaces) == 0)
                {
                    e.Graphics.DrawLine(faintLinePen, new Point(x1, graphOffsetY), new Point(x1, panel1.Height));
                    e.Graphics.DrawString((i - 1).ToString(), numFont, numBrush, new PointF(x1 - 4, 0));
                }

                var px1 = graphOffsetX + (int)(((i - 1) / keyCount) * graphWidth);
                var px2 = graphOffsetX + (int)((i / keyCount) * graphWidth);
                var py1 = panel1.Height - h1;
                var py2 = panel1.Height - h2;
                
                e.Graphics.DrawLine(linePen, 
                    new Point(px1, py1),
                    new Point(px2, py2));

                if (i - 1 < KeyFrames.Count)
                {
                    if (KeyFrames[i - 1].InterpolationType != GXInterpolationType.HSD_A_OP_NONE)
                        e.Graphics.FillRectangle(pointBrush, new RectangleF(px1 - 2, py1 - 2, 4, 4));
                }
            }
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            panel1.Invalidate();
        }
    }
}