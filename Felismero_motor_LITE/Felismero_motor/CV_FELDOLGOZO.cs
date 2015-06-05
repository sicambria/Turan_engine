/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

/***************************************************************************
               Parts of cvoicecontrol.c  -  a simple speech recognizer
                             -------------------
    begin                : Sat Feb 12 2000
    author               : (C) 2000 by Daniel Kiecza
    email                : daniel@kiecza.de
 ***************************************************************************/

/*                       AAL SPEECH RECOGNIZER C#                          */

/***************************************************************************
                  CV_FELDOLGOZO.cs  -  mel filtering
                             -------------------
    begin                : June 2010    
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/


using System;
using System.Collections.Generic;
using System.Text;

namespace Felismero_motor
{
    public class CV_FELDOLGOZO
    {
        static int do_mean_sub;
        static int i, j;
        static int[] filter_banks = new int[17];

        static float[] channel_mean;

        //----------

        static int FFT_SIZE = 256;
        static int HAMMING_SIZE = FFT_SIZE;
        static int VECSIZE = H_FELDOLGOZO.mfcc_lpc_vect_num;
        static int FEAT_VEC_SIZE = VECSIZE;

        public CV_FELDOLGOZO()
        {

        }

        public static void init_mel_filter_banks()
        {
            /* initiate mel scale filters */

            filter_banks[0] = 0;
            filter_banks[1] = 2;
            filter_banks[2] = 6;
            filter_banks[3] = 10;
            filter_banks[4] = 14;
            filter_banks[5] = 18;
            filter_banks[6] = 22;
            filter_banks[7] = 26;
            filter_banks[8] = 30;
            filter_banks[9] = 35;
            filter_banks[10] = 41;
            filter_banks[11] = 48;
            filter_banks[12] = 57;
            filter_banks[13] = 68; // 

            //filter_banks[14] = 80; // 8000Hz?

            filter_banks[14] = 81;

            filter_banks[15] = 97;
            filter_banks[16] = 116;

            do_mean_sub = 1; /***** turn substraction of channel mean vector on! */
        }


        //public static int InitMelFilterFrame(double[] power_spec, ref double[] result)
        //{

        //    /***** mel scale reduction */

        //    channel_mean = new float[power_spec.Length];
        //    for (i = 0; i < channel_mean.Length - 1; i++)
        //    {
        //        channel_mean[i] = 0;
        //    }

        //    for (i = 0; i < FEAT_VEC_SIZE; i++)
        //    {
        //        int from = (int)filter_banks[i];
        //        int to = (int)filter_banks[i + 1];

        //        if (to > power_spec.Length - 1)
        //        {
        //            to = power_spec.Length - 1; // if out of range
        //        }

        //        if (from == 0)
        //            result[i] = power_spec[0];
        //        else
        //            result[i] = power_spec[from] / 2.0;
        //        result[i] += power_spec[to] / 2.0;
        //        for (j = from + 1; j <= to - 1; j++)
        //            result[i] += power_spec[j];

        //        if (result[i] < 0.0)
        //        {
        //            result[i] *= -1.0;  //ABS
        //        }

        //        result[i] = Math.Log(result[i] + 1.0, Math.E) / Math.Log(2, Math.E);   //0.69314718055994530942;


        //        //if (result[i] == double.NaN) // Not a Number, e.g. lg(-10)
        //        //{
        //        //    result[i] = 0.0;
        //        //}


        //        /***** substraction of channel mean */

        //        if (do_mean_sub == 1)
        //            result[i] = result[i] - channel_mean[i];
        //    }

        //    return 1;
        //}

        public static int Window_Mel_Scale_Reduction(double[,] power_spec, ref double[,] result, int num_of_frames)
        {

            /***** mel scale reduction */

            for (int frame = 0; frame < num_of_frames; frame++)
            {
                channel_mean = new float[power_spec.Length];
                for (i = 0; i < channel_mean.Length - 1; i++)
                {
                    channel_mean[i] = 0;
                }

                for (i = 0; i < FEAT_VEC_SIZE; i++)
                {
                    int from = (int)filter_banks[i];

                    int to = (int)filter_banks[i];

                    if (i != FEAT_VEC_SIZE - 1)
                    {
                        to = (int)filter_banks[i + 1];
                    }

                    if (to > power_spec.Length - 1)
                    {
                        to = power_spec.Length - 1; //ha túlmutatna a tömb határain
                    }

                    if (from == 0)
                        result[frame, i] = power_spec[frame, 0];
                    else
                        result[frame, i] = power_spec[frame, from] / 2.0;
                    result[frame, i] += power_spec[frame, to] / 2.0;
                    for (j = from + 1; j <= to - 1; j++)
                        result[frame, i] += power_spec[frame, j];

                    if (result[frame, i] < 0.0)
                    {
                        result[frame, i] *= -1.0;  //ABS
                    }

                    result[frame, i] = Math.Log(result[frame, i] + 1.0, Math.E) / Math.Log(2, Math.E);   //0.69314718055994530942;


                    //if (result[frame, i] == double.NaN) // Not a Number, e.g. lg(-10)
                    //{
                    //    result[frame, i] = 0.0;
                    //}
           
                    /***** substraction of channel mean */

                    if (do_mean_sub == 1)
                        result[frame, i] = result[frame, i] - channel_mean[i];
                }
            }
            return 1;
        }


    }
}
