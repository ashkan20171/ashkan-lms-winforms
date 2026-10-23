using System;
using System.Drawing;
using System.Windows.Forms;
using AshkanLMS.Core;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        // Applied after each page is constructed. Do not override semantic status colors,
        // chart canvases or owner-drawn controls.
        private void ApplyPageTheme101(Control root)
        {
            if (root == null) return;
            foreach (Control control in root.Controls)
            {
                var grid = control as DataGridView;
                if (grid != null)
                {
                    StyleGrid101(grid);
                }
                else if (control is TextBox || control is ComboBox || control is NumericUpDown || control is DateTimePicker)
                {
                    control.BackColor = Theme.DarkMode ? Color.FromArgb(42, 56, 75) : Color.White;
                    control.ForeColor = Theme.Text;
                }
                else if (control is RadioButton || control is CheckBox)
                {
                    control.ForeColor = Theme.Text;
                }
                ApplyPageTheme101(control);
            }
        }

        private void StyleGrid101(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Theme.Card;
            grid.GridColor = Theme.DarkMode ? Color.FromArgb(58, 73, 94) : Color.FromArgb(222, 231, 241);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.DarkMode ? Color.FromArgb(40, 58, 79) : Theme.Header;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Text;
            grid.DefaultCellStyle.BackColor = Theme.Card;
            grid.DefaultCellStyle.ForeColor = Theme.Text;
            grid.DefaultCellStyle.SelectionBackColor = Theme.DarkMode ? Color.FromArgb(52, 89, 135) : Color.FromArgb(210, 228, 252);
            grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.DarkMode ? Color.FromArgb(41, 56, 75) : Color.FromArgb(242, 247, 253);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Theme.Text;
            grid.RowHeadersDefaultCellStyle.BackColor = Theme.Header;
            grid.RowHeadersDefaultCellStyle.ForeColor = Theme.Text;
            grid.RightToLeft = AppSession.Persian ? RightToLeft.Yes : RightToLeft.No;
            grid.ColumnHeadersDefaultCellStyle.Alignment = AppSession.Persian ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.Alignment = AppSession.Persian ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft;
        }
    }
}
