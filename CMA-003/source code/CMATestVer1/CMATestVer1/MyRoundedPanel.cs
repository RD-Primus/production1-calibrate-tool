using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CMA003AVer2
{
    public class MyRoundedPanel : Panel
    {
        private int _borderRadius = 30;

        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = value; this.Invalidate(); } // วาดใหม่ทันทีที่เปลี่ยนค่า
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = new GraphicsPath())
            {
                float curveSize = _borderRadius * 2F;
                // ป้องกันไม่ให้ส่วนโค้งใหญ่กว่าขนาด Panel
                if (curveSize > Height) curveSize = Height;
                if (curveSize > Width) curveSize = Width;

                path.StartFigure();
                path.AddArc(0, 0, curveSize, curveSize, 180, 90);
                path.AddArc(Width - curveSize, 0, curveSize, curveSize, 270, 90);
                path.AddArc(Width - curveSize, Height - curveSize, curveSize, curveSize, 0, 90);
                path.AddArc(0, Height - curveSize, curveSize, curveSize, 90, 90);
                path.CloseFigure();

                this.Region = new Region(path); // ตัดขอบ Panel ให้มนตามเส้นวาด
            }
        }
    }
}
