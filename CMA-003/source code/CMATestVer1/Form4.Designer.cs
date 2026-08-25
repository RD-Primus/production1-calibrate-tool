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
            myRoundedPanel65 = new CMA003AVer2.MyRoundedPanel();
            label59 = new Label();
            label1 = new Label();
            myRoundedPanel1 = new CMA003AVer2.MyRoundedPanel();
            label7 = new Label();
            label6 = new Label();
            dot0 = new PictureBox();
            myRoundedPanel3 = new CMA003AVer2.MyRoundedPanel();
            label8 = new Label();
            dot1 = new PictureBox();
            label2 = new Label();
            myRoundedPanel4 = new CMA003AVer2.MyRoundedPanel();
            label9 = new Label();
            dot2 = new PictureBox();
            label3 = new Label();
            myRoundedPanel6 = new CMA003AVer2.MyRoundedPanel();
            label10 = new Label();
            dot3 = new PictureBox();
            label4 = new Label();
            myRoundedPanel5 = new CMA003AVer2.MyRoundedPanel();
            label11 = new Label();
            dot4 = new PictureBox();
            label5 = new Label();
            countdown = new Label();
            timer1 = new System.Windows.Forms.Timer(components);
            timer2 = new System.Windows.Forms.Timer(components);
            CountdownTick = new System.Windows.Forms.Timer(components);
            myRoundedPanel7 = new CMA003AVer2.MyRoundedPanel();
            btnRefreshDisplay = new Button();
            WriteQueueTimer = new System.Windows.Forms.Timer(components);
            myRoundedPanel2.SuspendLayout();
            myRoundedPanel65.SuspendLayout();
            myRoundedPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dot0).BeginInit();
            myRoundedPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dot1).BeginInit();
            myRoundedPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dot2).BeginInit();
            myRoundedPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dot3).BeginInit();
            myRoundedPanel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dot4).BeginInit();
            myRoundedPanel7.SuspendLayout();
            SuspendLayout();
            // 
            // RecieverBox
            // 
            RecieverBox.BackColor = Color.FromArgb(26, 42, 26);
            RecieverBox.Font = new Font("Cordia New", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            RecieverBox.ForeColor = Color.White;
            RecieverBox.Location = new Point(553, 149);
            RecieverBox.Margin = new Padding(3, 4, 3, 4);
            RecieverBox.Name = "RecieverBox";
            RecieverBox.Size = new Size(390, 615);
            RecieverBox.TabIndex = 87;
            RecieverBox.Text = "";
            // 
            // testDis
            // 
            testDis.BackColor = Color.Khaki;
            testDis.Font = new Font("Arial", 27.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            testDis.ForeColor = Color.SaddleBrown;
            testDis.Location = new Point(17, 677);
            testDis.Margin = new Padding(3, 4, 3, 4);
            testDis.Name = "testDis";
            testDis.Size = new Size(518, 88);
            testDis.TabIndex = 88;
            testDis.Text = "Test";
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
            myRoundedPanel2.Controls.Add(myRoundedPanel65);
            myRoundedPanel2.Controls.Add(label1);
            myRoundedPanel2.Controls.Add(myRoundedPanel1);
            myRoundedPanel2.Controls.Add(myRoundedPanel3);
            myRoundedPanel2.Controls.Add(myRoundedPanel4);
            myRoundedPanel2.Controls.Add(myRoundedPanel6);
            myRoundedPanel2.Controls.Add(myRoundedPanel5);
            myRoundedPanel2.Location = new Point(17, 24);
            myRoundedPanel2.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel2.Name = "myRoundedPanel2";
            myRoundedPanel2.Size = new Size(518, 643);
            myRoundedPanel2.TabIndex = 89;
            // 
            // myRoundedPanel65
            // 
            myRoundedPanel65.BackColor = Color.FromArgb(26, 61, 26);
            myRoundedPanel65.BorderRadius = 13;
            myRoundedPanel65.Controls.Add(label59);
            myRoundedPanel65.Location = new Point(25, 15);
            myRoundedPanel65.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel65.Name = "myRoundedPanel65";
            myRoundedPanel65.Size = new Size(468, 51);
            myRoundedPanel65.TabIndex = 98;
            // 
            // label59
            // 
            label59.AutoSize = true;
            label59.Font = new Font("Arial Black", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label59.ForeColor = Color.White;
            label59.Location = new Point(118, 3);
            label59.Name = "label59";
            label59.Size = new Size(265, 48);
            label59.TabIndex = 92;
            label59.Text = "Test Process";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 18F);
            label1.Location = new Point(94, 220);
            label1.Name = "label1";
            label1.Size = new Size(0, 41);
            label1.TabIndex = 100;
            label1.Click += label1_Click;
            // 
            // myRoundedPanel1
            // 
            myRoundedPanel1.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel1.BorderRadius = 13;
            myRoundedPanel1.Controls.Add(label7);
            myRoundedPanel1.Controls.Add(label6);
            myRoundedPanel1.Controls.Add(dot0);
            myRoundedPanel1.Location = new Point(25, 92);
            myRoundedPanel1.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel1.Name = "myRoundedPanel1";
            myRoundedPanel1.Size = new Size(468, 96);
            myRoundedPanel1.TabIndex = 99;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Tahoma", 13.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label7.Location = new Point(118, 54);
            label7.Name = "label7";
            label7.Size = new Size(280, 28);
            label7.TabIndex = 151;
            label7.Text = " => กดปุ่ม F ค้างไว้ 5 วินาที";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Tahoma", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.Location = new Point(125, 15);
            label6.Name = "label6";
            label6.Size = new Size(276, 30);
            label6.TabIndex = 149;
            label6.Text = "LED ติดครบ / โชว์120 ";
            // 
            // dot0
            // 
            dot0.Location = new Point(22, 15);
            dot0.Margin = new Padding(3, 4, 3, 4);
            dot0.Name = "dot0";
            dot0.Size = new Size(57, 67);
            dot0.TabIndex = 150;
            dot0.TabStop = false;
            // 
            // myRoundedPanel3
            // 
            myRoundedPanel3.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel3.BorderRadius = 13;
            myRoundedPanel3.Controls.Add(label8);
            myRoundedPanel3.Controls.Add(dot1);
            myRoundedPanel3.Controls.Add(label2);
            myRoundedPanel3.Location = new Point(25, 200);
            myRoundedPanel3.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel3.Name = "myRoundedPanel3";
            myRoundedPanel3.Size = new Size(468, 96);
            myRoundedPanel3.TabIndex = 151;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Tahoma", 13.8F);
            label8.Location = new Point(177, 56);
            label8.Name = "label8";
            label8.Size = new Size(137, 28);
            label8.TabIndex = 144;
            label8.Text = "=> กดปุ่มลด";
            // 
            // dot1
            // 
            dot1.Location = new Point(22, 20);
            dot1.Margin = new Padding(3, 4, 3, 4);
            dot1.Name = "dot1";
            dot1.Size = new Size(57, 67);
            dot1.TabIndex = 139;
            dot1.TabStop = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Tahoma", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(188, 16);
            label2.Name = "label2";
            label2.Size = new Size(125, 30);
            label2.TabIndex = 143;
            label2.Text = "เจอ I n P ";
            // 
            // myRoundedPanel4
            // 
            myRoundedPanel4.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel4.BorderRadius = 13;
            myRoundedPanel4.Controls.Add(label9);
            myRoundedPanel4.Controls.Add(dot2);
            myRoundedPanel4.Controls.Add(label3);
            myRoundedPanel4.Location = new Point(25, 309);
            myRoundedPanel4.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel4.Name = "myRoundedPanel4";
            myRoundedPanel4.Size = new Size(468, 96);
            myRoundedPanel4.TabIndex = 152;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Tahoma", 13.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label9.Location = new Point(177, 54);
            label9.Name = "label9";
            label9.Size = new Size(128, 28);
            label9.TabIndex = 148;
            label9.Text = "=> กดปุ่ม F";
            // 
            // dot2
            // 
            dot2.Location = new Point(22, 15);
            dot2.Margin = new Padding(3, 4, 3, 4);
            dot2.Name = "dot2";
            dot2.Size = new Size(57, 67);
            dot2.TabIndex = 138;
            dot2.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Tahoma", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(200, 15);
            label3.Name = "label3";
            label3.Size = new Size(92, 30);
            label3.TabIndex = 147;
            label3.Text = "เจอ 07";
            // 
            // myRoundedPanel6
            // 
            myRoundedPanel6.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel6.BorderRadius = 13;
            myRoundedPanel6.Controls.Add(label10);
            myRoundedPanel6.Controls.Add(dot3);
            myRoundedPanel6.Controls.Add(label4);
            myRoundedPanel6.Location = new Point(25, 420);
            myRoundedPanel6.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel6.Name = "myRoundedPanel6";
            myRoundedPanel6.Size = new Size(468, 96);
            myRoundedPanel6.TabIndex = 154;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Tahoma", 13.8F);
            label10.Location = new Point(177, 52);
            label10.Name = "label10";
            label10.Size = new Size(147, 28);
            label10.TabIndex = 146;
            label10.Text = "=> กดปุ่มเพิ่ม";
            // 
            // dot3
            // 
            dot3.Location = new Point(22, 19);
            dot3.Margin = new Padding(3, 4, 3, 4);
            dot3.Name = "dot3";
            dot3.Size = new Size(57, 67);
            dot3.TabIndex = 140;
            dot3.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Tahoma", 15F, FontStyle.Bold);
            label4.Location = new Point(196, 16);
            label4.Name = "label4";
            label4.Size = new Size(117, 30);
            label4.TabIndex = 145;
            label4.Text = "เจอ PUS ";
            // 
            // myRoundedPanel5
            // 
            myRoundedPanel5.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel5.BorderRadius = 13;
            myRoundedPanel5.Controls.Add(label11);
            myRoundedPanel5.Controls.Add(dot4);
            myRoundedPanel5.Controls.Add(label5);
            myRoundedPanel5.Location = new Point(25, 529);
            myRoundedPanel5.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel5.Name = "myRoundedPanel5";
            myRoundedPanel5.Size = new Size(468, 96);
            myRoundedPanel5.TabIndex = 153;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Tahoma", 13.8F);
            label11.Location = new Point(177, 54);
            label11.Name = "label11";
            label11.Size = new Size(128, 28);
            label11.TabIndex = 149;
            label11.Text = "=> กดปุ่ม F";
            // 
            // dot4
            // 
            dot4.Location = new Point(22, 16);
            dot4.Margin = new Padding(3, 4, 3, 4);
            dot4.Name = "dot4";
            dot4.Size = new Size(57, 67);
            dot4.TabIndex = 141;
            dot4.TabStop = false;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Tahoma", 15F, FontStyle.Bold);
            label5.Location = new Point(211, 16);
            label5.Name = "label5";
            label5.Size = new Size(76, 30);
            label5.TabIndex = 148;
            label5.Text = "เจอ0 ";
            // 
            // countdown
            // 
            countdown.Font = new Font("Tahoma", 24F, FontStyle.Italic, GraphicsUnit.Point, 0);
            countdown.ForeColor = SystemColors.ControlLightLight;
            countdown.Location = new Point(0, 9);
            countdown.Name = "countdown";
            countdown.Size = new Size(391, 43);
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
            myRoundedPanel7.Location = new Point(553, 28);
            myRoundedPanel7.Margin = new Padding(3, 4, 3, 4);
            myRoundedPanel7.Name = "myRoundedPanel7";
            myRoundedPanel7.Size = new Size(391, 65);
            myRoundedPanel7.TabIndex = 91;
            // 
            // btnRefreshDisplay
            // 
            btnRefreshDisplay.Location = new Point(884, 111);
            btnRefreshDisplay.Margin = new Padding(3, 4, 3, 4);
            btnRefreshDisplay.Name = "btnRefreshDisplay";
            btnRefreshDisplay.Size = new Size(59, 31);
            btnRefreshDisplay.TabIndex = 91;
            btnRefreshDisplay.Text = "รีเฟรซ";
            btnRefreshDisplay.UseVisualStyleBackColor = true;
            btnRefreshDisplay.Click += btnRefreshDisplay_Click;
            // 
            // WriteQueueTimer
            // 
            WriteQueueTimer.Tick += WriteQueueTimer_Tick;
            // 
            // Form4
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(232, 240, 216);
            ClientSize = new Size(956, 777);
            Controls.Add(btnRefreshDisplay);
            Controls.Add(myRoundedPanel7);
            Controls.Add(myRoundedPanel2);
            Controls.Add(testDis);
            Controls.Add(RecieverBox);
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
            myRoundedPanel6.ResumeLayout(false);
            myRoundedPanel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dot3).EndInit();
            myRoundedPanel5.ResumeLayout(false);
            myRoundedPanel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dot4).EndInit();
            myRoundedPanel7.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private RichTextBox RecieverBox;
        private Button testDis;
        private System.Windows.Forms.Timer SilenceTimer;
        private System.Windows.Forms.Timer TimeoutTimer;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel2;
        private PictureBox dot4;
        private PictureBox dot3;
        private PictureBox dot1;
        private PictureBox dot2;
        private Label countdown;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Timer CountdownTick;
        private Label label1;
        private Label label2;
        private Label label5;
        private Label label3;
        private Label label4;
        private Label label6;
        private PictureBox dot0;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel65;
        private Label label59;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel1;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel3;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel4;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel6;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel5;
        private CMA003AVer2.MyRoundedPanel myRoundedPanel7;
        private Button btnRefreshDisplay;
        private Label label7;
        private Label label8;
        private Label label9;
        private Label label10;
        private Label label11;
        private System.Windows.Forms.Timer WriteQueueTimer;
    }
}