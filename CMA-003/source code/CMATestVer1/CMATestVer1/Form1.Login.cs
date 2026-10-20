namespace CMATestVer1
{
    public partial class Form1
    {
        private const string LoginApiPath = "/api/auth/login";
        private string _operatorToken = string.Empty;
        private string _operatorUsername = "admin";

        private bool IsOperatorLoggedIn => !string.IsNullOrWhiteSpace(_operatorToken);

        private void InitializeLoginFeature()
        {
            button1.Click += LoginButton_Click;
            login.Click += LoginButton_Click;
            label81.Click += LoginButton_Click;
            myRoundedPanel74.Click += LoginButton_Click;

            // ปิดการติ๊ก Checkbox ส่ง Server ไว้ก่อนจนกว่าจะ Login สำเร็จ
            sent_server.Enabled = false;
            sent_server.Checked = false;
        }

        private void LoginButton_Click(object? sender, EventArgs e)
        {
            if (!TryGetQcResultApiUrl(out string resultApiUrl))
                return;

            string loginApiUrl = new Uri(new Uri(resultApiUrl), LoginApiPath).AbsoluteUri;

            using var form3 = new Form3(loginApiUrl);
            if (form3.ShowDialog(this) == DialogResult.OK)
            {
                _operatorToken = form3.OperatorToken.Trim();
                _operatorUsername = string.IsNullOrWhiteSpace(form3.OperatorUsername)
                    ? "admin"
                    : form3.OperatorUsername.Trim();

                // อัปเดตสถานะปุ่ม button1
                button1.Text = "Login\nเรียบร้อย";
                button1.BackColor = Color.LightGreen;
                button1.ForeColor = Color.DarkGreen;
                button1.UseVisualStyleBackColor = false;

                // อัปเดตสถานะกรอบรูป Login ด้านบน
                label81.Text = "Login OK";
                myRoundedPanel74.BackColor = Color.FromArgb(212, 243, 218);

                // ปลดล็อก Checkbox ให้สามารถเลือกส่ง Server ได้
                sent_server.Enabled = true;

                lblResultStatus.Text = $"✅ เข้าสู่ระบบสำเร็จ (User: {_operatorUsername})";
                lblResultStatus.ForeColor = Color.Green;
                LogToRx($"[AUTH] เข้าสู่ระบบสำเร็จ โดยผู้ใช้: {_operatorUsername}", Color.Green);
            }
        }
    }
}
