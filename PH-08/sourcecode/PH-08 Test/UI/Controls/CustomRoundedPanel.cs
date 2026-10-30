using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PH_08_Test
{
    // สืบทอดจาก Panel และใช้ public เพื่อให้โชว์ใน Toolbox แน่นอน
    public class CustomRoundedPanel : Panel
    {
        public int BorderRadius { get; set; } = 15;
        public Color BorderColor { get; set; } = Color.DarkGreen;
        public int BorderThickness { get; set; } = 2;

        public CustomRoundedPanel()
        {
            this.BackColor = Color.White;
            // เปิดโหมดนี้เพื่อลดอาการกระพริบ (Flicker) เวลาหน้าจอวาดตัวเองใหม่
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // เปิดโหมดลบรอยหยัก
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);

            using (GraphicsPath path = GetRoundedPath(rect, BorderRadius))
            {
                // 1. ตัดรูปร่างของ Panel ให้โค้งมนจริงๆ (Clip area)
                this.Region = new Region(path);

                // 2. วาดเส้นขอบทับลงไป
                if (BorderThickness > 0)
                {
                    using (Pen pen = new Pen(BorderColor, BorderThickness))
                    {
                        // ให้อยู่ด้านในขอบเขต เพื่อให้เส้นไม่แหว่ง
                        pen.Alignment = PenAlignment.Inset;
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        // ฟังก์ชันคำนวณมุมโค้ง
        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            path.StartFigure();
            // มุมบนซ้าย
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            // มุมบนขวา
            path.AddArc(rect.Width - diameter, rect.Y, diameter, diameter, 270, 90);
            // มุมล่างขวา
            path.AddArc(rect.Width - diameter, rect.Height - diameter, diameter, diameter, 0, 90);
            // มุมล่างซ้าย
            path.AddArc(rect.X, rect.Height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}