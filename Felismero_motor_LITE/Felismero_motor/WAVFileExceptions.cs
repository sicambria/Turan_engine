// The Code Project Open License (CPOL) 1.02

// The main points subject to the terms of the License are:

//    * Source Code and Executable Files can be used in commercial applications;
//    * Source Code and Executable Files can be redistributed; and
//    * Source Code can be modified to create derivative works.
//    * No claim of suitability, guarantee, or any warranty whatsoever is provided. The software is provided "as-is".
//    * The Article(s) accompanying the Work may not be distributed or republished without the Author's consent

/* This file contains the set of exception classes that can be thrown by the WAVFile class.
 *
 * Date       Author         Description
 * 2009-03-16 erico          Created
 * 2009-03-17 erico          Added WAVFileReadException, WaveFileIOException, WAVFileWriteException,
 *                           WAVBitsPerSampleException and WAVSampleRateException.
 */

namespace EricOulashin
{
    /// <summary>
    /// Class for exception objects thrown by the WAVFile class when an error occurs
    /// </summary>
    class WAVFileException : System.Exception
    {
        public WAVFileException(System.String pErrorMessage, System.String pThrowingMethodName)
            : base(pErrorMessage)
        {
            mThrowingMethodName = pThrowingMethodName;
        }

        public System.String ThrowingMethodName
        {
            get { return mThrowingMethodName; }
        }

        private System.String mThrowingMethodName; // The method that threw the exception
    }

    /// <summary>
    /// This exception is thrown by the WAVFile class during audio file merging.
    /// </summary>
    class WAVFileAudioMergeException : WAVFileException
    {
        public WAVFileAudioMergeException(System.String pErrorMessage, System.String pThrowingMethodName)
            : base(pErrorMessage, pThrowingMethodName)
        {
        }
    }

    /// <summary>
    /// This exception is thrown by the WAVFile class for read errors.
    /// </summary>
    class WAVFileReadException : WAVFileException
    {
        public WAVFileReadException(System.String pErrorMessage, System.String pThrowingMethodName)
            : base(pErrorMessage, pThrowingMethodName)
        {
        }
    }

    /// <summary>
    /// This exception is thrown by the WAVFile class for write errors.
    /// </summary>
    class WAVFileWriteException : WAVFileException
    {
        public WAVFileWriteException(System.String pErrorMessage, System.String pThrowingMethodName)
            : base(pErrorMessage, pThrowingMethodName)
        {
        }
    }

    /// <summary>
    /// Represents an exception for general WAV file I/O
    /// </summary>
    class WAVFileIOException : WAVFileException
    {
        public WAVFileIOException(System.String pErrorMessage, System.String pThrowingMethodName)
            : base(pErrorMessage, pThrowingMethodName)
        {
        }
    }

    /// <summary>
    /// This exception is thrown by the WAVFile class for an unsupported number of bits per sample.
    /// </summary>
    class WAVFileBitsPerSampleException : WAVFileException
    {
        public WAVFileBitsPerSampleException(System.String pErrorMessage, System.String pThrowingMethodName, short pBitsPerSample)
            : base(pErrorMessage, pThrowingMethodName)
        {
            mBitsPerSample = pBitsPerSample;
        }

        public short BitsPerSample
        {
            get { return mBitsPerSample; }
        }

        private short mBitsPerSample; // The invalid value
    }

    /// <summary>
    /// This exception is thrown by the WAVFile class for an unsupported sample rate.
    /// </summary>
    class WAVFileSampleRateException : WAVFileException
    {
        public WAVFileSampleRateException(System.String pErrorMessage, System.String pThrowingMethodName, int pSampleRate)
            : base(pErrorMessage, pThrowingMethodName)
        {
            mSampleRate = pSampleRate;
        }

        public int SampleRate
        {
            get { return mSampleRate; }
        }

        private int mSampleRate; // The invalid value
    }
}