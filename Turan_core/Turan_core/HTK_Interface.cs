using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Turan_core
{
    static class HTK_Interface
    {
        //http://dotnetperls.com/binaryreader
        //http://csharp.net-informations.com/file/csharp-binaryreader.htm

        //static string htk_cmd_dir = ProgramFilesx86() + "\\HTK\\bin\\";
        static string htk_cmd_dir = "\\htk\\";

        public static void CreateMFCC_D_A_T(string wav_file_path, string config_file_path, string script_file)
        {
            Process hcopy_proc = new Process();
            hcopy_proc.StartInfo.WorkingDirectory = htk_cmd_dir;
            hcopy_proc.StartInfo.FileName = "HCopy.exe";


            // HCopy -C mfcc_config.txt -S teszt.scp

            //prcs.StartInfo.Arguments = " -C mfcc_config_E_D_A_T.txt -S mfcc_E_D_A_T.scp";
            hcopy_proc.StartInfo.Arguments = " -C " + config_file_path + " -S " + script_file;

            hcopy_proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                hcopy_proc.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public static double[,] ReadMFCC_D_A_T(string binary_file_path, int num_of_feature_vectors)
        {
            double[,] vector_array;
            //int num_of_coeff_types = 3;

            // 1.
            using (BinaryReader b = new BinaryReader(File.Open(binary_file_path, FileMode.Open)))
            {
                // 2.
                // Position and length variables.
                int pos = 0;
                // 2A.
                // Use BaseStream.
                //int length = (int)b.BaseStream.Length;
                //int num_of_frames = ((length - 12) / sizeof(float))/num_of_coeff_types;   // length-header / 4 / 3
                int current_frame = 0;

                #region HTK_header

                int nSamples = b.ReadInt32();
                int sampPeriod = b.ReadInt32();
                int sampSize = b.ReadInt16();
                int parmKind = b.ReadInt16();
                pos += 12; //header size

                //int A = b.ReadInt16();
                //int B = b.ReadInt16();

                #endregion

                vector_array = new double[nSamples, num_of_feature_vectors];

                //while (pos < length)
                while (current_frame < nSamples)
                {

                    // MFCC - Mel-frequency cepstral coefficients
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] = BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // D - Delta coefficients
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] += BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // A - Accelerator coefficients
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] += BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    // T - Third differential coefficients
                    for (int i = 0; i < num_of_feature_vectors; i++)
                    {
                        vector_array[current_frame, i] += BitConverter.ToSingle(b.ReadBytes(4), 0);
                        pos += sizeof(float);
                    }

                    current_frame++;

                    //                    ------------------------------ Source: teszt.wav -----------
                    //  Sample Bytes:  2        Sample Kind:   WAVEFORM
                    //  Num Comps:     1        Sample Period: 62.5 us
                    //  Num Samples:   25600    File Format:   WAV
                    //------------------------------------ Target ------------------------------------
                    //  Sample Bytes:  240      Sample Kind:   MFCC_D_A_T
                    //  Num Comps:     60       Sample Period: 10000.0 us
                    //  Num Samples:   158      File Format:   HTK
                    //---------------------------- Observation Structure -----------------------------
                    //x:      MFCC-1  MFCC-2  MFCC-3  MFCC-4  MFCC-5  MFCC-6  MFCC-7  MFCC-8  MFCC-9
                    //       MFCC-10 MFCC-11 MFCC-12 MFCC-13 MFCC-14 MFCC-15   Del-1   Del-2   Del-3
                    //         Del-4   Del-5   Del-6   Del-7   Del-8   Del-9  Del-10  Del-11  Del-12
                    //        Del-13  Del-14  Del-15   Acc-1   Acc-2   Acc-3   Acc-4   Acc-5   Acc-6
                    //         Acc-7   Acc-8   Acc-9  Acc-10  Acc-11  Acc-12  Acc-13  Acc-14  Acc-15
                    //        Acc-16  Acc-17  Acc-18  Acc-19  Acc-20  Acc-21  Acc-22  Acc-23  Acc-24
                    //        Acc-25  Acc-26  Acc-27  Acc-28  Acc-29  Acc-30
                    //-------------------------------- Samples: 0->-1 --------------------------------
                    //0:     -14.782   6.829 -11.618  -3.597 -13.995  -7.002  -9.560   6.043   9.038
                    //         8.853   4.820   1.876  -4.623   1.110  -1.423  -0.059   0.146   1.302
                    //         0.768  -0.802   0.125   1.647  -0.152   0.058  -0.813   0.043  -0.278
                    //        -1.803  -0.748   0.415  -0.034   0.161   0.001  -0.210   0.315   0.194
                    //         0.822   0.010  -0.348  -0.049  -0.621  -0.105   0.199   0.440   0.123
                    //         0.082   0.008  -0.135  -0.006   0.076  -0.036  -0.156  -0.031   0.004
                    //         0.092   0.208   0.234   0.181  -0.136  -0.207

                    //-------------
                    
                    // 3.
                    // Read integer.

                    //int v = b.ReadInt32();
                    //double vec = b.ReadInt32();
                    //Console.WriteLine(vec);

                    // 4.
                    // Advance our position variable.
                    //pos += sizeof(int);
                }
            }
            return vector_array;
        }

        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }


    }
}
