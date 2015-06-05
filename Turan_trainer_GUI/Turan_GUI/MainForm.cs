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

namespace Turan_GUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lab_explain.Text = "" +
                "A betanítás során felvételre kerülnek azok a hangminták,\r\n" +
                "melyek a rendszer vezérlését teszik lehetővé.\r\n\n" +
                "A tanítást lehetőleg a felhasználás helyén végezzük,\r\n" +
                "és kerüljük a nagy háttérzajt. A program segít az \r\n" +
                "optimális felvételi hangerő beállításában is.";

            toolStripStatusLabel1.Text = "Aktív profil: " + Properties.Settings.Default.ProfileName;
        }

        private void pb_numbers_Click(object sender, EventArgs e)
        {
            UserProfile profile = new UserProfile();
            profile.ShowDialog();
        }

        private void pb_commands_Click(object sender, EventArgs e)
        {
            Train train_commands = new Train();
            train_commands.ShowDialog();
        }

        private void pb_settings_Click(object sender, EventArgs e)
        {
            TrainerSettings train_settings = new TrainerSettings();
            train_settings.ShowDialog();
        }
    }
}
