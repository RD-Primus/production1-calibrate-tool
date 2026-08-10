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
            RecieverBox = new RichTextBox();
            testDis = new Button();
            SilenceTimer = new System.Windows.Forms.Timer(components);
            TimeoutTimer = new System.Windows.Forms.Timer(components);
            myRoundedPanel2 = new CMA003AVer2.MyRoundedPanel();
            myRoundedPanel65 = new CMA003AVer2.MyRoundedPanel();
            label59 = new Label();
            label1 = new Label();
            myRoundedPanel1 = new CMA003AVer2.MyRoundedPanel();
            label6 = new Label();
            dot0 = new PictureBox();
            myRoundedPanel3 = new CMA003AVer2.MyRoundedPanel();
            dot1 = new PictureBox();
            label2 = new Label();
            myRoundedPanel4 = new CMA003AVer2.MyRoundedPanel();
            dot2 = new PictureBox();
            label3 = new Label();
            myRoundedPanel6 = new CMA003AVer2.MyRoundedPanel();
            dot3 = new PictureBox();
            label4 = new Label();
            myRoundedPanel5 = new CMA003AVer2.MyRoundedPanel();
            dot4 = new PictureBox();
            label5 = new Label();
            countdown = new Label();
            timer1 = new System.Windows.Forms.Timer(components);
            timer2 = new System.Windows.Forms.Timer(components);
            CountdownTick = new System.Windows.Forms.Timer(components);
            myRoundedPanel7 = new CMA003AVer2.MyRoundedPanel();
            btnRefreshDisplay = new Button();
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
            RecieverBox.Location = new Point(623, 87);
            RecieverBox.Name = "RecieverBox";
            RecieverBox.Size = new Size(342, 484);
            RecieverBox.TabIndex = 87;
            RecieverBox.Text = "";
            // 
            // testDis
            // 
            testDis.BackColor = Color.Khaki;
            testDis.Font = new Font("Arial", 27.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            testDis.ForeColor = Color.SaddleBrown;
            testDis.Location = new Point(15, 508);
            testDis.Name = "testDis";
            testDis.Size = new Size(589, 66);
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
            myRoundedPanel2.Location = new Point(15, 18);
            myRoundedPanel2.Name = "myRoundedPanel2";
            myRoundedPanel2.Size = new Size(589, 482);
            myRoundedPanel2.TabIndex = 89;
            // 
            // myRoundedPanel65
            // 
            myRoundedPanel65.BackColor = Color.FromArgb(26, 61, 26);
            myRoundedPanel65.BorderRadius = 13;
            myRoundedPanel65.Controls.Add(label59);
            myRoundedPanel65.Location = new Point(22, 11);
            myRoundedPanel65.Name = "myRoundedPanel65";
            myRoundedPanel65.Size = new Size(545, 38);
            myRoundedPanel65.TabIndex = 98;
            // 
            // label59
            // 
            label59.AutoSize = true;
            label59.Font = new Font("Arial Black", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label59.ForeColor = Color.White;
            label59.Location = new Point(178, 0);
            label59.Name = "label59";
            label59.Size = new Size(211, 38);
            label59.TabIndex = 92;
            label59.Text = "Test Process";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 18F);
            label1.Location = new Point(82, 165);
            label1.Name = "label1";
            label1.Size = new Size(0, 32);
            label1.TabIndex = 100;
            label1.Click += label1_Click;
            // 
            // myRoundedPanel1
            // 
            myRoundedPanel1.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel1.BorderRadius = 13;
            myRoundedPanel1.Controls.Add(label6);
            myRoundedPanel1.Controls.Add(dot0);
            myRoundedPanel1.Location = new Point(22, 69);
            myRoundedPanel1.Name = "myRoundedPanel1";
            myRoundedPanel1.Size = new Size(545, 72);
            myRoundedPanel1.TabIndex = 99;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Cordia New", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label6.Location = new Point(77, 22);
            label6.Name = "label6";
            label6.Size = new Size(417, 33);
            label6.TabIndex = 149;
            label6.Text = "LED ติดครบ / โชว์120  => กดปุ่ม F ค้างไว้ 5 วินาที";
            // 
            // dot0
            // 
            dot0.Location = new Point(19, 11);
            dot0.Name = "dot0";
            dot0.Size = new Size(50, 50);
            dot0.TabIndex = 150;
            dot0.TabStop = false;
            // 
            // myRoundedPanel3
            // 
            myRoundedPanel3.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel3.BorderRadius = 13;
            myRoundedPanel3.Controls.Add(dot1);
            myRoundedPanel3.Controls.Add(label2);
            myRoundedPanel3.Location = new Point(22, 150);
            myRoundedPanel3.Name = "myRoundedPanel3";
            myRoundedPanel3.Size = new Size(545, 72);
            myRoundedPanel3.TabIndex = 151;
            // 
            // dot1
            // 
            dot1.Location = new Point(19, 15);
            dot1.Name = "dot1";
            dot1.Size = new Size(50, 50);
            dot1.TabIndex = 139;
            dot1.TabStop = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Cordia New", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(79, 24);
            label2.Name = "label2";
            label2.Size = new Size(196, 33);
            label2.TabIndex = 143;
            label2.Text = "เจอ I n P => กดปุ่มลด";
            // 
            // myRoundedPanel4
            // 
            myRoundedPanel4.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel4.BorderRadius = 13;
            myRoundedPanel4.Controls.Add(dot2);
            myRoundedPanel4.Controls.Add(label3);
            myRoundedPanel4.Location = new Point(22, 232);
            myRoundedPanel4.Name = "myRoundedPanel4";
            myRoundedPanel4.Size = new Size(545, 72);
            myRoundedPanel4.TabIndex = 152;
            // 
            // dot2
            // 
            dot2.Location = new Point(19, 11);
            dot2.Name = "dot2";
            dot2.Size = new Size(50, 50);
            dot2.TabIndex = 138;
            dot2.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Cordia New", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(78, 22);
            label3.Name = "label3";
            label3.Size = new Size(170, 33);
            label3.TabIndex = 147;
            label3.Text = "เจอ 07 => กดปุ่ม F";
            // 
            // myRoundedPanel6
            // 
            myRoundedPanel6.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel6.BorderRadius = 13;
            myRoundedPanel6.Controls.Add(dot3);
            myRoundedPanel6.Controls.Add(label4);
            myRoundedPanel6.Location = new Point(22, 315);
            myRoundedPanel6.Name = "myRoundedPanel6";
            myRoundedPanel6.Size = new Size(545, 72);
            myRoundedPanel6.TabIndex = 154;
            // 
            // dot3
            // 
            dot3.Location = new Point(19, 14);
            dot3.Name = "dot3";
            dot3.Size = new Size(50, 50);
            dot3.TabIndex = 140;
            dot3.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Cordia New", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.Location = new Point(78, 24);
            label4.Name = "label4";
            label4.Size = new Size(204, 33);
            label4.TabIndex = 145;
            label4.Text = "เจอ PUS => กดปุ่มเพิ่ม";
            // 
            // myRoundedPanel5
            // 
            myRoundedPanel5.BackColor = Color.FromArgb(224, 240, 224);
            myRoundedPanel5.BorderRadius = 13;
            myRoundedPanel5.Controls.Add(dot4);
            myRoundedPanel5.Controls.Add(label5);
            myRoundedPanel5.Location = new Point(22, 397);
            myRoundedPanel5.Name = "myRoundedPanel5";
            myRoundedPanel5.Size = new Size(545, 72);
            myRoundedPanel5.TabIndex = 153;
            // 
            // dot4
            // 
            dot4.Location = new Point(19, 12);
            dot4.Name = "dot4";
            dot4.Size = new Size(50, 50);
            dot4.TabIndex = 141;
            dot4.TabStop = false;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Cordia New", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.Location = new Point(79, 24);
            label5.Name = "label5";
            label5.Size = new Size(154, 33);
            label5.TabIndex = 148;
            label5.Text = "เจอ0 => กดปุ่ม F";
            // 
            // countdown
            // 
            countdown.Font = new Font("Cordia New", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            countdown.Location = new Point(0, 7);
            countdown.Name = "countdown";
            countdown.Size = new Size(342, 32);
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
            myRoundedPanel7.Controls.Add(btnRefreshDisplay);
            myRoundedPanel7.Controls.Add(countdown);
            myRoundedPanel7.Location = new Point(623, 18);
            myRoundedPanel7.Name = "myRoundedPanel7";
            myRoundedPanel7.Size = new Size(342, 49);
            myRoundedPanel7.TabIndex = 91;
            // 
            // btnRefreshDisplay
            // 
            btnRefreshDisplay.Location = new Point(277, 14);
            btnRefreshDisplay.Name = "btnRefreshDisplay";
            btnRefreshDisplay.Size = new Size(52, 23);
            btnRefreshDisplay.TabIndex = 91;
            btnRefreshDisplay.Text = "รีเฟรซ";
            btnRefreshDisplay.UseVisualStyleBackColor = true;
            btnRefreshDisplay.Click += btnRefreshDisplay_Click;
            // 
            // Form4
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(232, 240, 216);
            ClientSize = new Size(981, 583);
            Controls.Add(myRoundedPanel7);
            Controls.Add(myRoundedPanel2);
            Controls.Add(testDis);
            Controls.Add(RecieverBox);
            Name = "Form4";
            Text = "Test Process";
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
    }
}