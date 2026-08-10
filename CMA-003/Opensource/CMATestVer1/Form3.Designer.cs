namespace CMATestVer1
{
    partial class Form3
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form3));
            myRoundedPanel1 = new CMA003AVer2.MyRoundedPanel();
            pictureBox2 = new PictureBox();
            label1 = new Label();
            txtUser = new TextBox();
            txtPass = new TextBox();
            label48 = new Label();
            label2 = new Label();
            btnSubmit = new Button();
            lblStatus = new Label();
            NewUser = new Label();
            tabControl2 = new TabControl();
            tabPage3 = new TabPage();
            showpass = new PictureBox();
            tabPage4 = new TabPage();
            btnBack = new Button();
            btnSignUp = new Button();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            txtPassword = new TextBox();
            TxtEmail = new TextBox();
            label3 = new Label();
            txtName = new TextBox();
            pictureBox1 = new PictureBox();
            myRoundedPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            tabControl2.SuspendLayout();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)showpass).BeginInit();
            tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // myRoundedPanel1
            // 
            myRoundedPanel1.BackColor = Color.FromArgb(52, 103, 57);
            myRoundedPanel1.BorderRadius = 20;
            myRoundedPanel1.Controls.Add(pictureBox2);
            myRoundedPanel1.Controls.Add(label1);
            myRoundedPanel1.Location = new Point(24, 17);
            myRoundedPanel1.Name = "myRoundedPanel1";
            myRoundedPanel1.Size = new Size(457, 65);
            myRoundedPanel1.TabIndex = 0;
            // 
            // pictureBox2
            // 
            pictureBox2.BackColor = Color.FromArgb(121, 174, 111);
            pictureBox2.Image = Properties.Resources.primus;
            pictureBox2.Location = new Point(25, 0);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(62, 65);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 144;
            pictureBox2.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Arial Black", 26.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.White;
            label1.Location = new Point(169, 7);
            label1.Name = "label1";
            label1.Size = new Size(145, 50);
            label1.TabIndex = 86;
            label1.Text = "LOGIN";
            // 
            // txtUser
            // 
            txtUser.Anchor = AnchorStyles.Left;
            txtUser.Font = new Font("Arial", 18F);
            txtUser.Location = new Point(47, 126);
            txtUser.Name = "txtUser";
            txtUser.Size = new Size(368, 35);
            txtUser.TabIndex = 2;
            // 
            // txtPass
            // 
            txtPass.Font = new Font("Arial", 18F);
            txtPass.Location = new Point(47, 202);
            txtPass.Name = "txtPass";
            txtPass.PasswordChar = '*';
            txtPass.Size = new Size(368, 35);
            txtPass.TabIndex = 3;
            txtPass.KeyDown += txtPass_KeyDown_1;
            // 
            // label48
            // 
            label48.AutoSize = true;
            label48.BackColor = Color.FromArgb(232, 240, 216);
            label48.Font = new Font("Arial", 15.75F, FontStyle.Bold);
            label48.ForeColor = Color.FromArgb(65, 67, 27);
            label48.Location = new Point(47, 98);
            label48.Name = "label48";
            label48.Size = new Size(63, 24);
            label48.TabIndex = 137;
            label48.Text = "email";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.FromArgb(232, 240, 216);
            label2.Font = new Font("Arial", 15.75F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(65, 67, 27);
            label2.Location = new Point(47, 177);
            label2.Name = "label2";
            label2.Size = new Size(109, 24);
            label2.TabIndex = 138;
            label2.Text = "Password";
            // 
            // btnSubmit
            // 
            btnSubmit.Cursor = Cursors.Hand;
            btnSubmit.Font = new Font("Arial", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSubmit.Location = new Point(112, 293);
            btnSubmit.Name = "btnSubmit";
            btnSubmit.Size = new Size(256, 53);
            btnSubmit.TabIndex = 139;
            btnSubmit.Text = "Submit";
            btnSubmit.UseVisualStyleBackColor = true;
            btnSubmit.Click += btnSubmit_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(47, 261);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(90, 17);
            lblStatus.TabIndex = 140;
            lblStatus.Text = "กรุณาล็อคอิน.....";
            lblStatus.TextAlign = ContentAlignment.MiddleRight;
            // 
            // NewUser
            // 
            NewUser.AutoSize = true;
            NewUser.Cursor = Cursors.Hand;
            NewUser.Font = new Font("Segoe UI", 12F, FontStyle.Underline, GraphicsUnit.Point, 0);
            NewUser.Location = new Point(403, 350);
            NewUser.Name = "NewUser";
            NewUser.Size = new Size(78, 21);
            NewUser.TabIndex = 141;
            NewUser.Text = "New User";
            NewUser.TextAlign = ContentAlignment.MiddleRight;
            NewUser.Click += NewUser_Click;
            // 
            // tabControl2
            // 
            tabControl2.Controls.Add(tabPage3);
            tabControl2.Controls.Add(tabPage4);
            tabControl2.ItemSize = new Size(0, 1);
            tabControl2.Location = new Point(12, 6);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(507, 396);
            tabControl2.SizeMode = TabSizeMode.Fixed;
            tabControl2.TabIndex = 143;
            // 
            // tabPage3
            // 
            tabPage3.BackColor = Color.FromArgb(232, 240, 216);
            tabPage3.Controls.Add(showpass);
            tabPage3.Controls.Add(myRoundedPanel1);
            tabPage3.Controls.Add(NewUser);
            tabPage3.Controls.Add(label48);
            tabPage3.Controls.Add(lblStatus);
            tabPage3.Controls.Add(txtUser);
            tabPage3.Controls.Add(btnSubmit);
            tabPage3.Controls.Add(txtPass);
            tabPage3.Controls.Add(label2);
            tabPage3.Location = new Point(4, 5);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(499, 387);
            tabPage3.TabIndex = 0;
            tabPage3.Text = "tabPage3";
            // 
            // showpass
            // 
            showpass.BackColor = SystemColors.Window;
            showpass.BackgroundImageLayout = ImageLayout.Stretch;
            showpass.Cursor = Cursors.Hand;
            showpass.Image = Properties.Resources.eye_open;
            showpass.Location = new Point(381, 205);
            showpass.Name = "showpass";
            showpass.Size = new Size(30, 30);
            showpass.SizeMode = PictureBoxSizeMode.StretchImage;
            showpass.TabIndex = 143;
            showpass.TabStop = false;
            showpass.Click += showpass_Click;
            // 
            // tabPage4
            // 
            tabPage4.BackColor = Color.FromArgb(232, 240, 216);
            tabPage4.Controls.Add(btnBack);
            tabPage4.Controls.Add(btnSignUp);
            tabPage4.Controls.Add(label6);
            tabPage4.Controls.Add(label5);
            tabPage4.Controls.Add(label4);
            tabPage4.Controls.Add(txtPassword);
            tabPage4.Controls.Add(TxtEmail);
            tabPage4.Controls.Add(label3);
            tabPage4.Controls.Add(txtName);
            tabPage4.Controls.Add(pictureBox1);
            tabPage4.Location = new Point(4, 5);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(499, 387);
            tabPage4.TabIndex = 1;
            tabPage4.Text = "tabPage4";
            // 
            // btnBack
            // 
            btnBack.Cursor = Cursors.Hand;
            btnBack.Font = new Font("Arial", 15.75F, FontStyle.Bold);
            btnBack.Location = new Point(173, 325);
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(95, 34);
            btnBack.TabIndex = 9;
            btnBack.Text = "Back";
            btnBack.UseVisualStyleBackColor = true;
            btnBack.Click += btnBack_Click;
            // 
            // btnSignUp
            // 
            btnSignUp.Cursor = Cursors.Hand;
            btnSignUp.Font = new Font("Arial", 15.75F, FontStyle.Bold);
            btnSignUp.Location = new Point(274, 325);
            btnSignUp.Name = "btnSignUp";
            btnSignUp.Size = new Size(145, 34);
            btnSignUp.TabIndex = 8;
            btnSignUp.Text = "Sign Up";
            btnSignUp.UseVisualStyleBackColor = true;
            btnSignUp.Click += btnSignUp_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Arial", 12F);
            label6.Location = new Point(59, 277);
            label6.Name = "label6";
            label6.Size = new Size(82, 18);
            label6.TabIndex = 7;
            label6.Text = "Password:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Arial", 12F);
            label5.Location = new Point(86, 221);
            label5.Name = "label5";
            label5.Size = new Size(52, 18);
            label5.TabIndex = 6;
            label5.Text = "Email:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Arial", 12F);
            label4.Location = new Point(83, 164);
            label4.Name = "label4";
            label4.Size = new Size(54, 18);
            label4.TabIndex = 5;
            label4.Text = "Name:";
            // 
            // txtPassword
            // 
            txtPassword.Font = new Font("STXinwei", 14.2499981F, FontStyle.Regular, GraphicsUnit.Point, 134);
            txtPassword.Location = new Point(154, 271);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(265, 27);
            txtPassword.TabIndex = 4;
            // 
            // TxtEmail
            // 
            TxtEmail.Font = new Font("STXinwei", 14.2499981F, FontStyle.Regular, GraphicsUnit.Point, 134);
            TxtEmail.Location = new Point(154, 214);
            TxtEmail.Name = "TxtEmail";
            TxtEmail.Size = new Size(265, 27);
            TxtEmail.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Arial Black", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(86, 103);
            label3.Name = "label3";
            label3.Size = new Size(337, 33);
            label3.TabIndex = 2;
            label3.Text = "-- Create New Account --";
            // 
            // txtName
            // 
            txtName.Font = new Font("STXinwei", 14.2499981F, FontStyle.Regular, GraphicsUnit.Point, 134);
            txtName.Location = new Point(154, 156);
            txtName.Name = "txtName";
            txtName.Size = new Size(265, 27);
            txtName.TabIndex = 1;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.NewUser;
            pictureBox1.Location = new Point(203, 14);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(96, 86);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // Form3
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(121, 174, 111);
            ClientSize = new Size(531, 410);
            Controls.Add(tabControl2);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form3";
            Text = "Login Primus";
            myRoundedPanel1.ResumeLayout(false);
            myRoundedPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            tabControl2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)showpass).EndInit();
            tabPage4.ResumeLayout(false);
            tabPage4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private CMA003AVer2.MyRoundedPanel myRoundedPanel1;
        private Label label1;
        private TextBox txtUser;
        private TextBox txtPass;
        private Label label48;
        private Label label2;
        private Button btnSubmit;
        private Label lblStatus;
        private Label NewUser;
        private TabControl tabControl2;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private Label label3;
        private TextBox txtName;
        private PictureBox pictureBox1;
        private Button btnSignUp;
        private Label label6;
        private Label label5;
        private Label label4;
        private TextBox txtPassword;
        private TextBox TxtEmail;
        private Button btnBack;
        private PictureBox pictureBox2;
        private Button btnShowPass;
        private PictureBox showpass;
    }
}