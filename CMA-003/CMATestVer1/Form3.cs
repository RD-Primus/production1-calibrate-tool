using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMATestVer1
{
    public partial class Form3 : Form
    {
        // สร้าง HttpClient เตรียมไว้สำหรับยิง API
        private static readonly HttpClient client = new HttpClient();

        public Form3()
        {
            InitializeComponent();
        }

        //-- submit และ signup
        private async void btnSubmit_Click(object sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน", "แจ้งเตือน");
                return;
            }

            // 📦 แพ็คข้อมูลเตรียม POST ไปยืนยันตัวตน
            string jsonBody = $@"{{
                                    ""username"": ""{user}"",
                                    ""password"": ""{pass}""
                                 }}";

            string url = "http://192.168.109.170:5180/api/auth/login";

            try
            {
                btnSubmit.Enabled = false;
                lblStatus.Text = "⏳ กำลังตรวจสอบสิทธิ์...";
                lblStatus.ForeColor = System.Drawing.Color.Orange;

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    // ✅ แกะ Token ออกจาก JSON
                    using var doc = System.Text.Json.JsonDocument.Parse(responseText);
                    string receivedToken = doc.RootElement
                                                          .GetProperty("data")
                                                          .GetProperty("access_token")
                                                          .GetString() ?? "";

                    if (string.IsNullOrEmpty(receivedToken))
                    {
                        MessageBox.Show("ไม่พบ Token จากเซิร์ฟเวอร์!", "Error");
                        return;
                    }

                    MessageBox.Show("เข้าสู่ระบบสำเร็จ!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 🚀 เปิดหน้าหลัก Form1 พร้อมโยน Token ติดตัวไปด้วย
                    Form1 mainForm = new Form1(receivedToken);
                    mainForm.Show();

                    this.Hide(); // ซ่อนหน้า Login
                }
                else
                {
                    MessageBox.Show($"รหัสผ่านไม่ถูกต้อง ({response.StatusCode})", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    lblStatus.Text = "❌ รหัสผ่านไม่ถูกต้อง";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch
            {
                MessageBox.Show("ไม่สามารถติดต่อเซิร์ฟเวอร์ได้", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lblStatus.Text = "⚠ เชื่อมต่อล้มเหลว";
                lblStatus.ForeColor = System.Drawing.Color.DarkRed;
            }
            finally
            {
                btnSubmit.Enabled = true;
            }
        }
        private async void btnSignUp_Click(object sender, EventArgs e)
        {
            string Name = txtName.Text.Trim();
            string Email = TxtEmail.Text.Trim();
            string pass = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน", "แจ้งเตือน");
                return;
            }

            string jsonBody = $@"{{
                                     ""name"": ""{Name}"",
                                     ""email"": ""{Email}"",
                                     ""password"": ""{pass}""
                                 }}";

            string url = "http://192.168.109.170:5180/api/auth/login";

            try
            {
                btnSubmit.Enabled = false;
                lblStatus.Text = "⏳ กำลัง Sign Up ";
                lblStatus.ForeColor = System.Drawing.Color.Orange;

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    // 🎟️ สมมติว่าหลังบ้านส่งรหัส Token กลับมาดื้อๆ ในข้อความ (หรือแกะจาก JSON)
                    string receivedToken = responseText;

                    MessageBox.Show("สมัครเรียบร้อย", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 🚀 เปิดหน้าหลัก Form1 พร้อมโยน Token ติดตัวไปด้วย
                    Form1 mainForm = new Form1(receivedToken);
                    mainForm.Show();

                    this.Hide(); // ซ่อนหน้า Login
                }
                else
                {
                    MessageBox.Show($"สมัครไม่สำเร็จ ({response.StatusCode})", "Sign Up Failed", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    lblStatus.Text = "❌ สมัครไม่สำเร็จ";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch
            {
                MessageBox.Show("ไม่สามารถติดต่อเซิร์ฟเวอร์ได้", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lblStatus.Text = "⚠ เชื่อมต่อล้มเหลว";
                lblStatus.ForeColor = System.Drawing.Color.DarkRed;
            }
            finally
            {
                btnSubmit.Enabled = true;
            }

        }


        //-- ซ่อน password
        private void txtPass_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnSubmit_Click(this, new EventArgs());
            }
        }
        private void showpass_Click(object sender, EventArgs e)
        {
            if (txtPass.PasswordChar == '*')
            {
                txtPass.PasswordChar = '\0';
                showpass.Image = Properties.Resources.eye_close;
            }
            else
            {
                txtPass.PasswordChar = '*';
                showpass.Image = Properties.Resources.eye_open;
            }
        }


        //-- สลับหน้า
        private void NewUser_Click(object sender, EventArgs e)
        {
            tabControl2.SelectedIndex = 1; ;
        }
        private void btnBack_Click(object sender, EventArgs e)
        {
            tabControl2.SelectedIndex = 0; ;
        }
    }
}
