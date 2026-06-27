/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/


/***************************************************************************
             Creator.cs  -  Turan feature vector creation module
                             -------------------
    begin                : September 2010   
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
// BUG-12: BinaryFormatter removed from the producer (writer now uses the TRMS
// length-prefixed format). Creator has no reader/legacy-fallback path, so the
// deprecated System.Runtime.Serialization.Formatters.Binary import is gone.
using EricOulashin;
using SoundAnalysis;
using VorbisSharp;

namespace Turan_creator
{
    public class Creator
    {
        private static double[] pcmdata;
        //private double[] firdata;
        //private double[] hammingdata;

        static double[,] win_pcmdata;
        static double[,] win_hammingdata;
        static double[,] win_fftdata;
        static double[,] win_meldata;

        static double[,] win_lpcdata;

        static int num_of_frames = 0;
        static int extended_num_of_frames = 0;

        static bool no_overlap_lpc = true;  // No PCM data overlap in LPC mode  // false could cause freezing!

        public static bool No_overlap_lpc_f
        {
            get { return Creator.no_overlap_lpc; }
            set { Creator.no_overlap_lpc = value; }
        }

        static string application_path;

        public static string Application_path
        {
            get { return application_path; }
            set { application_path = value; }
        }

        static EngineMode engine_mode = EngineMode.mfcc;
        static VectorFileFormat vector_format = VectorFileFormat.htk;

        public Creator(EngineMode eng_mode, VectorFileFormat vformat)
        {
            engine_mode = eng_mode;
            vector_format = vformat;
        }

        public static EngineMode Engine_mode_f
        {
            get { return Creator.engine_mode; }
        }

        public enum EngineMode
        {
            lpc,
            mfcc   // BUG-03: produces LOG-MEL filterbank features (no DCT), not true MFCC. Name/extension kept for compat.
        }

        public enum VectorFileFormat
        {
            turan,
            htk
        }


        /// <summary>
        /// Calculates feature vectors from PCM data.
        /// </summary>
        /// <param name="pcm_filepaths">Filepath array of PCM(WAV) data (full path including extension)</param>
        /// <param name="engine_mod">Engine mode (EngineMode.lpc (default) or EngineMode.mfcc)</param>
        public static void CalculateFeatureVectors(string[] pcm_filepaths)
        {
            //engine_mode = engine_mod; // switch engine to LPC/MFCC

            if (vector_format == VectorFileFormat.turan)
            {

                foreach (string pcm_filepath in pcm_filepaths)
                {

                    WAVFile wreffile = new WAVFile();
                    //wreffile.Open(signal_filename, WAVFile.WAVFileMode.READ);
                    wreffile.Open(pcm_filepath, WAVFile.WAVFileMode.READ);
                    pcmdata = new double[wreffile.NumSamples];

                    int fpos = 0;
                    while (wreffile.FilePosition != wreffile.FileSizeBytes)
                    {
                        pcmdata[fpos] = wreffile.GetNextSampleAs16Bit();
                        fpos++;
                    }

                    wreffile.Close();

                    num_of_frames = pcmdata.Length / H_FELDOLGOZO.num_items_in_windowed_frame;
                    //num_of_frames++; // add 1 so the rest will also fit to the last frame

                    //extended_num_of_frames = (int)(num_of_frames * (1 / H_FELDOLGOZO.overlap_rate));
                    extended_num_of_frames = num_of_frames;

                    //--------------------------------------------------------------------------

                    //win_pcmdata = H_FELDOLGOZO.create_window(pcmdata, num_of_frames);


                    if (engine_mode == EngineMode.lpc)
                    {

                        if (no_overlap_lpc)
                        {
                            win_pcmdata = H_FELDOLGOZO.create_window_no_overlap(pcmdata, num_of_frames);
                        }
                        else
                        {
                            win_pcmdata = H_FELDOLGOZO.create_window(pcmdata, num_of_frames);
                        }


                        H_FELDOLGOZO.mfcc_lpc_vect_num = 12;

                        //with Hamming filtering
                        //win_hammingdata = H_FELDOLGOZO.win_fir_hamming(win_pcmdata, extended_num_of_frames);
                        //win_lpcdata = LPC_Calc_FrameByFrame(win_hammingdata);

                        //without Hamming filtering
                        win_lpcdata = LPCCalcFrameByFrame(win_pcmdata);
                    }

                    else if (engine_mode == EngineMode.mfcc)
                    {
                        // NOTE (BUG-03): "mfcc" here is a misnomer kept for on-disk/API
                        // compatibility. The serialized .mfcc file holds 15 LOG-MEL filterbank
                        // values per frame, NOT cepstra: no DCT is applied (H_FELDOLGOZO.mfccszamitas
                        // is dead code). Additionally win_fftdata below is computed but unused;
                        // Window_Mel_Scale_Reduction is fed time-domain win_hammingdata, so the mel
                        // bands are not from an FFT power spectrum (a separate, still-open DSP defect).
                        // Creator and Engine extract identically, so the system stays internally
                        // consistent. To produce TRUE MFCC see plans/BUG-03.md §6 (BREAKS existing
                        // .mfcc templates).
                        win_pcmdata = H_FELDOLGOZO.create_window(pcmdata, num_of_frames);

                        H_FELDOLGOZO.mfcc_lpc_vect_num = 15;

                        win_hammingdata = H_FELDOLGOZO.win_fir_hamming(win_pcmdata, extended_num_of_frames);

                        win_fftdata = FFTCalcFrameByFrame(win_hammingdata);

                        win_meldata = new double[extended_num_of_frames, H_FELDOLGOZO.mfcc_lpc_vect_num];

                        CV_FELDOLGOZO.init_mel_filter_banks();

                        CV_FELDOLGOZO.Window_Mel_Scale_Reduction(win_hammingdata, ref win_meldata, extended_num_of_frames);
                    }


                    // Truncate .wav and add new extension according to engine_mode
                    // Files are saved to the same location
                    string filepath_noext = Path.GetDirectoryName(pcm_filepath) + "\\" +
                        Path.GetFileNameWithoutExtension(pcm_filepath);


                    if (engine_mode == EngineMode.lpc)
                    {
                        try
                        {
                            SerializeArray(win_lpcdata, filepath_noext + ".lpc");
                        }
                        catch (IOException)
                        {
                            throw new IOException("A mentés nem sikerült! (SerializeArray/lpc) Fájl: " + filepath_noext + ".lpc");
                        }
                    }
                    else if (engine_mode == EngineMode.mfcc)
                    {
                        try
                        {
                            SerializeArray(win_meldata, filepath_noext + ".mfcc");
                        }
                        catch (IOException)
                        {
                            throw new IOException("A mentés nem sikerült! (SerializeArray/mfcc) Fájl: " + filepath_noext + ".mfcc");
                        }
                    }
                }
            }

            if (vector_format == VectorFileFormat.htk)
            {

                StreamWriter tw = new StreamWriter("dat\\temp.scp", false);
                foreach (string wavfile in pcm_filepaths)
                {
                    //string filepath_noext = Path.GetDirectoryName(wavfile) + "\\" +
                    //    Path.GetFileNameWithoutExtension(wavfile);

                    string filepath_noext = Path.GetFileNameWithoutExtension(wavfile);

                    tw.WriteLine(Path.GetFileName(wavfile) + " " + filepath_noext + ".mfc3");
                }

                tw.Close();

                try
                {
                    HTK_Interface.CreateMFCC_D_A_T("mfcc_config.txt", "temp.scp", Application_path);

                }
                catch (Exception)
                {
                    
                    throw;
                }
            }
        }


        public static double[,] FFTCalcFrameByFrame(double[,] input_win_hammingdata)
        {
            win_fftdata = new double[extended_num_of_frames, H_FELDOLGOZO.num_items_in_windowed_frame];

            int fft_failed_frames = 0;          // per-call (NOT static)
            Exception first_fft_failure = null;

            // Read, FFT, Write back

            for (int frame = 0; frame < extended_num_of_frames; frame++)
            {
                double[] temp_frame_line = new double[H_FELDOLGOZO.num_items_in_windowed_frame];

                for (int frame_item = 0; frame_item < H_FELDOLGOZO.num_items_in_windowed_frame; frame_item++)
                {
                    temp_frame_line[frame_item] = input_win_hammingdata[frame, frame_item];
                }

                // FFT per frame
                try
                {
                    temp_frame_line = SoundAnalysis.FftAlgorithm.Calculate(temp_frame_line);
                }
                catch (Exception ex)
                {
                    // BUG-07: never swallow silently. Deterministic zero fallback
                    // (same width as expected), record the cause, surface after loop.
                    fft_failed_frames++;
                    if (first_fft_failure == null) first_fft_failure = ex;
                    temp_frame_line = new double[H_FELDOLGOZO.num_items_in_windowed_frame];
                }

                // write back frame
                for (int frame_item = 0; frame_item < H_FELDOLGOZO.num_items_in_windowed_frame; frame_item++)
                {
                    win_fftdata[frame, frame_item] = temp_frame_line[frame_item];
                }

                // continue with the next frame
            }

            if (fft_failed_frames > 0)
            {
                throw new InvalidOperationException(
                    "FFT failed on " + fft_failed_frames + " of " + extended_num_of_frames
                    + " frame(s); MFCC features are unreliable. First error: "
                    + first_fft_failure.Message, first_fft_failure);
            }

            return win_fftdata;
        }



        public static double[,] LPCCalcFrameByFrame(double[,] input_win_pcmdata)
        {
            win_lpcdata = new double[extended_num_of_frames, H_FELDOLGOZO.mfcc_lpc_vect_num];

            int lpc_failed_frames = 0;          // per-call (NOT static)
            Exception first_lpc_failure = null;

            // Read, FFT, Write back

            for (int frame = 0; frame < extended_num_of_frames; frame++)
            {
                double[] temp_frame_line = new double[H_FELDOLGOZO.num_items_in_windowed_frame];
                double[] lpc_frame_line = new double[H_FELDOLGOZO.mfcc_lpc_vect_num];

                for (int frame_item = 0; frame_item < H_FELDOLGOZO.num_items_in_windowed_frame; frame_item++)
                {
                    temp_frame_line[frame_item] = input_win_pcmdata[frame, frame_item];
                }

                // LPC per frame

                try
                {
                    Lpc.lpc_from_data(temp_frame_line, ref lpc_frame_line, temp_frame_line.Length, H_FELDOLGOZO.mfcc_lpc_vect_num);
                }
                catch (Exception ex)
                {
                    // BUG-07: never swallow silently. lpc_frame_line is freshly
                    // allocated zeros (deterministic fallback); record + surface.
                    lpc_failed_frames++;
                    if (first_lpc_failure == null) first_lpc_failure = ex;
                }

                // write back frame

                for (int frame_item = 0; frame_item < H_FELDOLGOZO.mfcc_lpc_vect_num; frame_item++)
                {
                    win_lpcdata[frame, frame_item] = lpc_frame_line[frame_item];
                }

                // continue with the next frame
            }

            if (lpc_failed_frames > 0)
            {
                throw new InvalidOperationException(
                    "LPC failed on " + lpc_failed_frames + " of " + extended_num_of_frames
                    + " frame(s); LPC features are unreliable. First error: "
                    + first_lpc_failure.Message, first_lpc_failure);
            }

            return win_lpcdata;
        }




        // BUG-12: TRMS versioned template format constants. These MUST stay
        // byte-identical to the reader side (Engine.DeSerializeArray) and to the
        // LITE Form1.cs writer/reader. Layout (little-endian):
        //   [4-byte magic 'TRMS'][byte formatVersion][byte featVersion]
        //   [int32 rows = GetLength(0)][int32 cols = GetLength(1)]
        //   [rows*cols * float64, ROW-MAJOR]
        private const byte TRMS_MAGIC_0 = (byte)'T';
        private const byte TRMS_MAGIC_1 = (byte)'R';
        private const byte TRMS_MAGIC_2 = (byte)'M';
        private const byte TRMS_MAGIC_3 = (byte)'S';
        private const byte TRMS_FORMAT_VERSION = 1;
        // featVersion: bump when feature extraction changes (e.g. BUG-02) so old
        // templates become detectable as stale and can be flagged for regeneration.
        private const byte TRMS_FEAT_VERSION = 1;

        /// <summary>
        /// Serializes the array into the specified file.
        /// </summary>
        /// <param name="arList">2D double[,] array</param>
        /// <param name="fname">Output filename.</param>
        public static void SerializeArray(double[,] arList, string bin_fpath)
        {
            // BUG-12: explicit length-prefixed TRMS binary format (replaces the
            // deprecated/insecure BinaryFormatter). BinaryWriter is little-endian.
            using (FileStream fstream = new FileStream(bin_fpath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fstream))
            {
                int rows = arList.GetLength(0);
                int cols = arList.GetLength(1);
                bw.Write(TRMS_MAGIC_0);        // 1 byte 'T'
                bw.Write(TRMS_MAGIC_1);        // 1 byte 'R'
                bw.Write(TRMS_MAGIC_2);        // 1 byte 'M'
                bw.Write(TRMS_MAGIC_3);        // 1 byte 'S'
                bw.Write(TRMS_FORMAT_VERSION); // 1 byte
                bw.Write(TRMS_FEAT_VERSION);   // 1 byte
                bw.Write(rows);                // Int32, little-endian
                bw.Write(cols);                // Int32, little-endian
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        bw.Write(arList[r, c]); // Double (float64), little-endian
            }
        }


        public int[] GetNumberOfPCMFrames(string[] pcm_file_paths)
        {
            int[] number_of_frames = new int[pcm_file_paths.Length];


            for (int i = 0; i < pcm_file_paths.Length; i++)
            {
                WAVFile wreffile = new WAVFile();
                //wreffile.Open(signal_filename, WAVFile.WAVFileMode.READ);
                wreffile.Open(pcm_file_paths[i], WAVFile.WAVFileMode.READ);

                number_of_frames[i] = wreffile.NumSamples;

                wreffile.Close();
            }


            return number_of_frames;

        }



    }
}
