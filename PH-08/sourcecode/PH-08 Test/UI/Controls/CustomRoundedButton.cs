using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PH_08_Test
{
    // สืบทอดจาก Button เพื่อให้ได้คุณสมบัติของปุ่มมาครบถ้วน
    public class CustomRoundedButton : Button
    {
        // Property สำหรับปรับแต่งในหน้าต่าง Properties ของ Visual Studio
        public Color BorderColor { get; set; } = Color.DarkGreen;
        public int BorderThickness { get; set; } = 2;
        public int BorderRadius { get; set; } = 15; // ปรับตัวเลขนี้เพื่อให้มนมาก-น้อย

        public CustomRoundedButton()
        {
            // ตั้งค่าพื้นฐานให้ปุ่มดูแบนเรียบ ลบขอบสี่เหลี่ยมแบบเก่าทิ้ง
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.Cursor = Cursors.Hand; // เปลี่ยนเมาส์เป็นรูปมือเวลาชี้
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // ให้ระบบวาดปุ่มปกติก่อน (รวมถึงเอฟเฟกต์ตอนเอาเมาส์ชี้/คลิก)
            base.OnPaint(e);

            // เปิดโหมดลบรอยหยักให้เส้นโค้งดูเนียน
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // กำหนดพื้นที่ของปุ่ม
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);

            using (GraphicsPath path = GetRoundedPath(rect, BorderRadius))
            {
                // ตัดรูปร่างของปุ่มให้โค้งมนตาม Path
                this.Region = new Region(path);

                // วาดเส้นขอบทับลงไป
                if (BorderThickness > 0)
                {
                    using (Pen pen = new Pen(BorderColor, BorderThickness))
                    {
                        // วาดเส้นให้อยู่ด้านในขอบเขต เพื่อไม่ให้เส้นแหว่ง
                        pen.Alignment = PenAlignment.Inset;
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        // ฟังก์ชันคำนวณเส้นโค้ง
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