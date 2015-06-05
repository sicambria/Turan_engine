using System;
using System.Collections.Generic;
using System.Text;

namespace Felismero_motor
{
    class LPC
    {
        


        /////<summary>
        ///// Invented by N. Levinson in 1947, modified by J. Durbin in 1959. 
        ///// returns minimum mean square error    
        /////</summary>
        //public static double levinson_durbin(ref double ac ,ref double ref_1 ,ref double lpc)
        //{
        //    int i ,j;
        //    double r ,error = ac[0];
        //    if(ac[0] == 0)
        //    {
        //        for(i = 0;
        //        i < P_MAX;(i)++)
        //        {
        //            ref_1[i] = 0;
        //        }
        //        return 0;
        //    }
        //    for(i = 0;
        //    i < P_MAX;(i)++)
        //    {
        //        // Sum up this iteration's reflection coefficient.   
        //        r = -(ac[i + 1]);
        //        for(j = 0;
        //        j < i;(j)++)
        //        {
        //            r -= lpc[j ] * ac[i - j ];
        //        }
        //        ref_1[i ] = r /= error;
        //        //  Update LPC coefficients and total error.   
        //        lpc[i ] = r;
        //        for(j = 0;
        //        j < i / 2;(j)++)
        //        {
        //            double tmp = lpc[j ];
        //            lpc[j ] += r * lpc[i - 1 - j ];
        //            lpc[i - 1 - j ] += r * tmp;
        //        }
        //        if(i % 2)
        //        {
        //            lpc[j ] += lpc[j ] * r;
        //        }
        //        error *= 1.0 - r * r;
        //    }
        //    return error;
        //}




        /////<summary>
        /////find the order-P autocorrelation array, R, for the sequence x of length L and warping of lambda
        /////wAutocorrelate(&pfSrc[stIndex],siglen,R,P,0);
        /////</summary>
        //public static void wAutocorrelate(ref float x, uint L, ref float R, uint P, float lambda)
        //{
        //    IntPtr dl = new double();
        //    IntPtr Rt = new double();
        //    double r1, r2, r1t;
        //    R[0] = 0;
        //    Rt[0] = 0;
        //    r1 = 0;
        //    r2 = 0;
        //    r1t = 0;
        //    for (uint k = 0;
        //    k < L; (k)++)
        //    {
        //        Rt[0] += ((double)(x[k])) * ((double)(x[k]));
        //        dl[k] = r1 - ((double)lambda) * ((double)(x[k] - r2));
        //        r1 = x[k];
        //        r2 = dl[k];
        //    }
        //    for (uint i = 1;
        //    i <= P; (i)++)
        //    {
        //        Rt[i] = 0;
        //        r1 = 0;
        //        r2 = 0;
        //        for (uint k = 0;
        //        k < L; (k)++)
        //        {
        //            Rt[i] += ((double)(dl[k])) * ((double)(x[k]));
        //            r1t = dl[k];
        //            dl[k] = r1 - ((double)lambda) * ((double)(r1t - r2));
        //            r1 = r1t;
        //            r2 = dl[k];
        //        }
        //    }
        //    for (i = 0;
        //    i <= P; (i)++)
        //    {
        //        R[i] = ((float)(Rt[i]));
        //    }
        //    dl = null;
        //    Rt = null;
        //}


        /////<summary>
        ///// Calculate the Levinson-Durbin recursion for the autocorrelation sequence R of length P+1 and return the autocorrelation coefficients a and reflection coefficients K
        /////</summary>
        //public static void LevinsonRecursion(uint P, ref float R, ref float A, ref float K)
        //{
        //    double[] Am1;
        //    if (R[0] == 0.0)
        //    {
        //        for (uint i = 1;
        //        i <= P; (i)++)
        //        {
        //            K[i] = 0.0;
        //            A[i] = 0.0;
        //        }
        //    }
        //    else
        //    {
        //        double km, Em1, Em;
        //        uint k, s, m;
        //        for (k = 0;
        //        k <= P; (k)++)
        //        {
        //            A[0] = 0;
        //            Am1[0] = 0;
        //        }
        //        A[0] = 1;
        //        Am1[0] = 1;
        //        km = 0;
        //        Em1 = R[0];
        //        for (//m=2:N+1
        //        m = 1;
        //        m <= P; (m)++)
        //        {
        //            //err = 0;
        //            double err = 0.0f;
        //            for (//for k=2:m-1
        //            k = 1;
        //            k <= m - 1; (k)++)
        //            {
        //                // err = err + am1(k)*R(m-k+1);
        //                err += Am1[k] * R[m - k];
        //            }
        //            //km=(R(m)-err)/Em1;
        //            km = (R[m] - err) / Em1;
        //            K[m - 1] = -(((float)km));
        //            //am(m)=km;
        //            A[m] = ((float)km);
        //            for (//for k=2:m-1
        //            k = 1;
        //            k <= m - 1; (k)++)
        //            {
        //                // am(k)=am1(k)-km*am1(m-k+1);
        //                A[k] = ((float)(Am1[k] - km * Am1[m - k]));
        //            }
        //            //Em=(1-km*km)*Em1;
        //            Em = (1 - km * km) * Em1;
        //            for (//for s=1:N+1
        //            s = 0;
        //            s <= P; (s)++)
        //            {
        //                // am1(s) = am(s)
        //                Am1[s] = A[s];
        //            }
        //            //Em1 = Em;
        //            Em1 = Em;
        //        }
        //    }
        //    return 0;
        //}

    }
}
