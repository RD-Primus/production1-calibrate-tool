namespace CMATestVer1
{
    partial class Form4
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form4));
            RecieverBox = new RichTextBox();
            testDis = new Button();
            SilenceTimer = new System.Windows.Forms.Timer(components);
            TimeoutTimer = new System.Windows.Forms.Timer(components);
            myRoundedPanel2 = new CMA003AVer2.MyRoundedPanel();
            label6 = new Label();
            chk7Segment = new CheckBox();
            label4 = new Label();
            chkWL = new CheckBox();
            chkHP = new CheckBox();
            chkOut = new CheckBox();
            myRoundedPanel65 = new CMA003AVer2.MyRoundedPanel();
            label59 = new Label();
            label1 = new Label();
            myRoundedPanel1 = new CMA003AVer2.MyRoundedPanel();
            label5 = new Label();
            dot0 = new PictureBox();
            myRoundedPanel3 = new CMA003AVer2.MyRoundedPanel();
            dot1 = new PictureBox();
            label2 = new Label();
            myRoundedPanel4 = new CMA003AVer2.MyRoundedPanel();
            dot2 = new PictureBox();
            label3 = new Label();
            countdown = new Label();
            timer1 = new System.Windows.Forms.Timer(components);
            timer2 = new System.Windows.Forms.Timer(components);
            CountdownTick = new System.Windows.Forms.Timer(components);
            myRoundedPanel7 = new CMA003AVer2.MyRoundedPanel();
            btnRefreshDisplay = new Button();
            WriteQueueTimer = new System.Windows.Forms.Timer(components);
            myRoundedPanel5 = new CMA003AVer2.MyRoundedPanel();
            label7 = new Label();
            myRoundedPanel2.SuspendLayout();
            myRoundedPanel65.SuspendLayout();
            myRoundedPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dot0).BeginInit();
            myRoundedPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dot1).BeginInit();
            myRoundedPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dot2).BeginInit();
            myRoundedPanel7.SuspendLayout();
            myRoundedPanel5.SuspendLayout();
            SuspendLayout();
            // 
            // RecieverBox
            // 
            RecieverBox.BackColor = Color.FromArgb(26, 42, 26);
            RecieverBox.Font = new Font("Cordia New", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            RecieverBox.ForeColor = Color.White;
            RecieverBox.Location = new Point(484, 169);
            RecieverBox.Margin = new Padding(3, 4, 3, 4);
            RecieverBox.Name = "RecieverBox";
            RecieverBox.Size = new Size(378, 429);
            RecieverBox.TabIndex = 87;
            RecieverBox.Text = "";
            // 
            // testDis
            // 
            testDis.BackColor = Color.Khaki;
            testDis.Font = new Font("Tahoma", 28.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            testDis.ForeColor = Color.SaddleBrown;
            testDis.Location = new Point(17, 532);
            testDis.Margin = new Padding(3, 4, 3, 4);
            testDis.Name = "testDis";
            testDis.Size = new Size(432, 86);
            testDis.TabIndex = 88;
            testDis.Text = "เริ่มทดสอบ";
            testDis.UseVisualStyleBackColor = false;
            testDis.Click += testDis_Click;
            // 
            // SilenceTimer
            // 
            SilenceTimer.Tick += SilenceTimer_Tick;
            // 
            // TimeoutTimer
            // 
            TimeoutTimer.Interval = 15000;
            TimeoutTimer.Tick += TimeoutTimer_Tick;
            // 
            // myRoundedPanel2
            // 
            myRoundedPanel2.BackColor = Color.FromArgb(121, 174, 111);
            myRoundedPanel2.BorderRadius = 20;
            myRoundedPanel2.Controls.Add(label6);
            myRoundedPanel2.Controls.Add(chk7Segment);
            myRoundedPanel2.Controls.Add(label4);
            myRoundedPanel2.Controls.Add(chkWL);
            myRoundedPanel2.Controls.Add(chkHP);
            myRoundedPanel2.Controls.Add(chkOut);
            myRoundedPanel2.Controls.Add(myRoundedPanel65);
            myRoundedPanel2.Controls.Add(label1);
            myRoundedPanel2.Controls.Add(myRoundedPanel1);
            myRoundedPanel2.Controls.Add(myRoundedPanel3);
            myRoundedPanel2.Controls.Add(myRoundedPanel4);
            myRoundedPanel2.Location = new Point(17, 24);
            myRoundedPanel2.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel2.Name = "myRoundedPanel2";
            myRoundedPanel2.Size = new Size(432, 500);
            myRoundedPanel2.TabIndex = 89;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Tahoma", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.Location = new Point(26, 83);
            label6.Name = "label6";
            label6.Size = new Size(229, 30);
            label6.TabIndex = 149;
            label6.Text = "LED ติด / โชว์120 ";
            // 
            // chk7Segment
            // 
            chk7Segment.AutoSize = true;
            chk7Segment.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            chk7Segment.Location = new Point(226, 128);
            chk7Segment.Name = "chk7Segment";
            chk7Segment.Size = new Size(105, 24);
            chk7Segment.TabIndex = 156;
            chk7Segment.Text = "7-Segment";
            chk7Segment.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Tahoma", 10.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.Location = new Point(251, 87);
            label4.Name = "label4";
            label4.Size = new Size(146, 22);
            label4.TabIndex = 152;
            label4.Text = "(ติ้กเฉพาะไม่ติด)";
            // 
            // chkWL
            // 
            chkWL.AutoSize = true;
            chkWL.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            chkWL.Location = new Point(163, 128);
            chkWL.Name = "chkWL";
            chkWL.Size = new Size(52, 24);
            chkWL.TabIndex = 155;
            chkWL.Text = "WL";
            chkWL.UseVisualStyleBackColor = true;
            // 
            // chkHP
            // 
            chkHP.AutoSize = true;
            chkHP.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            chkHP.Location = new Point(104, 128);
            chkHP.Name = "chkHP";
            chkHP.Size = new Size(51, 24);
            chkHP.TabIndex = 154;
            chkHP.Text = "HP";
            chkHP.UseVisualStyleBackColor = true;
            // 
            // chkOut
            // 
            chkOut.AutoSize = true;
            chkOut.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            chkOut.Location = new Point(32, 128);
            chkOut.Name = "chkOut";
            chkOut.Size = new Size(61, 24);
            chkOut.TabIndex = 153;
            chkOut.Text = "OUT";
            chkOut.UseVisualStyleBackColor = true;
            // 
            // myRoundedPanel65
            // 
            myRoundedPanel65.BackColor = Color.FromArgb(26, 61, 26);
            myRoundedPanel65.BorderRadius = 13;
            myRoundedPanel65.Controls.Add(label59);
            myRoundedPanel65.Location = new Point(15, 13);
            myRoundedPanel65.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel65.Name = "myRoundedPanel65";
            myRoundedPanel65.Size = new Size(392, 51);
            myRoundedPanel65.TabIndex = 98;
            // 
            // label59
            // 
            label59.AutoSize = true;
            label59.Font = new Font("Tahoma", 19.8000011F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label59.ForeColor = Color.White;
            label59.Location = new Point(89, 4);
            label59.Name = "label59";
            label59.Size = new Size(233, 41);
            label59.TabIndex = 92;
            label59.Text = "Test Process";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 18F);
            label1.Location = new Point(95, 297);
            label1.Name = "label1";
            label1.Size = new Size(0, 41);
            label1.TabIndex = 100;
            label1.Click += label1_Click;
            // 
            // myRoundedPanel1
            // 
            myRoundedPanel1.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel1.BorderRadius = 13;
            myRoundedPanel1.Controls.Add(label5);
            myRoundedPanel1.Controls.Add(dot0);
            myRoundedPanel1.Location = new Point(26, 169);
            myRoundedPanel1.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel1.Name = "myRoundedPanel1";
            myRoundedPanel1.Size = new Size(381, 96);
            myRoundedPanel1.TabIndex = 99;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Tahoma", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.Location = new Point(175, 34);
            label5.Name = "label5";
            label5.Size = new Size(121, 30);
            label5.TabIndex = 145;
            label5.Text = "กดปุ่ม ลด";
            // 
            // dot0
            // 
            dot0.Location = new Point(39, 15);
            dot0.Margin = new Padding(3, 4, 3, 4);
            dot0.Name = "dot0";
            dot0.Size = new Size(67, 67);
            dot0.TabIndex = 150;
            dot0.TabStop = false;
            // 
            // myRoundedPanel3
            // 
            myRoundedPanel3.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel3.BorderRadius = 13;
            myRoundedPanel3.Controls.Add(dot1);
            myRoundedPanel3.Controls.Add(label2);
            myRoundedPanel3.Location = new Point(26, 277);
            myRoundedPanel3.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel3.Name = "myRoundedPanel3";
            myRoundedPanel3.Size = new Size(381, 96);
            myRoundedPanel3.TabIndex = 151;
            // 
            // dot1
            // 
            dot1.Location = new Point(39, 16);
            dot1.Margin = new Padding(3, 4, 3, 4);
            dot1.Name = "dot1";
            dot1.Size = new Size(67, 67);
            dot1.TabIndex = 139;
            dot1.TabStop = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Tahoma", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(160, 31);
            label2.Name = "label2";
            label2.Size = new Size(154, 30);
            label2.TabIndex = 143;
            label2.Text = "กดปุ่ม F ค้าง";
            // 
            // myRoundedPanel4
            // 
            myRoundedPanel4.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel4.BorderRadius = 13;
            myRoundedPanel4.Controls.Add(dot2);
            myRoundedPanel4.Controls.Add(label3);
            myRoundedPanel4.Location = new Point(26, 386);
            myRoundedPanel4.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel4.Name = "myRoundedPanel4";
            myRoundedPanel4.Size = new Size(381, 96);
            myRoundedPanel4.TabIndex = 152;
            // 
            // dot2
            // 
            dot2.Location = new Point(40, 15);
            dot2.Margin = new Padding(3, 4, 3, 4);
            dot2.Name = "dot2";
            dot2.Size = new Size(67, 67);
            dot2.TabIndex = 138;
            dot2.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Tahoma", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(171, 33);
            label3.Name = "label3";
            label3.Size = new Size(134, 30);
            label3.TabIndex = 147;
            label3.Text = "กดปุ่ม เพิ่ม";
            // 
            // countdown
            // 
            countdown.Font = new Font("Tahoma", 24F, FontStyle.Italic, GraphicsUnit.Point, 0);
            countdown.ForeColor = SystemColors.ControlLightLight;
            countdown.Location = new Point(0, 9);
            countdown.Name = "countdown";
            countdown.Size = new Size(412, 43);
            countdown.TabIndex = 90;
            countdown.Text = "00:00";
            countdown.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // timer1
            // 
            timer1.Interval = 15000;
            // 
            // CountdownTick
            // 
            CountdownTick.Interval = 1000;
            CountdownTick.Tick += CountdownTick_Tick;
            // 
            // myRoundedPanel7
            // 
            myRoundedPanel7.BackColor = Color.FromArgb(58, 122, 58);
            myRoundedPanel7.BorderRadius = 10;
            myRoundedPanel7.Controls.Add(countdown);
            myRoundedPanel7.Location = new Point(466, 28);
            myRoundedPanel7.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel7.Name = "myRoundedPanel7";
            myRoundedPanel7.Size = new Size(412, 65);
            myRoundedPanel7.TabIndex = 91;
            // 
            // btnRefreshDisplay
            // 
            btnRefreshDisplay.Location = new Point(307, 17);
            btnRefreshDisplay.Margin = new Padding(3, 4, 3, 4);
            btnRefreshDisplay.Name = "btnRefreshDisplay";
            btnRefreshDisplay.Size = new Size(68, 31);
            btnRefreshDisplay.TabIndex = 91;
            btnRefreshDisplay.Text = "รีเฟรซ";
            btnRefreshDisplay.UseVisualStyleBackColor = true;
            btnRefreshDisplay.Click += btnRefreshDisplay_Click;
            // 
            // WriteQueueTimer
            // 
            WriteQueueTimer.Tick += WriteQueueTimer_Tick;
            // 
            // myRoundedPanel5
            // 
            myRoundedPanel5.BackColor = Color.FromArgb(26, 42, 26);
            myRoundedPanel5.BorderRadius = 15;
            myRoundedPanel5.Controls.Add(label7);
            myRoundedPanel5.Controls.Add(btnRefreshDisplay);
            myRoundedPanel5.Location = new Point(466, 111);
            myRoundedPanel5.Name = "myRoundedPanel5";
            myRoundedPanel5.Size = new Size(412, 505);
            myRoundedPanel5.TabIndex = 92;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Tahoma", 16.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.White;
            label7.Location = new Point(16, 17);
            label7.Name = "label7";
            label7.Size = new Size(178, 34);
            label7.TabIndex = 93;
            label7.Text = "System Log";
            // 
            // Form4
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(232, 240, 216);
            ClientSize = new Size(894, 632);
            Controls.Add(myRoundedPanel7);
            Controls.Add(myRoundedPanel2);
            Controls.Add(testDis);
            Controls.Add(RecieverBox);
            Controls.Add(myRoundedPanel5);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            Name = "Form4";
            Text = "Test Display ";
            Load += Form4_Load;
            myRoundedPanel2.ResumeLayout(false);
            myRoundedPanel2.PerformLayout();
            myRoundedPanel65.ResumeLayout(false);
            myRoundedPanel65.PerformLayout();
            myRoundedPanel1.ResumeLayout(false);
            myRoundedPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dot0).EndInit();
            myRoundedPanel3.ResumeLayout(false);
            myRoundedPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dot1).EndInit();
            myRoundedPanel4.ResumeLayout(false);
            myRoundedPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dot2).EndInit();
            myRoundedPanel7.ResumeLayout(false);
            myRoundedPanel5.ResumeLayout(false);
            myRoundedPanel5.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private RichTextBox RecieverBox;
        private Button testDis;
        private System.Windows.Forms.Timer SilenceTimer;
        private System.Windows.Forms.Timer TimeoutTimer;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel2;
        private PictureBox dot1;
        private PictureBox dot2;
        private Label countdown;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Timer CountdownTick;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label6;
        private PictureBox dot0;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel65;
        private Label label59;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel1;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel3;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel4;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel7;
        private Button btnRefreshDisplay;
        private System.Windows.Forms.Timer WriteQueueTimer;
        private CheckBox chk7Segment;
        private Label label4;
        private CheckBox chkWL;
        private CheckBox chkHP;
        private CheckBox chkOut;
        private Label label5;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel5;
        private Label label7;
    }
}