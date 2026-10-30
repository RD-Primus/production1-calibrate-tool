using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PH_08_Test
{
    public partial class Form1
    {
        private Size _baseClientSize;
        private readonly Dictionary<Control, Rectangle> _originalBounds = new Dictionary<Control, Rectangle>();
        private readonly Dictionary<Control, float> _originalFontSize = new Dictionary<Control, float>();
        private readonly Dictionary<ComboBox, int> _originalItemHeight = new Dictionary<ComboBox, int>();
        private bool _isScaling = false;
        private const float ScalePadding = 1.0f;

        private void InitAutoScale()
        {
            _baseClientSize = this.ClientSize; // Baseline initial size
            CacheOriginalLayout(this);
            this.Resize += Form1_AutoScale_Resize;
        }

        private void CacheOriginalLayout(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                _originalBounds[c] = c.Bounds;
                _originalFontSize[c] = c.Font.Size;

                if (c is ComboBox cb)
                {
                    _originalItemHeight[cb] = cb.ItemHeight;
                }

                if (c.HasChildren)
                {
                    CacheOriginalLayout(c);
                }
            }
        }

        private void Form1_AutoScale_Resize(object? sender, EventArgs e)
        {
            if (_isScaling) return;
            if (_baseClientSize.Width <= 0 || _baseClientSize.Height <= 0) return;
            if (this.WindowState == FormWindowState.Minimized) return;

            // ถ้าหน้าต่างอยู่ในขนาดปกติ (ไม่ได้กดขยายเต็มจอ Maximize)
            // ให้ใช้พิกัดและขนาดเดิมตามหน้า Designer 100% เสมอ ห้ามทำการ Scale หรือย้ายตำแหน่งใดๆ ทั้งสิ้น
            if (this.WindowState != FormWindowState.Maximized)
            {
                _isScaling = true;
                try
                {
                    this.SuspendLayout();
                    RestoreOriginalLayout(this);
                    this.ResumeLayout(true);
                    RefreshPhaseIndicators();
                }
                finally
                {
                    _isScaling = false;
                }
                return;
            }

            _isScaling = true;
            try
            {
                float scaleX = (float)this.ClientSize.Width / _baseClientSize.Width;
                float scaleY = (float)this.ClientSize.Height / _baseClientSize.Height;
                float scale = Math.Min(scaleX, scaleY) * ScalePadding;

                int scaledWidth = (int)(_baseClientSize.Width * scale);
                int scaledHeight = (int)(_baseClientSize.Height * scale);
                int offsetX = (this.ClientSize.Width - scaledWidth) / 2;
                int offsetY = (this.ClientSize.Height - scaledHeight) / 2;

                this.SuspendLayout();
                ApplyScale(this, scale, offsetX, offsetY);
                this.ResumeLayout(true);
                RefreshPhaseIndicators();
            }
            finally
            {
                _isScaling = false;
            }
        }

        private void RestoreOriginalLayout(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (_originalBounds.TryGetValue(c, out Rectangle orig))
                {
                    c.Bounds = orig;
                }

                if (_originalFontSize.TryGetValue(c, out float origFontSize))
                {
                    c.Font = new Font(c.Font.FontFamily, origFontSize, c.Font.Style);
                }

                if (c is ComboBox cb && _originalItemHeight.TryGetValue(cb, out int origItemH))
                {
                    cb.ItemHeight = origItemH;
                }

                if (c.HasChildren)
                {
                    RestoreOriginalLayout(c);
                }
            }
        }

        private void ApplyScale(Control parent, float scale, int offsetX, int offsetY)
        {
            foreach (Control c in parent.Controls)
            {
                if (_originalBounds.TryGetValue(c, out Rectangle orig))
                {
                    int x = (int)(orig.X * scale);
                    int y = (int)(orig.Y * scale);

                    // Top-level controls get offset to center on form
                    if (parent == this)
                    {
                        x += offsetX;
                        y += offsetY;
                    }

                    c.Bounds = new Rectangle(x, y, (int)(orig.Width * scale), (int)(orig.Height * scale));
                }

                if (_originalFontSize.TryGetValue(c, out float origFontSize))
                {
                    float newSize = Math.Max(6f, origFontSize * scale);
                    c.Font = new Font(c.Font.FontFamily, newSize, c.Font.Style);
                }

                if (c is ComboBox cb && _originalItemHeight.TryGetValue(cb, out int origItemH))
                {
                    cb.ItemHeight = Math.Max(12, (int)(origItemH * scale));
                }

                if (c.HasChildren)
                {
                    ApplyScale(c, scale, offsetX, offsetY);
                }
            }
        }
    }
}
