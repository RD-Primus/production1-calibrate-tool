using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    internal sealed class LoginForm : Form
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        // ─── Theme Colors (Dark Theme matching Form1) ───
        private static readonly Color FormBg = Color.FromArgb(8, 21, 34);
        private static readonly Color HeaderBg = Color.FromArgb(12, 29, 46);
        private static readonly Color SectionHeaderColor = Color.FromArgb(73, 218, 242);
        private static readonly Color LabelColor = Color.FromArgb(180, 205, 225);
        private static readonly Color InputBg = Color.FromArgb(16, 38, 58);
        private static readonly Color InputBorderColor = Color.FromArgb(40, 75, 105);
        private static readonly Color DividerColor = Color.FromArgb(30, 60, 85);
        private static readonly Color ErrorColor = Color.FromArgb(255, 105, 105);
        private static readonly Color CancelBtnBg = Color.FromArgb(28, 52, 75);
        private static readonly Color LoginBtnBg = Color.FromArgb(46, 125, 50);
        private static readonly Color CloseBtnBg = Color.FromArgb(182, 56, 33);

        // ─── Controls ───
        private readonly TextBox _txtIp = new TextBox();
        private readonly TextBox _txtPort = new TextBox();
        private readonly TextBox _txtToken = new TextBox();
        private readonly Button _btnToggleToken = new Button();

        private readonly TextBox _txtUser = new TextBox();
        private readonly TextBox _txtPass = new TextBox();
        private readonly Button _btnTogglePass = new Button();

        private readonly Label _lblStatus = new Label();
        private readonly Button _btnCancel = new Button();
        private readonly Button _btnLogin = new Button();

        // Dragging window
        private bool _isDragging;
        private Point _dragStartCursor;
        private Point _dragStartForm;

        internal string OperatorToken { get; private set; } = string.Empty;
        internal string OperatorUsername { get; private set; } = string.Empty;

        protected override CreateParams CreateParams
        {
            get
            {
                const int CsDropShadow = 0x00020000;
                CreateParams parameters = base.CreateParams;
                parameters.ClassStyle |= CsDropShadow;
                return parameters;
            }
        }

        internal LoginForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = FormBg;
            ClientSize = new Size(440, 520);
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            Text = "Server & Login Config";

            BuildUi();
            LoadSavedSettings();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedRegion(this, 10);
            _txtUser.Focus();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion(this, 10);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle borderBounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using var borderPen = new Pen(Color.FromArgb(60, SectionHeaderColor), 1.5F);
            using var borderPath = CreateRoundedPath(borderBounds, 8);
            e.Graphics.DrawPath(borderPen, borderPath);
        }

        private void LoadSavedSettings()
        {
            try
            {
                _txtIp.Text = string.IsNullOrWhiteSpace(Settings1.Default.SavedServerIp)
                    ? "192.168.109.54"
                    : Settings1.Default.SavedServerIp.Trim();

                _txtPort.Text = string.IsNullOrWhiteSpace(Settings1.Default.SavedServerPort)
                    ? "5088"
                    : Settings1.Default.SavedServerPort.Trim();

                _txtToken.Text = Settings1.Default.SavedApiToken ?? "";

                _txtUser.Text = string.Empty;
                _txtPass.Text = string.Empty;
            }
            catch
            {
                _txtIp.Text = "192.168.109.54";
                _txtPort.Text = "5088";
                _txtUser.Text = string.Empty;
                _txtPass.Text = string.Empty;
            }
        }

        private void BuildUi()
        {
            // 1. Header (Draggable + Close 'X')
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = HeaderBg,
                Cursor = Cursors.SizeAll
            };

            var headerTitle = new Label
            {
                Text = "LOGIN PRIMUS",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 10),
                AutoSize = true,
                Cursor = Cursors.SizeAll
            };

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = CloseBtnBg,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(38, 28),
                Location = new Point(Width - 46, 8),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            ApplyRoundedRegion(btnClose, 4);

            header.Controls.Add(headerTitle);
            header.Controls.Add(btnClose);

            header.MouseDown += Header_MouseDown;
            header.MouseMove += Header_MouseMove;
            header.MouseUp += Header_MouseUp;
            headerTitle.MouseDown += Header_MouseDown;
            headerTitle.MouseMove += Header_MouseMove;
            headerTitle.MouseUp += Header_MouseUp;

            Controls.Add(header);

            // 2. Body Panel
            var body = new Panel
            {
                Location = new Point(24, 52),
                Size = new Size(392, 452),
                BackColor = FormBg
            };

            int currentY = 8;

            // ─── SECTION 1: SERVER ───
            var lblServerHeader = new Label
            {
                Text = "SERVER",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = SectionHeaderColor,
                Location = new Point(0, currentY),
                AutoSize = true
            };
            body.Controls.Add(lblServerHeader);

            var serverDivider = new Panel
            {
                Location = new Point(74, currentY + 10),
                Size = new Size(318, 1),
                BackColor = DividerColor
            };
            body.Controls.Add(serverDivider);

            currentY += 28;

            // Row: IP and Port
            var lblIp = new Label
            {
                Text = "IP",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = LabelColor,
                Location = new Point(0, currentY),
                Size = new Size(260, 18)
            };
            body.Controls.Add(lblIp);

            var lblPort = new Label
            {
                Text = "Port",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = LabelColor,
                Location = new Point(275, currentY),
                Size = new Size(117, 18)
            };
            body.Controls.Add(lblPort);

            currentY += 20;

            // IP Input Box Container
            var ipBox = CreateInputContainer(new Point(0, currentY), new Size(260, 32));
            _txtIp.Location = new Point(8, 5);
            _txtIp.Size = new Size(244, 22);
            _txtIp.Font = new Font("Segoe UI", 10F);
            _txtIp.BackColor = InputBg;
            _txtIp.ForeColor = Color.White;
            _txtIp.BorderStyle = BorderStyle.None;
            _txtIp.KeyDown += Input_KeyDown;
            ipBox.Controls.Add(_txtIp);
            body.Controls.Add(ipBox);

            // Port Input Box Container
            var portBox = CreateInputContainer(new Point(275, currentY), new Size(117, 32));
            _txtPort.Location = new Point(8, 5);
            _txtPort.Size = new Size(101, 22);
            _txtPort.Font = new Font("Segoe UI", 10F);
            _txtPort.BackColor = InputBg;
            _txtPort.ForeColor = Color.White;
            _txtPort.BorderStyle = BorderStyle.None;
            _txtPort.KeyDown += Input_KeyDown;
            portBox.Controls.Add(_txtPort);
            body.Controls.Add(portBox);

            currentY += 40;

            // Token Key Label
            var lblToken = new Label
            {
                Text = "Token Key",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = LabelColor,
                Location = new Point(0, currentY),
                Size = new Size(392, 18)
            };
            body.Controls.Add(lblToken);

            currentY += 20;

            // Token Key Container (with eye toggle)
            var tokenBox = CreateInputContainer(new Point(0, currentY), new Size(392, 32));
            _txtToken.Location = new Point(8, 5);
            _txtToken.Size = new Size(346, 22);
            _txtToken.Font = new Font("Segoe UI", 10F);
            _txtToken.BackColor = InputBg;
            _txtToken.ForeColor = Color.White;
            _txtToken.BorderStyle = BorderStyle.None;
            _txtToken.PasswordChar = '●';
            _txtToken.KeyDown += Input_KeyDown;
            tokenBox.Controls.Add(_txtToken);

            _btnToggleToken.Location = new Point(358, 2);
            _btnToggleToken.Size = new Size(30, 28);
            _btnToggleToken.FlatStyle = FlatStyle.Flat;
            _btnToggleToken.FlatAppearance.BorderSize = 0;
            _btnToggleToken.BackColor = InputBg;
            _btnToggleToken.ForeColor = LabelColor;
            _btnToggleToken.Font = new Font("Segoe UI", 10F);
            _btnToggleToken.Text = "👁️";
            _btnToggleToken.Cursor = Cursors.Hand;
            _btnToggleToken.Click += BtnToggleToken_Click;
            tokenBox.Controls.Add(_btnToggleToken);

            body.Controls.Add(tokenBox);

            currentY += 46;

            // ─── SECTION 2: LOGIN ───
            var lblLoginHeader = new Label
            {
                Text = "LOGIN",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = SectionHeaderColor,
                Location = new Point(0, currentY),
                AutoSize = true
            };
            body.Controls.Add(lblLoginHeader);

            var loginDivider = new Panel
            {
                Location = new Point(66, currentY + 10),
                Size = new Size(326, 1),
                BackColor = DividerColor
            };
            body.Controls.Add(loginDivider);

            currentY += 28;

            // E-mail Label
            var lblUser = new Label
            {
                Text = "E-mail",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = LabelColor,
                Location = new Point(0, currentY),
                Size = new Size(392, 18)
            };
            body.Controls.Add(lblUser);

            currentY += 20;

            // E-mail Input Container
            var userBox = CreateInputContainer(new Point(0, currentY), new Size(392, 32));
            _txtUser.Location = new Point(8, 5);
            _txtUser.Size = new Size(376, 22);
            _txtUser.Font = new Font("Segoe UI", 10F);
            _txtUser.BackColor = InputBg;
            _txtUser.ForeColor = Color.White;
            _txtUser.BorderStyle = BorderStyle.None;
            _txtUser.KeyDown += Input_KeyDown;
            userBox.Controls.Add(_txtUser);
            body.Controls.Add(userBox);

            currentY += 40;

            // Password Label
            var lblPass = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = LabelColor,
                Location = new Point(0, currentY),
                Size = new Size(392, 18)
            };
            body.Controls.Add(lblPass);

            currentY += 20;

            // Password Input Container (with eye toggle)
            var passBox = CreateInputContainer(new Point(0, currentY), new Size(392, 32));
            _txtPass.Location = new Point(8, 5);
            _txtPass.Size = new Size(346, 22);
            _txtPass.Font = new Font("Segoe UI", 10F);
            _txtPass.BackColor = InputBg;
            _txtPass.ForeColor = Color.White;
            _txtPass.BorderStyle = BorderStyle.None;
            _txtPass.PasswordChar = '●';
            _txtPass.KeyDown += Input_KeyDown;
            passBox.Controls.Add(_txtPass);

            _btnTogglePass.Location = new Point(358, 2);
            _btnTogglePass.Size = new Size(30, 28);
            _btnTogglePass.FlatStyle = FlatStyle.Flat;
            _btnTogglePass.FlatAppearance.BorderSize = 0;
            _btnTogglePass.BackColor = InputBg;
            _btnTogglePass.ForeColor = LabelColor;
            _btnTogglePass.Font = new Font("Segoe UI", 10F);
            _btnTogglePass.Text = "👁️";
            _btnTogglePass.Cursor = Cursors.Hand;
            _btnTogglePass.Click += BtnTogglePass_Click;
            passBox.Controls.Add(_btnTogglePass);

            body.Controls.Add(passBox);

            currentY += 38;

            // Status / Error message
            _lblStatus.Text = string.Empty;
            _lblStatus.Font = new Font("Segoe UI", 9F);
            _lblStatus.ForeColor = ErrorColor;
            _lblStatus.Location = new Point(0, currentY);
            _lblStatus.Size = new Size(392, 22);
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            body.Controls.Add(_lblStatus);

            currentY += 26;

            // ─── Action Buttons: Cancel and Login ───
            _btnCancel.Text = "Cancel";
            _btnCancel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            _btnCancel.BackColor = CancelBtnBg;
            _btnCancel.ForeColor = Color.White;
            _btnCancel.FlatStyle = FlatStyle.Flat;
            _btnCancel.FlatAppearance.BorderColor = InputBorderColor;
            _btnCancel.FlatAppearance.BorderSize = 1;
            _btnCancel.Location = new Point(0, currentY);
            _btnCancel.Size = new Size(110, 40);
            _btnCancel.Cursor = Cursors.Hand;
            _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            ApplyRoundedRegion(_btnCancel, 6);
            body.Controls.Add(_btnCancel);

            _btnLogin.Text = "Login";
            _btnLogin.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            _btnLogin.BackColor = LoginBtnBg;
            _btnLogin.ForeColor = Color.White;
            _btnLogin.FlatStyle = FlatStyle.Flat;
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Location = new Point(125, currentY);
            _btnLogin.Size = new Size(267, 40);
            _btnLogin.Cursor = Cursors.Hand;
            _btnLogin.Click += BtnLogin_Click;
            ApplyRoundedRegion(_btnLogin, 6);
            body.Controls.Add(_btnLogin);

            Controls.Add(body);
        }

        private Panel CreateInputContainer(Point location, Size size)
        {
            var panel = new Panel
            {
                Location = location,
                Size = size,
                BackColor = InputBg
            };
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var p = new Pen(InputBorderColor, 1);
                using var path = CreateRoundedPath(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 4);
                e.Graphics.DrawPath(p, path);
            };
            ApplyRoundedRegion(panel, 4);
            return panel;
        }

        private void BtnToggleToken_Click(object? sender, EventArgs e)
        {
            if (_txtToken.PasswordChar == '●' || _txtToken.PasswordChar == '*')
            {
                _txtToken.PasswordChar = '\0';
                _btnToggleToken.ForeColor = Color.White;
            }
            else
            {
                _txtToken.PasswordChar = '●';
                _btnToggleToken.ForeColor = LabelColor;
            }
        }

        private void BtnTogglePass_Click(object? sender, EventArgs e)
        {
            if (_txtPass.PasswordChar == '●' || _txtPass.PasswordChar == '*')
            {
                _txtPass.PasswordChar = '\0';
                _btnTogglePass.ForeColor = Color.White;
            }
            else
            {
                _txtPass.PasswordChar = '●';
                _btnTogglePass.ForeColor = LabelColor;
            }
        }

        private void Input_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnLogin_Click(this, EventArgs.Empty);
            }
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            string ip = _txtIp.Text.Trim();
            string portText = _txtPort.Text.Trim();
            string tokenKey = _txtToken.Text.Trim();
            string user = _txtUser.Text.Trim();
            string pass = _txtPass.Text;

            // 1. Validate IP
            if (!IPAddress.TryParse(ip, out IPAddress? address)
                || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ShowError("กรุณากรอก IP Address ให้ถูกต้อง เช่น 192.168.109.54");
                _txtIp.Focus();
                return;
            }

            // 2. Validate Port
            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                ShowError("กรุณากรอก Port ให้ถูกต้อง (1-65535)");
                _txtPort.Focus();
                return;
            }

            // 3. Validate Token
            if (string.IsNullOrWhiteSpace(tokenKey))
            {
                ShowError("กรุณากรอก Token Key");
                _txtToken.Focus();
                return;
            }

            // 4. Validate User / Password
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ShowError("กรุณากรอก E-mail และ Password ให้ครบถ้วน");
                if (string.IsNullOrEmpty(user)) _txtUser.Focus();
                else _txtPass.Focus();
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
                _btnLogin.Enabled = false;
                _lblStatus.Text = "⏳ กำลังตรวจสอบสิทธิ์...";
                _lblStatus.ForeColor = Color.Orange;

                using var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using HttpResponseMessage response = await HttpClient.SendAsync(request, timeout.Token);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string receivedToken = ExtractToken(responseText);
                    if (string.IsNullOrEmpty(receivedToken))
                    {
                        ShowError("ไม่พบ Token ตอบกลับจากเซิร์ฟเวอร์");
                        return;
                    }

                    // Save all configurations to Settings
                    try
                    {
                        Settings1.Default.SavedServerIp = ip;
                        Settings1.Default.SavedServerPort = port.ToString();
                        Settings1.Default.SavedApiToken = tokenKey;
                        Settings1.Default.SavedOperatorUsername = user;
                        Settings1.Default.Save();
                    }
                    catch { }

                    OperatorToken = receivedToken.Trim();
                    OperatorUsername = user;

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    ShowError("E-mail หรือ Password ไม่ถูกต้อง");
                }
            }
            catch (OperationCanceledException)
            {
                ShowError("Server ไม่ตอบกลับภายใน 10 วินาที (Timeout)");
            }
            catch (Exception ex)
            {
                ShowError($"เชื่อมต่อเซิร์ฟเวอร์ไม่ได้: {ex.Message}");
            }
            finally
            {
                _btnLogin.Enabled = true;
            }
        }

        private void ShowError(string message)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = ErrorColor;
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

        private static void ApplyRoundedRegion(Control control, int cornerRadius)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            int diameter = Math.Min(cornerRadius * 2, Math.Min(control.Width, control.Height));
            if (diameter <= 0) return;

            int right = control.Width - 1;
            int bottom = control.Height - 1;

            using var path = new GraphicsPath();
            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddArc(right - diameter, 0, diameter, diameter, 270, 90);
            path.AddArc(right - diameter, bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(0, bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            Region? previousRegion = control.Region;
            control.Region = new Region(path);
            previousRegion?.Dispose();
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int cornerRadius)
        {
            int diameter = Math.Min(cornerRadius * 2, Math.Min(bounds.Width, bounds.Height));
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void Header_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _isDragging = true;
            _dragStartCursor = Cursor.Position;
            _dragStartForm = Location;
        }

        private void Header_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            Point currentCursor = Cursor.Position;
            Location = new Point(
                _dragStartForm.X + (currentCursor.X - _dragStartCursor.X),
                _dragStartForm.Y + (currentCursor.Y - _dragStartCursor.Y));
        }

        private void Header_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _isDragging = false;
        }
    }
}
