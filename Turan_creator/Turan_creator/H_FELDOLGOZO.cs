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
            tmparray = pcmdata;

            for (int i = 1; i < pcmdata.Length - 1; i++)
            {
                tmparray[i] = tmparray[i] - (0.95 * tmparray[i - 1]);
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

            for (int i = 0; i < firdata.Length - 1; i++)
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
                    if (frame_item == 0)
                    {
                        tmparray[frame, frame_item] = tmparray[frame, frame_item] - (0.95 * tmparray[frame, 0]);
                    }
                    else
                    {
                        tmparray[frame, frame_item] = tmparray[frame, frame_item] - (0.95 * tmparray[frame, frame_item - 1]);
                    }
                    tmparray[frame, frame_item] = (win_pcmdata[frame, frame_item] * (0.54 - (0.46 * Math.Cos((const_2pi * frame_item) / num_items_in_windowed_frame))));
                }
            }
            return tmparray;
        }


        /// <summary>
        /// DCT végrehajtása, az mfccarr (globális) tömböt tölti fel
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
                    mfccarr[kersz, m] = mfccarr[kersz, m] * (Math.Sqrt(2 / 24));
                }
            }
        }



        public static double[,] tavtomb = new double[256, mfcc_lpc_vect_num];
        public static double[,] ertomb = new double[256, mfcc_lpc_vect_num];
        public static int[] refcounters = new int[256];

        public static int refcounter1 = 0;
        public static int counter_ref = 0;
        public static double[] mfccref1;
        public static double[] mfccref2;
        public static double[] mfccref3;
        public static double[,] referencia;

        public static byte Refs = 0;
        public static double[] mfccrefs = new double[256];
        // itt a paraméter a "mivel" :

        public static int hasonlit(ref double[,] parameter, int count_param)
        {
            int result;
            int seged1;
            int seged2;
            int i;
            int j;
            int indx;
            double tmp = 0.0;
            for (i = 0; i < 255; i++)
            {
                for (j = 0; j < mfcc_lpc_vect_num; j++)
                {
                    tavtomb[i, j] = -1;
                    ertomb[i, j] = -1;
                }
            }
            // --a távolságtömb feltöltése : --
            for (i = 0; i < count_param; i++)
            {
                for (j = 0; j < mfcc_lpc_vect_num; j++)  //counter_ref
                //for (j = 0; j < counter; j++)
                {
                    //tmp = 0.0;
                    //for (indx = 1; indx <= Units.WavInit.mfccnum; indx++)
                    //for (indx = 0; indx < Units.WavInit.mfccnum; indx++)
                    for (indx = 0; indx < H_FELDOLGOZO.mfcc_lpc_vect_num; indx++)
                    {
                        tmp = tmp + (Math.Pow(Convert.ToDouble(parameter[i, indx] - referencia[j, indx]), 2));  // k=1-től N-ig SUM ( Xk - Yk )^2
                        tavtomb[i, j] = tmp;
                    }
                }
            }
            // --az eredménytömb számítása : --
            ertomb[0, 0] = tavtomb[0, 0];
            seged1 = 0;
            seged2 = 0;
            while ((tavtomb[seged1 + 1, seged2] != -1) && (seged2 < num_items_in_windowed_frame - 1))
            {
                seged1++;
                ertomb[seged1, seged2] = ertomb[seged1 - 1, seged2] + tavtomb[seged1, seged2];
            }
            seged1 = 0;
            //while ((tavtomb[seged1, seged2 + 1] != -1) && (seged2 < 256))
            while ((tavtomb[seged1, seged2] != -1) && (seged2 < mfcc_lpc_vect_num - 1))
            {
                seged2++;
                ertomb[seged1, seged2] = ertomb[seged1, seged2 - 1] + tavtomb[seged1, seged2];
            }
            seged1 = 0;
            seged2 = 0;
            //while ((tavtomb[0, seged2 + 1] != -1) && (seged2 < 256))
            while ((tavtomb[0, seged2] != -1) && (seged2 < mfcc_lpc_vect_num - 1))
            {
                seged1 = 0;
                seged2++;
                //while ((tavtomb[seged1 + 1, seged2] != -1) && (seged1 < 256))
                while ((tavtomb[seged1, seged2] != -1) && (seged1 <= 255))
                {
                    seged1++;
                    tmp = ertomb[seged1, seged2 - 1];
                    // az aktuális fölötti elem
                    if (ertomb[seged1 - 1, seged2 - 1] < tmp)
                    {
                        tmp = ertomb[seged1 - 1, seged2 - 1];
                    }
                    // atósan fölötti
                    if (ertomb[seged1 - 1, seged2] < tmp)
                    {
                        tmp = ertomb[seged1 - 1, seged2];
                    }
                    // és mögötti...
                    ertomb[seged1, seged2] = tavtomb[seged1, seged2] + tmp;
                }
            }
            result = (int)Math.Round(ertomb[seged1, seged2]);
            return result;
        }

        // end Comp


    }
}
