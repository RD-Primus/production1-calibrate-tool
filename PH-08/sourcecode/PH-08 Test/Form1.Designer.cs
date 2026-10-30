namespace PH_08_Test
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            pictureBox1 = new PictureBox();
            btn_connect = new CustomRoundedButton();
            badge_connection = new ConnectionStatusBadge();
            customRoundedPanel2 = new CustomRoundedPanel();
            customRoundedPanel9 = new CustomRoundedPanel();
            openadd_device = new CustomRoundedButton();
            label9 = new Label();
            label15 = new Label();
            label10 = new Label();
            cmb_ComportDevice = new CustomComboBox();
            label11 = new Label();
            txt_AddressDevice = new CustomTextBox();
            cmb_BaudrateDevice = new CustomComboBox();
            label8 = new Label();
            btn_Refresh = new CustomRoundedButton();
            label2 = new Label();
            customRoundedPanel8 = new CustomRoundedPanel();
            openadd_Ph08 = new CustomRoundedButton();
            label14 = new Label();
            cmb_ComportPh08 = new CustomComboBox();
            label17 = new Label();
            txt_AddressPh08 = new CustomTextBox();
            label18 = new Label();
            cmb_BaudratePh08 = new CustomComboBox();
            label19 = new Label();
            btn_ReadDevice = new CustomRoundedButton();
            btn_test = new CustomRoundedButton();
            txt_massage = new CustomRichTextBox();
            label7 = new Label();
            chk_debug = new CheckBox();
            txt_SN = new CustomTextBox();
            label6 = new Label();
            label16 = new Label();
            label23 = new Label();
            txt_Lot = new CustomTextBox();
            label24 = new Label();
            txt_ECN = new CustomTextBox();
            label25 = new Label();
            txt_FW = new CustomTextBox();
            label_Rev = new Label();
            txt_Rev = new CustomTextBox();
            label26 = new Label();
            label27 = new Label();
            btn_openfolder = new CustomRoundedButton();
            label28 = new Label();
            txt_folder = new CustomTextBox();
            txt_name = new CustomTextBox();
            label29 = new Label();
            label36 = new Label();
            label35 = new Label();
            phase4 = new PictureBox();
            phase3 = new PictureBox();
            label34 = new Label();
            phase1 = new PictureBox();
            label33 = new Label();
            phase2 = new PictureBox();
            btn_Clear = new CustomRoundedButton();
            btn_up = new CustomRoundedButton();
            btn_down = new CustomRoundedButton();
            btn_userPill = new AvatarPillButton();
            btn_manualGear = new CustomRoundedButton();
            customRoundedPanel4 = new CustomRoundedPanel();
            label3 = new Label();
            label1 = new Label();
            chk_resultToserver = new CheckBox();
            customRoundedPanel5 = new CustomRoundedPanel();
            customRoundedPanel6 = new CustomRoundedPanel();
            panel1 = new Panel();
            panel2 = new Panel();
            customRoundedPanel1 = new CustomRoundedPanel();
            customRoundedPanel3 = new CustomRoundedPanel();
            customRoundedPanel10 = new CustomRoundedPanel();
            customRoundedPanel11 = new CustomRoundedPanel();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            customRoundedPanel2.SuspendLayout();
            customRoundedPanel9.SuspendLayout();
            customRoundedPanel8.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)phase4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)phase3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)phase1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)phase2).BeginInit();
            customRoundedPanel4.SuspendLayout();
            customRoundedPanel5.SuspendLayout();
            customRoundedPanel6.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            customRoundedPanel1.SuspendLayout();
            customRoundedPanel3.SuspendLayout();
            customRoundedPanel10.SuspendLayout();
            customRoundedPanel11.SuspendLayout();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(3, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(48, 55);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // btn_connect
            // 
            btn_connect.BackColor = Color.FromArgb(46, 125, 50);
            btn_connect.BorderColor = Color.FromArgb(46, 125, 50);
            btn_connect.BorderRadius = 5;
            btn_connect.BorderThickness = 2;
            btn_connect.Cursor = Cursors.Hand;
            btn_connect.FlatAppearance.BorderSize = 0;
            btn_connect.FlatStyle = FlatStyle.Flat;
            btn_connect.Font = new Font("Leelawadee UI", 9F, FontStyle.Bold);
            btn_connect.ForeColor = SystemColors.ButtonHighlight;
            btn_connect.Location = new Point(862, 10);
            btn_connect.Name = "btn_connect";
            btn_connect.Size = new Size(138, 39);
            btn_connect.TabIndex = 2;
            btn_connect.Text = "🔗Connect";
            btn_connect.UseVisualStyleBackColor = false;
            // 
            // badge_connection
            // 
            badge_connection.BackColor = Color.Transparent;
            badge_connection.ConnectedBackColor = Color.FromArgb(209, 237, 217);
            badge_connection.ConnectedBorderColor = Color.FromArgb(180, 225, 195);
            badge_connection.ConnectedDotColor = Color.FromArgb(27, 94, 32);
            badge_connection.ConnectedText = "เชื่อมต่อแล้ว";
            badge_connection.ConnectedTextColor = Color.FromArgb(27, 94, 32);
            badge_connection.DisconnectedBackColor = Color.FromArgb(254, 226, 226);
            badge_connection.DisconnectedBorderColor = Color.FromArgb(254, 202, 202);
            badge_connection.DisconnectedDotColor = Color.FromArgb(220, 38, 38);
            badge_connection.DisconnectedText = "ยังไม่เชื่อมต่อ";
            badge_connection.DisconnectedTextColor = Color.FromArgb(185, 28, 28);
            badge_connection.Font = new Font("Leelawadee UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            badge_connection.IsConnected = false;
            badge_connection.Location = new Point(757, 13);
            badge_connection.Name = "badge_connection";
            badge_connection.Size = new Size(112, 38);
            badge_connection.TabIndex = 3;
            // 
            // customRoundedPanel2
            // 
            customRoundedPanel2.BackColor = Color.White;
            customRoundedPanel2.BorderColor = Color.FromArgb(224, 224, 224);
            customRoundedPanel2.BorderRadius = 10;
            customRoundedPanel2.BorderThickness = 2;
            customRoundedPanel2.Controls.Add(customRoundedPanel9);
            customRoundedPanel2.Controls.Add(label8);
            customRoundedPanel2.Controls.Add(btn_Refresh);
            customRoundedPanel2.Controls.Add(btn_connect);
            customRoundedPanel2.Controls.Add(label2);
            customRoundedPanel2.Controls.Add(customRoundedPanel8);
            customRoundedPanel2.Location = new Point(14, 74);
            customRoundedPanel2.Name = "customRoundedPanel2";
            customRoundedPanel2.Size = new Size(1017, 200);
            customRoundedPanel2.TabIndex = 4;
            // 
            // customRoundedPanel9
            // 
            customRoundedPanel9.BackColor = Color.White;
            customRoundedPanel9.BorderColor = Color.FromArgb(227, 232, 228);
            customRoundedPanel9.BorderRadius = 15;
            customRoundedPanel9.BorderThickness = 2;
            customRoundedPanel9.Controls.Add(openadd_device);
            customRoundedPanel9.Controls.Add(label9);
            customRoundedPanel9.Controls.Add(label15);
            customRoundedPanel9.Controls.Add(label10);
            customRoundedPanel9.Controls.Add(cmb_ComportDevice);
            customRoundedPanel9.Controls.Add(label11);
            customRoundedPanel9.Controls.Add(txt_AddressDevice);
            customRoundedPanel9.Controls.Add(cmb_BaudrateDevice);
            customRoundedPanel9.Location = new Point(516, 56);
            customRoundedPanel9.Name = "customRoundedPanel9";
            customRoundedPanel9.Size = new Size(485, 135);
            customRoundedPanel9.TabIndex = 31;
            // 
            // openadd_device
            // 
            openadd_device.BackColor = Color.FromArgb(241, 248, 241);
            openadd_device.BorderColor = Color.FromArgb(241, 248, 241);
            openadd_device.BorderRadius = 6;
            openadd_device.BorderThickness = 1;
            openadd_device.Cursor = Cursors.Hand;
            openadd_device.FlatAppearance.BorderSize = 0;
            openadd_device.FlatStyle = FlatStyle.Flat;
            openadd_device.Font = new Font("Leelawadee UI", 8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            openadd_device.ForeColor = Color.FromArgb(27, 94, 32);
            openadd_device.Location = new Point(388, 99);
            openadd_device.Name = "openadd_device";
            openadd_device.Size = new Size(82, 28);
            openadd_device.TabIndex = 32;
            openadd_device.Text = "เพิ่มเติม >";
            openadd_device.UseVisualStyleBackColor = false;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold | FontStyle.Italic);
            label9.ForeColor = Color.FromArgb(64, 64, 64);
            label9.Location = new Point(18, 40);
            label9.Name = "label9";
            label9.Size = new Size(58, 17);
            label9.TabIndex = 34;
            label9.Text = "Comport";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Font = new Font("Leelawadee UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label15.ForeColor = Color.FromArgb(27, 94, 32);
            label15.Location = new Point(12, 5);
            label15.Name = "label15";
            label15.Size = new Size(144, 28);
            label15.TabIndex = 17;
            label15.Text = "Local (Device)";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold | FontStyle.Italic);
            label10.ForeColor = Color.FromArgb(64, 64, 64);
            label10.Location = new Point(188, 40);
            label10.Name = "label10";
            label10.Size = new Size(53, 17);
            label10.TabIndex = 32;
            label10.Text = "Address";
            // 
            // cmb_ComportDevice
            // 
            cmb_ComportDevice.ArrowColor = Color.FromArgb(90, 90, 90);
            cmb_ComportDevice.BackColor = Color.White;
            cmb_ComportDevice.BorderColor = Color.FromArgb(200, 200, 200);
            cmb_ComportDevice.BorderFocusColor = Color.FromArgb(0, 120, 215);
            cmb_ComportDevice.BorderRadius = 5;
            cmb_ComportDevice.BorderSize = 1;
            cmb_ComportDevice.DisabledArrowColor = Color.FromArgb(160, 170, 180);
            cmb_ComportDevice.DisabledBackColor = Color.FromArgb(235, 238, 240);
            cmb_ComportDevice.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            cmb_ComportDevice.DisabledForeColor = Color.FromArgb(120, 130, 140);
            cmb_ComportDevice.DrawMode = DrawMode.OwnerDrawFixed;
            cmb_ComportDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb_ComportDevice.FlatStyle = FlatStyle.Flat;
            cmb_ComportDevice.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            cmb_ComportDevice.ForeColor = Color.FromArgb(40, 40, 40);
            cmb_ComportDevice.FormattingEnabled = true;
            cmb_ComportDevice.ItemHeight = 28;
            cmb_ComportDevice.ItemHoverColor = Color.FromArgb(0, 120, 215);
            cmb_ComportDevice.ItemHoverForeColor = Color.Empty;
            cmb_ComportDevice.Location = new Point(18, 60);
            cmb_ComportDevice.Name = "cmb_ComportDevice";
            cmb_ComportDevice.Size = new Size(154, 34);
            cmb_ComportDevice.TabIndex = 23;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold | FontStyle.Italic);
            label11.ForeColor = Color.FromArgb(64, 64, 64);
            label11.Location = new Point(314, 40);
            label11.Name = "label11";
            label11.Size = new Size(69, 17);
            label11.TabIndex = 33;
            label11.Text = "Baud Rate";
            // 
            // txt_AddressDevice
            // 
            txt_AddressDevice.BackColor = Color.White;
            txt_AddressDevice.BorderColor = Color.FromArgb(200, 200, 200);
            txt_AddressDevice.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_AddressDevice.BorderRadius = 5;
            txt_AddressDevice.BorderSize = 1;
            txt_AddressDevice.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_AddressDevice.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_AddressDevice.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_AddressDevice.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            txt_AddressDevice.ForeColor = Color.FromArgb(40, 40, 40);
            txt_AddressDevice.InnerPadding = 12;
            txt_AddressDevice.Location = new Point(185, 60);
            txt_AddressDevice.Name = "txt_AddressDevice";
            txt_AddressDevice.Size = new Size(118, 34);
            txt_AddressDevice.TabIndex = 23;
            // 
            // cmb_BaudrateDevice
            // 
            cmb_BaudrateDevice.ArrowColor = Color.FromArgb(90, 90, 90);
            cmb_BaudrateDevice.BackColor = Color.White;
            cmb_BaudrateDevice.BorderColor = Color.FromArgb(200, 200, 200);
            cmb_BaudrateDevice.BorderFocusColor = Color.FromArgb(0, 120, 215);
            cmb_BaudrateDevice.BorderRadius = 5;
            cmb_BaudrateDevice.BorderSize = 1;
            cmb_BaudrateDevice.DisabledArrowColor = Color.FromArgb(160, 170, 180);
            cmb_BaudrateDevice.DisabledBackColor = Color.FromArgb(235, 238, 240);
            cmb_BaudrateDevice.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            cmb_BaudrateDevice.DisabledForeColor = Color.FromArgb(120, 130, 140);
            cmb_BaudrateDevice.DrawMode = DrawMode.OwnerDrawFixed;
            cmb_BaudrateDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb_BaudrateDevice.FlatStyle = FlatStyle.Flat;
            cmb_BaudrateDevice.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            cmb_BaudrateDevice.ForeColor = Color.FromArgb(40, 40, 40);
            cmb_BaudrateDevice.FormattingEnabled = true;
            cmb_BaudrateDevice.ItemHeight = 28;
            cmb_BaudrateDevice.ItemHoverColor = Color.FromArgb(0, 120, 215);
            cmb_BaudrateDevice.ItemHoverForeColor = Color.Empty;
            cmb_BaudrateDevice.Location = new Point(314, 60);
            cmb_BaudrateDevice.Name = "cmb_BaudrateDevice";
            cmb_BaudrateDevice.Size = new Size(156, 34);
            cmb_BaudrateDevice.TabIndex = 23;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Leelawadee UI", 7.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label8.ForeColor = Color.DarkGray;
            label8.Location = new Point(138, 19);
            label8.Name = "label8";
            label8.Size = new Size(166, 17);
            label8.TabIndex = 29;
            label8.Text = "เลือกพอร์ต แล้วกด Connect";
            // 
            // btn_Refresh
            // 
            btn_Refresh.BackColor = Color.FromArgb(203, 211, 205);
            btn_Refresh.BorderColor = Color.FromArgb(203, 211, 205);
            btn_Refresh.BorderRadius = 5;
            btn_Refresh.BorderThickness = 2;
            btn_Refresh.Cursor = Cursors.Hand;
            btn_Refresh.FlatAppearance.BorderSize = 0;
            btn_Refresh.FlatStyle = FlatStyle.Flat;
            btn_Refresh.Font = new Font("Leelawadee UI", 9F, FontStyle.Bold);
            btn_Refresh.ForeColor = Color.FromArgb(27, 94, 32);
            btn_Refresh.Location = new Point(737, 10);
            btn_Refresh.Name = "btn_Refresh";
            btn_Refresh.Size = new Size(115, 39);
            btn_Refresh.TabIndex = 15;
            btn_Refresh.Text = "⭯Reload";
            btn_Refresh.UseVisualStyleBackColor = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Leelawadee UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(31, 41, 51);
            label2.Location = new Point(17, 12);
            label2.Name = "label2";
            label2.Size = new Size(115, 28);
            label2.TabIndex = 28;
            label2.Text = "การเชื่อมต่อ";
            // 
            // customRoundedPanel8
            // 
            customRoundedPanel8.BackColor = Color.White;
            customRoundedPanel8.BorderColor = Color.FromArgb(227, 232, 228);
            customRoundedPanel8.BorderRadius = 15;
            customRoundedPanel8.BorderThickness = 2;
            customRoundedPanel8.Controls.Add(openadd_Ph08);
            customRoundedPanel8.Controls.Add(label14);
            customRoundedPanel8.Controls.Add(cmb_ComportPh08);
            customRoundedPanel8.Controls.Add(label17);
            customRoundedPanel8.Controls.Add(txt_AddressPh08);
            customRoundedPanel8.Controls.Add(label18);
            customRoundedPanel8.Controls.Add(cmb_BaudratePh08);
            customRoundedPanel8.Controls.Add(label19);
            customRoundedPanel8.Location = new Point(17, 56);
            customRoundedPanel8.Name = "customRoundedPanel8";
            customRoundedPanel8.Size = new Size(485, 135);
            customRoundedPanel8.TabIndex = 30;
            // 
            // openadd_Ph08
            // 
            openadd_Ph08.BackColor = Color.FromArgb(241, 248, 241);
            openadd_Ph08.BorderColor = Color.FromArgb(241, 248, 241);
            openadd_Ph08.BorderRadius = 6;
            openadd_Ph08.BorderThickness = 1;
            openadd_Ph08.Cursor = Cursors.Hand;
            openadd_Ph08.FlatAppearance.BorderSize = 0;
            openadd_Ph08.FlatStyle = FlatStyle.Flat;
            openadd_Ph08.Font = new Font("Leelawadee UI", 8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            openadd_Ph08.ForeColor = Color.FromArgb(27, 94, 32);
            openadd_Ph08.Location = new Point(388, 99);
            openadd_Ph08.Name = "openadd_Ph08";
            openadd_Ph08.Size = new Size(82, 28);
            openadd_Ph08.TabIndex = 31;
            openadd_Ph08.Text = "เพิ่มเติม >";
            openadd_Ph08.UseVisualStyleBackColor = false;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Font = new Font("Leelawadee UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label14.ForeColor = Color.FromArgb(27, 94, 32);
            label14.Location = new Point(12, 5);
            label14.Name = "label14";
            label14.Size = new Size(163, 28);
            label14.TabIndex = 16;
            label14.Text = "Remote (PH-08)";
            // 
            // cmb_ComportPh08
            // 
            cmb_ComportPh08.ArrowColor = Color.FromArgb(90, 90, 90);
            cmb_ComportPh08.BackColor = Color.White;
            cmb_ComportPh08.BorderColor = Color.FromArgb(200, 200, 200);
            cmb_ComportPh08.BorderFocusColor = Color.FromArgb(0, 120, 215);
            cmb_ComportPh08.BorderRadius = 5;
            cmb_ComportPh08.BorderSize = 1;
            cmb_ComportPh08.DisabledArrowColor = Color.FromArgb(160, 170, 180);
            cmb_ComportPh08.DisabledBackColor = Color.FromArgb(235, 238, 240);
            cmb_ComportPh08.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            cmb_ComportPh08.DisabledForeColor = Color.FromArgb(120, 130, 140);
            cmb_ComportPh08.DrawMode = DrawMode.OwnerDrawFixed;
            cmb_ComportPh08.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb_ComportPh08.FlatStyle = FlatStyle.Flat;
            cmb_ComportPh08.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            cmb_ComportPh08.ForeColor = Color.FromArgb(40, 40, 40);
            cmb_ComportPh08.FormattingEnabled = true;
            cmb_ComportPh08.ItemHeight = 28;
            cmb_ComportPh08.ItemHoverColor = Color.FromArgb(0, 120, 215);
            cmb_ComportPh08.ItemHoverForeColor = Color.Empty;
            cmb_ComportPh08.Location = new Point(16, 60);
            cmb_ComportPh08.Name = "cmb_ComportPh08";
            cmb_ComportPh08.Size = new Size(154, 34);
            cmb_ComportPh08.TabIndex = 17;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold | FontStyle.Italic);
            label17.ForeColor = Color.FromArgb(64, 64, 64);
            label17.Location = new Point(16, 40);
            label17.Name = "label17";
            label17.Size = new Size(58, 17);
            label17.TabIndex = 8;
            label17.Text = "Comport";
            // 
            // txt_AddressPh08
            // 
            txt_AddressPh08.BackColor = Color.White;
            txt_AddressPh08.BorderColor = Color.FromArgb(200, 200, 200);
            txt_AddressPh08.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_AddressPh08.BorderRadius = 5;
            txt_AddressPh08.BorderSize = 1;
            txt_AddressPh08.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_AddressPh08.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_AddressPh08.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_AddressPh08.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            txt_AddressPh08.ForeColor = Color.FromArgb(40, 40, 40);
            txt_AddressPh08.InnerPadding = 12;
            txt_AddressPh08.Location = new Point(183, 60);
            txt_AddressPh08.Name = "txt_AddressPh08";
            txt_AddressPh08.Size = new Size(118, 34);
            txt_AddressPh08.TabIndex = 17;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold | FontStyle.Italic);
            label18.ForeColor = Color.FromArgb(64, 64, 64);
            label18.Location = new Point(184, 40);
            label18.Name = "label18";
            label18.Size = new Size(53, 17);
            label18.TabIndex = 4;
            label18.Text = "Address";
            // 
            // cmb_BaudratePh08
            // 
            cmb_BaudratePh08.ArrowColor = Color.FromArgb(90, 90, 90);
            cmb_BaudratePh08.BackColor = Color.White;
            cmb_BaudratePh08.BorderColor = Color.FromArgb(200, 200, 200);
            cmb_BaudratePh08.BorderFocusColor = Color.FromArgb(0, 120, 215);
            cmb_BaudratePh08.BorderRadius = 5;
            cmb_BaudratePh08.BorderSize = 1;
            cmb_BaudratePh08.DisabledArrowColor = Color.FromArgb(160, 170, 180);
            cmb_BaudratePh08.DisabledBackColor = Color.FromArgb(235, 238, 240);
            cmb_BaudratePh08.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            cmb_BaudratePh08.DisabledForeColor = Color.FromArgb(120, 130, 140);
            cmb_BaudratePh08.DrawMode = DrawMode.OwnerDrawFixed;
            cmb_BaudratePh08.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb_BaudratePh08.FlatStyle = FlatStyle.Flat;
            cmb_BaudratePh08.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            cmb_BaudratePh08.ForeColor = Color.FromArgb(40, 40, 40);
            cmb_BaudratePh08.FormattingEnabled = true;
            cmb_BaudratePh08.ItemHeight = 28;
            cmb_BaudratePh08.ItemHoverColor = Color.FromArgb(0, 120, 215);
            cmb_BaudratePh08.ItemHoverForeColor = Color.Empty;
            cmb_BaudratePh08.Location = new Point(314, 60);
            cmb_BaudratePh08.Name = "cmb_BaudratePh08";
            cmb_BaudratePh08.Size = new Size(156, 34);
            cmb_BaudratePh08.TabIndex = 19;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold | FontStyle.Italic);
            label19.ForeColor = Color.FromArgb(64, 64, 64);
            label19.Location = new Point(314, 41);
            label19.Name = "label19";
            label19.Size = new Size(69, 17);
            label19.TabIndex = 6;
            label19.Text = "Baud Rate";
            // 
            // btn_ReadDevice
            // 
            btn_ReadDevice.BackColor = Color.FromArgb(221, 227, 223);
            btn_ReadDevice.BorderColor = Color.FromArgb(221, 227, 223);
            btn_ReadDevice.BorderRadius = 5;
            btn_ReadDevice.BorderThickness = 2;
            btn_ReadDevice.Cursor = Cursors.Hand;
            btn_ReadDevice.FlatAppearance.BorderSize = 0;
            btn_ReadDevice.FlatStyle = FlatStyle.Flat;
            btn_ReadDevice.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btn_ReadDevice.ForeColor = Color.FromArgb(91, 102, 112);
            btn_ReadDevice.Location = new Point(143, 12);
            btn_ReadDevice.Name = "btn_ReadDevice";
            btn_ReadDevice.Size = new Size(82, 33);
            btn_ReadDevice.TabIndex = 18;
            btn_ReadDevice.Text = "STOP";
            btn_ReadDevice.UseVisualStyleBackColor = false;
            // 
            // btn_test
            // 
            btn_test.BackColor = Color.FromArgb(225, 230, 228);
            btn_test.BorderColor = Color.FromArgb(215, 220, 218);
            btn_test.BorderRadius = 5;
            btn_test.BorderThickness = 2;
            btn_test.Enabled = false;
            btn_test.FlatAppearance.BorderSize = 0;
            btn_test.FlatStyle = FlatStyle.Flat;
            btn_test.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_test.ForeColor = Color.FromArgb(125, 140, 148);
            btn_test.Location = new Point(18, 12);
            btn_test.Name = "btn_test";
            btn_test.Size = new Size(119, 57);
            btn_test.TabIndex = 17;
            btn_test.Text = "TEST";
            btn_test.UseVisualStyleBackColor = false;
            // 
            // txt_massage
            // 
            txt_massage.BackColor = Color.FromArgb(30, 36, 40);
            txt_massage.BorderColor = Color.FromArgb(30, 36, 40);
            txt_massage.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_massage.BorderRadius = 2;
            txt_massage.BorderSize = 1;
            txt_massage.Font = new Font("Leelawadee UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txt_massage.ForeColor = Color.White;
            txt_massage.InnerPadding = 12;
            txt_massage.Location = new Point(14, 41);
            txt_massage.Name = "txt_massage";
            txt_massage.Size = new Size(693, 149);
            txt_massage.TabIndex = 26;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Leelawadee UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.White;
            label7.Location = new Point(11, 8);
            label7.Name = "label7";
            label7.Size = new Size(113, 23);
            label7.TabIndex = 23;
            label7.Text = "Message Box";
            // 
            // chk_debug
            // 
            chk_debug.AutoSize = true;
            chk_debug.Font = new Font("Segoe UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chk_debug.ForeColor = SystemColors.ButtonHighlight;
            chk_debug.Location = new Point(578, 14);
            chk_debug.Name = "chk_debug";
            chk_debug.Size = new Size(69, 21);
            chk_debug.TabIndex = 35;
            chk_debug.Text = "Debug";
            chk_debug.UseVisualStyleBackColor = true;
            // 
            // txt_SN
            // 
            txt_SN.BackColor = Color.White;
            txt_SN.BorderColor = Color.FromArgb(200, 200, 200);
            txt_SN.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_SN.BorderRadius = 5;
            txt_SN.BorderSize = 1;
            txt_SN.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_SN.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_SN.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_SN.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            txt_SN.ForeColor = Color.FromArgb(40, 40, 40);
            txt_SN.InnerPadding = 12;
            txt_SN.Location = new Point(13, 65);
            txt_SN.Name = "txt_SN";
            txt_SN.Size = new Size(187, 32);
            txt_SN.TabIndex = 36;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Leelawadee UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.ForeColor = Color.FromArgb(31, 41, 51);
            label6.Location = new Point(7, 7);
            label6.Name = "label6";
            label6.Size = new Size(194, 28);
            label6.TabIndex = 37;
            label6.Text = "Device Information";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label16.ForeColor = Color.FromArgb(91, 102, 112);
            label16.Location = new Point(13, 45);
            label16.Name = "label16";
            label16.Size = new Size(33, 17);
            label16.TabIndex = 38;
            label16.Text = "S/N:";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label23.ForeColor = Color.FromArgb(91, 102, 112);
            label23.Location = new Point(15, 107);
            label23.Name = "label23";
            label23.Size = new Size(33, 17);
            label23.TabIndex = 40;
            label23.Text = "LOT:";
            // 
            // txt_Lot
            // 
            txt_Lot.BackColor = Color.White;
            txt_Lot.BorderColor = Color.FromArgb(200, 200, 200);
            txt_Lot.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_Lot.BorderRadius = 5;
            txt_Lot.BorderSize = 1;
            txt_Lot.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_Lot.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_Lot.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_Lot.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            txt_Lot.ForeColor = Color.FromArgb(40, 40, 40);
            txt_Lot.InnerPadding = 12;
            txt_Lot.Location = new Point(15, 129);
            txt_Lot.Name = "txt_Lot";
            txt_Lot.Size = new Size(258, 32);
            txt_Lot.TabIndex = 39;
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold);
            label24.ForeColor = Color.FromArgb(91, 102, 112);
            label24.Location = new Point(15, 170);
            label24.Name = "label24";
            label24.Size = new Size(60, 17);
            label24.TabIndex = 42;
            label24.Text = "ECN NO:";
            // 
            // txt_ECN
            // 
            txt_ECN.BackColor = Color.White;
            txt_ECN.BorderColor = Color.FromArgb(200, 200, 200);
            txt_ECN.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_ECN.BorderRadius = 5;
            txt_ECN.BorderSize = 1;
            txt_ECN.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_ECN.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_ECN.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_ECN.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            txt_ECN.ForeColor = Color.FromArgb(40, 40, 40);
            txt_ECN.InnerPadding = 8;
            txt_ECN.Location = new Point(15, 191);
            txt_ECN.Name = "txt_ECN";
            txt_ECN.Size = new Size(258, 32);
            txt_ECN.TabIndex = 41;
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold);
            label25.ForeColor = Color.FromArgb(91, 102, 112);
            label25.Location = new Point(15, 233);
            label25.Name = "label25";
            label25.Size = new Size(31, 17);
            label25.TabIndex = 44;
            label25.Text = "FW:";
            // 
            // txt_FW
            // 
            txt_FW.BackColor = Color.White;
            txt_FW.BorderColor = Color.FromArgb(200, 200, 200);
            txt_FW.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_FW.BorderRadius = 5;
            txt_FW.BorderSize = 1;
            txt_FW.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_FW.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_FW.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_FW.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            txt_FW.ForeColor = Color.FromArgb(40, 40, 40);
            txt_FW.InnerPadding = 8;
            txt_FW.Location = new Point(15, 254);
            txt_FW.Name = "txt_FW";
            txt_FW.Size = new Size(126, 32);
            txt_FW.TabIndex = 43;
            // 
            // label_Rev
            // 
            label_Rev.AutoSize = true;
            label_Rev.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold);
            label_Rev.ForeColor = Color.FromArgb(91, 102, 112);
            label_Rev.Location = new Point(153, 233);
            label_Rev.Name = "label_Rev";
            label_Rev.Size = new Size(34, 17);
            label_Rev.TabIndex = 45;
            label_Rev.Text = "REV:";
            // 
            // txt_Rev
            // 
            txt_Rev.BackColor = Color.White;
            txt_Rev.BorderColor = Color.FromArgb(200, 200, 200);
            txt_Rev.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_Rev.BorderRadius = 5;
            txt_Rev.BorderSize = 1;
            txt_Rev.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_Rev.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_Rev.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_Rev.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            txt_Rev.ForeColor = Color.FromArgb(40, 40, 40);
            txt_Rev.InnerPadding = 8;
            txt_Rev.Location = new Point(149, 254);
            txt_Rev.Name = "txt_Rev";
            txt_Rev.Size = new Size(124, 32);
            txt_Rev.TabIndex = 44;
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Font = new Font("Leelawadee UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label26.ForeColor = Color.FromArgb(232, 242, 233);
            label26.Location = new Point(79, 42);
            label26.Name = "label26";
            label26.Size = new Size(95, 20);
            label26.TabIndex = 46;
            label26.Text = "Version: 1.0.0";
            // 
            // label27
            // 
            label27.AutoSize = true;
            label27.Font = new Font("Leelawadee UI", 16.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label27.ForeColor = SystemColors.ButtonHighlight;
            label27.Location = new Point(75, 6);
            label27.Name = "label27";
            label27.Size = new Size(160, 38);
            label27.TabIndex = 47;
            label27.Text = "PH-08 Test";
            // 
            // btn_openfolder
            // 
            btn_openfolder.BackColor = Color.Silver;
            btn_openfolder.BorderColor = Color.DarkGray;
            btn_openfolder.BorderRadius = 5;
            btn_openfolder.BorderThickness = 2;
            btn_openfolder.Cursor = Cursors.Hand;
            btn_openfolder.FlatAppearance.BorderSize = 0;
            btn_openfolder.FlatStyle = FlatStyle.Flat;
            btn_openfolder.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_openfolder.Location = new Point(210, 319);
            btn_openfolder.Name = "btn_openfolder";
            btn_openfolder.Size = new Size(63, 32);
            btn_openfolder.TabIndex = 50;
            btn_openfolder.Text = "Open";
            btn_openfolder.UseVisualStyleBackColor = false;
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold);
            label28.ForeColor = Color.FromArgb(91, 102, 112);
            label28.Location = new Point(15, 298);
            label28.Name = "label28";
            label28.Size = new Size(50, 17);
            label28.TabIndex = 49;
            label28.Text = "Folder:";
            // 
            // txt_folder
            // 
            txt_folder.BackColor = Color.White;
            txt_folder.BorderColor = Color.FromArgb(200, 200, 200);
            txt_folder.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_folder.BorderRadius = 5;
            txt_folder.BorderSize = 1;
            txt_folder.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_folder.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_folder.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_folder.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            txt_folder.ForeColor = Color.FromArgb(40, 40, 40);
            txt_folder.InnerPadding = 12;
            txt_folder.Location = new Point(15, 319);
            txt_folder.Name = "txt_folder";
            txt_folder.Size = new Size(187, 32);
            txt_folder.TabIndex = 48;
            // 
            // txt_name
            // 
            txt_name.BackColor = Color.White;
            txt_name.BorderColor = Color.FromArgb(200, 200, 200);
            txt_name.BorderFocusColor = Color.FromArgb(0, 120, 215);
            txt_name.BorderRadius = 5;
            txt_name.BorderSize = 1;
            txt_name.DisabledBackColor = Color.FromArgb(235, 238, 240);
            txt_name.DisabledBorderColor = Color.FromArgb(215, 219, 223);
            txt_name.DisabledForeColor = Color.FromArgb(120, 130, 140);
            txt_name.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            txt_name.ForeColor = Color.FromArgb(40, 40, 40);
            txt_name.InnerPadding = 12;
            txt_name.Location = new Point(13, 384);
            txt_name.Name = "txt_name";
            txt_name.Size = new Size(256, 32);
            txt_name.TabIndex = 52;
            // 
            // label29
            // 
            label29.AutoSize = true;
            label29.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold);
            label29.ForeColor = Color.FromArgb(91, 102, 112);
            label29.Location = new Point(13, 361);
            label29.Name = "label29";
            label29.Size = new Size(61, 17);
            label29.TabIndex = 53;
            label29.Text = "ผู้ทดสอบ:";
            // 
            // label36
            // 
            label36.AutoSize = true;
            label36.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label36.ForeColor = Color.FromArgb(64, 64, 64);
            label36.Location = new Point(36, 95);
            label36.Name = "label36";
            label36.Size = new Size(87, 23);
            label36.TabIndex = 63;
            label36.Text = "Switch ID";
            // 
            // label35
            // 
            label35.AutoSize = true;
            label35.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label35.ForeColor = Color.FromArgb(64, 64, 64);
            label35.Location = new Point(27, 95);
            label35.Name = "label35";
            label35.Size = new Size(103, 23);
            label35.TabIndex = 62;
            label35.Text = "Led Display";
            // 
            // phase4
            // 
            phase4.Location = new Point(44, 17);
            phase4.Name = "phase4";
            phase4.Size = new Size(69, 65);
            phase4.SizeMode = PictureBoxSizeMode.Zoom;
            phase4.TabIndex = 3;
            phase4.TabStop = false;
            // 
            // phase3
            // 
            phase3.Location = new Point(44, 17);
            phase3.Name = "phase3";
            phase3.Size = new Size(69, 65);
            phase3.SizeMode = PictureBoxSizeMode.Zoom;
            phase3.TabIndex = 2;
            phase3.TabStop = false;
            // 
            // label34
            // 
            label34.AutoSize = true;
            label34.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label34.ForeColor = Color.FromArgb(64, 64, 64);
            label34.Location = new Point(31, 95);
            label34.Name = "label34";
            label34.Size = new Size(98, 23);
            label34.TabIndex = 61;
            label34.Text = "Local Com.";
            // 
            // phase1
            // 
            phase1.Location = new Point(42, 17);
            phase1.Name = "phase1";
            phase1.Size = new Size(69, 65);
            phase1.SizeMode = PictureBoxSizeMode.Zoom;
            phase1.TabIndex = 0;
            phase1.TabStop = false;
            // 
            // label33
            // 
            label33.AutoSize = true;
            label33.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label33.ForeColor = Color.FromArgb(64, 64, 64);
            label33.Location = new Point(18, 95);
            label33.Name = "label33";
            label33.Size = new Size(119, 23);
            label33.TabIndex = 60;
            label33.Text = "Remote Com.";
            // 
            // phase2
            // 
            phase2.Location = new Point(43, 17);
            phase2.Name = "phase2";
            phase2.Size = new Size(69, 65);
            phase2.SizeMode = PictureBoxSizeMode.Zoom;
            phase2.TabIndex = 1;
            phase2.TabStop = false;
            // 
            // btn_Clear
            // 
            btn_Clear.BackColor = Color.Silver;
            btn_Clear.BorderColor = Color.Silver;
            btn_Clear.BorderRadius = 5;
            btn_Clear.BorderThickness = 2;
            btn_Clear.Cursor = Cursors.Hand;
            btn_Clear.FlatAppearance.BorderSize = 0;
            btn_Clear.FlatStyle = FlatStyle.Flat;
            btn_Clear.Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_Clear.Location = new Point(651, 12);
            btn_Clear.Name = "btn_Clear";
            btn_Clear.Size = new Size(51, 23);
            btn_Clear.TabIndex = 56;
            btn_Clear.Text = "Clear";
            btn_Clear.UseVisualStyleBackColor = false;
            // 
            // btn_up
            // 
            btn_up.BackColor = Color.Silver;
            btn_up.BorderColor = Color.DarkGray;
            btn_up.BorderRadius = 5;
            btn_up.BorderThickness = 2;
            btn_up.Cursor = Cursors.Hand;
            btn_up.FlatAppearance.BorderSize = 0;
            btn_up.FlatStyle = FlatStyle.Flat;
            btn_up.Font = new Font("Arial Black", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_up.Location = new Point(208, 65);
            btn_up.Name = "btn_up";
            btn_up.Size = new Size(28, 32);
            btn_up.TabIndex = 65;
            btn_up.Text = "⬆";
            btn_up.UseVisualStyleBackColor = false;
            // 
            // btn_down
            // 
            btn_down.BackColor = Color.Silver;
            btn_down.BorderColor = Color.DarkGray;
            btn_down.BorderRadius = 5;
            btn_down.BorderThickness = 2;
            btn_down.Cursor = Cursors.Hand;
            btn_down.FlatAppearance.BorderSize = 0;
            btn_down.FlatStyle = FlatStyle.Flat;
            btn_down.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_down.Location = new Point(244, 65);
            btn_down.Name = "btn_down";
            btn_down.Size = new Size(29, 32);
            btn_down.TabIndex = 66;
            btn_down.Text = "⬇";
            btn_down.UseVisualStyleBackColor = false;
            // 
            // btn_userPill
            // 
            btn_userPill.AvatarBgColor = Color.FromArgb(209, 237, 217);
            btn_userPill.BackColor = Color.Transparent;
            btn_userPill.Font = new Font("Leelawadee UI", 7.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_userPill.IsLoggedIn = false;
            btn_userPill.Location = new Point(879, 13);
            btn_userPill.LoggedOutText = "Login";
            btn_userPill.Name = "btn_userPill";
            btn_userPill.OperatorName = "";
            btn_userPill.PillBackColor = Color.White;
            btn_userPill.PillBorderColor = Color.FromArgb(226, 232, 240);
            btn_userPill.PillHoverColor = Color.FromArgb(248, 250, 252);
            btn_userPill.PrimaryTextColor = Color.FromArgb(27, 94, 32);
            btn_userPill.RightAnchorX = 1188;
            btn_userPill.Size = new Size(155, 38);
            btn_userPill.TabIndex = 67;
            // 
            // btn_manualGear
            // 
            btn_manualGear.BackColor = Color.FromArgb(27, 94, 32);
            btn_manualGear.BorderColor = Color.FromArgb(27, 94, 32);
            btn_manualGear.BorderRadius = 8;
            btn_manualGear.BorderThickness = 1;
            btn_manualGear.Cursor = Cursors.Hand;
            btn_manualGear.FlatAppearance.BorderSize = 0;
            btn_manualGear.FlatStyle = FlatStyle.Flat;
            btn_manualGear.Font = new Font("Segoe UI Symbol", 13.8F, FontStyle.Bold);
            btn_manualGear.ForeColor = Color.White;
            btn_manualGear.Location = new Point(663, 2);
            btn_manualGear.Name = "btn_manualGear";
            btn_manualGear.Size = new Size(46, 37);
            btn_manualGear.TabIndex = 80;
            btn_manualGear.Text = "⚙";
            btn_manualGear.UseVisualStyleBackColor = false;
            // 
            // customRoundedPanel4
            // 
            customRoundedPanel4.BackColor = Color.FromArgb(27, 94, 32);
            customRoundedPanel4.BorderColor = Color.FromArgb(27, 94, 32);
            customRoundedPanel4.BorderRadius = 15;
            customRoundedPanel4.BorderThickness = 2;
            customRoundedPanel4.Controls.Add(btn_manualGear);
            customRoundedPanel4.Controls.Add(label3);
            customRoundedPanel4.Controls.Add(label1);
            customRoundedPanel4.Controls.Add(chk_resultToserver);
            customRoundedPanel4.Controls.Add(btn_test);
            customRoundedPanel4.Controls.Add(btn_ReadDevice);
            customRoundedPanel4.Location = new Point(321, 285);
            customRoundedPanel4.Name = "customRoundedPanel4";
            customRoundedPanel4.Size = new Size(710, 82);
            customRoundedPanel4.TabIndex = 69;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Leelawadee UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.DarkGray;
            label3.Location = new Point(305, 42);
            label3.Name = "label3";
            label3.Size = new Size(268, 20);
            label3.TabIndex = 68;
            label3.Text = "เชื่อมต่อ Device ให้สำเร็จก่อน จึงกด Test ได้";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Leelawadee UI", 10.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(239, 83, 80);
            label1.Location = new Point(305, 15);
            label1.Name = "label1";
            label1.Size = new Size(186, 25);
            label1.TabIndex = 32;
            label1.Text = "⚠️ ยังไม่พร้อมทดสอบ";
            // 
            // chk_resultToserver
            // 
            chk_resultToserver.AutoSize = true;
            chk_resultToserver.Font = new Font("Leelawadee UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chk_resultToserver.ForeColor = Color.DarkGray;
            chk_resultToserver.Location = new Point(143, 50);
            chk_resultToserver.Name = "chk_resultToserver";
            chk_resultToserver.Size = new Size(67, 21);
            chk_resultToserver.TabIndex = 67;
            chk_resultToserver.Text = "Server";
            chk_resultToserver.UseVisualStyleBackColor = true;
            // 
            // customRoundedPanel5
            // 
            customRoundedPanel5.BackColor = Color.FromArgb(30, 36, 40);
            customRoundedPanel5.BorderColor = Color.White;
            customRoundedPanel5.BorderRadius = 5;
            customRoundedPanel5.BorderThickness = 2;
            customRoundedPanel5.Controls.Add(txt_massage);
            customRoundedPanel5.Controls.Add(label7);
            customRoundedPanel5.Controls.Add(chk_debug);
            customRoundedPanel5.Controls.Add(btn_Clear);
            customRoundedPanel5.Location = new Point(320, 520);
            customRoundedPanel5.Name = "customRoundedPanel5";
            customRoundedPanel5.Size = new Size(711, 193);
            customRoundedPanel5.TabIndex = 70;
            // 
            // customRoundedPanel6
            // 
            customRoundedPanel6.BackColor = Color.White;
            customRoundedPanel6.BorderColor = Color.White;
            customRoundedPanel6.BorderRadius = 5;
            customRoundedPanel6.BorderThickness = 2;
            customRoundedPanel6.Controls.Add(txt_name);
            customRoundedPanel6.Controls.Add(btn_down);
            customRoundedPanel6.Controls.Add(btn_up);
            customRoundedPanel6.Controls.Add(label6);
            customRoundedPanel6.Controls.Add(label16);
            customRoundedPanel6.Controls.Add(txt_SN);
            customRoundedPanel6.Controls.Add(label23);
            customRoundedPanel6.Controls.Add(txt_Lot);
            customRoundedPanel6.Controls.Add(txt_ECN);
            customRoundedPanel6.Controls.Add(txt_FW);
            customRoundedPanel6.Controls.Add(txt_Rev);
            customRoundedPanel6.Controls.Add(label29);
            customRoundedPanel6.Controls.Add(label24);
            customRoundedPanel6.Controls.Add(label25);
            customRoundedPanel6.Controls.Add(label_Rev);
            customRoundedPanel6.Controls.Add(label28);
            customRoundedPanel6.Controls.Add(btn_openfolder);
            customRoundedPanel6.Controls.Add(txt_folder);
            customRoundedPanel6.Location = new Point(14, 285);
            customRoundedPanel6.Name = "customRoundedPanel6";
            customRoundedPanel6.Size = new Size(287, 428);
            customRoundedPanel6.TabIndex = 71;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(27, 94, 32);
            panel1.Controls.Add(label27);
            panel1.Controls.Add(label26);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(badge_connection);
            panel1.Controls.Add(btn_userPill);
            panel1.Location = new Point(-3, -1);
            panel1.Name = "panel1";
            panel1.Size = new Size(1052, 66);
            panel1.TabIndex = 73;
            // 
            // panel2
            // 
            panel2.BackColor = Color.White;
            panel2.Controls.Add(pictureBox1);
            panel2.Location = new Point(17, 6);
            panel2.Name = "panel2";
            panel2.Size = new Size(53, 55);
            panel2.TabIndex = 0;
            // 
            // customRoundedPanel1
            // 
            customRoundedPanel1.BackColor = Color.White;
            customRoundedPanel1.BorderColor = Color.FromArgb(227, 232, 228);
            customRoundedPanel1.BorderRadius = 15;
            customRoundedPanel1.BorderThickness = 2;
            customRoundedPanel1.Controls.Add(phase1);
            customRoundedPanel1.Controls.Add(label33);
            customRoundedPanel1.Location = new Point(321, 375);
            customRoundedPanel1.Name = "customRoundedPanel1";
            customRoundedPanel1.Size = new Size(156, 136);
            customRoundedPanel1.TabIndex = 74;
            // 
            // customRoundedPanel3
            // 
            customRoundedPanel3.BackColor = Color.White;
            customRoundedPanel3.BorderColor = Color.FromArgb(227, 232, 228);
            customRoundedPanel3.BorderRadius = 15;
            customRoundedPanel3.BorderThickness = 2;
            customRoundedPanel3.Controls.Add(phase2);
            customRoundedPanel3.Controls.Add(label34);
            customRoundedPanel3.Location = new Point(505, 375);
            customRoundedPanel3.Name = "customRoundedPanel3";
            customRoundedPanel3.Size = new Size(156, 136);
            customRoundedPanel3.TabIndex = 75;
            // 
            // customRoundedPanel10
            // 
            customRoundedPanel10.BackColor = Color.White;
            customRoundedPanel10.BorderColor = Color.FromArgb(227, 232, 228);
            customRoundedPanel10.BorderRadius = 15;
            customRoundedPanel10.BorderThickness = 2;
            customRoundedPanel10.Controls.Add(phase3);
            customRoundedPanel10.Controls.Add(label35);
            customRoundedPanel10.Location = new Point(689, 375);
            customRoundedPanel10.Name = "customRoundedPanel10";
            customRoundedPanel10.Size = new Size(156, 136);
            customRoundedPanel10.TabIndex = 76;
            // 
            // customRoundedPanel11
            // 
            customRoundedPanel11.BackColor = Color.White;
            customRoundedPanel11.BorderColor = Color.FromArgb(227, 232, 228);
            customRoundedPanel11.BorderRadius = 15;
            customRoundedPanel11.BorderThickness = 2;
            customRoundedPanel11.Controls.Add(phase4);
            customRoundedPanel11.Controls.Add(label36);
            customRoundedPanel11.Location = new Point(873, 375);
            customRoundedPanel11.Name = "customRoundedPanel11";
            customRoundedPanel11.Size = new Size(158, 136);
            customRoundedPanel11.TabIndex = 77;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(238, 242, 239);
            ClientSize = new Size(1046, 725);
            Controls.Add(customRoundedPanel11);
            Controls.Add(customRoundedPanel10);
            Controls.Add(customRoundedPanel3);
            Controls.Add(customRoundedPanel1);
            Controls.Add(panel1);
            Controls.Add(customRoundedPanel2);
            Controls.Add(customRoundedPanel4);
            Controls.Add(customRoundedPanel5);
            Controls.Add(customRoundedPanel6);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "PH-08 Test App";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            customRoundedPanel2.ResumeLayout(false);
            customRoundedPanel2.PerformLayout();
            customRoundedPanel9.ResumeLayout(false);
            customRoundedPanel9.PerformLayout();
            customRoundedPanel8.ResumeLayout(false);
            customRoundedPanel8.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)phase4).EndInit();
            ((System.ComponentModel.ISupportInitialize)phase3).EndInit();
            ((System.ComponentModel.ISupportInitialize)phase1).EndInit();
            ((System.ComponentModel.ISupportInitialize)phase2).EndInit();
            customRoundedPanel4.ResumeLayout(false);
            customRoundedPanel4.PerformLayout();
            customRoundedPanel5.ResumeLayout(false);
            customRoundedPanel5.PerformLayout();
            customRoundedPanel6.ResumeLayout(false);
            customRoundedPanel6.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            customRoundedPanel1.ResumeLayout(false);
            customRoundedPanel1.PerformLayout();
            customRoundedPanel3.ResumeLayout(false);
            customRoundedPanel3.PerformLayout();
            customRoundedPanel10.ResumeLayout(false);
            customRoundedPanel10.PerformLayout();
            customRoundedPanel11.ResumeLayout(false);
            customRoundedPanel11.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private PictureBox pictureBox1;
        private CustomRoundedButton btn_connect;
        private ConnectionStatusBadge badge_connection;
        private CustomRoundedPanel customRoundedPanel2;
        private Label label14;
        private CustomRoundedButton btn_Refresh;
        private Label label15;
        private CustomRoundedButton btn_ReadDevice;
        private CustomRoundedButton btn_test;
        private CustomComboBox cmb_BaudratePh08;
        private CustomComboBox cmb_ComportPh08;
        private CustomTextBox txt_AddressPh08;
        private CustomComboBox cmb_BaudrateDevice;
        private CustomTextBox txt_AddressDevice;
        private CustomComboBox cmb_ComportDevice;
        private CustomRichTextBox txt_massage;
        private Label label7;
        private Label label17;
        private Label label18;
        private Label label19;
        private Label label2;
        private CheckBox chk_debug;
        private CustomTextBox txt_SN;
        private Label label6;
        private Label label16;
        private Label label23;
        private CustomTextBox txt_Lot;
        private Label label24;
        private CustomTextBox txt_ECN;
        private Label label25;
        private CustomTextBox txt_FW;
        private Label label_Rev;
        private CustomTextBox txt_Rev;
        private Label label26;
        private Label label27;
        private CustomRoundedButton btn_openfolder;
        private Label label28;
        private CustomTextBox txt_folder;
        private CustomTextBox txt_name;
        private Label label29;
        private CustomRoundedButton btn_Clear;
        private Label label33;
        private PictureBox phase4;
        private PictureBox phase3;
        private PictureBox phase2;
        private PictureBox phase1;
        private Label label36;
        private Label label35;
        private Label label34;
        private CustomRoundedButton btn_up;
        private CustomRoundedButton btn_down;
        private AvatarPillButton btn_userPill;
        private CustomRoundedButton btn_manualGear;
        private CustomRoundedPanel customRoundedPanel4;
        private CustomRoundedPanel customRoundedPanel5;
        private CustomRoundedPanel customRoundedPanel6;
        private CheckBox chk_resultToserver;
        private Panel panel1;
        private Panel panel2;
        private Label label8;
        private CustomRoundedPanel customRoundedPanel8;
        private CustomRoundedButton openadd_Ph08;
        private CustomRoundedPanel customRoundedPanel9;
        private CustomRoundedButton openadd_device;
        private Label label9;
        private Label label10;
        private Label label11;
        private Label label3;
        private Label label1;
        private CustomRoundedPanel customRoundedPanel1;
        private CustomRoundedPanel customRoundedPanel3;
        private CustomRoundedPanel customRoundedPanel10;
        private CustomRoundedPanel customRoundedPanel11;
    }
}
