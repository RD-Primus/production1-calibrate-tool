using System;
using System.Drawing;
using System.Windows.Forms;

namespace PH_08_Test
{
    public partial class Form1
    {
        private string _operatorToken = string.Empty;
        private string _operatorEmail = string.Empty;
        private string _operatorName = string.Empty;
        private string _serverIp = string.Empty;
        private int _serverPort = 8080;
        private string _serverTokenKey = string.Empty;

        public bool IsOperatorLoggedIn => !string.IsNullOrWhiteSpace(_operatorToken);
        public string OperatorToken => _operatorToken;
        public string OperatorEmail => _operatorEmail;
        public string OperatorName => _operatorName;

        private void InitLoginControls()
        {
            // ล็อก Checkbox ส่ง Server ไว้ก่อนจนกว่าจะ Login สำเร็จ (แสดงสีเทา)
            UpdateServerCheckboxState(false);

            if (chk_resultToserver != null)
            {
                chk_resultToserver.Click += (s, e) =>
                {
                    if (!IsOperatorLoggedIn)
                    {
                        chk_resultToserver.Checked = false;
                    }
                };
            }

            if (btn_userPill != null)
            {
                btn_userPill.SetState(false, string.Empty);
                btn_userPill.Click += (s, e) => HandleUserPillClick();
            }
        }

        private void UpdateServerCheckboxState(bool isLoggedIn)
        {
            if (chk_resultToserver == null) return;

            if (isLoggedIn)
            {
                chk_resultToserver.AutoCheck = true;
                chk_resultToserver.Cursor = Cursors.Hand;
                chk_resultToserver.ForeColor = Color.FromArgb(241, 248, 241); // สีขาวนวลเมื่อปลดล็อก
            }
            else
            {
                chk_resultToserver.Checked = false;
                chk_resultToserver.AutoCheck = false; // ล็อกไม่ให้ติ๊กได้
                chk_resultToserver.Cursor = Cursors.No;
                chk_resultToserver.ForeColor = Color.DarkGray; // สีเทาเมื่อล็อก
            }
        }

        private void HandleUserPillClick()
        {
            if (IsOperatorLoggedIn)
            {
                // ถามยืนยันออกจากระบบ
                var res = MessageBox.Show(
                    this,
                    $"คุณต้องการออกจากระบบ (Logout) หรือไม่?\nผู้ใช้งานปัจจุบัน: {_operatorName}",
                    "ยืนยันการออกจากระบบ",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    _operatorToken = string.Empty;
                    _operatorEmail = string.Empty;
                    _operatorName = string.Empty;

                    btn_userPill?.SetState(false, string.Empty);
                    UpdateServerCheckboxState(false);

                    LogMessage("[AUTH] ออกจากระบบเรียบร้อยแล้ว", LogNormalColor);
                }
            }
            else
            {
                using var loginForm = new FormLogin();
                if (loginForm.ShowDialog(this) == DialogResult.OK)
                {
                    _operatorToken = loginForm.OperatorToken.Trim();
                    _operatorEmail = loginForm.Email.Trim();
                    _operatorName = loginForm.OperatorName.Trim();
                    _serverIp = loginForm.ServerIp.Trim();
                    int.TryParse(loginForm.ServerPort.Trim(), out _serverPort);
                    _serverTokenKey = loginForm.TokenKey.Trim();

                    // อัปเดตสถานะปุ่ม Avatar Pill
                    btn_userPill?.SetState(true, _operatorName);
                    UpdateServerCheckboxState(true);

                    LogMessage($"[AUTH] เข้าสู่ระบบสำเร็จ: {_operatorName} ({_operatorEmail})", LogSuccessColor);
                }
            }
        }
    }
}
