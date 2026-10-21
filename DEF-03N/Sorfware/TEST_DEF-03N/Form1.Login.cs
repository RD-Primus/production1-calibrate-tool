namespace TEST_DEF_03N
{
    public partial class Form1
    {
        private string _operatorToken = string.Empty;
        private string _operatorUsername = string.Empty;

        private bool IsOperatorLoggedIn => !string.IsNullOrWhiteSpace(_operatorToken);

        private void InitializeLoginFeature()
        {
            if (btn_login != null)
            {
                btn_login.Click += LoginButton_Click;
                btn_login.Text = "Login..";
                btn_login.UseVisualStyleBackColor = true;
            }

            // ปิดการติ๊ก Checkbox ส่ง Server ไว้ก่อนจนกว่า Operator จะ Login สำเร็จ
            sent_server.Enabled = false;
            sent_server.Checked = false;
        }

        private void LoginButton_Click(object? sender, EventArgs e)
        {
            using var loginForm = new LoginForm();
            if (loginForm.ShowDialog(this) == DialogResult.OK)
            {
                _operatorToken = loginForm.OperatorToken.Trim();
                _operatorUsername = loginForm.OperatorUsername.Trim();

                try
                {
                    Settings1.Default.SavedOperatorUsername = _operatorUsername;
                    Settings1.Default.Save();
                }
                catch { }

                if (btn_login != null)
                {
                    btn_login.Text = "Login OK";
                    btn_login.BackColor = Color.FromArgb(46, 125, 50);
                    btn_login.ForeColor = Color.White;
                    btn_login.UseVisualStyleBackColor = false;
                }

                sent_server.Enabled = true;
                LogToRx($"[AUTH] เข้าสู่ระบบสำเร็จ โดยผู้ใช้: {_operatorUsername}", Color.Green);
            }
        }
    }
}
