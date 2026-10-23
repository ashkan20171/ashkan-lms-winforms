using System;
using System.Drawing;
using System.Net.Mail;
using System.Windows.Forms;
using AshkanLMS.Core;

namespace AshkanLMS.UI
{
    public partial class MainForm
    {
        // Avoid fixed-width academic cards overflowing the main content region.
        // The handler is tied to the lifetime of the card and removed on disposal.
        private void FitAcademicCard102(Panel card, DataGridView grid)
        {
            if (card == null || grid == null) return;
            EventHandler resize = (sender, args) =>
            {
                if (card.IsDisposed || content == null || content.IsDisposed) return;
                card.Width = Math.Max(650, content.ClientSize.Width - 48);
                grid.Width = Math.Max(500, card.ClientSize.Width - 36);
                // Buttons retain their original order; keep them inside the card.
                foreach (Control control in card.Controls)
                {
                    if (control is Button && control.Right > card.ClientSize.Width - 12)
                        control.Left = Math.Max(18, card.ClientSize.Width - control.Width - 18);
                }
            };
            content.Resize += resize;
            card.Disposed += (sender, args) => content.Resize -= resize;
            resize(this, EventArgs.Empty);
        }

        private static bool ValidAcademicEmail102(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 254) return false;
            try
            {
                var parsed = new MailAddress(value.Trim());
                return string.Equals(parsed.Address, value.Trim(), StringComparison.OrdinalIgnoreCase)
                    && parsed.Host.Contains(".");
            }
            catch (FormatException) { return false; }
        }
    }
}
