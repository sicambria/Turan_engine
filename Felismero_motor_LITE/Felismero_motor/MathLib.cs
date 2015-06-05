/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

/*
FourierRocks. By Primiano Tucci, 2007.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
See the GNU General Public License for more details. 
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Felismero_motor
{
    static class MathLib
    {
        public enum FFT_Quality
        {
            Low = 64,
            Average = 128,
            Good = 256,
            Best = 512
        }

        static int _fftsamples = (int)FFT_Quality.Average;

        public static int FFT_Samples
        {
            get { return _fftsamples; }
        }

        public static void SetQuality(FFT_Quality iQ)
        {
            _fftsamples = (int)iQ;
        }




        public static float CrossCorrelation(float[] iF1, float[] iF2) { return CrossCorrelation(iF1, iF2, 0); }

        public static float CrossCorrelation(float[] iF1, float[] iF2, int iT)
        {
            float outVal = 0;
            long len = Math.Min(iF1.Length, iF2.Length);
            for (long i = 0; i < len; i++)
            {
                try
                {
                    if (iT > 0)
                        outVal += iF1[i] * iF2[i + iT];
                    else if (iT < 0)
                        outVal += iF1[i - iT] * iF2[i];
                    else
                        outVal += iF1[i] * iF2[i];
                }
                catch { break; ; }
            }
            return outVal;
        }

        public static float AutoCorrelation(float[] iF)
        {
            return CrossCorrelation(iF, iF);
        }

        public static float FindAbsPeak(float[] iF)
        {
            float peak = 1;
            for (int i = 0; i < iF.Length; i++)
            {
                if (Math.Abs(iF[i]) > peak) peak = Math.Abs(iF[i]);
            }
            return peak;
        }

        public static float[] FFT(float[] iF, UInt32 iOffset)
        {
            FFTLib.Fourier fourier = new FFTLib.Fourier();
            float[] buf;
            buf = fourier.DoFFT(iF, iOffset, FFT_Samples);
            ArrayABS(ref buf);
            return buf;
        }

        public static float[] ArrayDiff(float[] iF1, float[] iF2)
        {
            long len = Math.Min(iF1.Length, iF2.Length);
            float[] outArr = new float[len];
            for (long i = 0; i < len; i++)
            {
                outArr[i] = iF2[i] - iF1[i];
            }
            return outArr;
        }

        public static float[] ArrayAbsDiff(float[] iF1, float[] iF2)
        {
            long len = Math.Min(iF1.Length, iF2.Length);
            float[] outArr = new float[len];
            for (long i = 0; i < len; i++)
            {
                outArr[i] = Math.Abs(iF1[i] - iF2[i]);
            }
            return outArr;
        }

        public static void ArraySum(ref float[] iDstArr, float[] iOperand)
        {
            long len = Math.Min(iDstArr.Length, iOperand.Length);

            for (long i = 0; i < len; i++)
            {
                iDstArr[i] += iOperand[i];
            }
        }



        public static void ArrayRatio(ref float[] iDstArr, float iRatio)
        {
            long len = iDstArr.Length;

            for (long i = 0; i < len; i++)
            {
                iDstArr[i] /= iRatio;
            }

        }

        public static void ArrayToDB(ref float[] iDstArr)
        {
            long len = iDstArr.Length;

            for (long i = 0; i < len; i++)
            {
                iDstArr[i] = (float)(10 * Math.Log10(iDstArr[i]));
                if (float.IsPositiveInfinity(iDstArr[i])) iDstArr[i] = 100;
                else if (float.IsNegativeInfinity(iDstArr[i])) iDstArr[i] = -100;
            }

        }

        /// <summary>
        /// Returns the ABS of items in the given float[] array
        /// </summary>
        /// <param name="iDstArr">Array of float.</param>
        public static void ArrayABS(ref float[] iDstArr)
        {
            long len = iDstArr.Length;

            for (long i = 0; i < len; i++)
            {
                if (iDstArr[i] < 0)
                    iDstArr[i] = -iDstArr[i];
            }
        }


        public static float ArrayAverage(float[] iF, long iLength)
        {
            if (iLength == 0) iLength = iF.Length;
            float avg = 0;
            for (long i = 0; i < iLength; i++)
                avg += iF[i];
            avg /= iLength;
            return avg;
        }


        public static float[] SpectrumAttenuation_dB(float[] iF1, float[] iF2)
        {
            long len = Math.Min(iF1.Length, iF2.Length);
            float[] outArr = new float[len];

            for (long i = 0; i < len; i++)
            {
                if (iF2[i] == iF1[i]) { outArr[i] = 0; continue; }
                outArr[i] = (float)(10 * Math.Log10(iF2[i] / iF1[i]));
                if (float.IsInfinity(outArr[i])) outArr[i] = float.IsPositiveInfinity(outArr[i]) ? 100 : -100;
            }
            return outArr;
        }

        public static float CalculateCorrelationPercent(float iCrossCorrelation, float iAutoCorrelation) { return CalculateCorrelationPercent(iCrossCorrelation, iAutoCorrelation, 1E-12f); }

        public static float CalculateCorrelationPercent(float iCrossCorrelation, float iAutoCorrelation, float iSensitivity)
        {

            return (float)Math.Round(100 * Math.Exp(-iSensitivity * Math.Abs(iCrossCorrelation - iAutoCorrelation)), 2);
        }
    }
}
