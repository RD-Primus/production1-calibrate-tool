using System.Drawing;
using System.Windows.Forms;

namespace TEST_DEF_03N
{
    public partial class Form1
    {
        private sealed class LiveParameterDefinition
        {
            public ushort Address { get; }
            public string Caption { get; }
            public short Minimum { get; }
            public short Maximum { get; }
            public int Column { get; }
            public int Row { get; }

            public LiveParameterDefinition(ushort address, string caption,
                short minimum, short maximum, int column, int row)
            {
                Address = address;
                Caption = caption;
                Minimum = minimum;
                Maximum = maximum;
                Column = column;
                Row = row;
            }
        }

        // Reg.6 ScaleCF ไม่แสดงตามหน้าต่างที่ผู้ใช้กำหนด
        private static readonly LiveParameterDefinition[] LiveParameterDefinitions =
        {
            new(2,  "INP-Input",              0,    3, 0, 0),
            new(3,  "PUS-PVAdjust",        -999,  999, 0, 1),
            new(4,  "LPF-PVFilter",           0,   15, 0, 2),
            new(5,  "DP-DecimalPoint",        0,    1, 0, 3),

            new(7,  "LOC-LockKey",            0,    3, 1, 0),
            new(8,  "St-StartTime",            1,  599, 1, 1),
            new(9,  "rL-FunctionRelay",       10,   21, 1, 2),
            new(10, "Con-ModeControl",         0,   11, 1, 3),

            new(11, "HY1-PositionOn1",         0,  999, 2, 0),
            new(12, "HY-PositionOff1",      -199,  999, 2, 1),
            new(13, "DT1-OutputDelayON",       0,  599, 2, 2),
            new(14, "DT2-OutputDelayOff",      0,  599, 2, 3),

            new(15, "FU1-FunctionAlarm1",      0,   14, 3, 0),
            new(16, "HI1-AlarmLimitHigh1",  -999, 9999, 3, 1),
            new(17, "LO1-AlarmLimitLow1",   -999, 9999, 3, 2),
            new(18, "HS1-HysteresisAlarm1",    0,  999, 3, 3),

            new(19, "FU2-FunctionAlarm2",      0,   18, 4, 0),
            new(20, "HI2-AlarmLimitHigh2",  -999, 9999, 4, 1),
            new(21, "LO2-AlarmLimitLow2",   -999, 9999, 4, 2),
            new(22, "HS2-HysteresisAlarm2",    0,  999, 4, 3)
        };

        private Form? _liveParameterPopup;
        private Label? _liveParameterStatus;
        private readonly Dictionary<ushort, TextBox> _liveParameterInputs = new();
        private readonly Dictionary<ushort, Label> _liveParameterRangeLabels = new();
        private readonly HashSet<ushort> _liveParameterDirty = new();
        private bool _updatingLiveParameterUi;
        private bool _liveParameterWriteBusy;
        private bool _closeLiveParameterForDisconnect;
        private short? _liveParameterOption;

        private void ShowLiveParameterPopup()
        {
            if (!CheckManualConnections(true, false, false)) return;
            if (_isCalibrating || _modelTestAllActive || _buttonTestActive || _isWriting)
            {
                MessageBox.Show(this,
                    "ขณะนี้ระบบกำลังทดสอบหรือเขียนข้อมูล กรุณารอให้การทำงานเสร็จก่อน",
                    "ตั้งค่าพารามิเตอร์", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_liveParameterPopup != null && !_liveParameterPopup.IsDisposed)
            {
                _liveParameterPopup.Activate();
                return;
            }

            Color background = Color.FromArgb(7, 23, 35);
            Color card = Color.FromArgb(20, 40, 55);
            Color cyan = Color.FromArgb(73, 218, 242);
            Color muted = Color.FromArgb(166, 185, 199);

            var popup = new Form
            {
                Text = "ตั้งค่าพารามิเตอร์ DEF-03N",
                ClientSize = new Size(1050, 610),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = background,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoScaleMode = AutoScaleMode.Dpi
            };
            _liveParameterPopup = popup;
            _liveParameterDirty.Clear();
            _liveParameterInputs.Clear();
            _liveParameterRangeLabels.Clear();
            _liveParameterOption = txt_model.Text.Trim() == ModelA ? (short)0 :
                txt_model.Text.Trim() == ModelAB ? (short)1 : null;

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = background
            };
            header.Controls.Add(new Label
            {
                Text = "ตั้งค่าพารามิเตอร์",
                Bounds = new Rectangle(24, 10, 1000, 43),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = cyan,
                TextAlign = ContentAlignment.MiddleLeft
            });
            header.Controls.Add(new Label
            {
                Text = "Slave ID 1 • Live update • กด Enter หรือออกจากช่องเพื่อเขียนค่า",
                Bounds = new Rectangle(28, 54, 990, 25),
                ForeColor = muted
            });

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 4,
                BackColor = background,
                Padding = new Padding(12, 4, 12, 4),
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };
            for (int i = 0; i < 5; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            for (int i = 0; i < 4; i++)
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            foreach (LiveParameterDefinition definition in LiveParameterDefinitions)
            {
                var parameterCard = new MyRoundedPanel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(7),
                    Padding = new Padding(12),
                    BorderRadius = 12,
                    BackColor = card
                };
                var caption = new Label
                {
                    Text = definition.Caption,
                    Dock = DockStyle.Top,
                    Height = 25,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.White,
                    AutoEllipsis = true
                };
                var range = new Label
                {
                    Text = GetLiveParameterRangeText(definition),
                    Dock = DockStyle.Top,
                    Height = 21,
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = muted,
                    AutoEllipsis = true
                };
                var input = new TextBox
                {
                    Name = $"PopupRegis{definition.Address}",
                    Dock = DockStyle.Bottom,
                    Height = 34,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    TextAlign = HorizontalAlignment.Center,
                    BackColor = Color.FromArgb(47, 61, 72),
                    ForeColor = muted,
                    ReadOnly = true,
                    Text = "รออ่าน...",
                    Tag = null
                };

                input.Enter += (_, _) =>
                {
                    if (input.ReadOnly || input.Tag is not short) return;
                    input.SelectAll();
                };
                input.TextChanged += (_, _) =>
                    HandleLiveParameterTextChanged(definition, input);
                input.KeyDown += (_, e) =>
                {
                    // ระหว่าง Modbus กำลังเขียน ให้คงหน้าตาช่องเดิมไว้
                    // แต่ไม่รับการกดปุ่มหรือเริ่มคำสั่งเขียนซ้อน
                    if (_liveParameterWriteBusy)
                    {
                        e.SuppressKeyPress = true;
                        return;
                    }
                    if (e.KeyCode != Keys.Enter) return;
                    e.SuppressKeyPress = true;
                    popup.ActiveControl = null;
                };
                input.Leave += async (_, _) =>
                    await WriteLiveParameterValueAsync(definition, input);

                // ใส่ย้อนลำดับเพราะ DockStyle.Top จะจัดจากลำดับ Control
                parameterCard.Controls.Add(input);
                parameterCard.Controls.Add(range);
                parameterCard.Controls.Add(caption);
                table.Controls.Add(parameterCard, definition.Column, definition.Row);
                _liveParameterInputs[definition.Address] = input;
                _liveParameterRangeLabels[definition.Address] = range;
            }

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = background,
                Padding = new Padding(22, 6, 22, 8)
            };
            _liveParameterStatus = new Label
            {
                Text = "กำลังรอ Polling รอบถัดไป...",
                Dock = DockStyle.Fill,
                ForeColor = cyan,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            footer.Controls.Add(_liveParameterStatus);

            popup.FormClosing += (_, e) =>
            {
                if (!_liveParameterWriteBusy || _closeLiveParameterForDisconnect) return;
                e.Cancel = true;
                SetLiveParameterStatus("กรุณารอให้การเขียนและตรวจสอบค่าเสร็จก่อน", Color.Orange);
            };
            popup.FormClosed += (_, _) =>
            {
                _liveParameterInputs.Clear();
                _liveParameterRangeLabels.Clear();
                _liveParameterDirty.Clear();
                _liveParameterStatus = null;
                _liveParameterOption = null;
                _liveParameterWriteBusy = false;
                _closeLiveParameterForDisconnect = false;
                if (ReferenceEquals(_liveParameterPopup, popup))
                    _liveParameterPopup = null;
            };

            popup.Controls.Add(table);
            popup.Controls.Add(footer);
            popup.Controls.Add(header);
            UpdateLiveParameterAvailability();
            popup.Shown += (_, _) =>
            {
                // WinForms จะ Focus TextBox ตัวแรกให้อัตโนมัติ จึงเคลียร์ Focus
                // หลัง Popup แสดงเสร็จ เพื่อให้ Polling เติมค่าทุกช่องก่อน
                popup.BeginInvoke(new Action(() =>
                {
                    if (!popup.IsDisposed)
                        popup.ActiveControl = null;
                }));
            };
            popup.ShowDialog(this);
        }

        // เรียกจาก UpdateMainBoardUI ซึ่งอยู่บน UI thread อยู่แล้ว
        private void UpdateParameterPopupRegister(ushort address, short value)
        {
            if (_liveParameterPopup == null || _liveParameterPopup.IsDisposed) return;

            if (address == ModelOptionRegister)
            {
                _liveParameterOption = value;
                UpdateLiveParameterAvailability();
                return;
            }

            if (!_liveParameterInputs.TryGetValue(address, out TextBox? input)) return;
            if (_liveParameterWriteBusy || input.Focused || _liveParameterDirty.Contains(address)) return;

            _updatingLiveParameterUi = true;
            try
            {
                input.Tag = value;
                input.Text = value.ToString();
                input.ForeColor = input.ReadOnly ? Color.FromArgb(166, 185, 199) : Color.Blue;
            }
            finally
            {
                _updatingLiveParameterUi = false;
            }

            if (address is 9 or 10 or 15 or 19)
                UpdateLiveParameterAvailability();

            int loaded = _liveParameterInputs.Values.Count(t => t.Tag is short);
            SetLiveParameterStatus(
                loaded == _liveParameterInputs.Count
                    ? $"Live: อ่านค่าครบ {loaded} รายการแล้ว"
                    : $"Live: อ่านแล้ว {loaded}/{_liveParameterInputs.Count} รายการ",
                loaded == _liveParameterInputs.Count ? Color.LightGreen : Color.FromArgb(73, 218, 242));
        }

        private void HandleLiveParameterTextChanged(
            LiveParameterDefinition definition, TextBox input)
        {
            if (_updatingLiveParameterUi || input.Tag is not short originalValue) return;

            if (short.TryParse(input.Text.Trim(), out short currentValue) &&
                currentValue == originalValue)
            {
                _liveParameterDirty.Remove(definition.Address);
                input.ForeColor = Color.Blue;
            }
            else
            {
                _liveParameterDirty.Add(definition.Address);
                input.ForeColor = Color.DarkOrange;
            }
        }

        private async Task WriteLiveParameterValueAsync(
            LiveParameterDefinition definition, TextBox input)
        {
            if (_updatingLiveParameterUi || _liveParameterWriteBusy ||
                !_liveParameterDirty.Contains(definition.Address)) return;
            if (input.Tag is not short originalValue)
            {
                _liveParameterDirty.Remove(definition.Address);
                return;
            }

            if (!short.TryParse(input.Text.Trim(), out short requestedValue))
            {
                RestoreLiveParameterInput(definition.Address, input, originalValue, Color.Red);
                MessageBox.Show(_liveParameterPopup,
                    $"{definition.Caption} ต้องเป็นเลขจำนวนเต็ม",
                    "ค่าพารามิเตอร์ไม่ถูกต้อง", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsLiveParameterValueAllowed(definition, requestedValue))
            {
                string allowed = GetLiveParameterRangeText(definition);
                RestoreLiveParameterInput(definition.Address, input, originalValue, Color.Red);
                MessageBox.Show(_liveParameterPopup,
                    $"{definition.Caption} ค่า {requestedValue} ไม่ถูกต้อง\n{allowed}",
                    "ค่าเกินช่วง", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _liveParameterWriteBusy = true;
            SetLiveParameterInputsBusy(true);
            input.ForeColor = Color.DarkOrange;
            SetLiveParameterStatus(
                $"กำลังเขียน ID 1, Reg.{definition.Address} = {requestedValue}...",
                Color.Orange);

            try
            {
                bool writeOk = await ExecuteWriteSingleRegisterVerifiedAsync(
                    slaveId: TargetID,
                    registerAddress: definition.Address,
                    value: requestedValue,
                    maxAttempts: 5,
                    timeoutMs: 1000);

                bool verified = false;
                short actualValue = originalValue;
                if (writeOk)
                {
                    for (int attempt = 1; attempt <= 5; attempt++)
                    {
                        try
                        {
                            await Task.Delay(100);
                            actualValue = await ReadSingleRegisterAsync(
                                definition.Address, timeoutMs: 500, retries: 0,
                                slaveId: TargetID);
                            if (actualValue == requestedValue)
                            {
                                verified = true;
                                break;
                            }
                        }
                        catch (TimeoutException)
                        {
                            // ทำเหมือน WriteRegisValueAsync: ลอง read-back รอบถัดไป
                        }
                    }
                }

                _liveParameterDirty.Remove(definition.Address);
                if (!verified)
                {
                    RestoreLiveParameterInput(definition.Address, input, originalValue, Color.Red);
                    SetLiveParameterStatus(
                        $"Reg.{definition.Address} เขียนหรืออ่านกลับไม่สำเร็จ",
                        Color.Red);
                    LogToRx($"[PARAMETER ERROR] Reg.{definition.Address} " +
                        $"{definition.Caption} เขียนหรืออ่านกลับไม่สำเร็จ", Color.Red);
                    return;
                }

                _updatingLiveParameterUi = true;
                try
                {
                    input.Tag = actualValue;
                    input.Text = actualValue.ToString();
                    input.ForeColor = Color.Blue;
                }
                finally
                {
                    _updatingLiveParameterUi = false;
                }

                SetLiveParameterStatus(
                    $"Reg.{definition.Address} = {actualValue} สำเร็จ — กลับสู่ Live polling แล้ว",
                    Color.LightGreen);
                LogToRx($"[PARAMETER] Reg.{definition.Address} " +
                    $"{definition.Caption} = {actualValue} สำเร็จ", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                _liveParameterDirty.Remove(definition.Address);
                RestoreLiveParameterInput(definition.Address, input, originalValue, Color.Red);
                SetLiveParameterStatus($"Reg.{definition.Address}: {ex.Message}", Color.Red);
                LogToRx($"[PARAMETER ERROR] Reg.{definition.Address}: {ex.Message}", Color.Red);
            }
            finally
            {
                _liveParameterWriteBusy = false;
                SetLiveParameterInputsBusy(false);
            }
        }

        private void RestoreLiveParameterInput(
            ushort address, TextBox input, short value, Color color)
        {
            _liveParameterDirty.Remove(address);
            _updatingLiveParameterUi = true;
            try
            {
                input.Text = value.ToString();
                input.ForeColor = color;
            }
            finally
            {
                _updatingLiveParameterUi = false;
            }
        }

        private void SetLiveParameterInputsBusy(bool busy)
        {
            foreach (TextBox input in _liveParameterInputs.Values)
                input.ShortcutsEnabled = !busy;

            if (!busy)
                UpdateLiveParameterAvailability();
        }

        private void UpdateLiveParameterAvailability()
        {
            if (_liveParameterPopup == null || _liveParameterPopup.IsDisposed) return;

            foreach (LiveParameterDefinition definition in LiveParameterDefinitions)
            {
                if (!_liveParameterInputs.TryGetValue(definition.Address, out TextBox? input)) continue;
                // สถานะ Modbus busy ใช้กันการพิมพ์/เขียนซ้อนเท่านั้น
                // ไม่ควรทำให้ทุกช่องเปลี่ยนเป็น ReadOnly และสีทึบ
                bool available = input.Tag is short &&
                    IsLiveParameterAvailable(definition.Address);

                bool readOnly = !available;
                Color backColor = available
                    ? Color.FromArgb(245, 246, 247)
                    : Color.FromArgb(47, 61, 72);
                Color foreColor = available ? Color.Blue : Color.FromArgb(166, 185, 199);

                // เปลี่ยนเฉพาะ Property ที่ต่างจริง เพื่อลดการวาด TextBox ทั้งหน้าใหม่
                if (input.ReadOnly != readOnly)
                    input.ReadOnly = readOnly;
                if (input.BackColor != backColor)
                    input.BackColor = backColor;
                if (!_liveParameterDirty.Contains(definition.Address) && input.ForeColor != foreColor)
                    input.ForeColor = foreColor;

                if (_liveParameterRangeLabels.TryGetValue(definition.Address, out Label? range))
                {
                    range.Text = available
                        ? GetLiveParameterRangeText(definition)
                        : "ไม่ใช้ในโหมดปัจจุบัน • ดูค่าได้อย่างเดียว";
                    range.ForeColor = available
                        ? Color.FromArgb(166, 185, 199)
                        : Color.FromArgb(117, 132, 143);
                }
            }
        }

        private bool IsLiveParameterAvailable(ushort address)
        {
            // กลุ่มพื้นฐานที่อยู่ในลำดับตั้งค่าของ Firmware เสมอ
            if (address is 2 or 3 or 4 or 5 or 7 or 8 or 9) return true;

            short? relay = GetLiveParameterDeviceValue(9);
            short? control = GetLiveParameterDeviceValue(10);
            short? alarm1 = GetLiveParameterDeviceValue(15);
            short? alarm2 = GetLiveParameterDeviceValue(19);

            bool controlOutput = relay is 20 or 21;
            bool alarm1Output = relay is 10 or 11;
            bool alarm2Output = relay is 11 or 21;

            return address switch
            {
                10 => controlOutput,
                11 or 12 => controlOutput && control is 0 or 10,
                13 or 14 => controlOutput && control is 1 or 11,
                15 => alarm1Output,
                16 => alarm1Output && AlarmUsesHigh(alarm1),
                17 => alarm1Output && AlarmUsesLow(alarm1),
                18 => alarm1Output && alarm1.HasValue && alarm1.Value != 0,
                19 => alarm2Output,
                20 => alarm2Output && AlarmUsesHigh(alarm2),
                21 => alarm2Output && AlarmUsesLow(alarm2),
                22 => alarm2Output && alarm2.HasValue && alarm2.Value != 0,
                _ => false
            };
        }

        private short? GetLiveParameterDeviceValue(ushort address) =>
            _liveParameterInputs.TryGetValue(address, out TextBox? input) &&
            input.Tag is short value ? value : null;

        private static bool AlarmUsesHigh(short? function) =>
            function.HasValue && function.Value != 0 &&
            function.Value is not (3 or 7 or 13 or 17);

        private static bool AlarmUsesLow(short? function) =>
            function.HasValue && function.Value != 0 &&
            function.Value is not (2 or 6 or 12 or 16);

        private bool IsLiveParameterValueAllowed(
            LiveParameterDefinition definition, short value)
        {
            return definition.Address switch
            {
                9 => (_liveParameterOption ?? 1) == 0
                    ? value is 10 or 20
                    : value is 10 or 11 or 20 or 21,
                10 => value is 0 or 1 or 10 or 11,
                15 => (value >= 0 && value <= 8) || (value >= 11 && value <= 14),
                19 => ((value >= 0 && value <= 8) ||
                    (value >= 11 && value <=
                        (GetLiveParameterDeviceValue(9) == 21 ? 18 : 14))),
                _ => value >= definition.Minimum && value <= definition.Maximum
            };
        }

        private string GetLiveParameterRangeText(LiveParameterDefinition definition)
        {
            return definition.Address switch
            {
                9 => (_liveParameterOption ?? 1) == 0
                    ? "ค่าที่ใช้ได้: 10, 20"
                    : "ค่าที่ใช้ได้: 10, 11, 20, 21",
                10 => "ค่าที่ใช้ได้: 0, 1, 10, 11",
                15 => "ค่าที่ใช้ได้: 0–8, 11–14",
                19 => GetLiveParameterDeviceValue(9) == 21
                    ? "ค่าที่ใช้ได้: 0–8, 11–18"
                    : "ค่าที่ใช้ได้: 0–8, 11–14",
                _ => $"ช่วง {definition.Minimum} ถึง {definition.Maximum}"
            };
        }

        private void SetLiveParameterStatus(string text, Color color)
        {
            if (_liveParameterStatus == null || _liveParameterStatus.IsDisposed) return;
            _liveParameterStatus.Text = text;
            _liveParameterStatus.ForeColor = color;
        }

        private void CloseParameterPopupOnDisconnect()
        {
            if (_liveParameterPopup == null || _liveParameterPopup.IsDisposed) return;
            _closeLiveParameterForDisconnect = true;
            _liveParameterPopup.Close();
        }
    }
}
