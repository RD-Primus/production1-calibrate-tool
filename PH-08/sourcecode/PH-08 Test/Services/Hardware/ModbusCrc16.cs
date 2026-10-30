// =========================================================================================
// [DEVELOPER GUIDE]
// ModbusCrc16.cs: "คำนวณและตรวจสอบ CRC-16 ของโปรโตคอล Modbus RTU"
// - ใช้อัลกอริทึมมาตรฐาน Modbus RTU (Polynomial 0xA001, Initial Value 0xFFFF)
// - ส่งคืนค่า CRC ในลำดับ Little-Endian (Low Byte ก่อน High Byte)
// - Method 'Calculate': ใช้สร้าง Checksum 2 bytes ท้ายเฟรมคำสั่งที่จะส่งออก
// - Method 'Validate': ใช้ตรวจสอบ Checksum ของเฟรมคำตอบที่ได้รับจากอุปกรณ์
// รายละเอียดเชิงลึกดูได้ที่ README_DEVELOPER.md
// =========================================================================================

using System;

namespace PH_08_Test.Services.Hardware
{
    public static class ModbusCrc16
    {
        public static byte[] Calculate(byte[] data, int length = -1)
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

        public static bool Validate(byte[] frame, int frameLength)
        {
            if (frame == null || frameLength < 3) return false;
            byte[] calc = Calculate(frame, frameLength - 2);
            return frame[frameLength - 2] == calc[0] && frame[frameLength - 1] == calc[1];
        }
    }
}
