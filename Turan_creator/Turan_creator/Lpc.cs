/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Library General Public License as       *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

/***************************************************************************
                   VorbisSharp lpc.cs  -  LPC functions
                             -------------------
    begin                : 2000
    author               : ymnk, JCraft,Inc., Mark Crichton, Uwe L. Korn
 ***************************************************************************/

/*                       AAL SPEECH RECOGNIZER C#                          */

/***************************************************************************
           Lpc.cs  -  Produce LPC coefficients from PCM (WAV) data
                             -------------------
    begin                : August 2010    
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/


/* Vorbis# (original: csvorbis)
 * Copyright (C) 2000 ymnk, JCraft,Inc.
 *  
 * Written by: 2000 ymnk<ymnk@jcraft.com>
 * Ported to C# from JOrbis by: Mark Crichton <crichton@gimp.org> 
 * Ported to C# 2.0 from C# by: Uwe L. Korn <xhochy@gmx.de>
 *   
 * Thanks go to the JOrbis team, for licencing the code under the
 * LGPL, making my job a lot easier.
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public License
 * as published by the Free Software Foundation; either version 2 of
 * the License, or (at your option) any later version.
   
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Library General Public License for more details.
 * 
 * You should have received a copy of the GNU Library General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

using System;
using OggSharp;

namespace VorbisSharp
{
    public class Lpc
    {
        Drft fft = new Drft();

        int ln;
        int m;

        // Autocorrelation LPC coeff generation algorithm invented by
        // N. Levinson in 1947, modified by J. Durbin in 1959.

        // Input : n elements of time doamin data
        // Output: m lpc coefficients, excitation energy

        public static double lpc_from_data(double[] data, ref double[] lpc, int n_elements_of_timedomain_data, int num_of_produced_lpc_coeff)
        {
            double[] aut = new double[num_of_produced_lpc_coeff + 1];
            double error;
            int i, j;

            // autocorrelation, p+1 lag coefficients

            j = num_of_produced_lpc_coeff + 1;
            while (j-- != 0)
            {
                double d = 0.0;
                for (i = j; i < n_elements_of_timedomain_data; i++) d += data[i] * data[i - j];
                aut[j] = d;
            }

            // Generate lpc coefficients from autocorr values

            error = aut[0];
            /*
            if(error==0){
              for(int k=0; k<m; k++) lpc[k]=0.0;
              return 0;
            }
            */

            for (i = 0; i < num_of_produced_lpc_coeff; i++)
            {
                double r = -aut[i + 1];

                if (error == 0)
                {
                    for (int k = 0; k < num_of_produced_lpc_coeff; k++) lpc[k] = 0.0;
                    return 0;
                }

                // Sum up this iteration's reflection coefficient; note that in
                // Vorbis we don't save it.  If anyone wants to recycle this code
                // and needs reflection coefficients, save the results of 'r' from
                // each iteration.

                for (j = 0; j < i; j++) r -= lpc[j] * aut[i - j];
                r /= error;

                // Update LPC coefficients and total error

                lpc[i] = r;
                for (j = 0; j < i / 2; j++)
                {
                    double tmp = lpc[j];
                    lpc[j] += r * lpc[i - 1 - j];
                    lpc[i - 1 - j] += r * tmp;
                }
                if (i % 2 != 0) lpc[j] += lpc[j] * r;

                error *= (1.0 - r * r);
            }

            // we need the error value to know how big an impulse to hit the
            // filter with later

            return error;
        }


        internal void init(int mapped, int m)
        {
            //memset(l,0,sizeof(lpc_lookup));

            ln = mapped;
            this.m = m;

            // we cheat decoding the LPC spectrum via FFTs
            fft.init(mapped * 2);
        }

        void clear()
        {
            fft.clear();
        }

        static float FAST_HYPOT(float a, float b)
        {
            return (float)Math.Sqrt((a) * (a) + (b) * (b));
        }

    }
}