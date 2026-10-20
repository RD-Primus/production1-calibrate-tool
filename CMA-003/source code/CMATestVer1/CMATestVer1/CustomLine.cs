using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CMA003AVer2
{
    public class CustomLine : Control
    {
        private int _thickness = 2;
        private Color _lineColor = Color.Silver;
        private bool _isVertical = false;

        public CustomLine()
        {
            this.Size = new Size(100, 2); // ขนาดเริ่มต้น
            this.DoubleBuffered = true;
        }

        public int Thickness
        {
            get => _thickness;
            set { _thickness = value; this.Invalidate(); }
        }

        public Color LineColor
        {
            get => _lineColor;
            set { _lineColor = value; this.Invalidate(); }
        }

        public bool IsVertical
        {
            get => _isVertical;
            set { _isVertical = value; this.Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(_lineColor, _thickness))
            {
                // ทำให้ปลายเส้นมน (Round Cap)
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                if (_isVertical)
                {
                    // วาดแนวตั้ง
                    e.Graphics.DrawLine(pen, Width / 2, 0, Width / 2, Height);
                }
                else
                {
                    // วาดแนวนอน
                    e.Graphics.DrawLine(pen, 0, Height / 2, Width, Height / 2);
                }
            }
        }
    }
}
