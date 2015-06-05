namespace Felismero_motor
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.gb_wav_data = new System.Windows.Forms.GroupBox();
            this.lb_wav_info = new System.Windows.Forms.ListBox();
            this.btn_record_settings = new System.Windows.Forms.Button();
            this.btn_record_ref = new System.Windows.Forms.Button();
            this.btn_playbutton = new System.Windows.Forms.Button();
            this.btn_browse_wav = new System.Windows.Forms.Button();
            this.tb_wav_path = new System.Windows.Forms.TextBox();
            this.cbox_engine_mode = new System.Windows.Forms.ComboBox();
            this.btn_create_all_reference = new System.Windows.Forms.Button();
            this.cb_refresh_dtw_distances = new System.Windows.Forms.CheckBox();
            this.cb_disp_mfcc_data = new System.Windows.Forms.CheckBox();
            this.btn_win_mfcc_allin1 = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.cb_disp_dtw_data = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lab_dispersion = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.lab_average = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_load_dtw_refs = new System.Windows.Forms.Button();
            this.btn_dtw_calc = new System.Windows.Forms.Button();
            this.lb_dtw_values = new System.Windows.Forms.ListBox();
            this.lb_mfcc_files = new System.Windows.Forms.ListBox();
            this.ofd_openwav = new System.Windows.Forms.OpenFileDialog();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer_disp_wavdata = new System.Windows.Forms.Timer(this.components);
            this.sfd_save_mfcc = new System.Windows.Forms.SaveFileDialog();
            this.ofd_load_mfcc = new System.Windows.Forms.OpenFileDialog();
            this.textBoxConsole = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lab_strip_status = new System.Windows.Forms.ToolStripStatusLabel();
            this.bw_analyze = new System.ComponentModel.BackgroundWorker();
            this.bw_compare = new System.ComponentModel.BackgroundWorker();
            this.groupBox1.SuspendLayout();
            this.gb_wav_data.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.gb_wav_data);
            this.groupBox1.Controls.Add(this.btn_playbutton);
            this.groupBox1.Controls.Add(this.btn_browse_wav);
            this.groupBox1.Controls.Add(this.tb_wav_path);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(172, 272);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "WAV fájl betöltése";
            // 
            // gb_wav_data
            // 
            this.gb_wav_data.Controls.Add(this.lb_wav_info);
            this.gb_wav_data.Location = new System.Drawing.Point(10, 62);
            this.gb_wav_data.Name = "gb_wav_data";
            this.gb_wav_data.Size = new System.Drawing.Size(148, 124);
            this.gb_wav_data.TabIndex = 4;
            this.gb_wav_data.TabStop = false;
            this.gb_wav_data.Text = "Fájl tulajdonságok";
            // 
            // lb_wav_info
            // 
            this.lb_wav_info.FormattingEnabled = true;
            this.lb_wav_info.Location = new System.Drawing.Point(6, 19);
            this.lb_wav_info.Name = "lb_wav_info";
            this.lb_wav_info.Size = new System.Drawing.Size(136, 95);
            this.lb_wav_info.TabIndex = 2;
            // 
            // btn_record_settings
            // 
            this.btn_record_settings.BackColor = System.Drawing.Color.DarkSalmon;
            this.btn_record_settings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_record_settings.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_record_settings.Location = new System.Drawing.Point(22, 529);
            this.btn_record_settings.Name = "btn_record_settings";
            this.btn_record_settings.Size = new System.Drawing.Size(154, 34);
            this.btn_record_settings.TabIndex = 1;
            this.btn_record_settings.Text = "Felvétel beállításai";
            this.btn_record_settings.UseVisualStyleBackColor = false;
            this.btn_record_settings.Click += new System.EventHandler(this.btn_record_settings_Click);
            // 
            // btn_record_ref
            // 
            this.btn_record_ref.BackColor = System.Drawing.Color.OrangeRed;
            this.btn_record_ref.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_record_ref.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_record_ref.Location = new System.Drawing.Point(22, 473);
            this.btn_record_ref.Name = "btn_record_ref";
            this.btn_record_ref.Size = new System.Drawing.Size(154, 50);
            this.btn_record_ref.TabIndex = 1;
            this.btn_record_ref.Text = "Felvétel bekapcsolva";
            this.btn_record_ref.UseVisualStyleBackColor = false;
            this.btn_record_ref.Click += new System.EventHandler(this.btn_record_ref_Click);
            // 
            // btn_playbutton
            // 
            this.btn_playbutton.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.btn_playbutton.Location = new System.Drawing.Point(16, 223);
            this.btn_playbutton.Name = "btn_playbutton";
            this.btn_playbutton.Size = new System.Drawing.Size(142, 34);
            this.btn_playbutton.TabIndex = 1;
            this.btn_playbutton.Text = "Lejátszás";
            this.btn_playbutton.UseVisualStyleBackColor = false;
            this.btn_playbutton.Click += new System.EventHandler(this.btn_playwav_Click);
            // 
            // btn_browse_wav
            // 
            this.btn_browse_wav.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btn_browse_wav.Location = new System.Drawing.Point(16, 19);
            this.btn_browse_wav.Name = "btn_browse_wav";
            this.btn_browse_wav.Size = new System.Drawing.Size(136, 37);
            this.btn_browse_wav.TabIndex = 1;
            this.btn_browse_wav.Text = "Tallóz";
            this.btn_browse_wav.UseVisualStyleBackColor = false;
            this.btn_browse_wav.Click += new System.EventHandler(this.btn_browse_wav_Click);
            // 
            // tb_wav_path
            // 
            this.tb_wav_path.Location = new System.Drawing.Point(10, 192);
            this.tb_wav_path.Name = "tb_wav_path";
            this.tb_wav_path.Size = new System.Drawing.Size(148, 20);
            this.tb_wav_path.TabIndex = 0;
            // 
            // cbox_engine_mode
            // 
            this.cbox_engine_mode.FormattingEnabled = true;
            this.cbox_engine_mode.Items.AddRange(new object[] {
            "LPC",
            "MFCC"});
            this.cbox_engine_mode.Location = new System.Drawing.Point(22, 335);
            this.cbox_engine_mode.Name = "cbox_engine_mode";
            this.cbox_engine_mode.Size = new System.Drawing.Size(154, 21);
            this.cbox_engine_mode.TabIndex = 7;
            this.cbox_engine_mode.Text = "LPC";
            this.cbox_engine_mode.SelectedIndexChanged += new System.EventHandler(this.cbox_engine_mode_SelectedIndexChanged);
            // 
            // btn_create_all_reference
            // 
            this.btn_create_all_reference.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btn_create_all_reference.Location = new System.Drawing.Point(22, 368);
            this.btn_create_all_reference.Name = "btn_create_all_reference";
            this.btn_create_all_reference.Size = new System.Drawing.Size(154, 42);
            this.btn_create_all_reference.TabIndex = 6;
            this.btn_create_all_reference.Text = "A dat könyvtárban lévő összes fájl elemzése";
            this.btn_create_all_reference.UseVisualStyleBackColor = false;
            this.btn_create_all_reference.Click += new System.EventHandler(this.btn_create_all_reference_Click);
            // 
            // cb_refresh_dtw_distances
            // 
            this.cb_refresh_dtw_distances.AutoSize = true;
            this.cb_refresh_dtw_distances.Checked = true;
            this.cb_refresh_dtw_distances.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cb_refresh_dtw_distances.Enabled = false;
            this.cb_refresh_dtw_distances.Location = new System.Drawing.Point(24, 289);
            this.cb_refresh_dtw_distances.Name = "cb_refresh_dtw_distances";
            this.cb_refresh_dtw_distances.Size = new System.Drawing.Size(129, 17);
            this.cb_refresh_dtw_distances.TabIndex = 5;
            this.cb_refresh_dtw_distances.Text = "Referenciák frissítése";
            this.cb_refresh_dtw_distances.UseVisualStyleBackColor = true;
            // 
            // cb_disp_mfcc_data
            // 
            this.cb_disp_mfcc_data.AutoSize = true;
            this.cb_disp_mfcc_data.Location = new System.Drawing.Point(24, 312);
            this.cb_disp_mfcc_data.Name = "cb_disp_mfcc_data";
            this.cb_disp_mfcc_data.Size = new System.Drawing.Size(160, 17);
            this.cb_disp_mfcc_data.TabIndex = 5;
            this.cb_disp_mfcc_data.Text = "MFCC adatok megjelenítése";
            this.cb_disp_mfcc_data.UseVisualStyleBackColor = true;
            // 
            // btn_win_mfcc_allin1
            // 
            this.btn_win_mfcc_allin1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btn_win_mfcc_allin1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_win_mfcc_allin1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_win_mfcc_allin1.Location = new System.Drawing.Point(22, 416);
            this.btn_win_mfcc_allin1.Name = "btn_win_mfcc_allin1";
            this.btn_win_mfcc_allin1.Size = new System.Drawing.Size(154, 52);
            this.btn_win_mfcc_allin1.TabIndex = 1;
            this.btn_win_mfcc_allin1.Text = "WIN_MFCC 1 lépésben";
            this.btn_win_mfcc_allin1.UseVisualStyleBackColor = false;
            this.btn_win_mfcc_allin1.Click += new System.EventHandler(this.btn_win_mfcc_allin1_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.progressBar1);
            this.groupBox3.Controls.Add(this.checkBox1);
            this.groupBox3.Controls.Add(this.cb_disp_dtw_data);
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Controls.Add(this.lab_dispersion);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.lab_average);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.btn_load_dtw_refs);
            this.groupBox3.Controls.Add(this.btn_dtw_calc);
            this.groupBox3.Controls.Add(this.lb_dtw_values);
            this.groupBox3.Controls.Add(this.lb_mfcc_files);
            this.groupBox3.Location = new System.Drawing.Point(197, 18);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(375, 311);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "DTW";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(6, 140);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(155, 22);
            this.progressBar1.TabIndex = 6;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(10, 120);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(121, 17);
            this.checkBox1.TabIndex = 5;
            this.checkBox1.Text = "Pontosság becslése";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // cb_disp_dtw_data
            // 
            this.cb_disp_dtw_data.AutoSize = true;
            this.cb_disp_dtw_data.Location = new System.Drawing.Point(10, 277);
            this.cb_disp_dtw_data.Name = "cb_disp_dtw_data";
            this.cb_disp_dtw_data.Size = new System.Drawing.Size(124, 17);
            this.cb_disp_dtw_data.TabIndex = 4;
            this.cb_disp_dtw_data.Text = "Tömb adatok kiírása";
            this.cb_disp_dtw_data.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label4.Location = new System.Drawing.Point(124, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 20);
            this.label4.TabIndex = 3;
            this.label4.Text = "null";
            // 
            // lab_dispersion
            // 
            this.lab_dispersion.AutoSize = true;
            this.lab_dispersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lab_dispersion.Location = new System.Drawing.Point(65, 182);
            this.lab_dispersion.Name = "lab_dispersion";
            this.lab_dispersion.Size = new System.Drawing.Size(30, 17);
            this.lab_dispersion.TabIndex = 3;
            this.lab_dispersion.Text = "null";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label10.Location = new System.Drawing.Point(6, 182);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(56, 17);
            this.label10.TabIndex = 3;
            this.label10.Text = "Szórás:";
            // 
            // lab_average
            // 
            this.lab_average.AutoSize = true;
            this.lab_average.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lab_average.Location = new System.Drawing.Point(65, 165);
            this.lab_average.Name = "lab_average";
            this.lab_average.Size = new System.Drawing.Size(30, 17);
            this.lab_average.TabIndex = 3;
            this.lab_average.Text = "null";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label9.Location = new System.Drawing.Point(6, 165);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(44, 17);
            this.label9.TabIndex = 3;
            this.label9.Text = "Átlag:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label2.Location = new System.Drawing.Point(6, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(23, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "null";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label3.Location = new System.Drawing.Point(6, 19);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(121, 20);
            this.label3.TabIndex = 3;
            this.label3.Text = "Fő referencia:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label5.Location = new System.Drawing.Point(6, 44);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(107, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Jelöld ki a referenciát";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(6, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 20);
            this.label1.TabIndex = 3;
            this.label1.Text = "Felismert fájl:";
            // 
            // btn_load_dtw_refs
            // 
            this.btn_load_dtw_refs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_load_dtw_refs.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_load_dtw_refs.Location = new System.Drawing.Point(167, 263);
            this.btn_load_dtw_refs.Name = "btn_load_dtw_refs";
            this.btn_load_dtw_refs.Size = new System.Drawing.Size(198, 42);
            this.btn_load_dtw_refs.TabIndex = 2;
            this.btn_load_dtw_refs.Text = "Referenciák frissítése";
            this.btn_load_dtw_refs.UseVisualStyleBackColor = true;
            this.btn_load_dtw_refs.Click += new System.EventHandler(this.btn_load_dtw_refs_Click);
            // 
            // btn_dtw_calc
            // 
            this.btn_dtw_calc.BackColor = System.Drawing.Color.Coral;
            this.btn_dtw_calc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_dtw_calc.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btn_dtw_calc.Location = new System.Drawing.Point(6, 215);
            this.btn_dtw_calc.Name = "btn_dtw_calc";
            this.btn_dtw_calc.Size = new System.Drawing.Size(155, 42);
            this.btn_dtw_calc.TabIndex = 1;
            this.btn_dtw_calc.Text = "Összehasonlítás";
            this.btn_dtw_calc.UseVisualStyleBackColor = false;
            this.btn_dtw_calc.Click += new System.EventHandler(this.btn_dtw_calc_Click);
            // 
            // lb_dtw_values
            // 
            this.lb_dtw_values.FormattingEnabled = true;
            this.lb_dtw_values.Location = new System.Drawing.Point(288, 19);
            this.lb_dtw_values.Name = "lb_dtw_values";
            this.lb_dtw_values.Size = new System.Drawing.Size(77, 238);
            this.lb_dtw_values.TabIndex = 1;
            // 
            // lb_mfcc_files
            // 
            this.lb_mfcc_files.FormattingEnabled = true;
            this.lb_mfcc_files.Location = new System.Drawing.Point(167, 19);
            this.lb_mfcc_files.Name = "lb_mfcc_files";
            this.lb_mfcc_files.Size = new System.Drawing.Size(115, 238);
            this.lb_mfcc_files.TabIndex = 1;
            // 
            // ofd_openwav
            // 
            this.ofd_openwav.Filter = "WAV hangfájlok|*.wav";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timer_disp_wavdata
            // 
            this.timer_disp_wavdata.Enabled = true;
            this.timer_disp_wavdata.Interval = 10;
            this.timer_disp_wavdata.Tick += new System.EventHandler(this.timer_disp_wavdata_Tick);
            // 
            // sfd_save_mfcc
            // 
            this.sfd_save_mfcc.DefaultExt = "mf2";
            this.sfd_save_mfcc.Filter = "MFCC fájlok|*.mf2";
            // 
            // ofd_load_mfcc
            // 
            this.ofd_load_mfcc.DefaultExt = "mf2";
            this.ofd_load_mfcc.Filter = "MFCC fájlok|*.mfcc";
            this.ofd_load_mfcc.Title = "MFCC fájlokat tartalmazó mappa kiválasztása";
            // 
            // textBoxConsole
            // 
            this.textBoxConsole.Location = new System.Drawing.Point(203, 335);
            this.textBoxConsole.Multiline = true;
            this.textBoxConsole.Name = "textBoxConsole";
            this.textBoxConsole.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxConsole.Size = new System.Drawing.Size(369, 228);
            this.textBoxConsole.TabIndex = 5;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lab_strip_status});
            this.statusStrip1.Location = new System.Drawing.Point(0, 579);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(601, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lab_strip_status
            // 
            this.lab_strip_status.Name = "lab_strip_status";
            this.lab_strip_status.Size = new System.Drawing.Size(249, 17);
            this.lab_strip_status.Text = "Az indításhoz töltsd be a referencia adatbázist.";
            // 
            // bw_analyze
            // 
            this.bw_analyze.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bw_analyze_DoWork);
            this.bw_analyze.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bw_analyze_RunWorkerCompleted);
            // 
            // bw_compare
            // 
            this.bw_compare.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bw_compare_DoWork);
            this.bw_compare.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bw_compare_RunWorkerCompleted);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(601, 601);
            this.Controls.Add(this.cbox_engine_mode);
            this.Controls.Add(this.btn_record_settings);
            this.Controls.Add(this.btn_record_ref);
            this.Controls.Add(this.textBoxConsole);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.btn_create_all_reference);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_win_mfcc_allin1);
            this.Controls.Add(this.cb_refresh_dtw_distances);
            this.Controls.Add(this.cb_disp_mfcc_data);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AAL beszédfelismerő";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gb_wav_data.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btn_browse_wav;
        private System.Windows.Forms.TextBox tb_wav_path;
        private System.Windows.Forms.OpenFileDialog ofd_openwav;
        private System.Windows.Forms.Button btn_playbutton;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer_disp_wavdata;
        private System.Windows.Forms.ListBox lb_mfcc_files;
        private System.Windows.Forms.GroupBox gb_wav_data;
        private System.Windows.Forms.ListBox lb_wav_info;
        private System.Windows.Forms.Button btn_dtw_calc;
        private System.Windows.Forms.Button btn_load_dtw_refs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.SaveFileDialog sfd_save_mfcc;
        private System.Windows.Forms.OpenFileDialog ofd_load_mfcc;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btn_win_mfcc_allin1;
        private System.Windows.Forms.CheckBox cb_disp_mfcc_data;
        private System.Windows.Forms.ListBox lb_dtw_values;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox cb_disp_dtw_data;
        private System.Windows.Forms.CheckBox cb_refresh_dtw_distances;
        private System.Windows.Forms.Button btn_record_ref;
        private System.Windows.Forms.TextBox textBoxConsole;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lab_strip_status;
        private System.Windows.Forms.Button btn_record_settings;
        private System.Windows.Forms.Button btn_create_all_reference;
        private System.Windows.Forms.Label lab_dispersion;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label lab_average;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.ComponentModel.BackgroundWorker bw_analyze;
        private System.ComponentModel.BackgroundWorker bw_compare;
        private System.Windows.Forms.ComboBox cbox_engine_mode;
    }
}

