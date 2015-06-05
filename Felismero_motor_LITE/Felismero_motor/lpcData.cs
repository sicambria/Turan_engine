/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

/***************************************************************************
       Parts of lpcData.java  -  LPC loader class
                             -------------------
    begin                : 1997
    author               : Institute for Signal and Information Processing
                           Mississippi State University
    web                  : http://www.isip.piconepress.com/projects/speech/index.html
 ***************************************************************************/

/***************************************************************************
                lpcData.cs  -  LPC loader class
                             -------------------
    begin                : July 2010   
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Felismero_motor
{
    public class lpcData
    {
        //**********************************************************************
        //
        // declare global variables
        //
        //**********************************************************************

        /**
         * array to store lpc vectors
         * this applet using 12-order lpc coded signal
         */

        //private static int num_of_lpc_vectors = 13;
        private static int num_of_mfcc_lpc_vectors = H_FELDOLGOZO.mfcc_lpc_vect_num;

        //skip header in LPC - don't change, unless you change file format that contains vectors
        int num_of_header_lines = 5;

        public int Num_of_lpc_vectors
        {
            get { return num_of_mfcc_lpc_vectors; }
            set { num_of_mfcc_lpc_vectors = value; }
        }

        static int num_of_frames = 5;  // !!!

        public double[,] data = new double[num_of_frames, num_of_mfcc_lpc_vectors];

        // length of signal
        //
        private int length;

        // location of original file
        //
        private string url = null;

        //******************************************************************
        //
        // declare class constructor
        //
        //******************************************************************

        /**
         * This constructor opens file specified by input argument, read in
         * signal, and store in data[][].
         *
         * @param file URL of the file to be loaded
         */
        public lpcData(string lpc_file)
        {
            url = lpc_file;
            loadData(url);
        }

        public lpcData(double[,] source_vector_array)
        {
            data = source_vector_array;
        }

        public lpcData()
        {

        }

        // *****************************************************************
        // 
        // declare class methods
        //
        // *****************************************************************

        /**
         * access method for length
         *
         * @return length of signal
         */
        public int getRowLength()
        {
            //return length;
            //return data.Length;
            return data.Length / H_FELDOLGOZO.mfcc_lpc_vect_num;  // NULLREF TO DATA!!!
        }

        // method: loadData
        //
        // arguments: none
        // return   : none
        // 
        // read the ascii file of a lpc coded signal/template, fill in the data 
        // array, each frame is a vector of 13 elements of double number
        //
        private void loadData(string signalURL)
        {

            // index for frame and element
            //
            int frameIndex, elementIndex;

            // index of first and last pointers of one ascii-form double 
            // number
            //
            int begin = 0, end = 0;

            // temp: string to store a line of chars
            // num:  string to store the chars for a double number
            //
            String temp = " ", num = " ";

            // input stream of signal file
            //
            //InputStreamReader isr = null;

            StreamReader isr = null;
            StreamReader br = null;

            //Stream isr = null;


            //BufferedReader br = null;

            //BufferedStream br = null;

            // open the input file
            //
            try
            {
                isr = new StreamReader(signalURL);
                //br = new BufferedStream(isr);
                br = isr;


                //isr = new InputStreamReader(signalURL.openStream());
                //br = new BufferedReader(isr);
            }
            catch (Exception)
            {
                //System.out.println(e);
                //data = null;
                //length = 0;
                //url = null;
                return;
            }

            // skip the header
            //
            try
            {

                for (int i = 0; i <= num_of_header_lines - 1; i++)
                {
                    br.ReadLine();
                }

                //br.ReadLine();
                //br.ReadLine();
                //br.ReadLine();
                //br.ReadLine();
                //br.ReadLine();

            }
            catch (IOException e)
            {
                //System.out.println(e);
                //System.Windows.Forms.MessageBox.Show(e.ToString());
                //data = null;
                //url = null;
                //length = 0;
                return;
            }

            // initialize length and frame index
            //
            frameIndex = -1;
            length = -1;

            // each line read from the file would be one lpc vector.
            // elements in one vector are seperated by ","
            //
            while (true)
            {
                try
                {

                    // store the meaningful line in string temp
                    //
                    temp = br.ReadLine();

                    // the line to seperate two vectors will be skipped
                    //
                    br.ReadLine();

                    //Example file format:
                    //------------------------------------------------
                    //# Sof v1.0 #
                    //# Model for sil #
                    //name = "LPC";
                    //num_char = 1;
                    //values = { 
                    //  1.000000,-0.516688,-0.187368,-0.097466,-0.041980,-0.018559,-0.019858,-0.004153,0.012837,0.001350,0.018320,-0.004592,0.012182
                    //}, {
                    //  1.000000,-0.572605,-0.034764,-0.067261,0.033337,-0.024960,0.005680,-0.012053,0.001656,0.000296,0.013788,-0.009353,0.002787
                    //};
                    //------------------------------------------------

                }
                catch (IOException e)
                {
                    //System.out.println(e);
                    //System.Windows.Forms.MessageBox.Show(e.ToString());
                    //data = null;
                    //url = null;
                    //length = 0;
                    return;
                }

                // touch the end of file
                //
                if (temp == null) break;

                // increase frame index and signal length by 1
                //
                frameIndex += 1;
                length += 1;

                // initialize element index, starting and ending char
                // pointer for an element
                //
                elementIndex = 0;
                begin = 0;
                end = 0;

                // scan string temp, convert substring of temp flagged by
                // (begin, end) as double number while end hits ',' or the last
                // char of string temp 
                //
                while (true)
                {

                    // scans to the last char of temp, need to convert substring
                    // of temp, and prepare to read in next line
                    //
                    if (end == temp.Length)
                    {
                        num = temp.Substring(begin, end);
                        data[frameIndex, elementIndex] = Double.Parse(num);
                        //data[frameIndex][elementIndex] = Double.valueOf(num).doubleValue();
                        break;
                    }

                    // pointer end hits a ',', need to convert substring of temp
                    // reset pointer begin and end
                    //
                    //if (temp.charAt(end) == ',')
                    if (temp[end] == ',')
                    {
                        num = temp.Substring(begin, end);
                        data[frameIndex, elementIndex] = Double.Parse(num);
                        //data[frameIndex][elementIndex] = Double.valueOf(num).doubleValue();

                        // begin and end point to the first char of next element
                        //
                        end += 1;
                        begin = end;

                        // increase index of element by 1
                        //
                        elementIndex += 1;
                    }
                    end += 1;
                }
            }

            // read in all frames. since frame index starts from 0, length 
            // should increase by 1
            //
            length++;

            // close input stream
            //
            try
            {
                isr.Close();
                br.Close();
            }
            catch (IOException)
            {
                //System.out.println(e+"error close file");
            }
        }
    }
}
