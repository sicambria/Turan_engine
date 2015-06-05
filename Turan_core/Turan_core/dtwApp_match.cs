/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

/***************************************************************************
       Parts of match.java  -  match is the search engine of the recognizer
                             -------------------
    begin                : 1997
    author               : Institute for Signal and Information Processing
                           Mississippi State University
    web                  : http://www.isip.piconepress.com/projects/speech/index.html
 ***************************************************************************/

/***************************************************************************
        dtwApp_match.cs  -  Dynamic Time Warping (DTW) method
                             -------------------
    begin                : June 2010   
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace Turan_core
{
    class dtwApp_match
    {

        //match is the search engine of the recognizer


        // the data array to store all of the templates
        private lpcData[] template = new lpcData[num_of_templates];

        public lpcData[] Template
        {
            get { return template; }
            set { template = value; }
        }

        public void AddTemplate(double[,] vector_array)
        {
            int ind = 0;
            while (template[ind] != null)
            {
                ind++;
            }
            template[ind] = new lpcData(vector_array);
        }

        // the data to compute the matching score
        private lpcData signal;
        private lpcData reference;



        // the url of lpc signal
        //private URL lpcURL = null;
        // the url of raw signal
        //private URL rawURL = null;

        // the number of templates
        //private static int num_of_templates=11;  //11

        private static int num_of_templates; // MUST BE SET!

        public static int Num_of_templates
        {
            get { return dtwApp_match.num_of_templates; }
            set { dtwApp_match.num_of_templates = value; }
        }

        // the number of items in vectors
        //private static int num_of_vectoritems = H_FELDOLGOZO.mfccnum-1; //13  // 13 LPC vektor fixen kódolva!
        //private static int num_of_vectoritems = 15;

        private static int num_of_vectoritems = Engine.mfcc_lpc_vect_num;

        // the index of template for best match method
        private int templateIndex;

        // recognize result, index to show the template
        private int recogResult;

        public int RecogResult
        {
            get { return recogResult; }
            set { recogResult = value; }
        }

        // the slope constraint value
        private double slope = 0.0;

        // the distance type between frames
        private String distanceType = "Euclidean";  //Itakura-error!, Euclidean

        // the applet that this program will be run in
        //private dtwApplet applet;

        // declare the length of path
        private int[] pathLength;

        /**
         * array to store the best path for each template
         */
        public int[,] pathRecord;
        //public int[] pathRecord;

        public List<int[,]> pathRecordList = new List<int[,]>();

        /**
         * array to store the associated cost for each template
         */
        public double[] costRecord;

        /**
         * array to store the total cost for the best path of each template
         */
        private double[] totalCost;

        public double[] TotalCost
        {
            get { return totalCost; }
            set { totalCost = value; }
        }

        // url to store the audio file for the test signal
        // private URL audiofile = null;



        public dtwApp_match()
        {
            templateIndex = -1;
            recogResult = -1;

            //pathRecord = new int[num_of_templates][];
            //pathRecord = new int[num_of_templates, num_of_vectoritems];
            pathRecord = new int[num_of_templates, 1];
            costRecord = new double[120];
            pathLength = new int[num_of_templates];
            totalCost = new double[num_of_templates];

            signal = null;
            reference = null;
        }


        public dtwApp_match(double[,] signal_vector_array) //double[,] references_vector_array
        {
            templateIndex = -1;
            recogResult = -1;

            //pathRecord = new int[num_of_templates][];
            //pathRecord = new int[num_of_templates, num_of_vectoritems];
            pathRecord = new int[num_of_templates, 1];
            costRecord = new double[120];
            pathLength = new int[num_of_templates];
            totalCost = new double[num_of_templates];

            //signal = null;
            //reference = null;

            signal = new lpcData(signal_vector_array);
            //reference = new lpcData(references_vector_array);

        }


        public double[] GetReferenceVector(int frame)
        {
            //double[] temp_ref = new double[reference.data.GetUpperBound(1)];
            double[] temp_ref = new double[Engine.mfcc_lpc_vect_num];

            for (int iter = 0; iter < temp_ref.Length; iter++)
            {
                temp_ref[iter] = reference.data[frame, iter];
            }

            return temp_ref;
        }

        public double[] GetSignalVector(int frame)
        {
            //double[] temp_ref = new double[signal.data.GetUpperBound(1)];
            double[] temp_ref = new double[Engine.mfcc_lpc_vect_num];

            for (int iter = 0; iter < temp_ref.Length; iter++)
            {
                temp_ref[iter] = signal.data[frame, iter];
            }

            return temp_ref;
        }

        public double[] GetVector(int frame, double[,] source_vectorarray)
        {
            double[] temp_ref = new double[source_vectorarray.GetUpperBound(1)];

            for (int iter = 0; iter < temp_ref.Length; iter++)
            {
                temp_ref[iter] = source_vectorarray[frame, iter];
            }

            return temp_ref;
        }
        

        // methods to compute distance between two frames    
        private double frameDistance(double[] f1, double[] f2)
        {
            double dis = 0.0;
            if (distanceType == "Euclidean")
                dis = EuclideanDistance(f1, f2);
            else if (distanceType == "Absolute")
                dis = AbsDistance(f1, f2);
            else if (distanceType == "Itakura")
                dis = ITDDistance(f1, f2);
            return dis;
        }

        // method to compute the Euclidean distance between two vectors
        private double EuclideanDistance(double[] frame1, double[] frame2)
        {
            int i;
            double dis = 0.0;

            for (i = 0; i < num_of_vectoritems; i++)  //13
            {
                dis = dis + (frame1[i] - frame2[i]) * (frame1[i] - frame2[i]);
            }
            return dis;
        }
        // method to compute the absolute distance between two vectors
        private double AbsDistance(double[] frame1, double[] frame2)
        {
            int i;
            double dis = 0.0;

            for (i = 0; i < 13; i++)
            {
                dis = dis + Math.Abs(frame1[i] - frame2[i]);
            }
            return dis;
        }

        // method to compute the Itakura distance between two vectors
        private double ITDDistance(double[] ar2, double[] ar1)
        {
            int magic13 = 13;

            double[] m2 = new double[magic13];  //13
            double[] rf = new double[magic13];
            double[] rf1 = new double[magic13];
            double k, d;

            int i, j;

            //for (i = 0; i < 13; i++)
            for (i = 0; i < magic13; i++)
            {
                m2[i] = 0;
                rf[i] = ar1[i];
            }

            //autocorrelation of ar2 (lpcar2ra)
            //for (i = 0; i < 13; i++)
            for (i = 0; i < magic13; i++)
            {
                //for (j = 0; j < 13 - i; j++)
                for (j = 0; j < magic13 - i; j++)
                    m2[i] += ar2[j] * ar2[i + j];
            }

            //reflection coefficients from ar1 (lpcar2rf)
            for (j = 11; j > 0; j--)
            {
                k = rf[j + 1];
                d = 1.0 / (1.0 - k * k);
                for (i = 1; i <= j; i++)
                {
                    rf1[i] = (rf[i] - k * rf[j - i + 1]) * d;
                }
                for (i = 1; i <= j; i++)
                    rf[i] = rf1[i];
            }

            // autocorrelation coefs from rf (lpcrf2rr)
            double[] rr = new double[magic13];
            double[] a = new double[magic13];
            double sum;
            //for (i = 0; i < 13; i++)
            for (i = 0; i < magic13; i++)
            {
                rr[i] = 0.0;
                a[i] = 0.0;
            }
            a[0] = rf[1];
            rr[0] = 1.0;
            rr[1] = -a[0];
            double e = a[0] * a[0] - 1.0;

            for (i = 1; i < 12; i++)
            {
                k = rf[i + 1];
                sum = 0.0;
                for (j = i; j >= 1; j--)
                    sum += rr[j] * a[i - j];

                rr[i + 1] = k * e - sum;

                double[] aa = new double[magic13];
                for (j = 0; j < i; j++)
                    aa[j] = a[j] + k * a[i - j - 1];
                for (j = 0; j < i; j++)
                    a[j] = aa[j];
                a[i] = k;

                e = e * (1.0 - k * k);
            }

            double[] ar = new double[magic13];
            ar[0] = 1.0;
            for (i = 0; i < magic13; i++)  //12
                ar[i + 1] = a[i];

            sum = 0.0;
            for (i = 0; i < magic13; i++)
                sum += rr[i] * ar[i];

            double r0 = 1.0 / sum;

            for (i = 0; i < magic13; i++)
                rr[i] *= r0;

            m2[0] *= 0.5;
            sum = 0.0;
            for (i = 0; i < magic13; i++)
                sum += rr[i] * m2[i];
            sum *= 2;
            sum = Math.Log10(sum);

            if (Math.Abs(sum) < 1e-6) return 0.0;
            return sum;
        }



        public bool lefttorightMatch()
        {
            if ((signal == null) || (reference == null))
            {
                return false;
            }

            int I, J;
            int i, j, k;
            //int n;

            double[,] cost;
            int[,] path;
            int maxJ;
            int minJ;
            int tempJ;
            double tempcost;
            double temp = 0.0;
            int mink = -1;
            double minc = 1000000.0;

            // I is the length of test signal, and J is the length of reference
            I = signal.getRowLength();
            J = reference.getRowLength();
            //	System.out.println("Length of signal: " + I);
            //	System.out.println("Length of referece: " + J);

            path = new int[I, J];
            cost = new double[I, J];
            // initiate the path and cost array
            // -1 is to indicate that this element is not computed yet
            for (i = 0; i < I; i++)
            {
                for (j = 0; j < J; j++)
                {
                    path[i, j] = -1;
                    cost[i, j] = -1.0;
                }
            }

            // the starting point is (0, 0)
            //cost[0,0] = frameDistance(reference.data[0], signal.data[0]);

            //double[] temp1 = new double[reference.data.GetUpperBound(1)];
            //double[] temp2 = new double[signal.data.GetUpperBound(1)];

            double[] temp1 = new double[Engine.mfcc_lpc_vect_num];
            double[] temp2 = new double[Engine.mfcc_lpc_vect_num];

            for (int iter = 0; iter < temp1.Length; iter++)
            {
                temp1[iter] = reference.data[0, iter];
            }

            for (int iter = 0; iter < temp2.Length; iter++)
            {
                temp2[iter] = signal.data[0, iter];  //out of bound warning!
            }

            //cost[0, 0] = frameDistance(reference.data[0], signal.data[0]);

            cost[0, 0] = frameDistance(temp1, temp2);


            for (i = 1; i < I; i++)
            {
                // calculate the legal search region defined by slope
                //	    System.out.println("Temp output for column " + i);
                if (slope == 0)
                {
                    maxJ = J - 1;
                    minJ = 0;
                }
                else
                {
                    maxJ = (int)(slope * i) + 1;
                    tempJ = (int)((J - 1) * i * (1 / slope) / I + (J - 1) * (1 - 1 / slope)) + 1;

                    if (tempJ < maxJ)
                    {
                        maxJ = tempJ;
                    }
                    if (maxJ >= J)
                    {
                        maxJ = J - 1;
                    }

                    minJ = (int)((J - 1) * i / ((I - 1) * slope)) - 1;
                    tempJ = (int)(J - 1 - (I - 1 - i) * slope) - 1;
                    if (tempJ > minJ)
                    {
                        minJ = tempJ;
                    }
                    if (minJ < 0)
                    {
                        minJ = 0;
                    }
                }

                // for each legal node, search the predecessor with least cost
                //	    System.out.println("search area: maxJ=" + maxJ + ", minJ=" + minJ);
                //	    for (j=maxJ; j>=minJ; j--) {
                for (j = minJ; j <= maxJ; j++)
                {
                    if (j < J && j >= 0)
                    {

                        //double[] temp3 = new double[reference.data.GetUpperBound(1)]; // original
                        //double[] temp4 = new double[signal.data.GetUpperBound(1)];

                        double[] temp3 = new double[Engine.mfcc_lpc_vect_num];
                        double[] temp4 = new double[Engine.mfcc_lpc_vect_num];


                        //double[] temp3 = new double[H_FELDOLGOZO.num_items_in_windowed_frame];  //256 sized temp double[]
                        //double[] temp4 = new double[H_FELDOLGOZO.num_items_in_windowed_frame];


                        //for (int iter = 0; iter < temp3.Length; iter++)
                        for (int iter = 0; iter < temp3.Length; iter++)
                        {
                            temp3[iter] = reference.data[j, iter];
                        }

                        //for (int iter = 0; iter < temp4.Length; iter++)
                        for (int iter = 0; iter < temp4.Length; iter++)
                        {
                            //temp4[iter] = signal.data[i, iter];
                            temp4[iter] = signal.data[i, iter];
                        }

                        tempcost = frameDistance(GetReferenceVector(j), GetSignalVector(i));

                        //tempcost = frameDistance(reference.data[j], signal.data[i]);

                        //		    System.out.println(" j: " + j + ", df[i][j]: " + tempcost);
                        mink = -1;
                        minc = 1000000.0;
                        temp = 0.0;

                        for (k = j; k >= minJ; k--)
                        {
                            if (cost[i - 1, k] >= 0.0)
                            {
                                temp = cost[i - 1, k];
                                if (minc > temp)
                                {
                                    minc = temp;
                                    mink = k;
                                }
                            }
                        }

                        if (mink >= 0)
                        {
                            //			cost[i][j] = minc;
                            cost[i, j] = minc;
                            cost[i, j] = minc + tempcost;
                            path[i, j] = mink;
                        }
                        //		    System.out.println(" predecessor["+i+"]["+j+"]: " + path[i][j] +
                        //				       ", cost["+i+"]["+j+"]: " + cost[i][j]);
                    }
                }
            }
            // back trace the best matching path
            backTrace(path, cost, I, J);
            return true;
        }

        // method to back trace the path 
        private void backTrace(int[,] path2, double[,] cost2,
                  int testlength, int reflength)
        {
            int i, j;
            //double min = 10000.0;
            int minX = 0;
            int minY = 0;
            int[] temppath = new int[testlength + 1];
            int temppathlength = 0;

            minX = testlength - 1;
            minY = reflength - 1;

            j = minY;
            temppath[0] = minY;

            // trace the path from J-1 back to 0
            for (i = minX; i >= 0; i--)
            {
                j = path2[i, j];

                temppathlength++;
                temppath[temppathlength] = j;
            }

            // reverse the path
            for (i = 0; i <= temppathlength / 2; i++)
            {
                j = temppath[i];
                temppath[i] = temppath[temppathlength - i];
                temppath[temppathlength - i] = j;
            }

            for (i = 0; i < temppathlength; i++)
            {
                temppath[i] = temppath[i + 1];
            }

            pathLength[templateIndex] = temppathlength;

            // copy the total cost to totalCost array
            totalCost[templateIndex] = cost2[minX, minY];
        }

        /**
         * This method is used to recognize and
         * compute the matching path between test signal and each template and
         * find out the template with least cost
         */
        public void bestMatch()
        {
            pathRecordList.Clear();
            double temp = 10000.0;
            for (templateIndex = 0; templateIndex < num_of_templates; templateIndex++)
            {
                setReference(templateIndex);

                // reference=

                lefttorightMatch();
                pathRecordList.Add(pathRecord);

                if (totalCost[templateIndex] < temp)
                {
                    recogResult = templateIndex;
                    temp = totalCost[templateIndex];
                }
            }

            if (recogResult != -1)
            {
                reference = template[recogResult];
            }
        }


        public void setReference(int k)
        {
            reference = template[k];
            //if ((reference == null) || (reference.getLength() == 0))
            if ((reference == null) || (reference.getRowLength() == 0))
            {
                //control.setErrorInfo("Error opening template" + k);
                reference = null;
            }
        }

        /**
         * access method for setting the reference with URL
         *
         * @param url URL of reference to set
         */
        public void setReference(string url)
        {
            if (url != null)
            {
                reference = new lpcData(url);
                if (reference.getRowLength() == 0)
                {
                    //control.setErrorInfo("Error opening template");
                    reference = null;
                }
            }
            else
                reference = null;
        }

        /**
         * access method for getting the lpcData reference
         *
         * @return reference of lpcData type
         */
        public lpcData getReference()
        {
            return reference;
        }



    }
}
