using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMATestVer1
{
    public partial class Form3 : Form
    {
        private static readonly HttpClient client = new HttpClient();

        public string OperatorToken { get; private set; } = string.Empty;
        public string OperatorUsername { get; private set; } = string.Empty;
        private readonly string _loginApiUrl;

        public Form3() : this("http://192.168.109.54:5088/api/auth/login")
        {
        }

        public Form3(string loginApiUrl)
        {
            InitializeComponent();
            _loginApiUrl = string.IsNullOrWhiteSpace(loginApiUrl)
                ? "http://192.168.109.54:5088/api/auth/login"
                : loginApiUrl;
        }

        //-- submit และ signup
        private async void btnSubmit_Click(object sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string requestJson = JsonConvert.SerializeObject(new
            {
                username = user,
                password = pass
            });

            try
            {
                btnSubmit.Enabled = false;
                lblStatus.Text = "⏳ กำลังตรวจสอบสิทธิ์...";
                lblStatus.ForeColor = Color.Orange;

                using var request = new HttpRequestMessage(HttpMethod.Post, _loginApiUrl);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using HttpResponseMessage response = await client.SendAsync(request, timeout.Token);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string receivedToken = ExtractToken(responseText);

                    if (string.IsNullOrEmpty(receivedToken))
                    {
                        MessageBox.Show("ไม่พบ Token จากเซิร์ฟเวอร์ กรุณาตรวจสอบ Response", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblStatus.Text = "❌ ไม่พบ Token";
                        lblStatus.ForeColor = Color.Red;
                        return;
                    }

                    OperatorToken = receivedToken.Trim();
                    OperatorUsername = user;

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"รหัสผ่านไม่ถูกต้อง ({response.StatusCode})\n{responseText}", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    lblStatus.Text = "❌ รหัสผ่านไม่ถูกต้อง";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Server ไม่ตอบกลับภายใน 10 วินาที", "Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lblStatus.Text = "⚠ Server Timeout";
                lblStatus.ForeColor = Color.DarkRed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ไม่สามารถติดต่อเซิร์ฟเวอร์ได้: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lblStatus.Text = "⚠ เชื่อมต่อล้มเหลว";
                lblStatus.ForeColor = Color.DarkRed;
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
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string jsonBody = JsonConvert.SerializeObject(new
            {
                name = Name,
                email = Email,
                password = pass
            });

            try
            {
                btnSignUp.Enabled = false;
                lblStatus.Text = "⏳ กำลัง Sign Up ";
                lblStatus.ForeColor = Color.Orange;

                using var request = new HttpRequestMessage(HttpMethod.Post, _loginApiUrl);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using HttpResponseMessage response = await client.SendAsync(request, timeout.Token);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string receivedToken = ExtractToken(responseText);
                    OperatorToken = receivedToken.Trim();
                    OperatorUsername = Name;

                    MessageBox.Show("สมัครเรียบร้อย", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"สมัครไม่สำเร็จ ({response.StatusCode})\n{responseText}", "Sign Up Failed", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    lblStatus.Text = "❌ สมัครไม่สำเร็จ";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ไม่สามารถติดต่อเซิร์ฟเวอร์ได้: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lblStatus.Text = "⚠ เชื่อมต่อล้มเหลว";
                lblStatus.ForeColor = Color.DarkRed;
            }
            finally
            {
                btnSignUp.Enabled = true;
            }
        }

        private static string ExtractToken(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return string.Empty;

            try
            {
                JToken root = JToken.Parse(responseText);
                string[] tokenPaths =
                {
                    "data.access_token",
                    "data.token",
                    "access_token",
                    "token",
                    "data.device_token",
                    "data.api_key"
                };

                foreach (string path in tokenPaths)
                {
                    string? value = root.SelectToken(path)?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch
            {
                // Raw token string fallback
                if (!responseText.TrimStart().StartsWith("{") && !responseText.TrimStart().StartsWith("["))
                    return responseText.Trim();
            }

            return string.Empty;
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
            tabControl2.SelectedIndex = 1;
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            tabControl2.SelectedIndex = 0;
        }
    }
}
