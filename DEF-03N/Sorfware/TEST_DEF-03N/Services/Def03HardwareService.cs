// =========================================================================================
// [DEVELOPER GUIDE]
// Def03HardwareService.cs: "รวมศูนย์การจัดการฮาร์ดแวร์, Modbus RTU, CRC16, บอร์ด Relay และ Ohm Source"
// - รวบรวมฟังก์ชันเกี่ยวกับโปรโตคอลการสื่อสารทั้งหมดของ DEF-03N ไว้ในที่เดียว
// - คำนวณและตรวจสอบ Modbus RTU CRC16
// - สร้างเฟรมคำสั่ง Modbus Function 0x03 (Read), 0x06 (Write), 0x10, และ 0x55 (Calibration)
// - สร้างเฟรมคำสั่งควบคุมบอร์ด Relay / Alarm
// - แปลงค่าและสร้างเฟรมคำสั่งสำหรับตัวกำเนิดความต้านทาน (Ohm Source)
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;
using System.IO.Ports;
using System.Linq;

namespace TEST_DEF_03N.Services
{
    public static class Def03HardwareService
    {
        public const byte CalibrationSlaveId = 0x01;
        public const byte CalibrationFunction = 0x55;
        public const byte RelayCheckSlaveID = 0x01;

        #region Modbus RTU CRC-16

        /// <summary>
        /// คำนวณ CRC16 มาตรฐาน Modbus RTU (Polynomial 0xA001, Initial 0xFFFF)
        /// ส่งคืน byte[] 2 ช่อง: [0] = CRC Low, [1] = CRC High
        /// </summary>
        public static byte[] CalculateCrc(byte[] data, int length = -1)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            ushort crc = 0xFFFF;
            int count = length >= 0 ? length : data.Length;

            for (int j = 0; j < count; j++)
            {
                byte b = data[j];
                crc ^= b;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }

            return new byte[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
        }

        /// <summary>
        /// ตรวจสอบความถูกต้องของ Checksum ท้ายเฟรม Modbus
        /// </summary>
        public static bool ValidateCrc(byte[] frame, int frameLength)
        {
            if (frame == null || frameLength < 3) return false;
            byte[] calc = CalculateCrc(frame, frameLength - 2);
            return frame[frameLength - 2] == calc[0] && frame[frameLength - 1] == calc[1];
        }

        #endregion

        #region Modbus Frame Builders

        /// <summary>
        /// สร้างเฟรม Modbus RTU ขออ่าน Holding Registers (Function 0x03)
        /// </summary>
        public static byte[] BuildReadHoldingRegistersFrame(byte slaveId, ushort startAddress, ushort count)
        {
            byte[] frameWithoutCrc = new byte[]
            {
                slaveId,
                0x03,
                (byte)(startAddress >> 8),
                (byte)(startAddress & 0xFF),
                (byte)(count >> 8),
                (byte)(count & 0xFF)
            };

            byte[] crc = CalculateCrc(frameWithoutCrc);
            return new byte[]
            {
                frameWithoutCrc[0], frameWithoutCrc[1],
                frameWithoutCrc[2], frameWithoutCrc[3],
                frameWithoutCrc[4], frameWithoutCrc[5],
                crc[0], crc[1]
            };
        }

        /// <summary>
        /// สร้างเฟรม Modbus RTU สั่งเขียน Single Register (Function 0x06)
        /// </summary>
        public static byte[] BuildWriteSingleRegisterFrame(byte slaveId, ushort address, short value)
        {
            byte[] frameWithoutCrc = new byte[]
            {
                slaveId,
                0x06,
                (byte)(address >> 8),
                (byte)(address & 0xFF),
                (byte)(value >> 8),
                (byte)(value & 0xFF)
            };

            byte[] crc = CalculateCrc(frameWithoutCrc);
            return new byte[]
            {
                frameWithoutCrc[0], frameWithoutCrc[1],
                frameWithoutCrc[2], frameWithoutCrc[3],
                frameWithoutCrc[4], frameWithoutCrc[5],
                crc[0], crc[1]
            };
        }

        /// <summary>
        /// สร้างเฟรมคำสั่ง Calibration Function 0x55 สำหรับ DEF-03N
        /// </summary>
        public static byte[] BuildCalibrationControlFrame(byte slaveId, byte command)
        {
            byte[] frameWithoutCrc = new byte[]
            {
                slaveId,
                CalibrationFunction,
                command,
                0x00,
                0x02,
                0x7A,
                0x00
            };

            byte[] crc = CalculateCrc(frameWithoutCrc);
            return frameWithoutCrc.Concat(crc).ToArray();
        }

        #endregion

        #region Relay & Ohm Source Commands

        /// <summary>
        /// ประกอบเฟรมสั่งงานบอร์ด Relay / Alarm พร้อมเติม CRC16 2 ไบต์ท้าย
        /// </summary>
        public static byte[] BuildRelayFrame(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            byte[] crc = CalculateCrc(data);
            byte[] frame = new byte[data.Length + 2];
            Array.Copy(data, frame, data.Length);
            frame[data.Length] = crc[0];
            frame[data.Length + 1] = crc[1];
            return frame;
        }

        /// <summary>
        /// สร้างเฟรมคำสั่งสำหรับส่งไปยัง Ohm Source (ตัวกำเนิดความต้านทาน)
        /// รูปแบบ: Header 0x50 ตามด้วยค่าความต้านทานที่ผ่านการแปลง Hex และสลับไบต์
        /// </summary>
        public static byte[] BuildOhmSourceFrame(double ohmValue, bool multiplyBy100)
        {
            int multiplier = multiplyBy100 ? 100 : 10;
            int result = (int)Math.Round(ohmValue * multiplier);

            string resultHex = result.ToString("X").PadLeft(8, '0');
            string reversedStr = resultHex.Substring(6, 2) + resultHex.Substring(4, 2) +
                                 resultHex.Substring(2, 2) + resultHex.Substring(0, 2);
            reversedStr = reversedStr.PadRight(10, '0');

            byte[] byteArray = Enumerable.Range(0, reversedStr.Length / 2)
                                 .Select(x => Convert.ToByte(reversedStr.Substring(x * 2, 2), 16))
                                 .ToArray();

            return new byte[] { 0x50 }.Concat(byteArray).ToArray();
        }

        #endregion
    }
}
