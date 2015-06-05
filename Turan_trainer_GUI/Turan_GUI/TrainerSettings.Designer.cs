namespace Turan_GUI
{
    partial class TrainerSettings
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.trackb_treshold = new System.Windows.Forms.TrackBar();
            this.lab_treshold = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_ok = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.combo_freq = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackb_treshold)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.combo_freq);
            this.groupBox1.Controls.Add(this.trackb_treshold);
            this.groupBox1.Controls.Add(this.lab_treshold);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(223, 191);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // trackb_treshold
            // 
            this.trackb_treshold.LargeChange = 1000;
            this.trackb_treshold.Location = new System.Drawing.Point(172, 19);
            this.trackb_treshold.Maximum = 30000;
            this.trackb_treshold.Minimum = 1000;
            this.trackb_treshold.Name = "trackb_treshold";
            this.trackb_treshold.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackb_treshold.Size = new System.Drawing.Size(45, 166);
            this.trackb_treshold.SmallChange = 100;
            this.trackb_treshold.TabIndex = 2;
            this.trackb_treshold.TickFrequency = 10000;
            this.trackb_treshold.Value = 25000;
            this.trackb_treshold.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // lab_treshold
            // 
            this.lab_treshold.AutoSize = true;
            this.lab_treshold.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lab_treshold.Location = new System.Drawing.Point(4, 107);
            this.lab_treshold.Name = "lab_treshold";
            this.lab_treshold.Size = new System.Drawing.Size(48, 17);
            this.lab_treshold.TabIndex = 1;
            this.lab_treshold.Text = "25000";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label3.Location = new System.Drawing.Point(3, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(144, 17);
            this.label3.TabIndex = 1;
            this.label3.Text = "Mintavételi frekvencia";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label2.Location = new System.Drawing.Point(3, 85);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(160, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "Bakapcsolási határérték";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(83, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Hz";
            // 
            // btn_ok
            // 
            this.btn_ok.Location = new System.Drawing.Point(73, 209);
            this.btn_ok.Name = "btn_ok";
            this.btn_ok.Size = new System.Drawing.Size(75, 23);
            this.btn_ok.TabIndex = 1;
            this.btn_ok.Text = "OK";
            this.btn_ok.UseVisualStyleBackColor = true;
            this.btn_ok.Click += new System.EventHandler(this.btn_ok_Click);
            // 
            // btn_cancel
            // 
            this.btn_cancel.Location = new System.Drawing.Point(160, 209);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(75, 23);
            this.btn_cancel.TabIndex = 1;
            this.btn_cancel.Text = "Mégse";
            this.btn_cancel.UseVisualStyleBackColor = true;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            // 
            // combo_freq
            // 
            this.combo_freq.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.combo_freq.FormattingEnabled = true;
            this.combo_freq.Items.AddRange(new object[] {
            "8000",
            "10000",
            "12000",
            "16000",
            "20000",
            "24000",
            "32000",
            "48000"});
            this.combo_freq.Location = new System.Drawing.Point(6, 50);
            this.combo_freq.Name = "combo_freq";
            this.combo_freq.Size = new System.Drawing.Size(71, 24);
            this.combo_freq.TabIndex = 3;
            this.combo_freq.Text = "12000";
            this.combo_freq.SelectedIndexChanged += new System.EventHandler(this.combo_freq_SelectedIndexChanged);
            // 
            // TrainerSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(247, 237);
            this.Controls.Add(this.btn_cancel);
            this.Controls.Add(this.btn_ok);
            this.Controls.Add(this.groupBox1);
            this.Name = "TrainerSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Beállítások";
            this.Load += new System.EventHandler(this.TrainerSettings_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackb_treshold)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TrackBar trackb_treshold;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lab_treshold;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btn_ok;
        private System.Windows.Forms.Button btn_cancel;
        private System.Windows.Forms.ComboBox combo_freq;
    }
}