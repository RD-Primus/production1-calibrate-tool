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
    public partial class Form2 : Form
    {
        private Form1 _mainForm;

        public Form2(Form1 mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;

        }

        private void Form2_Load(object sender, EventArgs e)
        {
            txtOhm.Text = "กรอกค่า ohm";
            txtOhm.ForeColor = Color.Gray; // เปลี่ยนสีตัวอักษรเป็นสีเทาให้ดูเหมือนคำแนะนำ
        }

        //-- Switch WL
        private void btnOn_Click(object sender, EventArgs e)
        {
            _mainForm.btnWL_On_Click(null!, null!);
        }
        private void btnOff_Click(object sender, EventArgs e)
        {
            _mainForm.btnWL_Off_Click(null!, null!);
        }



        //-- Switch HP
        private void btnOn2_Click(object sender, EventArgs e)
        {
            _mainForm.btnHP_ON_Click(null!, null!);
        }
        private void btnOff2_Click(object sender, EventArgs e)
        {
            _mainForm.btnHP_Off_Click(null!, null!);
        }



        //-- bnt send Ohm
        private void btn200_Click(object sender, EventArgs e)
        {
            _mainForm.btn200_Click(null!, null!);
        }
        private void btn2000_Click(object sender, EventArgs e)
        {
            _mainForm.btn2000_Click(null!, null!);
        }
        private void btn8000_Click(object sender, EventArgs e)
        {
            _mainForm.btn8000_Click(null!, null!);
        }

        private void send0_Click(object sender, EventArgs e)
        {
            _mainForm.SendOhm(5397);
        }
        private void send10_Click(object sender, EventArgs e)
        {
            _mainForm.SendOhm(3684);
        }
        private void send20_Click(object sender, EventArgs e)
        {
            _mainForm.SendOhm(2489);
        }
        private void send30_Click(object sender, EventArgs e)
        {
            _mainForm.SendOhm(1703);
        }
        private void send40_Click(object sender, EventArgs e)
        {
            _mainForm.SendOhm(1158);
        }
        private void send50_Click(object sender, EventArgs e)
        {
            _mainForm.SendOhm(821);
        }



        //-- send Ohm
        private void btnSendCustom_Click(object sender, EventArgs e)
        {
            ExecuteSend();
        }
        private void txtOhm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // ปิดเสียงเตือน "ตึ๊ง" ของ Windows เวลาล็อกปุ่ม Enter
                ExecuteSend();
            }
        }

        private void ExecuteSend()
        {
            // ลบเครื่องหมายคอมมาออกก่อนนำไปดึงค่า
            string cleanText = txtOhm.Text.Replace(",", "").Trim();

            // 💡 เปลี่ยนจาก int.TryParse เป็น double.TryParse เพื่อให้รองรับทศนิยมได้
            if (double.TryParse(cleanText, out double ohm))
            {
                // ส่งค่า Ohm (ที่เป็นทศนิยมแล้ว) ไปฟอร์มหลัก
                _mainForm.SendOhm(ohm);

                // ลบค่าในช่องออกหลังจากส่งสำเร็จ
                txtOhm.Clear();
                txtOhm.Focus();
            }
            else
            {
                if (cleanText == "" || cleanText == "กรอกค่า ohm")
                {
                    txtOhm.Focus();
                    return;
                }

                // เปลี่ยนข้อความเตือนให้ครอบคลุมทศนิยม
                MessageBox.Show("กรุณากรอกค่า Ohm ให้ถูกต้องเป็นตัวเลขจำนวนเต็มหรือทศนิยม", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtOhm.SelectAll();
                txtOhm.Focus();
            }
        }

        private void PreCal_Click(object sender, EventArgs e)
        {
            _mainForm.SendPreCalibrationFrame(_mainForm.backgroundWorkerOhm, "รอบทดสอบ");
        }


        private void txtOhm_TextChanged(object sender, EventArgs e)
        {
            // 1. ถอด Event ออกชั่วคราวเพื่อป้องกัน Infinite Loop
            txtOhm.TextChanged -= txtOhm_TextChanged;

            try
            {
                if (txtOhm.Text == "กรอกค่า ohm" || string.IsNullOrWhiteSpace(txtOhm.Text))
                {
                    return;
                }

                // เก็บตำแหน่งเคอร์เซอร์และความยาวตัวอักษรก่อนจัดฟอร์แมต
                int selectionStart = txtOhm.SelectionStart;
                int originalLength = txtOhm.Text.Length;

                // ลบคอมมาเก่าออกเพื่อเอาไปคำนวณ
                string rawText = txtOhm.Text.Replace(",", "");

                // 💡 ทีเด็ด: แยกข้อความด้วยจุด '.' ออกเป็นฝั่งจำนวนเต็ม และฝั่งทศนิยม
                string[] parts = rawText.Split('.');

                if (parts.Length > 0 && long.TryParse(parts[0], out long integerPart))
                {
                    // ฟอร์แมตใส่คอมมาเฉพาะฝั่งจำนวนเต็มก่อน (เช่น 2000 -> 2,000)
                    string formattedText = integerPart.ToString("N0");

                    // ถ้าผู้ใช้มีการพิมพ์จุดทศนิยมไว้ ให้แปะฝั่งทศนิยมกลับคืนเข้าไปข้างหลัง
                    if (parts.Length > 1)
                    {
                        // และจำกัดเอาไว้แค่จุดเดียว (ป้องกันกรณีเผลอกดจุดซ้ำซ้อน)
                        formattedText += "." + parts[1];
                    }

                    txtOhm.Text = formattedText;

                    // คำนวณตำแหน่งเคอร์เซอร์ใหม่ ไม่ให้เคอร์เซอร์กระโดด
                    int newLength = txtOhm.Text.Length;
                    txtOhm.SelectionStart = Math.Max(0, selectionStart + (newLength - originalLength));
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                txtOhm.TextChanged += txtOhm_TextChanged;
            }
        }

        private void txtOhm_Enter(object sender, EventArgs e)
        {
            if (txtOhm.Text == "กรอกค่า ohm")
            {
                txtOhm.Text = "";
                txtOhm.ForeColor = Color.Black; // เปลี่ยนกลับเป็นสีดำตอนพิมพ์จริง
            }
        }

        private void txtOhm_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOhm.Text))
            {
                txtOhm.Text = "กรอกค่า ohm";
                txtOhm.ForeColor = Color.Gray;
            }
        }
    }
}
