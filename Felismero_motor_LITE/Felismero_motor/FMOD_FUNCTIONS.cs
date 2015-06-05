/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

/***************************************************************************
                  FMOD_FUNCTIONS.cs  -  FMOD support class                                
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Felismero_motor
{
    public class FMOD_FUNCTIONS
    {
        private FMOD.System system = null;
        private FMOD.Sound sound = null;
        //private FMOD.Channel channel = null;

        //private float[] spectrum;
        //private float[] spectrumx;
        //private float[] spectrumy;
        //private float[] WAVEDATA;

        Form1 p_form;

        public FMOD_FUNCTIONS(Form1 parent_form)
        {
            p_form = parent_form;
        }

        public void getSpectrumDataHamming(ref float[] spectrum, int SPECTRUMSIZE, int channeloffset)
        {
            
            //int numchannels = 0;
            //int dummy = 0;
            //FMOD.SOUND_FORMAT dummyformat = FMOD.SOUND_FORMAT.NONE;
            //FMOD.DSP_RESAMPLER dummyresampler = FMOD.DSP_RESAMPLER.LINEAR;
            //int count = 0;
            //int count2 = 0;

            system.getSpectrum(spectrum, SPECTRUMSIZE, channeloffset, FMOD.DSP_FFT_WINDOW.HAMMING);
        }


        public void ReadTags_Load(string filename)
        {
            FMOD.TAG tag = new FMOD.TAG();
            int numtags = 0, numtagsupdated = 0;
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
                //MessageBox.Show("Error!  You are using an old version of FMOD " + version.ToString("X") + ".  This program requires " + FMOD.VERSION.number.ToString("X") + ".");
                //Application.Exit();
                //return "FMOD VERSION ERROR";
                p_form.AddWavDataToListbox("Error!  You are using an old version of FMOD " + version.ToString("X") + ".  This program requires " + FMOD.VERSION.number.ToString("X") + ".");
            }
            result = system.init(100, FMOD.INITFLAGS.NORMAL, (IntPtr)null);
            ERRCHECK(result);

            /*
                Open the specified file. Use FMOD_CREATESTREAM and FMOD_DONTPREBUFFER so it opens quickly
            */
            //result = system.createSound("../../../../../examples/media/wave.mp3", (FMOD.MODE.SOFTWARE | FMOD.MODE._2D | FMOD.MODE.CREATESTREAM | FMOD.MODE.OPENONLY), ref sound);
            //result = system.createSound(@"d:\ZENE\24_ost-temple-of-light-tdpmp3.mp3", (FMOD.MODE.SOFTWARE | FMOD.MODE._2D | FMOD.MODE.CREATESTREAM | FMOD.MODE.OPENONLY), ref sound);
            result = system.createSound(filename, (FMOD.MODE.SOFTWARE | FMOD.MODE._2D | FMOD.MODE.CREATESTREAM | FMOD.MODE.OPENONLY), ref sound);



            ERRCHECK(result);

            /*
                Read and display all tags associated with this file
            */
            for (; ; )
            {
                /*
                    An index of -1 means "get the first tag that's new or updated".
                    If no tags are new or updated then getTag will return FMOD_ERR_TAGNOTFOUND.
                    This is the first time we've read any tags so they'll all be new but after we've read them, 
                    they won't be new any more.
                */
                if (sound.getTag(null, -1, ref tag) != FMOD.RESULT.OK)
                {
                    break;
                }
                if (tag.datatype == FMOD.TAGDATATYPE.STRING)
                {
                    //listBox.Items.Add(tag.name + " = " + Marshal.PtrToStringAnsi(tag.data) + " (" + tag.datalen + " bytes)");
                    p_form.AddWavDataToListbox(tag.name + " = " + Marshal.PtrToStringAnsi(tag.data) + " (" + tag.datalen + " bytes)");
                }
                else
                {
                    p_form.AddWavDataToListbox(tag.name + " = <binary> (" + tag.datalen + " bytes)");
                }
            }

            p_form.AddWavDataToListbox(" ");

            /*
                Read all the tags regardless of whether they're updated or not. Also show the tag type.
            */
            result = sound.getNumTags(ref numtags, ref numtagsupdated);
            ERRCHECK(result);
            for (int count = 0; count < numtags; count++)
            {
                string tagtext = null;

                result = sound.getTag(null, count, ref tag);
                ERRCHECK(result);

                switch (tag.type)
                {
                    case FMOD.TAGTYPE.UNKNOWN:
                        tagtext = "FMOD_TAGTYPE_UNKNOWN  ";
                        break;

                    case FMOD.TAGTYPE.ID3V1:
                        tagtext = "FMOD_TAGTYPE_ID3V1  ";
                        break;

                    case FMOD.TAGTYPE.ID3V2:
                        tagtext = "FMOD_TAGTYPE_ID3V2  ";
                        break;

                    case FMOD.TAGTYPE.VORBISCOMMENT:
                        tagtext = "FMOD_TAGTYPE_VORBISCOMMENT  ";
                        break;

                    case FMOD.TAGTYPE.SHOUTCAST:
                        tagtext = "FMOD_TAGTYPE_SHOUTCAST  ";
                        break;

                    case FMOD.TAGTYPE.ICECAST:
                        tagtext = "FMOD_TAGTYPE_ICECAST  ";
                        break;

                    case FMOD.TAGTYPE.ASF:
                        tagtext = "FMOD_TAGTYPE_ASF  ";
                        break;

                    case FMOD.TAGTYPE.FMOD:
                        tagtext = "FMOD_TAGTYPE_FMOD  ";
                        break;

                    case FMOD.TAGTYPE.USER:
                        tagtext = "FMOD_TAGTYPE_USER  ";
                        break;
                }

                if (tag.datatype == FMOD.TAGDATATYPE.STRING)
                {
                    tagtext += (tag.name + " = " + Marshal.PtrToStringAnsi(tag.data) + "(" + tag.datalen + " bytes)");
                }
                else
                {
                    tagtext += (tag.name + " = ??? (" + tag.datalen + " bytes)");
                }

                p_form.AddWavDataToListbox(tagtext);
            }

            p_form.AddWavDataToListbox(" ");

            /*
                 Find a specific tag by name. Specify an index > 0 to get access to multiple tags of the same name.
            */
            result = sound.getTag("ARTIST", 0, ref tag);
            ERRCHECK(result);
            p_form.AddWavDataToListbox(tag.name + " = " + Marshal.PtrToStringAnsi(tag.data) + " (" + tag.datalen + " bytes)");
        }


        private void ERRCHECK(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                p_form.AddWavDataToListbox("FMOD error! " + result + " - " + FMOD.Error.String(result));
                Environment.Exit(-1);
            }
        }


    }
}
