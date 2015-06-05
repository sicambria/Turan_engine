namespace Turan_tester
{
    partial class Form_tester
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
            this.listBox_files = new System.Windows.Forms.ListBox();
            this.listBox_score = new System.Windows.Forms.ListBox();
            this.btn_remove_file = new System.Windows.Forms.Button();
            this.btn_create_new_sample = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btn_rec_on_off = new System.Windows.Forms.Button();
            this.btn_exit = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.listBox_active = new System.Windows.Forms.ListBox();
            this.btn_add_to_active = new System.Windows.Forms.Button();
            this.btn_remove_from_active = new System.Windows.Forms.Button();
            this.btn_analyze_active = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.btn_add_all_to_active = new System.Windows.Forms.Button();
            this.btn_remove_all_from_active = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox_files
            // 
            this.listBox_files.FormattingEnabled = true;
            this.listBox_files.Location = new System.Drawing.Point(12, 12);
            this.listBox_files.Name = "listBox_files";
            this.listBox_files.Size = new System.Drawing.Size(166, 277);
            this.listBox_files.TabIndex = 0;
            this.listBox_files.DoubleClick += new System.EventHandler(this.listBox_files_DoubleClick);
            // 
            // listBox_score
            // 
            this.listBox_score.FormattingEnabled = true;
            this.listBox_score.Location = new System.Drawing.Point(384, 12);
            this.listBox_score.Name = "listBox_score";
            this.listBox_score.Size = new System.Drawing.Size(86, 277);
            this.listBox_score.TabIndex = 0;
            // 
            // btn_remove_file
            // 
            this.btn_remove_file.Location = new System.Drawing.Point(88, 295);
            this.btn_remove_file.Name = "btn_remove_file";
            this.btn_remove_file.Size = new System.Drawing.Size(90, 23);
            this.btn_remove_file.TabIndex = 1;
            this.btn_remove_file.Text = "Remove";
            this.btn_remove_file.UseVisualStyleBackColor = true;
            this.btn_remove_file.Click += new System.EventHandler(this.btn_remove_file_Click);
            // 
            // btn_create_new_sample
            // 
            this.btn_create_new_sample.Enabled = false;
            this.btn_create_new_sample.Location = new System.Drawing.Point(12, 363);
            this.btn_create_new_sample.Name = "btn_create_new_sample";
            this.btn_create_new_sample.Size = new System.Drawing.Size(166, 23);
            this.btn_create_new_sample.TabIndex = 2;
            this.btn_create_new_sample.Text = "Create new sample";
            this.btn_create_new_sample.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(12, 337);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(129, 20);
            this.textBox1.TabIndex = 3;
            // 
            // btn_rec_on_off
            // 
            this.btn_rec_on_off.Location = new System.Drawing.Point(334, 295);
            this.btn_rec_on_off.Name = "btn_rec_on_off";
            this.btn_rec_on_off.Size = new System.Drawing.Size(136, 45);
            this.btn_rec_on_off.TabIndex = 2;
            this.btn_rec_on_off.Text = "VOICE ACTIVATED RECORDING ON";
            this.btn_rec_on_off.UseVisualStyleBackColor = true;
            this.btn_rec_on_off.Click += new System.EventHandler(this.button4_Click);
            // 
            // btn_exit
            // 
            this.btn_exit.Location = new System.Drawing.Point(370, 398);
            this.btn_exit.Name = "btn_exit";
            this.btn_exit.Size = new System.Drawing.Size(100, 23);
            this.btn_exit.TabIndex = 2;
            this.btn_exit.Text = "Exit";
            this.btn_exit.UseVisualStyleBackColor = true;
            this.btn_exit.Click += new System.EventHandler(this.button5_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(146, 339);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = ".wav";
            // 
            // listBox_active
            // 
            this.listBox_active.FormattingEnabled = true;
            this.listBox_active.Location = new System.Drawing.Point(212, 12);
            this.listBox_active.Name = "listBox_active";
            this.listBox_active.Size = new System.Drawing.Size(166, 277);
            this.listBox_active.TabIndex = 0;
            this.listBox_active.DoubleClick += new System.EventHandler(this.listBox_active_DoubleClick);
            // 
            // btn_add_to_active
            // 
            this.btn_add_to_active.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_add_to_active.Location = new System.Drawing.Point(184, 93);
            this.btn_add_to_active.Name = "btn_add_to_active";
            this.btn_add_to_active.Size = new System.Drawing.Size(22, 46);
            this.btn_add_to_active.TabIndex = 5;
            this.btn_add_to_active.Text = ">";
            this.btn_add_to_active.UseVisualStyleBackColor = true;
            this.btn_add_to_active.Click += new System.EventHandler(this.btn_add_to_active_Click);
            // 
            // btn_remove_from_active
            // 
            this.btn_remove_from_active.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_remove_from_active.Location = new System.Drawing.Point(184, 145);
            this.btn_remove_from_active.Name = "btn_remove_from_active";
            this.btn_remove_from_active.Size = new System.Drawing.Size(22, 46);
            this.btn_remove_from_active.TabIndex = 5;
            this.btn_remove_from_active.Text = "<";
            this.btn_remove_from_active.UseVisualStyleBackColor = true;
            this.btn_remove_from_active.Click += new System.EventHandler(this.btn_remove_from_active_Click);
            // 
            // btn_analyze_active
            // 
            this.btn_analyze_active.Location = new System.Drawing.Point(12, 392);
            this.btn_analyze_active.Name = "btn_analyze_active";
            this.btn_analyze_active.Size = new System.Drawing.Size(166, 29);
            this.btn_analyze_active.TabIndex = 6;
            this.btn_analyze_active.Text = "Analyze ALL files";
            this.btn_analyze_active.UseVisualStyleBackColor = true;
            this.btn_analyze_active.Click += new System.EventHandler(this.btn_analyze_active_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 444);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(485, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(33, 17);
            this.toolStripStatusLabel1.Text = "Kész.";
            // 
            // btn_add_all_to_active
            // 
            this.btn_add_all_to_active.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_add_all_to_active.Location = new System.Drawing.Point(184, 41);
            this.btn_add_all_to_active.Name = "btn_add_all_to_active";
            this.btn_add_all_to_active.Size = new System.Drawing.Size(22, 46);
            this.btn_add_all_to_active.TabIndex = 5;
            this.btn_add_all_to_active.Text = ">>";
            this.btn_add_all_to_active.UseVisualStyleBackColor = true;
            this.btn_add_all_to_active.Click += new System.EventHandler(this.btn_add_all_to_active_Click);
            // 
            // btn_remove_all_from_active
            // 
            this.btn_remove_all_from_active.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_remove_all_from_active.Location = new System.Drawing.Point(184, 197);
            this.btn_remove_all_from_active.Name = "btn_remove_all_from_active";
            this.btn_remove_all_from_active.Size = new System.Drawing.Size(22, 46);
            this.btn_remove_all_from_active.TabIndex = 5;
            this.btn_remove_all_from_active.Text = "<<";
            this.btn_remove_all_from_active.UseVisualStyleBackColor = true;
            this.btn_remove_all_from_active.Click += new System.EventHandler(this.btn_remove_all_from_active_Click);
            // 
            // Form_tester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(485, 466);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btn_analyze_active);
            this.Controls.Add(this.btn_remove_from_active);
            this.Controls.Add(this.btn_remove_all_from_active);
            this.Controls.Add(this.btn_add_all_to_active);
            this.Controls.Add(this.btn_add_to_active);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.btn_rec_on_off);
            this.Controls.Add(this.btn_exit);
            this.Controls.Add(this.btn_create_new_sample);
            this.Controls.Add(this.btn_remove_file);
            this.Controls.Add(this.listBox_score);
            this.Controls.Add(this.listBox_active);
            this.Controls.Add(this.listBox_files);
            this.Name = "Form_tester";
            this.Text = "Turan RMS TESTER";
            this.Load += new System.EventHandler(this.Form_tester_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_tester_FormClosing);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox_files;
        private System.Windows.Forms.ListBox listBox_score;
        private System.Windows.Forms.Button btn_remove_file;
        private System.Windows.Forms.Button btn_create_new_sample;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btn_rec_on_off;
        private System.Windows.Forms.Button btn_exit;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listBox_active;
        private System.Windows.Forms.Button btn_add_to_active;
        private System.Windows.Forms.Button btn_remove_from_active;
        private System.Windows.Forms.Button btn_analyze_active;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Button btn_add_all_to_active;
        private System.Windows.Forms.Button btn_remove_all_from_active;
    }
}

