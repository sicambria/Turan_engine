/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/


/***************************************************************************
                              Turan_SC example
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

namespace Turan_SC_minimal
{
    public partial class Form1 : Form
    {
        Turan_SC.Recording trec;
        private delegate void SetGUI();
        static string working_dir_dat = Application.StartupPath + @"\dat\";
        string signal_filename = "signal.wav";


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(working_dir_dat);

            if (trec == null)
            {
                trec = new Turan_SC.Recording();
                trec.SetRecordingThreshold(8000);
                trec.SetSamplesPerSecond(16000);
                trec.TurnOnAndSave(); // Turn on voice activated recording
                trec.Signal_filename_f = signal_filename; // Set output filename
                // Define an event handler that is called after the recording is done
                trec.AudioEventDetected += new Recording.AudioEventDetectedHandler(soundDetected);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Shut down Turan_SC
            if (trec != null)
            {
                trec.StopRecording();
            }
        }

        public void soundDetected()
        {
            StripSilence();
            label1.Invoke(new SetGUI(GUIMuvelet));            
        }

        private void StripSilence()
        {
            try
            {
                if (File.Exists(working_dir_dat+signal_filename))
                {
                    clsWaveProcessor wa = new clsWaveProcessor();
                    //if (!wa.StripStartEndOptimized(working_dir_dat + signal_filename, false))
                    if (!wa.StripStartEndOptimized(working_dir_dat + signal_filename, false))
                    {
                        //MessageBox.Show("Levágás hiba...");
                    }
                    else
                    {
                        wa.StripStartEndOptimized(working_dir_dat+signal_filename, false);
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

        private void CommandCheck()
        {
            FileInfo wav_file = new FileInfo(working_dir_dat + signal_filename);
            this.Text = wav_file.Length.ToString();
                       
            // Delete long inputs (should be an error)
            //if (wav_file.Length>300000)
            //{
            //    File.Delete(working_dir_dat + signal_filename);
            //}
        }


        private void GUIMuvelet()
        {
            // Form manipulating commands in a thread-safe way
            label1.Text = "Last event: " + DateTime.Now.ToString();
            label2.Text = "Sampling frequency: " + trec.GetSamplesPerSecond().ToString() + " Hz";
            label3.Text = "Recording threshold: " + trec.GetRecordingThreshold().ToString();


            CommandCheck();


        }

    }
}
