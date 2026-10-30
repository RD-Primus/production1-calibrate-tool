using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PH_08_Test
{
    /// <summary>
    /// Utility class สำหรับจัดการเงา (Drop Shadow) และการตกแต่งขอบหน้าต่าง Popup
    /// ให้ดูลอยเด่น (Elevated) มีมิติ ชัดเจน ไม่กลืนไปกับหน้าต่างหลัก
    /// </summary>
    public static class FormDropShadowHelper
    {
        public const int CS_DROPSHADOW = 0x00020000;
        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMNCRP_ENABLED = 2;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        /// <summary>
        /// สีเส้นขอบของ Popup (Slate-400) มองเห็นชัดเจนบนพื้นหลังสีขาว ไม่กลืน
        /// </summary>
        public static readonly Color BorderColor = Color.FromArgb(148, 163, 184);

        /// <summary>
        /// สีพื้นหลังของ Header Bar ด้านบน (Slate-50)
        /// </summary>
        public static readonly Color HeaderBgColor = Color.FromArgb(248, 250, 252);

        /// <summary>
        /// สีเส้นแบ่ง Title Bar กับเนื้อหา (Slate-200)
        /// </summary>
        public static readonly Color DividerColor = Color.FromArgb(226, 232, 240);

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        /// <summary>
        /// เพิ่ม CS_DROPSHADOW เข้าไปใน CreateParams เพื่อให้ระบบปฏิบัติการสร้างเงารอบหน้าต่าง
        /// </summary>
        public static CreateParams ApplyCreateParams(CreateParams cp)
        {
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }

        /// <summary>
        /// เปิดใช้งาน DWM Ambient Drop Shadow และ Windows 11 Rounded Elevation
        /// </summary>
        public static void ApplyDropShadow(Form form)
        {
            if (form == null || !form.IsHandleCreated) return;

            try
            {
                int isEnabled = 0;
                DwmIsCompositionEnabled(ref isEnabled);
                if (isEnabled != 0)
                {
                    int val = DWMNCRP_ENABLED;
                    DwmSetWindowAttribute(form.Handle, DWMWA_NCRENDERING_POLICY, ref val, sizeof(int));

                    // กำหนด Frame Margin เพื่อกระตุ้นให้ DWM วาดเงาฟุ้ง (Ambient Drop Shadow) รอบขอบหน้าต่าง
                    MARGINS margins = new MARGINS { cxLeftWidth = 0, cxRightWidth = 0, cyTopHeight = 1, cyBottomHeight = 0 };
                    DwmExtendFrameIntoClientArea(form.Handle, ref margins);

                    // สำหรับ Windows 11: ขอให้ระบบตัดขอบโค้งมนแบบโมเดิร์นพร้อมเงาโค้งเนียนตา
                    int cornerPref = DWMWCP_ROUND;
                    DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));
                }
            }
            catch
            {
                // ไม่ให้ crash บน OS รุ่นเก่าหรือ environment ที่ไม่มี DWM
            }
        }

        /// <summary>
        /// วาดเส้นขอบ Form และแถบ Header ด้านบนให้มีมิติ นูนลอย สวยงาม
        /// </summary>
        public static void DrawElevatedBorder(Form form, Graphics g, float borderWidth = 1.5f, int headerHeight = 40)
        {
            // วาดขอบรอบนอก (Outer Border)
            using (var borderPen = new Pen(BorderColor, borderWidth))
            {
                g.DrawRectangle(borderPen, 0, 0, form.Width - 1, form.Height - 1);
            }

            if (headerHeight > 0)
            {
                // พื้นหลังแถบ Header ด้านบน
                using (var headerBg = new SolidBrush(HeaderBgColor))
                {
                    g.FillRectangle(headerBg, 1, 1, form.Width - 2, headerHeight - 1);
                }

                // เส้นแบ่งด้านล่าง Header
                using (var dividerPen = new Pen(DividerColor, 1.2f))
                {
                    g.DrawLine(dividerPen, 0, headerHeight, form.Width, headerHeight);
                }
            }
        }
    }
}
