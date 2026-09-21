using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    public partial class Form1
    {
        private CheckBox? hiddenDebugCheckBox;
        private Label? debugModeIndicator;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeHiddenDebugControl();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.D))
            {
                ToggleDebugControlVisibility();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitializeHiddenDebugControl()
        {
            hiddenDebugCheckBox = FindDebugCheckBox(this);
            if (hiddenDebugCheckBox == null)
                return;

            hiddenDebugCheckBox.CheckedChanged -= HiddenDebugCheckBox_CheckedChanged;
            hiddenDebugCheckBox.CheckedChanged += HiddenDebugCheckBox_CheckedChanged;
            hiddenDebugCheckBox.Checked = false;
            hiddenDebugCheckBox.Visible = false;

            debugModeIndicator = new Label
            {
                AutoSize = true,
                Text = "DEBUG MODE",
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                Font = new Font(Font, FontStyle.Bold),
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            Controls.Add(debugModeIndicator);
            debugModeIndicator.Location = new Point(
                Math.Max(0, ClientSize.Width - debugModeIndicator.PreferredWidth - 20),
                10);
            debugModeIndicator.BringToFront();
        }

        private void ToggleDebugControlVisibility()
        {
            hiddenDebugCheckBox ??= FindDebugCheckBox(this);
            if (hiddenDebugCheckBox == null)
                return;

            hiddenDebugCheckBox.Visible = !hiddenDebugCheckBox.Visible;
            if (!hiddenDebugCheckBox.Visible)
                hiddenDebugCheckBox.Checked = false;

            hiddenDebugCheckBox.BringToFront();
        }

        private void HiddenDebugCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (debugModeIndicator != null && hiddenDebugCheckBox != null)
                debugModeIndicator.Visible = hiddenDebugCheckBox.Checked;
        }

        private static CheckBox? FindDebugCheckBox(Control root)
        {
            foreach (Control control in root.Controls)
            {
                if (control is CheckBox checkBox &&
                    string.Equals(checkBox.Text.Trim(), "Debug", StringComparison.OrdinalIgnoreCase))
                {
                    return checkBox;
                }

                CheckBox? nested = FindDebugCheckBox(control);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
