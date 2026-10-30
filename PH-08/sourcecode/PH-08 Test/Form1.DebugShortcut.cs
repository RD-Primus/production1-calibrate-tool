using System;
using System.Drawing;
using System.Windows.Forms;

namespace PH_08_Test
{
    public partial class Form1
    {
        private Label? _debugModeIndicator;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.D))
            {
                ToggleDebugControlVisibility();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitDebugControls()
        {
            if (chk_debug == null) return;

            chk_debug.Checked = false;
            chk_debug.Visible = false;
            chk_debug.CheckedChanged -= Chk_debug_CheckedChanged;
            chk_debug.CheckedChanged += Chk_debug_CheckedChanged;

            _debugModeIndicator = new Label
            {
                AutoSize = true,
                Text = "DEBUG MODE",
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                Font = new Font(Font, FontStyle.Bold),
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            Controls.Add(_debugModeIndicator);
            _debugModeIndicator.Location = new Point(
                Math.Max(0, ClientSize.Width - _debugModeIndicator.PreferredWidth - 20),
                10);
            _debugModeIndicator.BringToFront();
        }

        private void ToggleDebugControlVisibility()
        {
            if (chk_debug == null) return;

            chk_debug.Visible = !chk_debug.Visible;
            if (!chk_debug.Visible)
            {
                chk_debug.Checked = false;
            }
            chk_debug.BringToFront();
        }

        private void Chk_debug_CheckedChanged(object? sender, EventArgs e)
        {
            if (_debugModeIndicator != null && chk_debug != null)
            {
                _debugModeIndicator.Visible = chk_debug.Checked;
            }
        }
    }
}
