/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/


/***************************************************************************
                   Engine.cs  -  Turan engine core
                             -------------------
    begin                : September 2010   
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Turan_core
{
    public class Engine
    {
        // BUG-01: mutated per recognition call to the live per-array width (see
        // RecognizeAndReturnIndex). Process-global; NOT thread-safe across concurrent
        // calls with differing mode/format — flagged for future BUG-14.
        public static byte mfcc_lpc_vect_num = 15;
        EngineMode engine_mode = EngineMode.mfcc;
        VectorFileFormat vector_format = VectorFileFormat.htk;

        // BUG-10: confidence-based rejection surface.
        // REJECTED is returned by RecognizeAndReturnIndex when no template is a
        // good enough match (distinct from -1 = "no templates / bad format").
        public const int REJECTED = dtwApp_match.REJECTED;

        // Mean per-frame DTW cost threshold; +Infinity disables rejection.
        public static double RejectionThreshold
        {
            get { return dtwApp_match.RejectionThreshold; }
            set { dtwApp_match.RejectionThreshold = value; }
        }

        // Normalized best cost from the most recent recognition (tuning aid).
        private double last_best_normalized_cost = double.PositiveInfinity;
        public double GetLastBestNormalizedCost() { return last_best_normalized_cost; }

        List<string> active_vector_filepaths = new List<string>();
        List<double> score_list = new List<double>();

        private double[,] win_REF_vector_data;
        private double[,] win_signal_data;

        public enum EngineMode
        {
            mfcc,
            lpc,
            framenum
        }

        public enum VectorFileFormat
        {
            turan,
            htk
        }

        public Engine(EngineMode mode, VectorFileFormat vformat)
        {
            engine_mode = mode;
            vector_format = vformat;
        }

        public void UpdateVectorList(string[] reference_vector_filepaths)
        {
            active_vector_filepaths.Clear();

            foreach (string filename in reference_vector_filepaths)
            {
                active_vector_filepaths.Add(filename);
            }
        }

        public double[,] GetSignalData(string signal_vector_filepath)
        {
            double[,] signal_data = DeSerializeArray(signal_vector_filepath);
            return signal_data;
        }

        public List<double> GetScoreList()
        {
            return score_list;
        }

        public int RecognizeAndReturnIndex(string signal_vector_filepath, string[] reference_vector_filepaths)
        {
            UpdateVectorList(reference_vector_filepaths);
            //--

            dtwApp_match.Num_of_templates = active_vector_filepaths.Count; // call this first!

            if (vector_format == VectorFileFormat.turan)
            {
                win_signal_data = GetSignalData(signal_vector_filepath);

                // BUG-01: pin the DTW width to the live per-array width so the
                // process-global is order-independent (15 native-MFCC / 12 native-LPC);
                // never inherits a stale 60 left by a prior HTK call.
                Engine.mfcc_lpc_vect_num = (byte)win_signal_data.GetLength(1);

                dtwApp_match dtwmatch = new dtwApp_match(win_signal_data);

                foreach (string fpath in active_vector_filepaths)
                {
                    win_REF_vector_data = DeSerializeArray(fpath);
                    dtwmatch.AddTemplate(win_REF_vector_data);
                }

                dtwmatch.bestMatch();
                last_best_normalized_cost = dtwmatch.BestNormalizedCost;

                score_list.Clear();

                foreach (double item in dtwmatch.TotalCost)
                {
                    score_list.Add(item);
                }

                return dtwmatch.RecogResult;
            }

            if (vector_format == VectorFileFormat.htk)
            {
                int num_of_feature_vectors = 15;
                if (engine_mode == EngineMode.lpc)
                {
                    num_of_feature_vectors = 12;
                }

                if (engine_mode == EngineMode.mfcc)
                {
                    num_of_feature_vectors = 15;
                }


                win_signal_data = HTK_Interface.ReadMFCC_D_A_T(signal_vector_filepath, num_of_feature_vectors);

                // BUG-01: HTK MFCC_D_A_T frames carry 4 concatenated streams
                // (static + Δ + ΔΔ + ΔΔΔ); the per-frame width is now 4*N.
                // Pin the DTW width to the live per-array width (== 4*num_of_feature_vectors
                // == 60 for MFCC / 48 for LPC) before any template is read or matched.
                Engine.mfcc_lpc_vect_num = (byte)win_signal_data.GetLength(1);

                dtwApp_match dtwmatch = new dtwApp_match(win_signal_data);

                foreach (string fpath in active_vector_filepaths)
                {
                    win_REF_vector_data = HTK_Interface.ReadMFCC_D_A_T(fpath, num_of_feature_vectors);
                    dtwmatch.AddTemplate(win_REF_vector_data);
                }

                dtwmatch.bestMatch();
                last_best_normalized_cost = dtwmatch.BestNormalizedCost;

                score_list.Clear();

                foreach (double item in dtwmatch.TotalCost)
                {
                    score_list.Add(item);
                }

                return dtwmatch.RecogResult;

            }
            return -1;
        }


        public int MatchLength(string signal_length_filepath, string[] reference_length_filepaths)
        {
            // BUG-11: implement duration matching (EngineMode.framenum intent).
            // "Length" == utterance length in frames == feature array dim 0.
            // Reuses the same format-aware loaders as RecognizeAndReturnIndex;
            // reads only the frame (row) count, so it is unaffected by feature
            // width / serialization changes (see BUG-01, BUG-12).

            if (reference_length_filepaths == null || reference_length_filepaths.Length == 0)
            {
                return -1;
            }

            // Feature width mirrors RecognizeAndReturnIndex (Engine.cs:111-123).
            int num_of_feature_vectors = 15;
            if (engine_mode == EngineMode.lpc)
            {
                num_of_feature_vectors = 12;
            }
            if (engine_mode == EngineMode.mfcc)
            {
                num_of_feature_vectors = 15;
            }

            int signal_frame_count = GetFrameCount(signal_length_filepath, num_of_feature_vectors);

            score_list.Clear();

            int result = -1;
            int best_diff = int.MaxValue;

            for (int idx = 0; idx < reference_length_filepaths.Length; idx++)
            {
                int ref_frame_count = GetFrameCount(reference_length_filepaths[idx], num_of_feature_vectors);
                int diff = Math.Abs(signal_frame_count - ref_frame_count);

                score_list.Add(diff);

                if (diff < best_diff)
                {
                    best_diff = diff;
                    result = idx;
                }
            }

            return result;
        }

        /// <summary>
        /// Loads a feature file (respecting the configured vector format / engine
        /// mode) and returns its frame count (number of rows, dim 0).
        /// </summary>
        private int GetFrameCount(string vector_filepath, int num_of_feature_vectors)
        {
            double[,] data;
            if (vector_format == VectorFileFormat.turan)
            {
                data = DeSerializeArray(vector_filepath);
            }
            else // VectorFileFormat.htk
            {
                data = HTK_Interface.ReadMFCC_D_A_T(vector_filepath, num_of_feature_vectors);
            }
            return data.GetLength(0);
        }

        // BUG-12: TRMS versioned template format constants. These MUST stay
        // byte-identical to the writer side (Creator.SerializeArray) and to the
        // LITE Form1.cs writer/reader. Layout (little-endian):
        //   [4-byte magic 'TRMS'][byte formatVersion][byte featVersion]
        //   [int32 rows = GetLength(0)][int32 cols = GetLength(1)]
        //   [rows*cols * float64, ROW-MAJOR]
        private const byte TRMS_MAGIC_0 = (byte)'T';
        private const byte TRMS_MAGIC_1 = (byte)'R';
        private const byte TRMS_MAGIC_2 = (byte)'M';
        private const byte TRMS_MAGIC_3 = (byte)'S';
        private const byte TRMS_FORMAT_VERSION = 1;
        // featVersion: current feature-extraction generation. A template carrying
        // a lower featVersion (or no magic at all) predates BUG-02's feature change
        // and should be regenerated; the reader warns rather than failing.
        private const byte TRMS_FEAT_VERSION = 1;

        /// <summary>
        /// Deserialize the binary file back into an array.
        /// </summary>
        /// <param name="fname">The input filename.</param>
        /// <returns>2D double[,] array</returns>
        public static double[,] DeSerializeArray(string file_path)
        {
            // BUG-12: versioned reader. New templates carry the "TRMS" magic and are
            // read with BinaryReader (little-endian). Legacy templates written by the
            // old BinaryFormatter builds have no magic -> rewind and fall back so
            // pre-existing .mfcc/.lpc files still load (graceful degrade).
            using (FileStream fstream = new FileStream(file_path, FileMode.Open, FileAccess.Read))
            {
                byte[] magic = new byte[4];
                int read = fstream.Read(magic, 0, 4);
                bool isTrms = (read == 4 &&
                               magic[0] == TRMS_MAGIC_0 && magic[1] == TRMS_MAGIC_1 &&
                               magic[2] == TRMS_MAGIC_2 && magic[3] == TRMS_MAGIC_3);

                if (isTrms)
                {
                    using (BinaryReader br = new BinaryReader(fstream))
                    {
                        byte formatVersion = br.ReadByte();
                        byte featVersion = br.ReadByte();
                        if (featVersion < TRMS_FEAT_VERSION)
                        {
                            // Staleness hook (BUG-02 / BUG-12): template predates the
                            // current feature-extraction change; flag for regeneration.
                            Console.Error.WriteLine("Turan BUG-12: template '" + file_path +
                                "' featVersion " + featVersion + " < current " + TRMS_FEAT_VERSION +
                                " (formatVersion " + formatVersion + "); predates the BUG-02" +
                                " feature change and should be regenerated.");
                        }
                        int rows = br.ReadInt32();
                        int cols = br.ReadInt32();
                        double[,] arr = new double[rows, cols];
                        for (int r = 0; r < rows; r++)
                            for (int c = 0; c < cols; c++)
                                arr[r, c] = br.ReadDouble();
                        return arr;
                    }
                }

                // ---- Legacy fallback: pre-TRMS BinaryFormatter stream ----
                Console.Error.WriteLine("Turan BUG-12: template '" + file_path +
                    "' has no TRMS magic (legacy BinaryFormatter format); loading via" +
                    " fallback. Regenerate to upgrade to the versioned format.");
                fstream.Seek(0, SeekOrigin.Begin);
                BinaryFormatter binFormat = new BinaryFormatter();
                return (double[,])binFormat.Deserialize(fstream);
            }
        }
    }
}
