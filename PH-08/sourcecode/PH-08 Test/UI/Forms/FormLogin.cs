using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PH_08_Test
{
    public class FormLogin : Form
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly string ConfigFilePath = Path.Combine(Application.StartupPath, "server_config.json");

        // Title Bar
        private Button btnClose = null!;

        // Server Section
        private CustomTextBox txtIp = null!;
        private CustomTextBox txtPort = null!;
        private CustomTextBox txtTokenKey = null!;
        private Button btnToggleToken = null!;

        // Login Section
        private CustomTextBox txtEmail = null!;
        private CustomTextBox txtPassword = null!;
        private Button btnTogglePassword = null!;

        // Error & Action
        private Label lblError = null!;
        private CustomRoundedButton btnCancel = null!;
        private CustomRoundedButton btnLogin = null!;

        // Results
        public string OperatorToken { get; private set; } = string.Empty;
        public string OperatorName { get; private set; } = string.Empty;

        public string ServerIp
        {
            get => txtIp.Text.Trim();
            set => txtIp.Text = value;
        }

        public string ServerPort
        {
            get => txtPort.Text.Trim();
            set => txtPort.Text = value;
        }

        public string TokenKey
        {
            get => txtTokenKey.Text.Trim();
            set => txtTokenKey.Text = value;
        }

        public string Email
        {
            get => txtEmail.Text.Trim();
            set => txtEmail.Text = value;
        }

        public string Password
        {
            get => txtPassword.Text.Trim();
            set => txtPassword.Text = value;
        }

        public string ErrorMessage
        {
            get => lblError.Text;
            set
            {
                lblError.Text = value;
                lblError.Visible = !string.IsNullOrWhiteSpace(value);
            }
        }

        protected override CreateParams CreateParams => FormDropShadowHelper.ApplyCreateParams(base.CreateParams);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FormDropShadowHelper.ApplyDropShadow(this);
        }

        public FormLogin()
        {
            InitializeComponentCustom();
            LoadConfig();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Login";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(460, 490);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            // Form Dragging
            this.MouseDown += Form_MouseDown;
            this.KeyDown += Form_KeyDown;

            // Top-right close button
            btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252),
                Size = new Size(32, 30),
                Location = new Point(418, 5),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 226, 226);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(100, 116, 139);
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnClose);

            int padX = 28;
            int contentW = 404;

            // ----------------------------------------------------
            // SECTION 1: SERVER
            // ----------------------------------------------------
            var lblServerHeader = new Label
            {
                Text = "SERVER",
                Font = new Font("Leelawadee UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 108, 67),
                Location = new Point(padX, 55),
                AutoSize = true
            };
            this.Controls.Add(lblServerHeader);

            var lineServer = new CustomLine
            {
                LineColor = Color.FromArgb(230, 235, 238),
                Thickness = 1,
                Location = new Point(padX + 70, 65),
                Size = new Size(contentW - 70, 2)
            };
            this.Controls.Add(lineServer);

            // Row 1: IP & Port
            int ipW = 285;
            int gap = 14;
            int portW = contentW - ipW - gap;

            var lblIp = new Label
            {
                Text = "IP",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(85, 95, 105),
                Location = new Point(padX, 86),
                AutoSize = true
            };
            this.Controls.Add(lblIp);

            var lblPort = new Label
            {
                Text = "Port",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(85, 95, 105),
                Location = new Point(padX + ipW + gap, 86),
                AutoSize = true
            };
            this.Controls.Add(lblPort);

            txtIp = new CustomTextBox
            {
                Location = new Point(padX, 106),
                Size = new Size(ipW, 36),
                BorderRadius = 6,
                BorderSize = 1,
                BorderColor = Color.FromArgb(209, 213, 219),
                BorderFocusColor = Color.FromArgb(20, 108, 67),
                Font = new Font("Leelawadee UI", 10F, FontStyle.Regular),
                Text = "192.168.1.10",
                InnerPadding = 8
            };
            this.Controls.Add(txtIp);

            txtPort = new CustomTextBox
            {
                Location = new Point(padX + ipW + gap, 106),
                Size = new Size(portW, 36),
                BorderRadius = 6,
                BorderSize = 1,
                BorderColor = Color.FromArgb(209, 213, 219),
                BorderFocusColor = Color.FromArgb(20, 108, 67),
                Font = new Font("Leelawadee UI", 10F, FontStyle.Regular),
                Text = "8080",
                InnerPadding = 8
            };
            this.Controls.Add(txtPort);

            // Row 2: Token Key
            var lblToken = new Label
            {
                Text = "Token Key",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(85, 95, 105),
                Location = new Point(padX, 150),
                AutoSize = true
            };
            this.Controls.Add(lblToken);

            txtTokenKey = new CustomTextBox
            {
                Location = new Point(padX, 170),
                Size = new Size(contentW, 36),
                BorderRadius = 6,
                BorderSize = 1,
                BorderColor = Color.FromArgb(209, 213, 219),
                BorderFocusColor = Color.FromArgb(20, 108, 67),
                Font = new Font("Leelawadee UI", 10F, FontStyle.Regular),
                UseSystemPasswordChar = true,
                Text = "",
                InnerPadding = 8
            };
            this.Controls.Add(txtTokenKey);

            // Eye button to toggle Token Key visibility
            btnToggleToken = new Button
            {
                Text = "👁",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(120, 130, 140),
                BackColor = Color.Transparent,
                Size = new Size(30, 30),
                Location = new Point(padX + contentW - 34, 173),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnToggleToken.FlatAppearance.BorderSize = 0;
            btnToggleToken.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 243, 246);
            btnToggleToken.Click += (s, e) =>
            {
                txtTokenKey.UseSystemPasswordChar = !txtTokenKey.UseSystemPasswordChar;
                btnToggleToken.ForeColor = txtTokenKey.UseSystemPasswordChar
                    ? Color.FromArgb(120, 130, 140)
                    : Color.FromArgb(20, 108, 67);
            };
            this.Controls.Add(btnToggleToken);
            btnToggleToken.BringToFront();

            // ----------------------------------------------------
            // SECTION 2: LOGIN
            // ----------------------------------------------------
            var lblLoginHeader = new Label
            {
                Text = "LOGIN",
                Font = new Font("Leelawadee UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 108, 67),
                Location = new Point(padX, 222),
                AutoSize = true
            };
            this.Controls.Add(lblLoginHeader);

            var lineLogin = new CustomLine
            {
                LineColor = Color.FromArgb(230, 235, 238),
                Thickness = 1,
                Location = new Point(padX + 60, 232),
                Size = new Size(contentW - 60, 2)
            };
            this.Controls.Add(lineLogin);

            // Row 3: E-mail
            var lblEmail = new Label
            {
                Text = "E-mail",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(85, 95, 105),
                Location = new Point(padX, 252),
                AutoSize = true
            };
            this.Controls.Add(lblEmail);

            txtEmail = new CustomTextBox
            {
                Location = new Point(padX, 272),
                Size = new Size(contentW, 36),
                BorderRadius = 6,
                BorderSize = 1,
                BorderColor = Color.FromArgb(209, 213, 219),
                BorderFocusColor = Color.FromArgb(20, 108, 67),
                Font = new Font("Leelawadee UI", 10F, FontStyle.Regular),
                Text = "",
                InnerPadding = 8
            };
            this.Controls.Add(txtEmail);

            // Row 4: Password
            var lblPassword = new Label
            {
                Text = "Password",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(85, 95, 105),
                Location = new Point(padX, 318),
                AutoSize = true
            };
            this.Controls.Add(lblPassword);

            txtPassword = new CustomTextBox
            {
                Location = new Point(padX, 338),
                Size = new Size(contentW, 36),
                BorderRadius = 6,
                BorderSize = 1,
                BorderColor = Color.FromArgb(209, 213, 219),
                BorderFocusColor = Color.FromArgb(20, 108, 67),
                Font = new Font("Leelawadee UI", 10F, FontStyle.Regular),
                UseSystemPasswordChar = true,
                Text = "",
                InnerPadding = 8
            };
            this.Controls.Add(txtPassword);

            // Eye button to toggle password visibility
            btnTogglePassword = new Button
            {
                Text = "👁",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(120, 130, 140),
                BackColor = Color.Transparent,
                Size = new Size(30, 30),
                Location = new Point(padX + contentW - 34, 341),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 243, 246);
            btnTogglePassword.Click += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                btnTogglePassword.ForeColor = txtPassword.UseSystemPasswordChar
                    ? Color.FromArgb(120, 130, 140)
                    : Color.FromArgb(20, 108, 67);
            };
            this.Controls.Add(btnTogglePassword);
            btnTogglePassword.BringToFront();

            // Row 5: Error / Status Label
            lblError = new Label
            {
                Text = "E-mail หรือ Password ไม่ถูกต้อง",
                Font = new Font("Leelawadee UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(211, 47, 47),
                Location = new Point(padX, 384),
                AutoSize = true,
                Visible = false
            };
            this.Controls.Add(lblError);

            // Row 6: Action Buttons (Cancel & Login)
            int cancelW = 110;
            int loginW = contentW - cancelW - gap;

            btnCancel = new CustomRoundedButton
            {
                Text = "Cancel",
                Font = new Font("Leelawadee UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 80, 90),
                BackColor = Color.White,
                BorderColor = Color.FromArgb(209, 213, 219),
                BorderThickness = 1,
                BorderRadius = 8,
                Location = new Point(padX, 415),
                Size = new Size(cancelW, 40),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);

            btnLogin = new CustomRoundedButton
            {
                Text = "Login",
                Font = new Font("Leelawadee UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(22, 138, 70),
                BorderColor = Color.FromArgb(22, 138, 70),
                BorderThickness = 0,
                BorderRadius = 8,
                Location = new Point(padX + cancelW + gap, 415),
                Size = new Size(loginW, 40),
                Cursor = Cursors.Hand
            };
            btnLogin.Click += async (s, e) => await SubmitLoginAsync();
            this.Controls.Add(btnLogin);
        }

        private async Task SubmitLoginAsync()
        {
            string ip = txtIp.Text.Trim();
            string portText = txtPort.Text.Trim();
            string tokenKey = txtTokenKey.Text.Trim();
            string user = txtEmail.Text.Trim();
            string pass = txtPassword.Text;

            // 1. ตรวจสอบ IP
            if (!IPAddress.TryParse(ip, out IPAddress? address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                lblError.ForeColor = Color.FromArgb(211, 47, 47);
                ErrorMessage = "กรุณากรอก IP Address ให้ถูกต้อง เช่น 192.168.1.10";
                txtIp.Focus();
                return;
            }

            // 2. ตรวจสอบ Port
            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                lblError.ForeColor = Color.FromArgb(211, 47, 47);
                ErrorMessage = "กรุณากรอก Port ให้ถูกต้อง (1-65535)";
                txtPort.Focus();
                return;
            }

            // 3. ตรวจสอบ Token Key
            if (string.IsNullOrWhiteSpace(tokenKey))
            {
                lblError.ForeColor = Color.FromArgb(211, 47, 47);
                ErrorMessage = "กรุณากรอก Token Key";
                txtTokenKey.Focus();
                return;
            }

            // 4. ตรวจสอบ User / Password
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                lblError.ForeColor = Color.FromArgb(211, 47, 47);
                ErrorMessage = "กรุณากรอก E-mail และ Password ให้ครบถ้วน";
                if (string.IsNullOrWhiteSpace(user)) txtEmail.Focus();
                else txtPassword.Focus();
                return;
            }

            string loginUrl = new UriBuilder("http", address.ToString(), port, "/api/auth/login").Uri.AbsoluteUri;

            string requestJson = JsonSerializer.Serialize(new
            {
                username = user,
                password = pass
            });

            try
            {
                btnLogin.Enabled = false;
                lblError.ForeColor = Color.DarkOrange;
                lblError.Text = "⏳ กำลังตรวจสอบสิทธิ์...";
                lblError.Visible = true;

                using var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(tokenKey))
                {
                    request.Headers.TryAddWithoutValidation("X-API-KEY", tokenKey);
                }

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using HttpResponseMessage response = await HttpClient.SendAsync(request, timeout.Token);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string receivedToken = ExtractToken(responseText);
                    if (string.IsNullOrEmpty(receivedToken))
                    {
                        lblError.ForeColor = Color.FromArgb(211, 47, 47);
                        ErrorMessage = "ไม่พบ Token ตอบกลับจากเซิร์ฟเวอร์";
                        return;
                    }

                    OperatorToken = receivedToken.Trim();
                    string extractedName = ExtractOperatorName(responseText);
                    OperatorName = !string.IsNullOrWhiteSpace(extractedName) ? extractedName : user;
                    SaveConfig();

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblError.ForeColor = Color.FromArgb(211, 47, 47);
                    ErrorMessage = "E-mail หรือ Password ไม่ถูกต้อง";
                }
            }
            catch (OperationCanceledException)
            {
                lblError.ForeColor = Color.FromArgb(211, 47, 47);
                ErrorMessage = "Server ไม่ตอบกลับภายใน 10 วินาที (Timeout)";
            }
            catch (Exception ex)
            {
                lblError.ForeColor = Color.FromArgb(211, 47, 47);
                ErrorMessage = $"เชื่อมต่อเซิร์ฟเวอร์ไม่ได้: {ex.Message}";
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }

        private static string ExtractToken(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(responseText);
                JsonElement root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    string[] directKeys = { "access_token", "token", "device_token", "api_key", "key" };
                    foreach (var key in directKeys)
                    {
                        if (root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                        {
                            string val = prop.GetString() ?? "";
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                    }

                    if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var key in directKeys)
                        {
                            if (data.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                            {
                                string val = prop.GetString() ?? "";
                                if (!string.IsNullOrWhiteSpace(val)) return val;
                            }
                        }
                    }
                }
            }
            catch
            {
                if (!responseText.TrimStart().StartsWith("{") && !responseText.TrimStart().StartsWith("["))
                    return responseText.Trim();
            }

            return string.Empty;
        }

        private static string ExtractOperatorName(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(responseText);
                JsonElement root = doc.RootElement;

                string[] nameKeys = { "name", "full_name", "fullname", "display_name", "displayName", "operator_name", "operator", "employee_name", "thai_name", "username" };

                string? FindName(JsonElement element)
                {
                    if (element.ValueKind != JsonValueKind.Object) return null;
                    foreach (var key in nameKeys)
                    {
                        if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                        {
                            string val = prop.GetString()?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                    }
                    return null;
                }

                if (root.ValueKind == JsonValueKind.Object)
                {
                    string? name = FindName(root);
                    if (!string.IsNullOrEmpty(name)) return name;

                    if (root.TryGetProperty("user", out var userEl))
                    {
                        name = FindName(userEl);
                        if (!string.IsNullOrEmpty(name)) return name;
                    }

                    if (root.TryGetProperty("data", out var dataEl))
                    {
                        name = FindName(dataEl);
                        if (!string.IsNullOrEmpty(name)) return name;

                        if (dataEl.TryGetProperty("user", out var dataUserEl))
                        {
                            name = FindName(dataUserEl);
                            if (!string.IsNullOrEmpty(name)) return name;
                        }
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("ServerIp", out var ipProp)) txtIp.Text = ipProp.GetString() ?? "192.168.1.10";
                    if (root.TryGetProperty("ServerPort", out var portProp)) txtPort.Text = portProp.GetString() ?? "8080";
                    if (root.TryGetProperty("TokenKey", out var tokenProp)) txtTokenKey.Text = tokenProp.GetString() ?? "";
                }
            }
            catch { }

            txtEmail.Text = string.Empty;
            txtPassword.Text = string.Empty;
        }

        private void SaveConfig()
        {
            try
            {
                var config = new
                {
                    ServerIp = txtIp.Text.Trim(),
                    ServerPort = txtPort.Text.Trim(),
                    TokenKey = txtTokenKey.Text.Trim(),
                    Email = txtEmail.Text.Trim()
                };

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch { }
        }

        private async void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SubmitLoginAsync();
            }
        }

        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y <= 40)
            {
                this.Capture = false;
                Message msg = Message.Create(this.Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
                this.DefWndProc(ref msg);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Form Background & Elevated Border with Header
            using (var bgBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }
            FormDropShadowHelper.DrawElevatedBorder(this, g, borderWidth: 1.5f, headerHeight: 40);

            // Header Title
            using (var titleFont = new Font("Leelawadee UI", 10.5F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.DrawString("Login", titleFont, titleBrush, 20, 10);
            }
        }
    }
}
