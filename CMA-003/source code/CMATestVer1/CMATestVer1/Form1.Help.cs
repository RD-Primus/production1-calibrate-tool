using System;
using System.Drawing;
using System.Windows.Forms;

namespace CMATestVer1
{
    public partial class Form1
    {
        private void InitializeHelpFeature()
        {
            btn_help.Cursor = Cursors.Hand;
            btn_help.Click += BtnHelp_Click;
        }

        private void BtnHelp_Click(object? sender, EventArgs e)
        {
            // Dimmer Overlay ครอบคลุมเฉพาะพื้นที่ Client Area ของ Form1 แบบพอดีเป๊ะ
            Point clientScreenLoc = this.PointToScreen(Point.Empty);
            Size clientSize = this.ClientSize;

            using var overlay = new Form
            {
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                Opacity = 0.40,
                BackColor = Color.Black,
                ShowInTaskbar = false,
                Location = clientScreenLoc,
                Size = clientSize,
                Owner = this
            };

            overlay.Show(this);

            using (var helpPopup = new HelpPopupForm())
            {
                helpPopup.ShowDialog(overlay);
            }

            overlay.Close();
        }
    }
}
