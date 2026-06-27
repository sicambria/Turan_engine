using System;
using System.Collections.Generic;
using System.Text;

namespace Felismero_motor
{
    // package comirva.audio.util;

    /// <summary>
    /// Class for computing a windowed fast Fourier transform.
    /// Implements some of the window functions for the STFT from
    /// Harris (1978), Proc. IEEE, 66, 1, 51-83.
    ///
    /// Provenance: Java->C# port of Klaus Seyerlehner's CoMIRVA
    /// <c>comirva.audio.util.FFT</c>.
    ///
    /// UNUSED - not part of any build. See README.md in this folder.
    /// Code-review-verified, NOT compiler-verified.
    /// </summary>
    public class FFT
    {
        // NOTE: these were `public static int` in Java. In C# a `switch` case label
        // must be a compile-time constant, and both transform() and
        // setWindowFunction() switch on these, so they MUST be `const`.

        /// <summary>used to specify a forward Fourier transform</summary>
        public const int FFT_FORWARD = -1;
        /// <summary>used to specify an inverse Fourier transform</summary>
        public const int FFT_REVERSE = 1;
        /// <summary>used to specify a magnitude Fourier transform</summary>
        public const int FFT_MAGNITUDE = 2;
        /// <summary>used to specify a magnitude phase Fourier transform</summary>
        public const int FFT_MAGNITUDE_PHASE = 3;
        /// <summary>used to specify a normalized power Fourier transform</summary>
        public const int FFT_NORMALIZED_POWER = 4;
        /// <summary>used to specify a power Fourier transform</summary>
        public const int FFT_POWER = 5;
        /// <summary>used to specify a power phase Fourier transform</summary>
        public const int FFT_POWER_PHASE = 6;
        /// <summary>used to specify a inline power phase Fourier transform</summary>
        public const int FFT_INLINE_POWER_PHASE = 7;

        /// <summary>used to specify no window function</summary>
        public const int WND_NONE = -1;
        /// <summary>used to specify a rectangular window function</summary>
        public const int WND_RECT = 0;
        /// <summary>used to specify a Hamming window function</summary>
        public const int WND_HAMMING = 1;
        /// <summary>used to specify a 61-dB 3-sample Blackman-Harris window function</summary>
        public const int WND_BH3 = 2;
        /// <summary>used to specify a 74-dB 4-sample Blackman-Harris window function</summary>
        public const int WND_BH4 = 3;
        /// <summary>used to specify a minimum 3-sample Blackman-Harris window function</summary>
        public const int WND_BH3MIN = 4;
        /// <summary>used to specify a minimum 4-sample Blackman-Harris window function</summary>
        public const int WND_BH4MIN = 5;
        /// <summary>used to specify a Gaussian window function</summary>
        public const int WND_GAUSS = 6;
        /// <summary>used to specify a Hanning window function</summary>
        public const int WND_HANNING = 7;
        /// <summary>used to specify a user defined window function</summary>
        public const int WND_USER_DEFINED = 8;
        /// <summary>used to specify a Hanning Z window function</summary>
        public const int WND_HANNINGZ = 9;

        private double[] windowFunction;
        private double windowFunctionSum;
        private int windowFunctionType;
        private int transformationType;
        private int windowSize;
        private const double twoPI = 2.0 * Math.PI;

        public FFT(int transformationType, int windowSize)
            : this(transformationType, windowSize, WND_NONE, windowSize)
        {
        }

        public FFT(int transformationType, int windowSize, int windowFunctionType)
            : this(transformationType, windowSize, windowFunctionType, windowSize)
        {
        }

        public FFT(int transformationType, int windowSize, int windowFunctionType, int support)
        {
            //check and set fft type
            this.transformationType = transformationType;
            if (transformationType < -1 || transformationType > 7)
            {
                transformationType = FFT_FORWARD;
                throw new ArgumentException("unknown fft type");
            }

            //check and set windowSize
            this.windowSize = windowSize;
            if (windowSize != (1 << ((int)Math.Round(Math.Log(windowSize) / Math.Log(2)))))
                throw new ArgumentException("fft data must be power of 2");

            //create window function buffer and set window function
            this.windowFunction = new double[windowSize];
            setWindowFunction(windowFunctionType, support);
        }

        public FFT(int transformationType, int windowSize, double[] windowFunction)
            : this(transformationType, windowSize, WND_NONE, windowSize)
        {
            if (windowFunction.Length != windowSize)
            {
                throw new ArgumentException("size of window function match window size");
            }
            else
            {
                this.windowFunction = windowFunction;
                this.windowFunctionType = WND_USER_DEFINED;
                calculateWindowFunctionSum();
            }
        }

        public void transform(double[] re, double[] im)
        {
            //check for correct size of the real part data array
            if (re.Length < windowSize)
                throw new ArgumentException("data array smaller than fft window size");

            //apply the window function to the real part
            applyWindowFunction(re);

            //perform the transformation
            switch (transformationType)
            {
                case FFT_FORWARD:
                    //check for correct size of the imaginary part data array
                    if (im.Length < windowSize)
                        throw new ArgumentException("data array smaller than fft window size");
                    else
                        fft(re, im, FFT_FORWARD);
                    break;
                case FFT_INLINE_POWER_PHASE:
                    if (im.Length < windowSize)
                        throw new ArgumentException("data array smaller than fft window size");
                    else
                        powerPhaseIFFT(re, im);
                    break;
                case FFT_MAGNITUDE:
                    magnitudeFFT(re);
                    break;
                case FFT_MAGNITUDE_PHASE:
                    if (im.Length < windowSize)
                        throw new ArgumentException("data array smaller than fft window size");
                    else
                        magnitudePhaseFFT(re, im);
                    break;
                case FFT_NORMALIZED_POWER:
                    normalizedPowerFFT(re);
                    break;
                case FFT_POWER:
                    powerFFT(re);
                    break;
                case FFT_POWER_PHASE:
                    if (im.Length < windowSize)
                        throw new ArgumentException("data array smaller than fft window size");
                    else
                        powerPhaseFFT(re, im);
                    break;
                case FFT_REVERSE:
                    if (im.Length < windowSize)
                        throw new ArgumentException("data array smaller than fft window size");
                    else
                        fft(re, im, FFT_REVERSE);
                    break;
            }
        }

        /// <summary>
        /// The FFT method. Calculation is inline, for complex data stored
        /// in 2 separate arrays. Length of input data must be a power of two.
        /// </summary>
        /// <param name="re">the real part of the complex input and output data</param>
        /// <param name="im">the imaginary part of the complex input and output data</param>
        /// <param name="direction">the direction of the Fourier transform (FORWARD or REVERSE)</param>
        private void fft(double[] re, double[] im, int direction)
        {
            int n = re.Length;
            int bits = (int)Math.Round(Math.Log(n) / Math.Log(2));

            if (n != (1 << bits))
                throw new ArgumentException("fft data must be power of 2");

            int localN;
            int j = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (i < j)
                {
                    double temp = re[j];
                    re[j] = re[i];
                    re[i] = temp;
                    temp = im[j];
                    im[j] = im[i];
                    im[i] = temp;
                }

                int k = n / 2;

                while ((k >= 1) && (k - 1 < j))
                {
                    j = j - k;
                    k = k / 2;
                }

                j = j + k;
            }

            for (int m = 1; m <= bits; m++)
            {
                localN = 1 << m;
                double Wjk_r = 1;
                double Wjk_i = 0;
                double theta = twoPI / localN;
                double Wj_r = Math.Cos(theta);
                double Wj_i = direction * Math.Sin(theta);
                int nby2 = localN / 2;
                for (j = 0; j < nby2; j++)
                {
                    for (int k = j; k < n; k += localN)
                    {
                        int id = k + nby2;
                        double tempr = Wjk_r * re[id] - Wjk_i * im[id];
                        double tempi = Wjk_r * im[id] + Wjk_i * re[id];
                        re[id] = re[k] - tempr;
                        im[id] = im[k] - tempi;
                        re[k] += tempr;
                        im[k] += tempi;
                    }
                    double wtemp = Wjk_r;
                    Wjk_r = Wj_r * Wjk_r - Wj_i * Wjk_i;
                    Wjk_i = Wj_r * Wjk_i + Wj_i * wtemp;
                }
            }
        }

        /// <summary>
        /// Computes the power spectrum of a real sequence (in place).
        /// </summary>
        /// <param name="re">the real input and output data; Length must be a power of 2</param>
        private void powerFFT(double[] re)
        {
            double[] im = new double[re.Length];

            fft(re, im, FFT_FORWARD);

            for (int i = 0; i < re.Length; i++)
                re[i] = re[i] * re[i] + im[i] * im[i];
        }

        /// <summary>
        /// Computes the magnitude spectrum of a real sequence (in place).
        /// </summary>
        /// <param name="re">the real input and output data; Length must be a power of 2</param>
        private void magnitudeFFT(double[] re)
        {
            double[] im = new double[re.Length];

            fft(re, im, FFT_FORWARD);

            for (int i = 0; i < re.Length; i++)
                re[i] = Math.Sqrt(re[i] * re[i] + im[i] * im[i]);
        }

        /// <summary>
        /// Computes the normalized power spectrum of a real sequence (in place).
        /// </summary>
        /// <param name="re">the real input and output data; Length must be a power of 2</param>
        private void normalizedPowerFFT(double[] re)
        {
            double[] im = new double[re.Length];
            double r, i;

            fft(re, im, FFT_FORWARD);

            for (int j = 0; j < re.Length; j++)
            {
                r = re[j] / windowFunctionSum * 2;
                i = im[j] / windowFunctionSum * 2;
                re[j] = r * r + i * i;
            }
        }

        /// <summary>
        /// Converts a real power sequence to magnitude representation,
        /// by computing the square root of each value.
        /// </summary>
        /// <param name="re">the real input (power) and output (magnitude) data; Length must be a power of 2</param>
        private void toMagnitude(double[] re)
        {
            for (int i = 0; i < re.Length; i++)
                re[i] = Math.Sqrt(re[i]);
        }

        /// <summary>
        /// Computes a complex (or real if im[] == {0,...}) FFT and converts
        /// the results to polar coordinates (power and phase). Both arrays
        /// must be the same Length, which is a power of 2.
        /// </summary>
        private void powerPhaseFFT(double[] re, double[] im)
        {
            fft(re, im, FFT_FORWARD);

            for (int i = 0; i < re.Length; i++)
            {
                double pow = re[i] * re[i] + im[i] * im[i];
                im[i] = Math.Atan2(im[i], re[i]);
                re[i] = pow;
            }
        }

        /// <summary>
        /// Inline computation of the inverse FFT given spectral input data
        /// in polar coordinates (power and phase).
        /// Both arrays must be the same Length, which is a power of 2.
        /// </summary>
        private void powerPhaseIFFT(double[] pow, double[] ph)
        {
            toMagnitude(pow);

            for (int i = 0; i < pow.Length; i++)
            {
                double re = pow[i] * Math.Cos(ph[i]);
                ph[i] = pow[i] * Math.Sin(ph[i]);
                pow[i] = re;
            }

            fft(pow, ph, FFT_REVERSE);
        }

        /// <summary>
        /// Computes a complex (or real if im[] == {0,...}) FFT and converts
        /// the results to polar coordinates (magnitude and phase). Both arrays
        /// must be the same Length, which is a power of 2.
        /// </summary>
        private void magnitudePhaseFFT(double[] re, double[] im)
        {
            powerPhaseFFT(re, im);
            toMagnitude(re);
        }

        /// <summary>
        /// Fill the window buffer with the values of a standard Hamming window function.
        /// </summary>
        private void hamming(int size)
        {
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;
            double scale = 1.0 / (double)size / 0.54;
            double factor = twoPI / (double)size;

            for (int i = 0; start < stop; start++, i++)
                windowFunction[i] = scale * (25.0 / 46.0 - 21.0 / 46.0 * Math.Cos(factor * i));
        }

        /// <summary>
        /// Fill the window buffer with the values of a standard Hanning window function.
        /// </summary>
        private void hanning(int size)
        {
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;
            double factor = twoPI / (size - 1.0d);

            for (int i = 0; start < stop; start++, i++)
                windowFunction[i] = 0.5 * (1 - Math.Cos(factor * i));
        }

        /// <summary>
        /// In MATLAB, picking up the standard hanning window gives an incorrect periodicity,
        /// because the boundary samples are non-zero; in OCTAVE, both boundary samples
        /// are zero, which still gives an incorrect periodicity. This is why we use hanningz,
        /// a modified version of the hanning window: w = .5*(1 - cos(2*pi*(0:n-1)'/(n)));
        /// </summary>
        private void hanningz(int size)
        {
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;

            for (int i = 0; start < stop; start++, i++)
                windowFunction[i] = 0.5 * (1 - Math.Cos((twoPI * i) / size));
        }

        /// <summary>
        /// Fill the window buffer with the values of a minimum 4-sample Blackman-Harris window.
        /// </summary>
        private void blackmanHarris4sMin(int size)
        {
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;
            double scale = 1.0 / (double)size / 0.36;

            for (int i = 0; start < stop; start++, i++)
                windowFunction[i] = scale * (0.35875 -
                                    0.48829 * Math.Cos(twoPI * i / size) +
                                    0.14128 * Math.Cos(2 * twoPI * i / size) -
                                    0.01168 * Math.Cos(3 * twoPI * i / size));
        }

        /// <summary>
        /// Fill the window buffer with the values of a 74-dB 4-sample Blackman-Harris window.
        /// </summary>
        private void blackmanHarris4s(int size)
        {
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;
            double scale = 1.0 / (double)size / 0.4;

            for (int i = 0; start < stop; start++, i++)
                windowFunction[i] = scale * (0.40217 -
                                    0.49703 * Math.Cos(twoPI * i / size) +
                                    0.09392 * Math.Cos(2 * twoPI * i / size) -
                                    0.00183 * Math.Cos(3 * twoPI * i / size));
        }

        /// <summary>
        /// Fill the window buffer with the values of a minimum 3-sample Blackman-Harris window.
        /// </summary>
        private void blackmanHarris3sMin(int size)
        {
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;
            double scale = 1.0 / (double)size / 0.42;

            for (int i = 0; start < stop; start++, i++)
                windowFunction[i] = scale * (0.42323 -
                                    0.49755 * Math.Cos(twoPI * i / size) +
                                    0.07922 * Math.Cos(2 * twoPI * i / size));
        }

        /// <summary>
        /// Fill the window buffer with the values of a 61-dB 3-sample Blackman-Harris window.
        /// </summary>
        private void blackmanHarris3s(int size)
        {
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;
            double scale = 1.0 / (double)size / 0.45;

            for (int i = 0; start < stop; start++, i++)
                windowFunction[i] = scale * (0.44959 -
                                    0.49364 * Math.Cos(twoPI * i / size) +
                                    0.05677 * Math.Cos(2 * twoPI * i / size));
        }

        /// <summary>
        /// Fill the window buffer with the values of a Gaussian window function.
        /// </summary>
        private void gauss(int size)
        { // ?? between 61/3 and 74/4 BHW
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;
            double delta = 5.0 / size;
            double x = (1 - size) / 2.0 * delta;
            double c = -Math.PI * Math.Exp(1.0) / 10.0;
            double sum = 0;

            for (int i = start; i < stop; i++)
            {
                windowFunction[i] = Math.Exp(c * x * x);
                x += delta;
                sum += windowFunction[i];
            }

            for (int i = start; i < stop; i++)
                windowFunction[i] /= sum;
        }

        /// <summary>
        /// Fill the window buffer with the values of a rectangular window function.
        /// </summary>
        private void rectangle(int size)
        {
            int start = (windowFunction.Length - size) / 2;
            int stop = (windowFunction.Length + size) / 2;

            for (int i = start; i < stop; i++)
                windowFunction[i] = 1.0 / (double)size;
        }

        /// <summary>
        /// Change the window function to one of the predefined window function types.
        /// </summary>
        /// <param name="windowFunctionType">the type of the window function</param>
        /// <param name="support">the number of non-zero values</param>
        public void setWindowFunction(int windowFunctionType, int support)
        {
            if (support > windowSize)
                support = windowSize;

            switch (windowFunctionType)
            {
                case WND_NONE: break;
                case WND_RECT: rectangle(support); break;
                case WND_HAMMING: hamming(support); break;
                case WND_HANNING: hanning(support); break;
                case WND_BH3: blackmanHarris3s(support); break;
                case WND_BH4: blackmanHarris4s(support); break;
                case WND_BH3MIN: blackmanHarris3sMin(support); break;
                case WND_BH4MIN: blackmanHarris4sMin(support); break;
                case WND_GAUSS: gauss(support); break;
                case WND_HANNINGZ: hanningz(support); break;
                default:
                    windowFunctionType = WND_NONE;
                    throw new ArgumentException("unknown window function specified");
            }

            this.windowFunctionType = windowFunctionType;
            calculateWindowFunctionSum();
        }

        public int getTransformationType()
        {
            return transformationType;
        }

        public int getWindowFunctionType()
        {
            return windowFunctionType;
        }

        /// <summary>
        /// Applies a window function to an array of data, storing the result in the data array.
        /// Performs a dot product of the data and window arrays.
        /// </summary>
        /// <param name="data">the array of input data, also used for output</param>
        private void applyWindowFunction(double[] data)
        {
            if (windowFunctionType != WND_NONE)
            {
                for (int i = 0; i < data.Length; i++)
                    data[i] *= windowFunction[i];
            }
        }

        private void calculateWindowFunctionSum()
        {
            windowFunctionSum = 0;
            for (int i = 0; i < windowFunction.Length; i++)
                windowFunctionSum += windowFunction[i];
        }
    }
}
