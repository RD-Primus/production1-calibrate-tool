// =========================================================================================
// [DEVELOPER GUIDE]
// Form1.Device.cs: "การสื่อสาร Serial Port กับสาย Device (Local RS-485)"
// - จัดการการเชื่อมต่อพอร์ต Serial ของ Device (Auto-connect & รับเฟรม Modbus 0x03)
// - ทำหน้าที่จำลอง Slave ตอบกลับคำขออ่านค่า หรือดักรับ Trigger Frame จาก PH-08
// - ส่งมอบสิทธิ์การอ่านเฟรม Trigger ให้กับ IPh08TestHardwareProvider ในช่วง Phase 2
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace PH_08_Test
{
    public partial class Form1
    {
        private SerialPort? _serialPortDevice;
        private readonly List<byte> _deviceRxBuffer = new List<byte>();
        private readonly object _deviceLock = new object();
        private byte _currentDeviceSlaveId = 1;
        private bool _hasReceived03Command = false;
        private byte[]? _lastReceived03Frame = null;
        private byte[]? _lastSentSlaveResponseFrame = null;
        private bool _autoRespondAsSlave = true;

        // Serial Port Settings for Device
        public int DeviceDataBits { get; set; } = 8;
        public StopBits DeviceStopBits { get; set; } = StopBits.One;
        public Parity DeviceParity { get; set; } = Parity.None;

        private void InitDeviceControls()
        {
            // 1. Initial value for Address TextBox (1 - 255)
            txt_AddressDevice.Text = "1";

            // 2. Initial items for Baudrate
            cmb_BaudrateDevice.Items.Clear();
            cmb_BaudrateDevice.Items.AddRange(new object[] { "2400", "4800", "9600", "19200", "38400", "57600", "115200" });
            cmb_BaudrateDevice.SelectedItem = "9600";

            // 3. Refresh COM Port list
            RefreshDevicePortList();

            // 4. Bind Advance Popup button
            openadd_device.Click += Openadd_device_Click;

            this.FormClosing += Form1_Device_FormClosing;
        }

        private void Openadd_device_Click(object? sender, EventArgs e)
        {
            using var popup = new FormSerialSettingsPopup("Device", DeviceDataBits, DeviceStopBits, DeviceParity);
            popup.SetPositionBelow(openadd_device);
            if (popup.ShowDialog(this) == DialogResult.OK)
            {
                DeviceDataBits = popup.SelectedDataBits;
                DeviceStopBits = popup.SelectedStopBits;
                DeviceParity = popup.SelectedParity;
            }
        }

        private void RefreshDevicePortList()
        {
            string? currentSelection = cmb_ComportDevice.SelectedItem?.ToString();
            cmb_ComportDevice.Items.Clear();

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cmb_ComportDevice.Items.Add(port);
            }

            if (ports.Length > 0)
            {
                if (!string.IsNullOrEmpty(currentSelection) && cmb_ComportDevice.Items.Contains(currentSelection))
                {
                    cmb_ComportDevice.SelectedItem = currentSelection;
                }
                else
                {
                    cmb_ComportDevice.SelectedIndex = -1;
                }
            }
        }

        private bool OpenDeviceConnection(out string errorMessage)
        {
            errorMessage = "";

            if (cmb_ComportDevice.SelectedItem == null || string.IsNullOrWhiteSpace(cmb_ComportDevice.SelectedItem.ToString()))
            {
                errorMessage = "กรุณาเลือก COM Port สำหรับ Device";
                return false;
            }

            if (!byte.TryParse(txt_AddressDevice.Text.Trim(), out byte address) || address < 1)
            {
                errorMessage = "กรุณากรอก Device Address ที่ถูกต้อง (1 - 255)";
                return false;
            }
            _currentDeviceSlaveId = address;

            string portName = cmb_ComportDevice.SelectedItem.ToString()!;
            int baudRate = int.TryParse(cmb_BaudrateDevice.SelectedItem?.ToString(), out int b) ? b : 9600;
            int dataBits = DeviceDataBits;
            StopBits stopBits = DeviceStopBits;
            Parity parity = DeviceParity;

            try
            {
                CloseDeviceSerialPort();

                lock (_deviceLock)
                {
                    _deviceRxBuffer.Clear();
                    _hasReceived03Command = false;
                    _lastReceived03Frame = null;
                }

                _serialPortDevice = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    DtrEnable = true,
                    RtsEnable = true
                };
                _serialPortDevice.DataReceived += Device_DataReceived;
                _serialPortDevice.Open();

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"ไม่สามารถเชื่อมต่อ Device บนพอร์ต {portName}:\n{ex.Message}";
                CloseDeviceSerialPort();
                return false;
            }
        }

        #region Device Passive Listener (Detect 0x03 Command)

        private void Device_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPortDevice == null || !_serialPortDevice.IsOpen) return;

            try
            {
                int bytesToRead = _serialPortDevice.BytesToRead;
                if (bytesToRead <= 0) return;

                byte[] buffer = new byte[bytesToRead];
                _serialPortDevice.Read(buffer, 0, bytesToRead);

                lock (_deviceLock)
                {
                    _deviceRxBuffer.AddRange(buffer);
                    ParseDeviceIncomingModbus();
                }
            }
            catch (Exception ex)
            {
                if (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                {
                    HandleDisconnect($"พอร์ต Device หลุดระหว่างอ่านข้อมูล ({ex.Message})");
                }
            }
        }

        private void ParseDeviceIncomingModbus()
        {
            // Modbus Read Request (Function 0x03) has 8 bytes:
            // [SlaveID] [0x03] [StartRegHi] [StartRegLo] [QtyHi] [QtyLo] [CRCLo] [CRCHi]
            while (_deviceRxBuffer.Count >= 8)
            {
                // Check if Function Code at index 1 is 0x03
                if (_deviceRxBuffer[1] != 0x03)
                {
                    _deviceRxBuffer.RemoveAt(0);
                    continue;
                }

                byte[] frame = _deviceRxBuffer.Take(8).ToArray();

                // Verify CRC16 of the 8-byte request frame
                byte[] dataForCrc = frame.Take(6).ToArray();
                byte[] calcCrc = CalculateCRC(dataForCrc);

                if (calcCrc[0] == frame[6] && calcCrc[1] == frame[7])
                {
                    // Valid 0x03 Read Request received from PH-08!
                    _hasReceived03Command = true;
                    _lastReceived03Frame = frame;
                    _deviceRxBuffer.RemoveRange(0, 8);

                    // Build Simulated Slave Response (Function 0x03)
                    // Register 0 = 1, Register 1 = 1
                    byte slaveId = frame[0];
                    ushort startReg = (ushort)((frame[2] << 8) | frame[3]);
                    ushort quantity = (ushort)((frame[4] << 8) | frame[5]);

                    if (quantity > 0 && quantity <= 64)
                    {
                        byte byteCount = (byte)(quantity * 2);
                        byte[] payload = new byte[3 + byteCount];
                        payload[0] = slaveId;
                        payload[1] = 0x03;
                        payload[2] = byteCount;

                        for (int i = 0; i < quantity; i++)
                        {
                            ushort currentReg = (ushort)(startReg + i);
                            ushort regVal = 1; // Simulate Register 0 and 1 = 1
                            payload[3 + (i * 2)] = (byte)(regVal >> 8);
                            payload[3 + (i * 2) + 1] = (byte)(regVal & 0xFF);
                        }

                        byte[] crc = CalculateCRC(payload);
                        byte[] responseFrame = new byte[payload.Length + 2];
                        Array.Copy(payload, 0, responseFrame, 0, payload.Length);
                        responseFrame[responseFrame.Length - 2] = crc[0];
                        responseFrame[responseFrame.Length - 1] = crc[1];

                        _lastSentSlaveResponseFrame = responseFrame;

                        if (_autoRespondAsSlave && _serialPortDevice != null && _serialPortDevice.IsOpen)
                        {
                            try
                            {
                                _serialPortDevice.Write(responseFrame, 0, responseFrame.Length);
                            }
                            catch (Exception ex)
                            {
                                if (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                                {
                                    HandleDisconnect($"พอร์ต Device หลุดระหว่างส่งข้อมูล ({ex.Message})");
                                }
                            }
                        }
                    }

                    continue;
                }
                else
                {
                    // CRC mismatch, advance 1 byte
                    _deviceRxBuffer.RemoveAt(0);
                }
            }
        }

        #endregion

        private void SetDeviceInputsEnabled(bool enabled)
        {
            cmb_ComportDevice.Enabled = enabled;
            txt_AddressDevice.Enabled = enabled;
            cmb_BaudrateDevice.Enabled = enabled;
            openadd_device.Enabled = enabled;

            if (openadd_device != null)
            {
                if (enabled)
                {
                    openadd_device.BackColor = Color.FromArgb(241, 248, 241);
                    openadd_device.BorderColor = Color.FromArgb(241, 248, 241);
                    openadd_device.ForeColor = Color.FromArgb(27, 94, 32);
                    openadd_device.Cursor = Cursors.Hand;
                }
                else
                {
                    openadd_device.BackColor = Color.FromArgb(235, 238, 240);
                    openadd_device.BorderColor = Color.FromArgb(215, 219, 223);
                    openadd_device.ForeColor = Color.FromArgb(125, 140, 148);
                    openadd_device.Cursor = Cursors.Default;
                }
                openadd_device.Invalidate();
            }
        }

        private void CloseDeviceSerialPort()
        {
            try
            {
                if (_serialPortDevice != null)
                {
                    if (_serialPortDevice.IsOpen)
                    {
                        _serialPortDevice.Close();
                    }
                    _serialPortDevice.Dispose();
                    _serialPortDevice = null;
                }
            }
            catch
            {
                // Ignored
            }
        }

        private void Form1_Device_FormClosing(object? sender, FormClosingEventArgs e)
        {
            CloseDeviceSerialPort();
        }
    }
}
