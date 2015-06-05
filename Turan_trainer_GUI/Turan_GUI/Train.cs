/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/


/***************************************************************************
             Turan_GUI  -  GUI to create sound samples for the Turan engine
                             -------------------
    begin                : September 2010   
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
using Turan_SC;
using System.IO;
using System.Threading;
using FMOD;
using System.Runtime.InteropServices;
using System.Media;

namespace Turan_GUI
{

    public partial class Train : Form
    {
        #region Variables Declaration

        Turan_SC.Recording trec;
        private delegate void SetGUI();
        static string working_dir_dat = Application.StartupPath + @"\dat\";
        //string signal_filename = "signal.wav";

        static string commandlist_fname = "commands.txt";
        double[] volumeArray = new double[80];

        int currentword_index = 0;

        static string[,] wordsToTrain = new string[200, 2]; // MAX 200 commands
        static int num_of_commands = 0;




        public enum CommandType
        {
            number,
            command,
            confirm,
            custom
        }

        #endregion

        #region fmod_variables

        private FMOD.System system = null;
        private FMOD.Sound sound = null;
        private FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();
        //private FMOD.RESULT result;
        private FMOD.OUTPUTTYPE output;
        private FMOD.Channel channel = null;
        private FMOD.ChannelGroup channelgroup = null;
        private FMOD.DSP dsp = null;
        private FMOD.DSPConnection dspconnection = null;
        private FMOD.DSP_DESCRIPTION dspdesc = new FMOD.DSP_DESCRIPTION();
        private float[] WAVEDATA = new float[512];
        private float max_ch1 = 0.0f;

        #endregion

        public Train()
        {
            InitializeComponent();
            Directory.CreateDirectory(working_dir_dat);

            if (trec == null)
            {
                trec = new Turan_SC.Recording();
                trec.TurnOffAndSave();
                trec.AudioEventDetected += new Recording.AudioEventDetectedHandler(soundDetected);

                int samplerate = Properties.Settings.Default.DefSampleRate;
                int treshold = Properties.Settings.Default.AmplitudeThreshold;

                trec.SetSamplesPerSecond(samplerate);
                trec.SetRecordingThreshold(treshold);

            }
        }

        List<string> FillNumberList()
        {
            List<string> numList = new List<string>();

            for (int i = 0; i < 10; i++)
            {
                numList.Add(i.ToString() + ".wav");
            }

            return numList;
        }

        public void SetWordToTrain(int index)
        {
            if (wordsToTrain[index, 0] != null)
            {
                trec.Signal_filename_f = wordsToTrain[index, 0];
                lab_current_word.Text = "Tanított minta: " + wordsToTrain[index, 1];
            }

        }


        //List<string[,]> FillAALCommandFileNameList()
        string[,] FillAALCommandFileNameList()
        {
            //List<string> aalCommands = new List<string>();
            string[,] aalFileNames = new string[30, 2];



            return aalFileNames;
        }



        private void ActivateRecording()
        {

            btn_rec_start.Text = "Felvétel hangra indul";
            toolStripStatusLabel1.Text = "Felvétel hangra indul";
            btn_rec_start.BackColor = Color.Green;
            lab_speaktomic.Visible = true;
            lab_saylab.Visible = true;
            //lab_speaktomic.Text = "be + " + wordsToTrain[currentword_index, 1];
            lab_speaktomic.Text = wordsToTrain[currentword_index, 1];
            lab_current_word.Visible = true;
            lab_volume_cap.ForeColor = Color.Green;
            btn_back.Enabled = true;
            btn_foward.Enabled = true;
            trec.TurnOnAndSave();


            //if (trec == null)
            //{
            //    trec = new Turan_SC.Recording();
            //    trec.AudioEventDetected += new Recording.AudioEventDetectedHandler(soundDetected);
            //}

            //volumeTrackerTimer.Enabled = true;

        }


        public void soundDetected()
        {
            StripSilence();
            lab_speaktomic.Invoke(new SetGUI(GUIMuvelet));

            //try
            //{
            //pb_cover.Invoke(new SetGUI(GUIMuvelet));
            //}
            //catch (Exception) { }

        }


        private void GUIMuvelet()
        {
            lab_speaktomic.Visible = false;
            toolStripStatusLabel1.Text = DateTime.Now.ToString();
            btn_rec_start.Text = "Újra felvesz";
            trec.TurnOff();
            EnableButtonsIfFileExists();
        }

        private void szalMuveletVegzo(string message)
        {

        }

        private void Train_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (trec != null)
            {
                trec.StopRecording();
            }
        }

        private void btn_rec_start_Click(object sender, EventArgs e)
        {
            ActivateRecording();
            EnableButtonsIfFileExists();
        }

        private void btn_home_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Megszakítsuk a tanítást?", "Megerősítés", MessageBoxButtons.YesNo) ==
                DialogResult.Yes)
            {
                this.Dispose();
            }

        }



        private void Train_Load(object sender, EventArgs e)
        {
            try
            {
                TextReader commands = new StreamReader(commandlist_fname);

                int index = 0;
                string temp = "";
                string[] temp2 = new string[2];

                while (commands.Peek() >= 0)
                {
                    temp = commands.ReadLine();
                    if (temp.Substring(0, 1) == "*")
                    {
                        // skip lines starting with *
                    }
                    else
                    {
                        temp2 = temp.Split(';');

                        wordsToTrain[index, 0] = temp2[0]; // filename
                        wordsToTrain[index, 1] = temp2[1]; // description
                        index++;
                    }
                }
                SetWordToTrain(currentword_index);
                lab_current_word.Text = "Tanított minta: " + wordsToTrain[currentword_index, 1];

                progress_train.Maximum = index - 1;
                num_of_commands = index;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            //-- FMOD VolumeMeter ELEJE --//
            FMOD.RESULT result;
            result = FMOD.Factory.System_Create(ref system);
            /* lekérjük a hangkártya kimeneteket */
            StringBuilder drivername = new StringBuilder(256);
            result = system.setOutput(FMOD.OUTPUTTYPE.DSOUND);  //beállítjuk a DirectSound-ot kimenetnek
            result = system.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)null);
            exinfo.cbsize = Marshal.SizeOf(exinfo);
            exinfo.numchannels = 2;
            exinfo.format = FMOD.SOUND_FORMAT.PCM16;
            exinfo.defaultfrequency = 44100;
            exinfo.length = (uint)(exinfo.defaultfrequency * 2 * exinfo.numchannels * 2);
            /* csinalunk egy 1 csatornás DSP-t a hangero levetelere */
            dspdesc.channels = 1;
            result = system.createDSP(ref dspdesc, ref dsp);
            result = system.addDSP(dsp, ref dspconnection);
            result = dsp.setActive(true);
            result = dsp.setBypass(true);
            /* létrehozzuk a hangfolyamot */
            system.createSound((string)null, (FMOD.MODE.LOOP_NORMAL | FMOD.MODE.SOFTWARE | FMOD.MODE._2D | FMOD.MODE.OPENUSER), ref exinfo, ref sound);
            result = system.recordStart(0, sound, true);    //elindul a felvétel ami majd átirányítunk a DSP láncba

            system.getOutput(ref output);
            if (output != FMOD.OUTPUTTYPE.ASIO)
            {
                Thread.Sleep(50);       //50ms-ot kell várni a felvétel és a lejátszás között
            }

            dsp.setBypass(false);   //DSP kikapcs
            system.playSound(FMOD.CHANNELINDEX.FREE, sound, false, ref channel);    //lejátszás elindít

            channel.getDSPHead(ref dsp);
            dsp.getInput(1, ref dsp, ref dspconnection);    //a 1-es eszközről indul az adatfolyam a DSP-be (mikrofon)
            dspconnection.setMix(0.0f);    //lekeverjük a hangerőt (gyk. lenémítjuk a lejátszandó adatfolyamot, így csak az amplitudoját elmezzuk majd)

            channelgroup = new ChannelGroup();
            system.createChannelGroup("00ch", ref channelgroup);    //létrehozunk a system-hez egy csatorna csoportot
            channel.setChannelGroup(channelgroup);      //hozzákapcsoljuk a channel kimenetét a channelgroup-hoz, ezt fogja elemezni a timer amplitudo szempontjából
            /*-- FMOD VolumeMeter VÉGE */
        }

        private void btn_foward_Click(object sender, EventArgs e)
        {
            if (currentword_index < num_of_commands - 1)
            {
                currentword_index++;
                SetWordToTrain(currentword_index);
                //lab_speaktomic.Text = "be + " + wordsToTrain[currentword_index, 1];
                lab_speaktomic.Text = wordsToTrain[currentword_index, 1];
                progress_train.Value++;
                ActivateRecording();
                EnableButtonsIfFileExists();

            }
        }

        private void btn_back_Click(object sender, EventArgs e)
        {
            if (currentword_index > 0)
            {
                currentword_index--;
                SetWordToTrain(currentword_index);
                lab_speaktomic.Text = wordsToTrain[currentword_index, 1];
                progress_train.Value--;
                ActivateRecording();
                EnableButtonsIfFileExists();
            }

        }

        void EnableButtonsIfFileExists()
        {
            if (File.Exists(working_dir_dat + wordsToTrain[currentword_index, 0]))
            {
                btn_play.Enabled = true;
                btn_delete.Enabled = true;
            }
            else
            {
                btn_play.Enabled = false;
                btn_delete.Enabled = false;
            }
        }

        private void btn_play_Click(object sender, EventArgs e)
        {
            try
            {
                SoundPlayer simpleSound = new SoundPlayer(working_dir_dat + wordsToTrain[currentword_index, 0]);
                simpleSound.Play();
            }
            catch (Exception)
            { }
        }

        private void FMOD_REC_VOLUME_Tick(object sender, EventArgs e)
        {
            /* FMOD VMETER ELEJE */
            if (system != null && sound != null)
            {
                max_ch1 = 0.0f;

                channelgroup.getWaveData(WAVEDATA, 512, 0);  //512
                for (int i = 0; i < 512; i++)    //megkeressuk a maximális spektrumértéket a progressbar maximumának
                {
                    if (max_ch1 < Math.Abs(WAVEDATA[i]))
                        max_ch1 = Math.Abs(WAVEDATA[i]);
                }
                progressBar1.Value = (int)(max_ch1 * 100);

                if (max_ch1 * 100 > 95)
                {
                    lab_volume_cap.ForeColor = Color.Red;
                }

                //if (progressBar1.Value > 94)
                //{
                //    lab_vol_ok.Text = "Túl hangos";
                //    lab_vol_ok.ForeColor = Color.Red;
                //}
                //else
                //{
                //    lab_vol_ok.Text = "_";
                //    lab_vol_ok.ForeColor = Color.Black;
                //}
                //if (progressBar1.Value > 20 && progressBar1.Value <= 80)
                //{
                //    lab_vol_ok.Text = "Hangerő rendben";
                //    lab_vol_ok.ForeColor = Color.Green;
                //}

            }
            if (system != null)
            {
                system.update();
            }
            /* FMOD VMETER VEGE */
        }







        private void volumeTrackerTimer_Tick(object sender, EventArgs e)
        {
            //label2.Text = trec.GetCurrentAmplitude().ToString();
            ////bar_rec_volume.Value = (int)trec.GetCurrentAmplitude();
            //if (vol_arr_index < volumeArray.Length)
            //{
            //    volumeArray[vol_arr_index++] = (int)trec.GetCurrentAmplitude();
            //}
            //else
            //{
            //    double sum_amp = 0.0;
            //    for (int i = 0; i < volumeArray.Length; i++)
            //    {
            //        sum_amp += volumeArray[i];
            //    }
            //    vol_arr_index = 0;
            //    int avg_amp = (int)(sum_amp / volumeArray.Length);
            //    bar_rec_volume.Value = avg_amp;
            //    label3.Text = (sum_amp / volumeArray.Length).ToString();


            //    if (avg_amp < 500)
            //    {
            //        lab_vol_ok.Text = ""; //halk
            //    }
            //    else if (avg_amp > 2000)
            //    {
            //        lab_vol_ok.Text = ""; //hangos
            //    }
            //    else
            //    {
            //        lab_vol_ok.Text = "rendben";
            //    }

            //}

        }

        private void btn_delete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Töröljük ezt a hangot?\n" + wordsToTrain[currentword_index, 0], "Törlés", MessageBoxButtons.YesNo) ==
              DialogResult.Yes)
            {
                File.Delete(working_dir_dat + wordsToTrain[currentword_index, 0]);
                EnableButtonsIfFileExists();
            }
        }


        private void StripSilence()
        {
            try
            {
                if (File.Exists(working_dir_dat + wordsToTrain[currentword_index, 0]))
                {
                    clsWaveProcessor wa = new clsWaveProcessor();
                    if (!wa.StripStartEndOptimized(working_dir_dat + wordsToTrain[currentword_index, 0], false))
                    {
                        //MessageBox.Show("Levágás hiba...");
                    }
                    else
                    {
                        wa.StripStartEndOptimized(working_dir_dat + wordsToTrain[currentword_index, 0], false);
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


    }
}




//----------------------------------




//WaveIn waveIn;
//SampleAggregator sampleAggregator;
//UnsignedMixerControl volumeControl;
//double desiredVolume = 100;
//RecordingState recordingState;
//WaveFileWriter writer;
//WaveFormat recordingFormat;

//public event EventHandler Stopped = delegate { };

//public void AudioRecorder()
//{
//    sampleAggregator = new SampleAggregator();
//    RecordingFormat = new WaveFormat(8000, 1);
//}

//public WaveFormat RecordingFormat
//{
//    get
//    {
//        return recordingFormat;
//    }
//    set
//    {
//        recordingFormat = value;
//        sampleAggregator.NotificationCount = value.SampleRate / 10;
//    }
//}

//public void BeginMonitoring(int recordingDevice)
//{
//    if (recordingState != RecordingState.Stopped)
//    {
//        throw new InvalidOperationException("Can't begin monitoring while we are in this state: " + recordingState.ToString());
//    }
//    waveIn = new WaveIn();
//    waveIn.DeviceNumber = recordingDevice;
//    //waveIn.DataAvailable += waveIn_DataAvailable;
//    waveIn.RecordingStopped += new EventHandler(waveIn_RecordingStopped);
//    waveIn.WaveFormat = recordingFormat;
//    waveIn.StartRecording();
//    TryGetVolumeControl();
//    recordingState = RecordingState.Monitoring;
//}

//void waveIn_RecordingStopped(object sender, EventArgs e)
//{
//    recordingState = RecordingState.Stopped;
//    writer.Dispose();
//    Stopped(this, EventArgs.Empty);
//}

//public void BeginRecording(string waveFileName)
//{
//    if (recordingState != RecordingState.Monitoring)
//    {
//        throw new InvalidOperationException("Can't begin recording while we are in this state: " + recordingState.ToString());
//    }
//    writer = new WaveFileWriter(waveFileName, recordingFormat);
//    recordingState = RecordingState.Recording;
//}

//public void Stop()
//{
//    if (recordingState == RecordingState.Recording)
//    {
//        recordingState = RecordingState.RequestedStop;
//        waveIn.StopRecording();
//    }
//}

//private void TryGetVolumeControl()
//{
//    int waveInDeviceNumber = waveIn.DeviceNumber;
//    if (Environment.OSVersion.Version.Major >= 6) // Vista and over
//    {
//        var mixerLine = new MixerLine((IntPtr)waveInDeviceNumber, 0, MixerFlags.WaveIn);
//        foreach (var control in mixerLine.Controls)
//        {
//            if (control.ControlType == MixerControlType.Volume)
//            {
//                volumeControl = control as UnsignedMixerControl;
//                MicrophoneLevel = desiredVolume;
//                break;
//            }
//        }
//    }
//    else
//    {
//        var mixer = new Mixer(waveInDeviceNumber);
//        foreach (var destination in mixer.Destinations)
//        {
//            if (destination.ComponentType == MixerLineComponentType.DestinationWaveIn)
//            {
//                foreach (var source in destination.Sources)
//                {
//                    if (source.ComponentType == MixerLineComponentType.SourceMicrophone)
//                    {
//                        foreach (var control in source.Controls)
//                        {
//                            if (control.ControlType == MixerControlType.Volume)
//                            {
//                                volumeControl = control as UnsignedMixerControl;
//                                MicrophoneLevel = desiredVolume;
//                                break;
//                            }
//                        }
//                    }
//                }
//            }
//        }
//    }

//}

//public double MicrophoneLevel
//{
//    get
//    {
//        return desiredVolume;
//    }
//    set
//    {
//        desiredVolume = value;
//        if (volumeControl != null)
//        {
//            volumeControl.Percent = value;
//        }
//    }
//}




//public class FileNameStruct
//{
//    string filename;
//    string name;

//    public FileNameStruct(string filename, string name)
//    {
//        this.filename = filename;
//        this.name = name;
//    }

//}


//struct FileList
//{            
//    string filename;
//    string name;

//}



