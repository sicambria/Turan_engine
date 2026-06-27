/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

/***************************************************************************
                    Parts of WavInit.pas (Delphi)
                             -------------------
    begin                : 2003
    author               : Lécz Dezső, Zahorján András    
 ***************************************************************************/

/*                       AAL SPEECH RECOGNIZER C#                          */

/***************************************************************************
          H_FELDOLGOZO.cs  -  preprocess, mel filtering, DTW
                             -------------------
    begin                : June 2010 
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace Turan_creator
{
    public class H_FELDOLGOZO
    {
        public static double[,] mfccarr;
        public static byte mfcc_lpc_vect_num = 12; // 15 MFCC vector, 0-14; 12 LPC vector, 0-11       
        public static int num_items_in_windowed_frame = 256; // items in a frame, 128 from the previous, 128 from the current
        //  old   ++++++128[i-1]+++++++
        //        -------------------i.frame------------------
        //                             ++++++++128[i]+++++++++ new

        static double const_2pi = 2 * Math.PI;
        public static double overlap_rate = 0.5; // 50% overlap


        public static double[,] create_window(double[] pcmdata, int num_of_frames)
        {
            int remainder = pcmdata.Length % num_items_in_windowed_frame;

            double[,] window_array = new double[(int)(num_of_frames * (1 / overlap_rate)), num_items_in_windowed_frame];
            int pos = 0;

            for (int frame = 0; pos < pcmdata.Length; frame++)
            {
                if (frame == 0)
                {
                    for (pos = 0; pos < num_items_in_windowed_frame; pos++)
                    {
                        window_array[frame, pos] = pcmdata[pos];
                    }
                }
                else
                {
                    for (int from_prev_frame = 128, firsthalf = 0; from_prev_frame < num_items_in_windowed_frame; from_prev_frame++, firsthalf++)
                    {
                        window_array[frame, firsthalf] = window_array[frame - 1, from_prev_frame];
                    }

                    for (int second_half = 128; second_half < 256; second_half++)
                    {
                        if (pos < pcmdata.Length)
                        {
                            window_array[frame, second_half] = pcmdata[pos++];
                        }
                    }

                }
            }
            return window_array;
        }

        public static double[,] create_window_no_overlap(double[] pcmdata, int num_of_frames)
        {
            double[,] window_array = new double[num_of_frames, num_items_in_windowed_frame];
            int pos = 0;            
            //----

            for (int frame = 0; pos < pcmdata.Length-8; frame++)
            {
                //if (frame == 0)
                //{


                for (int in_frame = 0; in_frame < num_items_in_windowed_frame; in_frame++)
                {
                    window_array[frame, in_frame] = pcmdata[pos];
                    pos++;
                }


                // }
            }
            return window_array;
        }



        //public static double[,] create_window(double[] pcmdata, int num_of_frames)
        //{
        //    //int keretek_szama = pcmdata.Length / 256;
        //    int remainder = pcmdata.Length % num_items_in_windowed_frame;
        //    bool leave_first_frame = true;

        //    double[,] window_array = new double[num_of_frames, num_items_in_windowed_frame];
        //    int pos = 0;

        //    for (int frame = 0; frame < num_of_frames; frame++)
        //    {
        //        for (int frame_item = 0; frame_item < num_items_in_windowed_frame*atlapol; frame_item++)
        //        {
        //            if (leave_first_frame)
        //            {
        //                for (pos = 0; pos < num_items_in_windowed_frame; pos++)
        //                {
        //                    window_array[frame, pos] = pcmdata[pos];
        //                }
        //                leave_first_frame = false;
        //            }

        //            //window_array[frame, frame_item] = pcmdata[pos++];
        //        }
        //    }
        //    return window_array;
        //}


        /// <summary>
        /// FIR filtering of PCM data (WAV)
        /// </summary>
        /// <param name="pcmdata">WAV (PCM array)</param>
        /// <returns></returns>
        public static double[] fir_filter(double[] pcmdata)
        {
            double[] tmparray = new double[pcmdata.Length];

            if (pcmdata.Length > 0)
            {
                tmparray[0] = pcmdata[0];                 // y[0] = x[0]; no x[-1]
            }
            for (int i = 1; i < pcmdata.Length; i++)
            {
                tmparray[i] = pcmdata[i] - (0.95 * pcmdata[i - 1]);   // FIR: read ORIGINAL input
            }
            return tmparray;
        }



        /// <summary>
        /// Hamming window: W(i) = 0,54-0,46*cos(2*PI/N)
        /// </summary>
        /// <param name="firdata">Filtered (FIR) PCM data</param>
        /// <returns></returns>
        public static double[] hamming_ablak(double[] firdata)
        {
            double[] tmparray = new double[firdata.Length];

            for (int i = 0; i < firdata.Length; i++)
            {
                tmparray[i] = (firdata[i] * (0.54 - (0.46 * Math.Cos((const_2pi * i) / num_items_in_windowed_frame))));
            }
            return tmparray;
        }

        public static double[,] win_hamming_ablak(double[,] firdata, int num_of_frames)
        {
            double[,] tmparray = new double[num_of_frames, num_items_in_windowed_frame];


            for (int frame = 0; frame < num_of_frames; frame++)
            {
                for (int frame_item = 0; frame_item < num_items_in_windowed_frame; frame_item++)
                {
                    tmparray[frame, frame_item] = (firdata[frame, frame_item] * (0.54 - (0.46 * Math.Cos((const_2pi * frame_item) / num_items_in_windowed_frame))));
                }
            }

            return tmparray;
        }



        //------------------ALLIN-1--------------------



        public static double[,] win_fir_hamming(double[,] win_pcmdata, int num_of_frames)
        {
            double[,] tmparray = new double[num_of_frames, num_items_in_windowed_frame];

            for (int frame = 0; frame < num_of_frames; frame++)
            {
                for (int frame_item = 0; frame_item < num_items_in_windowed_frame; frame_item++)
                {
                    // Pre-emphasis FIR: y[i] = x[i] - 0.95*x[i-1], read from the INPUT.
                    // Frame-local: index 0 has no in-frame predecessor -> y[0] = x[0].
                    double preemphasized;
                    if (frame_item == 0)
                    {
                        preemphasized = win_pcmdata[frame, frame_item];
                    }
                    else
                    {
                        preemphasized = win_pcmdata[frame, frame_item] - (0.95 * win_pcmdata[frame, frame_item - 1]);
                    }

                    // Hamming window applied to the pre-emphasized sample.
                    tmparray[frame, frame_item] = preemphasized * (0.54 - (0.46 * Math.Cos((const_2pi * frame_item) / num_items_in_windowed_frame)));
                }
            }
            return tmparray;
        }


        /// <summary>
        /// DCT végrehajtása, az mfccarr (globális) tömböt tölti fel
        /// DEAD CODE (BUG-03): not called from anywhere. DO NOT wire in as-is — it is incompatible
        /// with the live mel output: expects uint[,] (live = double[,]), 24 input bands (live = 15),
        /// and writes mfccarr 1-based into a [num_items_in_windowed_frame, num_items_in_windowed_frame]
        /// (~256x256) buffer (live frame layout differs). A correct DCT must be written fresh; see
        /// plans/BUG-03.md §6.
        /// </summary>
        /// <param name="osszegek">The osszegek.</param>
        /// <param name="dbszam">The dbszam.</param>
        public static void mfccszamitas(ref uint[,] osszegek, int dbszam)        //ushort dbszam
        {
            mfccarr = new double[H_FELDOLGOZO.num_items_in_windowed_frame, H_FELDOLGOZO.num_items_in_windowed_frame]; //256

            byte indx;
            byte i;
            byte m;
            byte kersz;
            double[,] sumfloat = new double[H_FELDOLGOZO.num_items_in_windowed_frame, 24];   //255+1   23+1
            for (i = 0; i <= 255; i++)
            {
                for (m = 1; m <= mfcc_lpc_vect_num; m++)
                {
                    mfccarr[i, m] = 0;
                }
            }
            for (i = 0; i <= 255; i++)
            {
                for (m = 0; m <= 23; m++)
                {
                    sumfloat[i, m] = 0;
                }
            }
            // ln :
            for (indx = 0; indx <= dbszam; indx++)
            {
                for (i = 0; i <= 23; i++)
                {
                    if (osszegek[indx, i] == 0)
                    {
                        // !!!
                        sumfloat[indx, i] = 0.0001;
                    }
                    else
                    {
                        sumfloat[indx, i] = Math.Log(Convert.ToDouble(osszegek[indx, i]));
                    }
                }
            }
            // DCT :
            for (kersz = 0; kersz <= dbszam; kersz++)
            {
                // kersz : keretindex
                for (m = 1; m <= mfcc_lpc_vect_num; m++)
                {
                    // m : MFCC index
                    for (i = 0; i <= 23; i++)
                    {
                        mfccarr[kersz, m] = mfccarr[kersz, m] + (sumfloat[kersz, i] * Math.Cos((m * (i - 0.5) * Math.PI) / 24));
                    }
                    mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2.0 / 24.0));
                }
            }
        }





    }
}
