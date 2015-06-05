namespace Turan_GUI
{
    partial class Train
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Train));
            this.lab_speaktomic = new System.Windows.Forms.Label();
            this.lab_volume_cap = new System.Windows.Forms.Label();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_home = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btn_back = new System.Windows.Forms.ToolStripButton();
            this.btn_foward = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btn_play = new System.Windows.Forms.ToolStripButton();
            this.btn_delete = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.lab_current_word = new System.Windows.Forms.Label();
            this.btn_rec_start = new System.Windows.Forms.Button();
            this.progress_train = new System.Windows.Forms.ProgressBar();
            this.volumeTrackerTimer = new System.Windows.Forms.Timer(this.components);
            this.lab_saylab = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.FMOD_REC_VOLUME = new System.Windows.Forms.Timer(this.components);
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lab_speaktomic
            // 
            this.lab_speaktomic.AutoSize = true;
            this.lab_speaktomic.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lab_speaktomic.Location = new System.Drawing.Point(209, 156);
            this.lab_speaktomic.Name = "lab_speaktomic";
            this.lab_speaktomic.Size = new System.Drawing.Size(96, 26);
            this.lab_speaktomic.TabIndex = 7;
            this.lab_speaktomic.Text = "parancs";
            this.lab_speaktomic.Visible = false;
            // 
            // lab_volume_cap
            // 
            this.lab_volume_cap.AutoSize = true;
            this.lab_volume_cap.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lab_volume_cap.Location = new System.Drawing.Point(337, 99);
            this.lab_volume_cap.Name = "lab_volume_cap";
            this.lab_volume_cap.Size = new System.Drawing.Size(71, 20);
            this.lab_volume_cap.TabIndex = 6;
            this.lab_volume_cap.Text = "Hangerő";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_home,
            this.toolStripSeparator1,
            this.btn_back,
            this.btn_foward,
            this.toolStripSeparator2,
            this.btn_play,
            this.btn_delete});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(495, 55);
            this.toolStrip1.TabIndex = 16;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btn_home
            // 
            this.btn_home.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_home.Image = ((System.Drawing.Image)(resources.GetObject("btn_home.Image")));
            this.btn_home.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_home.Name = "btn_home";
            this.btn_home.Size = new System.Drawing.Size(52, 52);
            this.btn_home.Text = "Kezdőoldal";
            this.btn_home.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.btn_home.Click += new System.EventHandler(this.btn_home_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 55);
            // 
            // btn_back
            // 
            this.btn_back.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_back.Enabled = false;
            this.btn_back.Image = ((System.Drawing.Image)(resources.GetObject("btn_back.Image")));
            this.btn_back.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_back.Name = "btn_back";
            this.btn_back.Size = new System.Drawing.Size(52, 52);
            this.btn_back.Text = "Előző";
            this.btn_back.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btn_back.Click += new System.EventHandler(this.btn_back_Click);
            // 
            // btn_foward
            // 
            this.btn_foward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_foward.Enabled = false;
            this.btn_foward.Image = ((System.Drawing.Image)(resources.GetObject("btn_foward.Image")));
            this.btn_foward.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_foward.Name = "btn_foward";
            this.btn_foward.Size = new System.Drawing.Size(52, 52);
            this.btn_foward.Text = "Következő";
            this.btn_foward.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btn_foward.Click += new System.EventHandler(this.btn_foward_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 55);
            // 
            // btn_play
            // 
            this.btn_play.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_play.Enabled = false;
            this.btn_play.Image = ((System.Drawing.Image)(resources.GetObject("btn_play.Image")));
            this.btn_play.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_play.Name = "btn_play";
            this.btn_play.Size = new System.Drawing.Size(52, 52);
            this.btn_play.Text = "Lejátszás";
            this.btn_play.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btn_play.Click += new System.EventHandler(this.btn_play_Click);
            // 
            // btn_delete
            // 
            this.btn_delete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_delete.Enabled = false;
            this.btn_delete.Image = ((System.Drawing.Image)(resources.GetObject("btn_delete.Image")));
            this.btn_delete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_delete.Name = "btn_delete";
            this.btn_delete.Size = new System.Drawing.Size(52, 52);
            this.btn_delete.Text = "Törlés";
            this.btn_delete.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btn_delete.Click += new System.EventHandler(this.btn_delete_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 227);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(495, 22);
            this.statusStrip1.TabIndex = 17;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(71, 17);
            this.toolStripStatusLabel1.Text = "Indításra vár";
            // 
            // lab_current_word
            // 
            this.lab_current_word.AutoSize = true;
            this.lab_current_word.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lab_current_word.Location = new System.Drawing.Point(12, 65);
            this.lab_current_word.Name = "lab_current_word";
            this.lab_current_word.Size = new System.Drawing.Size(110, 20);
            this.lab_current_word.TabIndex = 18;
            this.lab_current_word.Text = "Tanított minta:";
            this.lab_current_word.Visible = false;
            // 
            // btn_rec_start
            // 
            this.btn_rec_start.BackColor = System.Drawing.Color.Tomato;
            this.btn_rec_start.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_rec_start.Location = new System.Drawing.Point(35, 99);
            this.btn_rec_start.Name = "btn_rec_start";
            this.btn_rec_start.Size = new System.Drawing.Size(211, 47);
            this.btn_rec_start.TabIndex = 19;
            this.btn_rec_start.Text = "Indítás";
            this.btn_rec_start.UseVisualStyleBackColor = false;
            this.btn_rec_start.Click += new System.EventHandler(this.btn_rec_start_Click);
            // 
            // progress_train
            // 
            this.progress_train.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progress_train.Location = new System.Drawing.Point(0, 204);
            this.progress_train.Name = "progress_train";
            this.progress_train.Size = new System.Drawing.Size(495, 23);
            this.progress_train.TabIndex = 11;
            // 
            // volumeTrackerTimer
            // 
            this.volumeTrackerTimer.Interval = 10;
            this.volumeTrackerTimer.Tick += new System.EventHandler(this.volumeTrackerTimer_Tick);
            // 
            // lab_saylab
            // 
            this.lab_saylab.AutoSize = true;
            this.lab_saylab.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lab_saylab.Location = new System.Drawing.Point(46, 160);
            this.lab_saylab.Name = "lab_saylab";
            this.lab_saylab.Size = new System.Drawing.Size(157, 20);
            this.lab_saylab.TabIndex = 21;
            this.lab_saylab.Text = "Mikrofonba mondani:";
            this.lab_saylab.Visible = false;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(275, 122);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(200, 14);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 22;
            // 
            // FMOD_REC_VOLUME
            // 
            this.FMOD_REC_VOLUME.Enabled = true;
            this.FMOD_REC_VOLUME.Interval = 50;
            this.FMOD_REC_VOLUME.Tick += new System.EventHandler(this.FMOD_REC_VOLUME_Tick);
            // 
            // Train
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(495, 249);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.lab_saylab);
            this.Controls.Add(this.progress_train);
            this.Controls.Add(this.btn_rec_start);
            this.Controls.Add(this.lab_current_word);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.lab_speaktomic);
            this.Controls.Add(this.lab_volume_cap);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "Train";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Hangok felvétele";
            this.Load += new System.EventHandler(this.Train_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Train_FormClosing);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lab_speaktomic;
        private System.Windows.Forms.Label lab_volume_cap;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btn_home;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btn_back;
        private System.Windows.Forms.ToolStripButton btn_foward;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btn_play;
        private System.Windows.Forms.ToolStripButton btn_delete;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Label lab_current_word;
        private System.Windows.Forms.Button btn_rec_start;
        private System.Windows.Forms.ProgressBar progress_train;
        private System.Windows.Forms.Timer volumeTrackerTimer;
        private System.Windows.Forms.Label lab_saylab;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Timer FMOD_REC_VOLUME;
    }
}