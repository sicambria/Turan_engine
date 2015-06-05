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
using System.IO;

namespace Turan_GUI
{
    public partial class UserProfile : Form
    {
        static string[,] profiles = new string[50, 2]; // MAX 50 profiles
        static string working_dir_dat = Application.StartupPath + "\\dat\\";
        static string profile_list_fname = "profiles.txt";
        int num_of_profiles = 0;

        public UserProfile()
        {
            InitializeComponent();
        }

        private void btn_newprofile_Click(object sender, EventArgs e)
        {
            if (tb_username.Text != "")
            {
                try
                {
                    //string clean_dir_name = tb_username.Text.Trim();
                    Directory.CreateDirectory(working_dir_dat + tb_username.Text);
                    Properties.Settings.Default.ProfileName = tb_username.Text;
                    Properties.Settings.Default.ProfileDir = tb_username.Text;
                    Properties.Settings.Default.Save();

                    profiles[profiles.Length, 0] = tb_username.Text;
                    profiles[profiles.Length, 1] = tb_username.Text;
                    SaveProfileList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Meg kell adnod az új profil nevét.\n(Ne használj speciális karaktereket.)");
            }

        }

        private void btn_select_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(working_dir_dat + combo_users.SelectedItem.ToString()))
            {
                Properties.Settings.Default.ProfileName = combo_users.SelectedItem.ToString();
                Properties.Settings.Default.ProfileDir = combo_users.SelectedItem.ToString();
                Properties.Settings.Default.Save();
            }
            else
            {
                Directory.CreateDirectory(working_dir_dat + combo_users.SelectedItem.ToString());
                Properties.Settings.Default.ProfileName = combo_users.Text;
                Properties.Settings.Default.ProfileDir = combo_users.Text;
                Properties.Settings.Default.Save();
                MessageBox.Show("Új profilmappa létrehozva!");
            }

            this.Dispose();
            Application.Restart();
        }

        private void UserProfile_Load(object sender, EventArgs e)
        {
            try
            {
                combo_users.Text = Properties.Settings.Default.ProfileName;

                TextReader profile_list = new StreamReader(profile_list_fname);

                int index = 0;
                string temp = "";
                string[] temp2 = new string[2];

                while (profile_list.Peek() >= 0)
                {
                    temp = profile_list.ReadLine();
                    if (temp.Substring(0, 1) == "*")
                    {
                        // skip lines starting with *
                    }
                    else
                    {
                        temp2 = temp.Split(';');

                        profiles[index, 0] = temp2[0]; // profile directory
                        profiles[index, 1] = temp2[1]; // description

                        combo_users.Items.Add(temp2[1]);


                        index++;
                    }
                }
                num_of_profiles = index;
                combo_users.Text = profiles[0, 1];
                profile_list.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //int GetUserCount()
        //{
        //    TextReader profile_list = new StreamReader(profile_list_fname);

        //    int index = 0;
        //    string temp = "";

        //    while (profile_list.Peek() >= 0)
        //    {
        //        temp = profile_list.ReadLine();
        //        index++;
        //    }
        //    profile_list.Close();

        //    return index;
        //}

        void SaveProfileList()
        {
            TextWriter profile_list = new StreamWriter(profile_list_fname);

            for (int i = 0; i < num_of_profiles; i++)
            {
                if (profiles[i, 0] != null)
                {
                    profile_list.WriteLine(profiles[i, 0] + ";" + profiles[i, 1]);
                }
            }

            profile_list.Close();

            this.Dispose();
            Application.Restart();

        }

    }
}
