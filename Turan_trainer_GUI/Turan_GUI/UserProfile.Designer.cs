namespace Turan_GUI
{
    partial class UserProfile
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserProfile));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btn_newprofile = new System.Windows.Forms.Button();
            this.tb_username = new System.Windows.Forms.TextBox();
            this.combo_users = new System.Windows.Forms.ComboBox();
            this.btn_select = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(122, 133);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // btn_newprofile
            // 
            this.btn_newprofile.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_newprofile.Location = new System.Drawing.Point(140, 37);
            this.btn_newprofile.Name = "btn_newprofile";
            this.btn_newprofile.Size = new System.Drawing.Size(178, 43);
            this.btn_newprofile.TabIndex = 1;
            this.btn_newprofile.Text = "Új felhasználó";
            this.btn_newprofile.UseVisualStyleBackColor = true;
            this.btn_newprofile.Click += new System.EventHandler(this.btn_newprofile_Click);
            // 
            // tb_username
            // 
            this.tb_username.Location = new System.Drawing.Point(140, 12);
            this.tb_username.Name = "tb_username";
            this.tb_username.Size = new System.Drawing.Size(178, 20);
            this.tb_username.TabIndex = 2;
            // 
            // combo_users
            // 
            this.combo_users.FormattingEnabled = true;
            this.combo_users.Location = new System.Drawing.Point(140, 86);
            this.combo_users.Name = "combo_users";
            this.combo_users.Size = new System.Drawing.Size(178, 21);
            this.combo_users.TabIndex = 3;
            // 
            // btn_select
            // 
            this.btn_select.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_select.Location = new System.Drawing.Point(140, 113);
            this.btn_select.Name = "btn_select";
            this.btn_select.Size = new System.Drawing.Size(178, 43);
            this.btn_select.TabIndex = 1;
            this.btn_select.Text = "Kiválaszt";
            this.btn_select.UseVisualStyleBackColor = true;
            this.btn_select.Click += new System.EventHandler(this.btn_select_Click);
            // 
            // UserProfile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(338, 175);
            this.Controls.Add(this.combo_users);
            this.Controls.Add(this.tb_username);
            this.Controls.Add(this.btn_select);
            this.Controls.Add(this.btn_newprofile);
            this.Controls.Add(this.pictureBox1);
            this.Name = "UserProfile";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Felhasználó kiválasztása";
            this.Load += new System.EventHandler(this.UserProfile_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btn_newprofile;
        private System.Windows.Forms.TextBox tb_username;
        private System.Windows.Forms.ComboBox combo_users;
        private System.Windows.Forms.Button btn_select;
    }
}