/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

/***************************************************************************
               Form1.cs  -  a simple speech recognizer
                             -------------------
    begin                : June 2010    
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Media;
using System.IO;
using EricOulashin; // WawFile class
using SoundAnalysis; // Preform FFT calculation
using System.Runtime.Serialization.Formatters.Binary; // Serialize vector data
using SoundCatcher; // Voice activated recording by monitoring amplitude events (record above selected treshold)
using VorbisSharp; // Calculate LPC coefficients from PCM data

namespace Felismero_motor
{
    public partial class Form1 : Form
    {
        private const int SPECTRUMSIZE = 512;
        private const int WAVEDATASIZE = 256;

        private FMOD.System system = null;
        private FMOD.Sound sound = null;
        private FMOD.Channel channel = null;

        private float[] spectrum = new float[SPECTRUMSIZE];
        private float[] spectrumx = new float[SPECTRUMSIZE];
        private float[] spectrumy = new float[SPECTRUMSIZE];
        private float[] WAVEDATA = new float[WAVEDATASIZE];

        private float[] MELDATA = new float[SPECTRUMSIZE];
        int num_of_frames = 0;
        int extended_num_of_frames = 0;

        bool isplaying = false;

        private double[] pcmdata;
        private double[] firdata;
        private double[] hammingdata;
        //private double[] fftdata;
        //private double[] meldata;
        //private double[] mfccdata;
        //private double[,] win_mfccdata;

        private double[,] win_pcmdata;
        private double[,] win_hammingdata;
        private double[,] win_fftdata;
        private double[,] win_meldata;

        private double[,] win_lpcdata;

        private double[,] win_REF_vector_data;

        bool dtw_ref_ready = false;
        string signal_filename = "signal.wav";
        string signal_filename_mfcc = "signal.wav.mfcc";
        string signal_filename_lpc = "signal.wav.lpc";
        string working_dir_dat = Application.StartupPath + @"\dat\";

        EngineMode engine_mode = EngineMode.lpc;
        List<string> active_vector_filenames = new List<string>();
        List<double> scoreList = new List<double>();


        public enum EngineMode
        {
            mfcc,
            lpc
        }


        public Form1()
        {
            InitializeComponent();

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            cbox_engine_mode.Text = Properties.Settings.Default.SettingEngineMode;

            // set number of vectors
            if (engine_mode == EngineMode.lpc)
            {
                H_FELDOLGOZO.mfcc_lpc_vect_num = 12;
            }
            else if (engine_mode == EngineMode.mfcc)
            {
                H_FELDOLGOZO.mfcc_lpc_vect_num = 15;
            }

            //-------------------------------------------

            uint version = 0;
            FMOD.RESULT result;

            /*
                Create a System object and initialize.
            */
            result = FMOD.Factory.System_Create(ref system);
            ERRCHECK(result);

            result = system.getVersion(ref version);
            ERRCHECK(result);
            if (version < FMOD.VERSION.number)
            {
                MessageBox.Show("Error!  You are using an old version of FMOD " + version.ToString("X") + ".  This program requires " + FMOD.VERSION.number.ToString("X") + ".");
                Application.Exit();
            }

            result = system.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)null);
            ERRCHECK(result);



            //---------------------SC------------------

            Directory.CreateDirectory(working_dir_dat);

            if (WaveNative.waveInGetNumDevs() == 0)
            {
                textBoxConsole.AppendText(DateTime.Now.ToString() + " : Felvevő eszköz nem található\r\n");
            }
            else
            {
                textBoxConsole.AppendText(DateTime.Now.ToString() + " : Felvevő eszköz észlelve\r\n");
                if (_isPlayer == true)
                    _streamOut = new FifoStream();
                _audioFrame = new AudioFrame(_isTest);
                _audioFrame.IsDetectingEvents = Properties.Settings.Default.SettingIsDetectingEvents;
                _audioFrame.AmplitudeThreshold = Properties.Settings.Default.SettingAmplitudeThreshold;
                _streamMemory = new MemoryStream();
                Start();
            }

            RefreshRecordState();

            //-------------------SC-END----------------

        }


        private void btn_browse_wav_Click(object sender, EventArgs e)
        {
            if (sound != null)
            {
                if (channel != null)
                {
                    channel.stop();
                    channel = null;
                }
                sound.release();
                sound = null;
            }

            if (ofd_openwav.ShowDialog() == DialogResult.OK)
            {

                GetWavData(ofd_openwav.FileName);

                FMOD.RESULT result;

                result = system.createStream(ofd_openwav.FileName, FMOD.MODE.SOFTWARE | FMOD.MODE._2D, ref sound);
                ERRCHECK(result);

                tb_wav_path.Text = ofd_openwav.FileName;


                try
                {
                    //FMOD_FUNCTIONS fmodfunc = new FMOD_FUNCTIONS(this);
                    //fmodfunc.ReadTags_Load(ofd_openwav.FileName);   // err
                }
                catch (Exception ex)
                {
                    lab_strip_status.Text = ex.Message;
                }
            }
        }


        private void GetWavData(string filename)
        {
            string csatornaszam = "";

            lb_wav_info.Items.Clear();

            FileInfo fi = new FileInfo(filename);
            lb_wav_info.Items.Add("Méret: " + fi.Length.ToString() + " bájt");

            WAVFile wfile = new WAVFile();
            wfile.Open(filename, WAVFile.WAVFileMode.READ);

            pcmdata = new double[wfile.NumSamples];

            lb_wav_info.Items.Add("Bit/minta: " + wfile.AudioFormat.BitsPerSample.ToString() + " bit");
            lb_wav_info.Items.Add("Frekvencia: " + wfile.AudioFormat.SampleRateHz.ToString() + " Hz");

            if (wfile.AudioFormat.IsStereo)
            {
                csatornaszam = " (Sztereó)";
            }
            else
            {
                csatornaszam = " (Monó)";
            }
            lb_wav_info.Items.Add("Csatornák: " + wfile.AudioFormat.NumChannels.ToString() + csatornaszam);


            int i = 0;
            while (wfile.FilePosition != wfile.FileSizeBytes)
            {
                pcmdata[i] = wfile.GetNextSampleAs16Bit();


                i++;
            }

            wfile.Close();
        }


        private void btn_playwav_Click(object sender, EventArgs e)
        {
            if (tb_wav_path.Text != "")
            {


                FMOD.RESULT result;
                bool isplaying = false;

                if (channel != null)
                {
                    channel.isPlaying(ref isplaying);
                }

                if (sound != null && !isplaying)
                {
                    result = system.playSound(FMOD.CHANNELINDEX.FREE, sound, false, ref channel);
                    ERRCHECK(result);

                    btn_playbutton.Text = "Stop";

                    //lb_wavdata.Text = result.ToString();
                    //lb_wavdata.Text += system.getRaw().ToString();                    
                }
                else
                {
                    if (channel != null)
                    {
                        channel.stop();
                        channel = null;
                    }
                    btn_playbutton.Text = "Lejátszás";
                }
            }
            else
            {
                MessageBox.Show("Nyiss meg egy WAV fájlt.");
            }
        }


        private void ERRCHECK(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                timer1.Stop();
                MessageBox.Show("FMOD error! " + result + " - " + FMOD.Error.String(result));
                Environment.Exit(-1);
            }
        }

        private void btn_disp_wavdata_Click(object sender, EventArgs e)
        {

            if (channel != null)
            {
                channel.isPlaying(ref isplaying);
            }

            if (sound != null && isplaying)
            {

                float max_ch1 = 0.0f;

                channel.getWaveData(WAVEDATA, WAVEDATASIZE, 0);

                for (int i = 0; i < WAVEDATASIZE; i++)    //megkeressuk a maximális spektrumértéket a progressbar maximumának
                {

                    if ((max_ch1 < WAVEDATA[i]))
                        max_ch1 = WAVEDATA[i];
                }

            }

        }


        private void timer_disp_wavdata_Tick(object sender, EventArgs e)
        {
            //FMOD.RESULT result;
            bool isplaying = false;

            if (channel != null)
            {
                channel.isPlaying(ref isplaying);
            }

            if (sound != null && isplaying)
            {
                float max_ch1 = 0.0f;

                channel.getWaveData(WAVEDATA, WAVEDATASIZE, 0);

                for (int i = 0; i < WAVEDATASIZE; i++)    //megkeressuk a maximális spektrumértéket a progressbar maximumának
                {

                    if ((max_ch1 < WAVEDATA[i]))
                        max_ch1 = WAVEDATA[i];
                }

            }
        }


        private void btn_fft_Click(object sender, EventArgs e)
        {
            //lb_fft_data.Items.Clear();

            //AP.Complex[] apc = new AP.Complex[hammingdata.Length];
            //alglib.fft.fftr1d(ref hammingdata, hammingdata.Length, ref apc);
            //fftdata = new double[apc.Length];

            //for (int i = 0; i < apc.Length - 1; i++)
            //{
            //    fftdata[i] = apc[i].x;
            //}

            //foreach (float fft_dat in fftdata)
            //{
            //    lb_fft_data.Items.Add(fft_dat.ToString());
            //}

            //btn_calc_mfcc.Enabled = true;
        }


        public void ClearWavDataListbox()
        {
            lb_wav_info.Items.Clear();
        }

        public void AddWavDataToListbox(string dataline)
        {
            lb_wav_info.Items.Add(dataline);
        }

        private void btn_calc_mfcc_Click(object sender, EventArgs e)
        {

            //lb_mfcc_data.Items.Clear();

            //meldata = new double[14];

            //CV_FELDOLGOZO.init_mel_filter_banks();
            //CV_FELDOLGOZO.InitMelFilterFrame(fftdata, ref meldata);

            //foreach (float mel_dat in meldata)
            //{
            //    lb_mfcc_data.Items.Add(mel_dat.ToString());
            //}
            //this.Text = meldata[0].ToString();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            btn_playbutton.Text = "Lejátszás";
        }

        private void btn_fir_Click(object sender, EventArgs e)
        {

            firdata = H_FELDOLGOZO.fir_filter(pcmdata);
        }

        private void btn_calc_hamming_Click(object sender, EventArgs e)
        {

            hammingdata = H_FELDOLGOZO.hamming_ablak(firdata);

        }


        private void btn_dtw_calc_Click(object sender, EventArgs e)
        {
            CalcDTWDistances_ISIP();


            //--------------------------------------------
            // TRHEAD SAFETY!

            //btn_dtw_calc.Enabled = false;            

            //if (!bw_compare.IsBusy)
            //{
            //    lb_dtw_values.Items.Clear();
            //    bw_compare.RunWorkerAsync(); //CalcDTWDistances_ISIP();
            //}
            //---------------------------------------------
        }


        private void CalcDTWDistances_ISIP()
        {
            if (!dtw_ref_ready)
            {
                lab_strip_status.Text = "Referencia nincs betöltve!";
                return;
            }

            //lb_dtw_values.Items.Clear();

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Reset();
            sw.Start();

            //dtwApp_match.Num_of_templates = lb_mfcc_files.Items.Count; // call this first!

            dtwApp_match.Num_of_templates = active_vector_filenames.Count; // call this first!


            // Set reference vector based on recognition mode (MFCC/LPC)

            //dtwApp_match dtwmatch = new dtwApp_match(win_meldata);

            //dtwApp_match dtwmatch=new dtwApp_match();


            //if (engine_mode == EngineMode.lpc)
            //{
            //    dtwmatch = new dtwApp_match(win_lpcdata);
            //}
            //else if (engine_mode == EngineMode.mfcc)
            //{
            //    dtwmatch = new dtwApp_match(win_meldata);
            //}



            if (engine_mode == EngineMode.mfcc)
            {
                win_lpcdata = win_meldata;
            }

            dtwApp_match dtwmatch = new dtwApp_match(win_lpcdata);


            //foreach (string fname in lb_mfcc_files.Items)
            //{
            //    win_REF_meldata = DeSerializeArray("dat\\" + fname);
            //    dtwmatch.AddTemplate(win_REF_meldata);
            //}

            //foreach (string fname in lb_mfcc_files.Items)
            //{
            //    win_REF_vector_data = DeSerializeArray("dat\\" + fname);
            //    dtwmatch.AddTemplate(win_REF_vector_data);
            //}

            foreach (string fname in active_vector_filenames)
            {
                win_REF_vector_data = DeSerializeArray("dat\\" + fname);
                dtwmatch.AddTemplate(win_REF_vector_data);
            }

            dtwmatch.bestMatch();

            scoreList.Clear();

            foreach (double item in dtwmatch.TotalCost)
            {
                //lb_dtw_values.Items.Add(item.ToString());
                scoreList.Add(item);
            }

            FillScoreListbox(); // GUI update

            if (dtwmatch.RecogResult == -1)
            {
                lab_strip_status.Text = "A hangparancsok még nincsenek beállítva!";
            }
            else
            {
                label2.Text = lb_mfcc_files.Items[dtwmatch.RecogResult].ToString();
            }

            sw.Stop();


            if (cb_disp_dtw_data.Checked)
            {
                //H_FELDOLGOZO.ShowArray(dtwmatch.costRecord, num_of_frames);
                //H_FELDOLGOZO.Show2dArray(dtwmatch.pathRecord, dtwmatch.pathRecord.Length / H_FELDOLGOZO.mfccnum);
                //H_FELDOLGOZO.ShowArray(dtwmatch.TotalCost, num_of_frames);



                foreach (int[,] item in dtwmatch.pathRecordList)
                {
                    H_FELDOLGOZO.Show2dArray(item, item.Length / H_FELDOLGOZO.mfcc_lpc_vect_num);
                }

            }

            this.Text = "DTW összehasonlítások az összes referenciára: " + sw.ElapsedMilliseconds.ToString() + " ms";
        }


        private void FillScoreListbox()
        {
            lb_dtw_values.Items.Clear();

            foreach (double value in scoreList)
            {
                lb_dtw_values.Items.Add(value.ToString());
            }
        }


        private void btn_load_dtw_refs_Click(object sender, EventArgs e)
        {
            if (engine_mode == EngineMode.lpc)
            {
                Get_LPC_Refs();
            }
            else if (engine_mode == EngineMode.mfcc)
            {
                GetMFCCRefs();
            }
        }


        private void GetMFCCRefs()
        {
            active_vector_filenames.Clear();

            string[] mfccfilePaths = Directory.GetFiles(Path.GetDirectoryName(working_dir_dat), "*.mfcc");

            foreach (string file in mfccfilePaths)
            {
                if (Path.GetFileName(file) != signal_filename_mfcc)
                {
                    //lb_mfcc_files.Items.Add(Path.GetFileName(file));
                    active_vector_filenames.Add(Path.GetFileName(file));
                }
            }
            FillVectorListbox();
        }

        private void Get_LPC_Refs()
        {
            active_vector_filenames.Clear();

            string[] lpc_filePaths = Directory.GetFiles(Path.GetDirectoryName(working_dir_dat), "*.lpc");

            foreach (string file in lpc_filePaths)
            {
                if (Path.GetFileName(file) != signal_filename_lpc)
                {
                    active_vector_filenames.Add(Path.GetFileName(file));
                }
            }
            FillVectorListbox(); // GUI update
        }

        private void FillVectorListbox()
        {
            lb_mfcc_files.Items.Clear();
            lb_dtw_values.Items.Clear();

            foreach (string filename in active_vector_filenames)
            {
                lb_mfcc_files.Items.Add(filename);
            }
        }


        private void btn_win_mfcc_allin1_Click(object sender, EventArgs e)
        {
            try
            {
                if (ofd_openwav.ShowDialog() == DialogResult.OK)
                {
                    AnalyzeSignal(ofd_openwav.FileName);
                }
            }
            catch (Exception ex)
            {
                lab_strip_status.Text = ex.Message;
            }
        }

        private void PreAnalyzeSignal_GUIupdate(string signal_filename)
        {
            dtw_ref_ready = true;

            label4.Text = "OK";
            label5.Text = Path.GetFileName(signal_filename);
        }

        private void AnalyzeSignalSuccess_GUIupdate()
        {

        }

        private void AnalyzeSignal(string signal_filename)
        {
            PreAnalyzeSignal_GUIupdate(signal_filename);


            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Reset();
            sw.Start();


            WAVFile wreffile = new WAVFile();
            wreffile.Open(signal_filename, WAVFile.WAVFileMode.READ);
            pcmdata = new double[wreffile.NumSamples];

            int fpos = 0;
            while (wreffile.FilePosition != wreffile.FileSizeBytes)
            {
                pcmdata[fpos] = wreffile.GetNextSampleAs16Bit();
                fpos++;
            }

            wreffile.Close();

            num_of_frames = pcmdata.Length / H_FELDOLGOZO.num_items_in_windowed_frame;
            extended_num_of_frames = (int)(num_of_frames * (1 / H_FELDOLGOZO.overlap_rate));
            //--------------------------------------------------------------------------

            win_pcmdata = H_FELDOLGOZO.create_window(pcmdata, num_of_frames);


            if (engine_mode == EngineMode.lpc)
            {
                //with Hamming filtering
                //win_hammingdata = H_FELDOLGOZO.win_fir_hamming(win_pcmdata, extended_num_of_frames);
                //win_lpcdata = LPC_Calc_FrameByFrame(win_hammingdata);

                //without Hamming filtering
                win_lpcdata = LPC_Calc_FrameByFrame(win_pcmdata);
            }

            else if (engine_mode == EngineMode.mfcc)
            {
                win_hammingdata = H_FELDOLGOZO.win_fir_hamming(win_pcmdata, extended_num_of_frames);

                win_fftdata = FFTCalc_FrameByFrame(win_hammingdata);

                win_meldata = new double[extended_num_of_frames, H_FELDOLGOZO.mfcc_lpc_vect_num];

                CV_FELDOLGOZO.init_mel_filter_banks();

                CV_FELDOLGOZO.Window_Mel_Scale_Reduction(win_hammingdata, ref win_meldata, extended_num_of_frames);
            }


            sw.Stop();

            //----------------------------------------

            //if (cb_disp_mfcc_data.Checked)
            //{
            //    string melarr = "";
            //    int lines = 0;

            //    foreach (object item in win_meldata)
            //    {
            //        melarr += item.ToString() + Environment.NewLine;
            //        lines++;
            //    }
            //    melarr += "---adatok vége---" + Environment.NewLine;
            //    melarr += Environment.NewLine;
            //    melarr += lines.ToString() + " adatsor" + Environment.NewLine;
            //    melarr += extended_num_of_frames.ToString() + " keret" + Environment.NewLine;
            //    melarr += H_FELDOLGOZO.mfcc_lpc_vect_num.ToString() + " adat keretenként.";

            //    DataView dw = new DataView();
            //    dw.SetData(melarr);
            //    dw.Show();
            //}



            if (engine_mode == EngineMode.lpc)
            {
                SerializeArray(win_lpcdata, signal_filename + ".lpc");
            }
            else if (engine_mode == EngineMode.mfcc)
            {
                SerializeArray(win_meldata, signal_filename + ".mfcc");
            }




            if (cb_refresh_dtw_distances.Checked)
            {

                if (engine_mode == EngineMode.lpc)
                {
                    Get_LPC_Refs();
                }
                else if (engine_mode == EngineMode.mfcc)
                {
                    GetMFCCRefs();
                }
                CalcDTWDistances_ISIP();
            }

            //----------------------------------------

            this.Text = "Utolsó WIN_MFCC elemezés: " + sw.ElapsedMilliseconds.ToString() + " ms";
        }


        public double[,] FFTCalc_FrameByFrame(double[,] input_win_hammingdata)
        {
            win_fftdata = new double[extended_num_of_frames, H_FELDOLGOZO.num_items_in_windowed_frame];

            // Read, FFT, Write back

            for (int frame = 0; frame < extended_num_of_frames; frame++)
            {
                double[] temp_frame_line = new double[H_FELDOLGOZO.num_items_in_windowed_frame];

                for (int frame_item = 0; frame_item < H_FELDOLGOZO.num_items_in_windowed_frame; frame_item++)
                {
                    temp_frame_line[frame_item] = input_win_hammingdata[frame, frame_item];
                }

                // FFT per frame

                try
                {
                    temp_frame_line = SoundAnalysis.FftAlgorithm.Calculate(temp_frame_line);
                }
                catch (Exception)
                {

                }

                // write back frame

                for (int frame_item = 0; frame_item < H_FELDOLGOZO.num_items_in_windowed_frame; frame_item++)
                {
                    win_fftdata[frame, frame_item] = temp_frame_line[frame_item];
                }

                // continue with the next frame
            }

            return win_fftdata;
        }



        public double[,] LPC_Calc_FrameByFrame(double[,] input_win_pcmdata)
        {
            win_lpcdata = new double[extended_num_of_frames, H_FELDOLGOZO.mfcc_lpc_vect_num];

            // Read, FFT, Write back

            for (int frame = 0; frame < extended_num_of_frames; frame++)
            {
                double[] temp_frame_line = new double[H_FELDOLGOZO.num_items_in_windowed_frame];
                double[] lpc_frame_line = new double[H_FELDOLGOZO.mfcc_lpc_vect_num];

                for (int frame_item = 0; frame_item < H_FELDOLGOZO.num_items_in_windowed_frame; frame_item++)
                {
                    temp_frame_line[frame_item] = input_win_pcmdata[frame, frame_item];
                }

                // FFT per frame

                try
                {
                    Lpc.lpc_from_data(temp_frame_line, ref lpc_frame_line, temp_frame_line.Length, H_FELDOLGOZO.mfcc_lpc_vect_num);
                }
                catch (Exception)
                {

                }

                // write back frame

                for (int frame_item = 0; frame_item < H_FELDOLGOZO.mfcc_lpc_vect_num; frame_item++)
                {
                    win_lpcdata[frame, frame_item] = lpc_frame_line[frame_item];
                }

                // continue with the next frame
            }

            return win_lpcdata;
        }



        /// <summary>
        /// Serializes the array into the specified file.
        /// </summary>
        /// <param name="arList">2D double[,] array</param>
        /// <param name="fname">Output filename.</param>
        public static void SerializeArray(double[,] arList, string fname)
        {
            //Console.WriteLine("Please wait while settings are saved...");
            FileStream fstream = new FileStream(fname, FileMode.Create, FileAccess.Write);
            BinaryFormatter binFormat = new BinaryFormatter();
            try
            {
                binFormat.Serialize(fstream, arList);
            }
            finally
            {
                fstream.Close();
                //Console.WriteLine("Transfer is complete!");
            }
        }


        /// <summary>
        /// Deserialize the binary file back into an array.
        /// </summary>
        /// <param name="fname">The input filename.</param>
        /// <returns>2D double[,] array</returns>
        public static double[,] DeSerializeArray(string fname)
        {
            FileStream fstream = new FileStream(fname, FileMode.Open, FileAccess.Read);
            BinaryFormatter binFormat = new BinaryFormatter();
            double[,] binArray;

            try
            {
                binArray = (double[,])binFormat.Deserialize(fstream);
            }
            finally
            {
                fstream.Close();
                //Console.WriteLine("Transfer is complete!");
            }

            return binArray;
        }


        private void btn_record_settings_Click(object sender, EventArgs e)
        {
            FormOptionsDialog form = new FormOptionsDialog();
            if (form.ShowDialog() == DialogResult.OK)
            {
                _audioFrame.IsDetectingEvents = form.IsDetectingEvents;
                _audioFrame.AmplitudeThreshold = form.AmplitudeThreshold;
            }

            RefreshRecordState();

        }

        private void RefreshRecordState()
        {
            if (_audioFrame.IsDetectingEvents)
            {
                btn_record_ref.Text = "Felvétel bekapcsolva";
            }
            else
            {
                btn_record_ref.Text = "Felvétel kikapcsolva";
            }
        }




        //-------------SC-INTEGRATION----------------------------

        //string signal_filename = "signal.wav";

        public string Signal_filename_f
        {
            get { return signal_filename; }
            set { signal_filename = value; }
        }


        private WaveInRecorder _recorder;
        private byte[] _recorderBuffer;
        private WaveOutPlayer _player;
        private byte[] _playerBuffer;
        private WaveFormat _waveFormat;
        private AudioFrame _audioFrame;
        private FifoStream _streamOut;
        private MemoryStream _streamMemory;

        private MemoryStream _streamMemorySmallBuffer;

        private Stream _streamWave;
        private FileStream _streamFile;
        private bool _isPlayer = false;  // audio output for testing
        private bool _isTest = false;  // signal generation for testing
        private bool _isSaving = false;
        //private bool _isShown = true;
        private string _sampleFilename;
        private DateTime _timeLastDetection;


        private void Start()
        {
            Stop();
            try
            {
                _waveFormat = new WaveFormat(Properties.Settings.Default.SettingSamplesPerSecond, Properties.Settings.Default.SettingBitsPerSample, Properties.Settings.Default.SettingChannels);

                _recorder = new WaveInRecorder(Properties.Settings.Default.SettingAudioInputDevice, _waveFormat, Properties.Settings.Default.SettingBytesPerFrame * Properties.Settings.Default.SettingChannels, 3, new BufferDoneEventHandler(DataArrived));

                if (_isPlayer == true)
                    _player = new WaveOutPlayer(Properties.Settings.Default.SettingAudioOutputDevice, _waveFormat, Properties.Settings.Default.SettingBytesPerFrame * Properties.Settings.Default.SettingChannels, 3, new BufferFillEventHandler(Filler));
            }
            catch (Exception)
            {

            }
        }
        private void Stop()
        {
            if (_recorder != null)
                try
                {
                    _recorder.Dispose();
                }
                finally
                {
                    _recorder = null;
                }
            if (_isPlayer == true)
            {
                if (_player != null)
                    try
                    {
                        _player.Dispose();
                    }
                    finally
                    {
                        _player = null;
                    }
                _streamOut.Flush(); // clear all pending data
            }
        }



        private void Filler(IntPtr data, int size)
        {
            if (_isPlayer == true)
            {
                if (_playerBuffer == null || _playerBuffer.Length < size)
                    _playerBuffer = new byte[size];
                if (_streamOut.Length >= size)
                    _streamOut.Read(_playerBuffer, 0, size);
                else
                    for (int i = 0; i < _playerBuffer.Length; i++)
                        _playerBuffer[i] = 0;
                System.Runtime.InteropServices.Marshal.Copy(_playerBuffer, 0, data, size);
            }
        }


        //   --- DataArrived ---


        private void DataArrived(IntPtr data, int size)
        {

            int circular_puffer_size = 90000000; //524288; //131072; // 65536;

            if (_isSaving == true)
            {
                byte[] recBuffer = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(data, recBuffer, 0, size);
                _streamMemory.Write(recBuffer, 0, recBuffer.Length);
            }
            else
            {
                byte[] recBuffer = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(data, recBuffer, 0, size);


                if (_streamMemory.Position >= circular_puffer_size)
                {
                    _streamMemory.Position = 0;
                }

                _streamMemory.Write(recBuffer, 0, recBuffer.Length);
                //_streamMemorySmallBuffer.Write(recBuffer, 0, recBuffer.Length);




            }

            if (_recorderBuffer == null || _recorderBuffer.Length != size)
            {
                _recorderBuffer = new byte[size];
            }

            if (_recorderBuffer != null)
            {
                System.Runtime.InteropServices.Marshal.Copy(data, _recorderBuffer, 0, size);
                if (_isPlayer == true)
                    _streamOut.Write(_recorderBuffer, 0, _recorderBuffer.Length);



                _audioFrame.Process(ref _recorderBuffer);

                if (_audioFrame.IsEventActive == true)
                {
                    if (_isSaving == false && Properties.Settings.Default.SettingIsSaving == true)
                    {
                        _sampleFilename = signal_filename;
                        _timeLastDetection = DateTime.Now;
                        _isSaving = true;
                    }
                    else
                    {
                        _timeLastDetection = DateTime.Now;
                    }
                    //Invoke(new MethodInvoker(AmplitudeEvent));
                    AmplitudeEvent();
                }

                if (_isSaving == true && DateTime.Now.Subtract(_timeLastDetection).Seconds > Properties.Settings.Default.SettingSecondsToSave)
                {
                    // felvétel lezárása

                    // HEADER + KÖRPUFFER TARTALOM


                    //byte[] korPuffer = new byte[circular_puffer_size];
                    //_streamMemorySmallBuffer.Read(korPuffer,0,korPuffer.Length);

                    //if (_streamMemorySmallBuffer.Position >= circular_puffer_size)
                    //{
                    //    _streamMemorySmallBuffer.Position = 0;
                    //}

                    //_streamMemory.Write(korPuffer, (int)_streamMemorySmallBuffer.Position, circular_puffer_size - (int)_streamMemorySmallBuffer.Position);

                    //_streamMemory.Write(korPuffer, 0, (int)_streamMemorySmallBuffer.Position);

                    ////----kp

                    //int counter = 0;

                    //byte[] korPuffer = new byte[64000];


                    //for (int i = 0; i < korPuffer.Length; i++)
                    //{
                    //    korPuffer[counter] = (byte)cBuff.Dequeue();
                    //    counter++;
                    //}

                    //_streamMemory.Write(korPuffer, 0, korPuffer.Length);

                    ////----kp



                    //_streamMemory.Write(bf1, 0, bf1.Length);


                    byte[] preBuffer = new byte[Properties.Settings.Default.SettingBitsPerSample];
                    _streamWave = WaveStream.CreateStream(_streamMemory, _waveFormat);

                    //preBuffer = new byte[_streamWave.Length - _streamWave.Position];
                    long wavStPos = 0;

                    if (_streamWave.Position - 3000 < 1)
                    {
                        wavStPos = 0;
                    }
                    else
                    {
                        wavStPos = _streamWave.Position - 3000;
                    }


                    preBuffer = new byte[wavStPos];

                    _streamWave.Read(preBuffer, 0, preBuffer.Length);




                    //----------------------------------------
                    //----------------------------------------

                    // FELVETT WAV (_streamWave)

                    byte[] waveBuffer = new byte[Properties.Settings.Default.SettingBitsPerSample];
                    // _streamWave = WaveStream.CreateStream(_streamMemory, _waveFormat);

                    waveBuffer = new byte[_streamWave.Length];
                    _streamWave.Read(waveBuffer, 0, waveBuffer.Length);

                    //----------------------------------------
                    //----------------------------------------

                    try
                    {
                        File.Delete(Properties.Settings.Default.SettingOutputPath + "\\" + signal_filename);
                    }
                    catch (Exception)
                    { }

                    try
                    {
                        if (Properties.Settings.Default.SettingOutputPath != "")
                            _streamFile = new FileStream(Properties.Settings.Default.SettingOutputPath + "\\" + _sampleFilename, FileMode.Create);
                        else
                            _streamFile = new FileStream(_sampleFilename, FileMode.Create);


                        _streamFile.Write(preBuffer, 0, preBuffer.Length);

                        _streamFile.Write(waveBuffer, 0, waveBuffer.Length);


                        if (_streamWave != null) { _streamWave.Close(); }
                        if (_streamFile != null) { _streamFile.Close(); }
                        _streamMemory = new MemoryStream();
                        _isSaving = false;

                        //throw new Exception("BR");

                        //cBuff.Clear();


                        //Invoke(new MethodInvoker(FileSavedEvent));

                    }
                    catch (Exception)
                    { }

                    //FileSavedEvent();

                    Invoke(new MethodInvoker(FileSavedEvent));
                          
                }
            }
        }


        private void AmplitudeEvent()
        {

            lab_strip_status.Text = "Utolsó esemény: " + _timeLastDetection.ToString();

        }


        private void FileSavedEvent()
        {

            StripSilence(working_dir_dat + signal_filename);

            if (CommandCheck(working_dir_dat + signal_filename))
            {                

                textBoxConsole.AppendText(_timeLastDetection.ToString() + " : " + _sampleFilename + " elmentve\r\n");

                //AnalyzeSignal(@"dat\" + signal_filename);

                AnalyzeSignal(working_dir_dat + signal_filename);

                CalcDTWDistances_ISIP();

                tb_wav_path.Text = working_dir_dat + signal_filename;

                //GetWavData(working_dir_dat + signal_filename);

            }
        }


        private bool CommandCheck(string signal_filepath)
        {
            FileInfo wav_file = new FileInfo(signal_filepath);
            //this.Text = wav_file.Length.ToString();


            if (wav_file.Length > 200000) //200kB, 8kHz
            {
                return false;
            }
            else
            {
                return true;
            }

            // Delete long inputs (should be an error)
            //if (wav_file.Length>300000)
            //{
            //    File.Delete(working_dir_dat + signal_filename);
            //}
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }


        private void StripSilence(string filepath)
        {
            try
            {
                if (File.Exists(filepath))
                {
                    clsWaveProcessor wa = new clsWaveProcessor();
                    //if (!wa.StripStartEndOptimized(working_dir_dat + signal_filename, false))
                    if (!wa.StripStartEndOptimized(filepath, false))
                    {
                        //MessageBox.Show("Levágás hiba...");
                    }
                    else
                    {
                        wa.StripStartEndOptimized(filepath, false);
                    }
                }
                else
                {
                    //MessageBox.Show(working_dir_dat+signal_filename+ " fájl hiányzik.");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }



        //-----


        private void btn_create_all_reference_Click(object sender, EventArgs e)
        {
            //THREAD SAFETY!
            //btn_create_all_reference.Enabled = false;
            //if (!bw_analyze.IsBusy)
            //{
            //    bw_analyze.RunWorkerAsync();
            //}
            //-----------------------------------------------

            string[] filePaths = Directory.GetFiles(Path.GetDirectoryName(working_dir_dat), "*.wav");

            foreach (string file in filePaths)
            {

                if (Path.GetFileName(file) != "signal.wav")
                {
                    AnalyzeSignal(file);
                }
            }
        }

        private void btn_record_ref_Click(object sender, EventArgs e)
        {
            if (_audioFrame.IsDetectingEvents)
            {
                _audioFrame.IsDetectingEvents = false;
                btn_record_ref.Text = "Felvétel kikapcsolva";
            }
            else
            {
                _audioFrame.IsDetectingEvents = true;
                btn_record_ref.Text = "Felvétel bekapcsolva";
            }


            Properties.Settings.Default.SettingIsDetectingEvents = _audioFrame.IsDetectingEvents;
            Properties.Settings.Default.Save();

            RefreshRecordState();
        }

        private void bw_analyze_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] filePaths = Directory.GetFiles(Path.GetDirectoryName(working_dir_dat), "*.wav");

            foreach (string file in filePaths)
            {

                if (Path.GetFileName(file) != "signal.wav")
                {
                    AnalyzeSignal(file);
                }
            }
        }

        private void bw_compare_DoWork(object sender, DoWorkEventArgs e)
        {
            CalcDTWDistances_ISIP();
        }

        private void bw_analyze_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btn_create_all_reference.Enabled = true;

            label4.Text = "OK";
            label5.Text = Path.GetFileName(signal_filename);
        }

        private void bw_compare_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btn_dtw_calc.Enabled = true;
        }

        private void cbox_engine_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbox_engine_mode.Text.ToUpper() == "LPC")
            {
                engine_mode = EngineMode.lpc;
            }
            if (cbox_engine_mode.Text.ToUpper() == "MFCC")
            {
                engine_mode = EngineMode.mfcc;
            }

            Properties.Settings.Default.SettingEngineMode = engine_mode.ToString().ToUpper();
            Properties.Settings.Default.Save();
        }



        //private void GetFFT()
        //{
        //    AP.Complex[] apc = new AP.Complex[hammingdata.Length];
        //    alglib.fft.fftr1d(ref hammingdata, hammingdata.Length, ref apc);
        //    fftdata = new double[apc.Length];

        //    for (int i = 0; i < apc.Length - 1; i++)
        //    {
        //        fftdata[i] = apc[i].x;
        //    }
        //}

        //public void GetFFT_FrameByFrame()
        //{

        //    win_fftdata = new double[num_of_frames, H_FELDOLGOZO.n];

        //    // ki kell szedni soronként 1 double tömbbe, FFT, aztán visszaírni

        //    for (int frame = 0; frame < num_of_frames; frame++)
        //    {
        //        double[] temp_frame_line = new double[H_FELDOLGOZO.n];

        //        for (int frame_item = 0; frame_item < H_FELDOLGOZO.n; frame_item++)
        //        {
        //            temp_frame_line[frame_item] = win_hammingdata[frame, frame_item];
        //        }

        //        // FFT keretenként

        //        AP.Complex[] apc = new AP.Complex[temp_frame_line.Length];
        //        alglib.fft.fftr1d(ref temp_frame_line, temp_frame_line.Length, ref apc);


        //        // keret visszaírása

        //        for (int frame_item = 0; frame_item < H_FELDOLGOZO.n; frame_item++)
        //        {
        //            win_fftdata[frame, frame_item] = apc[frame_item].x;
        //        }

        //        // továbblépés a következő keretre
        //    }

        //    //this.Text = win_fftdata[0,0].ToString();


        //    //for (int keret = 0; keret < keretek_szama; keret++)
        //    //{
        //    //    for (int keretelem = 0; keretelem < H_FELDOLGOZO.n; keretelem++)
        //    //    {

        //    //        //win_fftdata[keret, keretelem] = apc[keretelem].x;
        //    //    }
        //    //}
        //}



        //private void CalcDTWDistances()
        //{
        //    if (!dtw_ref_ready)
        //    {
        //        MessageBox.Show("Referencia nincs betöltve!");
        //        return;
        //    }

        //    lb_dtw_values.Items.Clear();


        //    //double[,] ref_array = new double[14, 14];

        //    //for (int i = 0; i < 14; i++)
        //    //{
        //    //    for (int j = 0; j < 14; j++)
        //    //    {
        //    //        ref_array[i, j] = 1;
        //    //    }
        //    //}


        //    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        //    sw.Reset();
        //    sw.Start();


        //    //H_FELDOLGOZO.referencia = ref_array;

        //    H_FELDOLGOZO.referencia = win_meldata;   // num of frames innen: btn_win_mfcc_allin1_Click

        //    //win_REF_meldata=DeSerializeArray(ofd_openwav.FileName + ".mfcc");

        //    H_FELDOLGOZO.counter_ref = num_of_frames;

        //    foreach (string fname in lb_mfcc_files.Items)
        //    {
        //        win_REF_meldata = DeSerializeArray("dat\\" + fname);

        //        int result = H_FELDOLGOZO.hasonlit(ref win_REF_meldata, Convert.ToInt32(win_REF_meldata.Length / 15));

        //        lb_dtw_values.Items.Add(result);

        //        this.Text = result.ToString();


        //        if (cb_disp_dtw_data.Checked)
        //        {
        //            H_FELDOLGOZO.Show2dArray(H_FELDOLGOZO.ertomb, win_REF_meldata.Length / 15);
        //        }

        //        //this.Text = result.ToString();

        //        //MessageBox.Show(result.ToString());
        //    }

        //    //win_REF_meldata = DeSerializeArray(lb_mfcc_files.Items[0].ToString());

        //    //int result = H_FELDOLGOZO.hasonlit(ref win_REF_meldata,num_of_frames);

        //    //MessageBox.Show(result.ToString());

        //    sw.Stop();

        //    this.Text = "DTW összehasonlítások az összes referenciára: " + sw.ElapsedMilliseconds.ToString() + " ms";

        //    // elemezni a tömb dimenzióit, funkciókat

        //    // a referenciákat tárolni kell valami fájlban, aztán beolvasni

        //    // DTW algoritmus futtatása: 1. megnyitott WAV fájl és 10 másik REFERENCIA

        //    //lb_dtw_data.Items.Add(H_FELDOLGOZO.hasonlit(ref ref_array, ref_array.Length).ToString());
        //}



        //private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        //{
        //    notifyIcon1.Visible = false;
        //    this.Visible = true;
        //    this.ShowInTaskbar = true;
        //    this.WindowState = FormWindowState.Normal;
        //    _isShown = true;
        //}
        //private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    FormAboutDialog form = new FormAboutDialog();
        //    form.Show();
        //}
        //private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    //this.Close();
        //    Application.Exit();

        //}
        //private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    FormOptionsDialog form = new FormOptionsDialog();
        //    if (form.ShowDialog() == DialogResult.OK)
        //    {
        //        _audioFrame.IsDetectingEvents = form.IsDetectingEvents;
        //        _audioFrame.AmplitudeThreshold = form.AmplitudeThreshold;
        //    }
        //}


        //----09-07-----


        private void btn_mfcc_allin1_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    if (ofd_openwav.ShowDialog() == DialogResult.OK)
            //    {
            //        lb_mfcc_data.Items.Clear();

            //        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //        sw.Reset();
            //        sw.Start();

            //        WAVFile wfile = new WAVFile();
            //        wfile.Open(ofd_openwav.FileName, WAVFile.WAVFileMode.READ);
            //        pcmdata = new double[wfile.NumSamples];


            //        int fpos = 0;
            //        while (wfile.FilePosition != wfile.FileSizeBytes)
            //        {
            //            pcmdata[fpos] = wfile.GetNextSampleAs16Bit();
            //            fpos++;
            //        }

            //        wfile.Close();

            //        firdata = H_FELDOLGOZO.fir_filter(pcmdata);

            //        hammingdata = H_FELDOLGOZO.hamming_ablak(firdata);

            //        AP.Complex[] apc = new AP.Complex[hammingdata.Length];
            //        alglib.fft.fftr1d(ref hammingdata, hammingdata.Length, ref apc);
            //        fftdata = new double[apc.Length];

            //        for (int i = 0; i < apc.Length - 1; i++)
            //        {
            //            fftdata[i] = apc[i].x;
            //        }

            //        meldata = new double[15];  // mfcc 0-14

            //        CV_FELDOLGOZO.init_mel_filter_banks();
            //        CV_FELDOLGOZO.InitMelFilterFrame(fftdata, ref meldata);

            //        sw.Stop();

            //        foreach (float mel_dat in meldata)
            //        {
            //            lb_mfcc_data.Items.Add(mel_dat.ToString());
            //        }

            //        this.Text = "Utolsó MFCC elemezés: " + sw.ElapsedMilliseconds.ToString() + " ms";

            //    }
            //}
            //catch (Exception ex)
            //{
            //    lab_strip_status.Text = ex.Message;
            //}
        }


        private void btn_save_mfcc_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    if (sfd_save_mfcc.ShowDialog() == DialogResult.OK)
            //    {
            //        if (meldata == null)
            //        {
            //            lab_strip_status.Text = "Üres az MFCCDATA tömb!";
            //            return;
            //        }

            //        TextWriter tw = new StreamWriter(sfd_save_mfcc.FileName);

            //        foreach (string item in lb_mfcc_data.Items)
            //        {
            //            tw.WriteLine(item);
            //        }

            //        tw.Close();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    lab_strip_status.Text = ex.Message;
            //}


        }


    }
}


