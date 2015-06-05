/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/


/***************************************************************************
                              Turan_tester
                             -------------------
    begin                : October 2010   
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
using System.IO;
using Turan_SC;
using Turan_core;
using Turan_creator;
using System.Diagnostics;

namespace Turan_tester
{
    public partial class Form_tester : Form
    {
        #region Variables Declaration

        Turan_SC.Recording trec;
        private delegate void SetGUI();
        static string working_dir_dat = Application.StartupPath + "\\dat\\";
        string signal_filename = "signal.wav";
        string signal_vector_filename = "signal.mfc3";
        double[] volumeArray = new double[80];

        int word_recognized = -1;


        List<string> files = new List<string>();
        List<string> active_files = new List<string>();
        List<double> score_list = new List<double>();
        

        #endregion


        public Form_tester()
        {
            InitializeComponent();
        }

        private void Form_tester_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(working_dir_dat);

            if (trec == null)
            {
                trec = new Turan_SC.Recording();
                trec.Signal_filename_f = signal_filename;
                trec.TurnOnAndSave();
                trec.AudioEventDetected += new Recording.AudioEventDetectedHandler(soundDetected);

                int samplerate = Properties.Settings.Default.DefSampleRate;
                int threshold = Properties.Settings.Default.AmplitudeThreshold;

                trec.SetSamplesPerSecond(samplerate);
                trec.SetRecordingThreshold(threshold);

            }

            fillFileList();
        }

        public void soundDetected()
        {

            string[] signal = new string[1];
            signal[0] = working_dir_dat + signal_filename;

            Turan_creator.Creator.Application_path = "dat\\";
            Turan_creator.Creator.CalculateFeatureVectors(signal);


            //string[] lpc_temp_paths = Directory.GetFiles(Path.GetDirectoryName(working_dir_dat), "*.lpc");

            int num_active_files = active_files.Count;
            string[] lpc_temp_paths = new string[num_active_files];

            for (int i = 0; i < num_active_files; i++)
            {
                //lpc_temp_paths[i] = working_dir_dat + active_files[i];
                lpc_temp_paths[i] = working_dir_dat + listBox_active.Items[i];
            }

            try
            {
                Turan_core.Engine tengine = new Engine(Engine.EngineMode.mfcc, Engine.VectorFileFormat.htk);
                doRecognizedAction(tengine.RecognizeAndReturnIndex
                    (working_dir_dat + signal_vector_filename, lpc_temp_paths));
                score_list = tengine.GetScoreList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            label1.Invoke(new SetGUI(GUIMuvelet));
        }

        public void doRecognizedAction(int number)
        {
            label1.Invoke(new SetGUI(GUIRecognized));
            word_recognized = number;
        }

        private void GUIRecognized()
        {
            listBox_score.Items.Clear();
            this.Text = word_recognized.ToString();

            foreach (double score in score_list)
            {
                listBox_score.Items.Add(score.ToString());
            }
        }

        private void GUIMuvelet()
        {
            // Show latest event
            toolStripStatusLabel1.Text = "Last event: " + DateTime.Now.ToString();
        }

        private void Form_tester_FormClosing(object sender, FormClosingEventArgs e)
        {
            trec.StopRecording();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (trec.IsRecordingActive())
            {
                trec.TurnOffAndSave();
                btn_rec_on_off.Text = "VOICE ACTIVATED RECORDING OFF";
            }
            else
            {
                trec.TurnOnAndSave();
                btn_rec_on_off.Text = "VOICE ACTIVATED RECORDING ON";
            }
        }

        private void btn_analyze_active_Click(object sender, EventArgs e)
        {
            string[] filePaths = Directory.GetFiles(Path.GetDirectoryName(working_dir_dat), "*.wav");
            //Turan_creator.Creator.No_overlap_lpc_f = false;

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            Turan_creator.Creator.Application_path = "dat\\";
            
            try
            {
                Turan_creator.Creator.CalculateFeatureVectors(filePaths);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); ;
            }
            sw.Stop();
            toolStripStatusLabel1.Text = "Elemzés időtartama: " + sw.ElapsedMilliseconds.ToString() + " ms";

            fillFileList();
        }

        void fillFileList()
        {
            files.Clear();
            active_files.Clear();
            listBox_files.Items.Clear();
            listBox_active.Items.Clear();

            string[] lpc_filePaths = new string[1];

            if (Turan_creator.Creator.Engine_mode_f == Turan_creator.Creator.EngineMode.lpc)
            {
                lpc_filePaths = Directory.GetFiles(Path.GetDirectoryName(working_dir_dat), "*.lpc");
            }

            if (Turan_creator.Creator.Engine_mode_f == Turan_creator.Creator.EngineMode.mfcc)
            {
                lpc_filePaths = Directory.GetFiles(Path.GetDirectoryName(working_dir_dat), "*.mfc3");
            }

            foreach (string file_path in lpc_filePaths)
            {
                listBox_files.Items.Add(Path.GetFileName(file_path));
                files.Add(Path.GetFileName(file_path));
            }
        }

        private void btn_add_to_active_Click(object sender, EventArgs e)
        {
            try
            {
                active_files.Add(listBox_files.SelectedItem.ToString());
                listBox_active.Items.Add(listBox_files.SelectedItem.ToString());

                files.Remove(listBox_files.SelectedItem.ToString());
                listBox_files.Items.Remove(listBox_files.SelectedItem.ToString());
            }
            catch (Exception)
            { }

        }

        private void btn_remove_from_active_Click(object sender, EventArgs e)
        {
            try
            {
                files.Add(listBox_active.SelectedItem.ToString());
                listBox_files.Items.Add(listBox_active.SelectedItem.ToString());

                active_files.Remove(listBox_active.SelectedItem.ToString());
                listBox_active.Items.Remove(listBox_active.SelectedItem.ToString());
            }
            catch (Exception)
            { }
        }

        private void listBox_files_DoubleClick(object sender, EventArgs e)
        {
            btn_add_to_active_Click(sender, e);
        }

        private void listBox_active_DoubleClick(object sender, EventArgs e)
        {
            btn_remove_from_active_Click(sender, e);
        }

        private void btn_remove_file_Click(object sender, EventArgs e)
        {
            try
            {
                files.Remove(listBox_files.SelectedItem.ToString());
                listBox_files.Items.Remove(listBox_files.SelectedItem.ToString());
            }
            catch (Exception)
            { }
        }

        private void btn_add_all_to_active_Click(object sender, EventArgs e)
        {
            foreach (string file in files)
            {
                active_files.Add(file);
                listBox_active.Items.Add(file);
            }
            files.Clear();
            listBox_files.Items.Clear();
        }

        private void btn_remove_all_from_active_Click(object sender, EventArgs e)
        {
            foreach (string act_file in active_files)
            {
                files.Add(act_file);
                listBox_files.Items.Add(act_file);
            }
            active_files.Clear();
            listBox_active.Items.Clear();
        }
    }
}
