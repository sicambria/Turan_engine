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

namespace Turan_GUI
{
    public partial class TrainerSettings : Form
    {
        public int AmplitudeThreshold;
        public int SampleFrequency;

        public TrainerSettings()
        {
            InitializeComponent();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            AmplitudeThreshold = trackb_treshold.Value;
            lab_treshold.Text = trackb_treshold.Value.ToString();            
        }

        private void btn_ok_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DefSampleRate = SampleFrequency;
            Properties.Settings.Default.AmplitudeThreshold = AmplitudeThreshold;       
            Properties.Settings.Default.Save();
            this.Dispose();
            Application.Restart();
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

     
        private void TrainerSettings_Load(object sender, EventArgs e)
        {
            AmplitudeThreshold = Properties.Settings.Default.AmplitudeThreshold;
            trackb_treshold.Value = AmplitudeThreshold;
            lab_treshold.Text = AmplitudeThreshold.ToString();

            SampleFrequency = Properties.Settings.Default.DefSampleRate;
            combo_freq.Text = SampleFrequency.ToString();
            
        }

        private void combo_freq_SelectedIndexChanged(object sender, EventArgs e)
        {
            SampleFrequency = Int32.Parse(combo_freq.Text);
        }
    }
}
