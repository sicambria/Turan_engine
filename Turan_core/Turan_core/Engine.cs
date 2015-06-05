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
        public static byte mfcc_lpc_vect_num = 15;
        EngineMode engine_mode = EngineMode.mfcc;
        VectorFileFormat vector_format = VectorFileFormat.htk;

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
                dtwApp_match dtwmatch = new dtwApp_match(win_signal_data);

                foreach (string fpath in active_vector_filepaths)
                {
                    win_REF_vector_data = DeSerializeArray(fpath);
                    dtwmatch.AddTemplate(win_REF_vector_data);
                }

                dtwmatch.bestMatch();

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
                dtwApp_match dtwmatch = new dtwApp_match(win_signal_data);

                foreach (string fpath in active_vector_filepaths)
                {
                    win_REF_vector_data = HTK_Interface.ReadMFCC_D_A_T(fpath, num_of_feature_vectors);
                    dtwmatch.AddTemplate(win_REF_vector_data);
                }

                dtwmatch.bestMatch();

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
            int result = -1;


            return result;

        }

        /// <summary>
        /// Deserialize the binary file back into an array.
        /// </summary>
        /// <param name="fname">The input filename.</param>
        /// <returns>2D double[,] array</returns>
        public static double[,] DeSerializeArray(string file_path)
        {
            FileStream fstream = new FileStream(file_path, FileMode.Open, FileAccess.Read);
            BinaryFormatter binFormat = new BinaryFormatter();
            double[,] binArray;

            try
            {
                binArray = (double[,])binFormat.Deserialize(fstream);
            }
            finally
            {
                fstream.Close();
                //Console.WriteLine("Transfer is complete!");
            }

            return binArray;
        }
    }
}
