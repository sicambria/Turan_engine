using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;	// for PlaySound()
using Microsoft.Win32;
using System.Data;	// RegistryKey

namespace Turan_GUI
{
    class PlayWav
    {
        private RegistryKey key1;
        private RegistryKey key2;
        //private PropertyCollection events;

        // PlaySound()
        [DllImport("winmm.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        static extern bool PlaySound(string pszSound,
            IntPtr hMod, SoundFlags sf);

        [Flags]
        public enum SoundFlags : int
        {
            SND_SYNC = 0x0000,  /* play synchronously (default) */
            SND_ASYNC = 0x0001,  /* play asynchronously */
            SND_NODEFAULT = 0x0002,  /* silence (!default) if sound not found */
            SND_MEMORY = 0x0004,  /* pszSound points to a memory file */
            SND_LOOP = 0x0008,  /* loop the sound until next sndPlaySound */
            SND_NOSTOP = 0x0010,  /* don't stop any currently playing sound */
            SND_NOWAIT = 0x00002000, /* don't wait if the driver is busy */
            SND_ALIAS = 0x00010000, /* name is a registry alias */
            SND_ALIAS_ID = 0x00110000, /* alias is a predefined ID */
            SND_FILENAME = 0x00020000, /* name is file name */
            SND_RESOURCE = 0x00040004  /* name is resource name or atom */
        }

        public static void Play(string filename)
        {
            int err = 0;	// last error

            try
            {
                // play the sound from the selected filename
                //if (!PlaySound(tbFileName.Text, IntPtr.Zero,
                if (!PlaySound(filename, IntPtr.Zero,
                    SoundFlags.SND_FILENAME | SoundFlags.SND_ASYNC))
                {


                    //MessageBox.Show(this,
                    //"Unable to find specified sound file or default Windows sound");
                }
            }
            catch
            {
                err = Marshal.GetLastWin32Error();
                if (err != 0)
                {
                    //MessageBox.Show(this,
                    //    "Error " + err.ToString(),
                    //    "PlaySound() failed",
                    //    MessageBoxButtons.OK,
                    //    MessageBoxIcon.Error);
                }
            }
        }

        //private void buttonBrowse_Click(object sender, System.EventArgs e)
        //{
        //    string sysRoot = System.Environment.SystemDirectory;
        //    OpenFileDialog dlg = new OpenFileDialog();
        //    dlg.AddExtension = true;
        //    dlg.Filter = "Wave files (*.wav)|*.wav|All files (*.*)|*.*";
        //    dlg.InitialDirectory = sysRoot + @"\..\Media";	// start in media folder

        //    // open dialog
        //    if (dlg.ShowDialog(this) == DialogResult.OK)
        //    {
        //        tbFileName.Text = dlg.FileName;
        //    }
        //}

        //private void tbFileName_TextChanged(object sender, System.EventArgs e)
        //{
        //    if (tbFileName.Text.Length > 0)
        //        buttonPlay.Enabled = true;
        //    else
        //        buttonPlay.Enabled = false;
        //}

        //private void PopulateDropDown()
        //{
        //    // fill our PropertyCollection object
        //    events = GetUserDefinedSounds();

        //    // disable if no sound events
        //    if (events.Keys.Count == 0)
        //        cbUserSound.Enabled = false;
        //    else
        //    {
        //        foreach (string key in events.Keys)
        //        {
        //            cbUserSound.Items.Add(key);
        //        }
        //    }
        //}

        /// <summary>
        /// Retrieves the user-defined sounds from the registry
        /// </summary>
        private PropertyCollection GetUserDefinedSounds()
        {
            string rootKey = "AppEvents\\Schemes\\Apps\\.Default";
            PropertyCollection coll = new PropertyCollection();

            try
            {
                // open root key
                key1 = Registry.CurrentUser.OpenSubKey(rootKey, false);

                // go through each subkey
                foreach (string subKey in key1.GetSubKeyNames())
                {
                    // open subkey
                    key2 = key1.OpenSubKey(subKey + "\\.Current", false);

                    // get filename, if any
                    if (key2 != null)
                        if (key2.GetValue(null).ToString().Length > 0)
                            coll.Add(subKey, key2.GetValue(null).ToString());
                }
            }
            catch (Exception)
            {
                //MessageBox.Show(this, ex.Message, "Yikes!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // close keys
                key1.Close();
                key2.Close();
            }

            return coll;
        }

        //private void cbUserSound_SelectedIndexChanged(object sender, System.EventArgs e)
        //{
        //    // return the filename from the key
        //    tbFileName.Text = events[cbUserSound.SelectedItem].ToString();
        //}
    }
}

