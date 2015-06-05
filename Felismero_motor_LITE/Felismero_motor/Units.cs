/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

/***************************************************************************
                      parts of  WavInit.pas (Delphi)
                             -------------------
    begin                : 2003
    author               : Lécz Dezső, Zahorján András    
 ***************************************************************************/

/*                       AAL SPEECH RECOGNIZER C#                          */

/***************************************************************************
          Units.cs  -  support class for a simple speech recognizer
                             -------------------
    begin                : June 2010  
    author               : Incze Gáspár
    email                : sicambria@users.sourceforge.net
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace Units
{
    public class WavInit
    {

        // filerbank szűrő tipus, alsó, felső, középső értékkel
        public struct TFbank
        {
            public int down;
            public int cent;
            public int up;
        } // end TFbank

        public struct TRiffheader
        {
            public string[] rID;
            public uint rLen;
        } // end TRiffheader

        public struct TWavheader
        {
            public string[] wID;
        } // end TWavheader

        public struct TFormat
        {
            public string[] fID;
            public uint fLen;
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort FormatSpecific;
        } // end TFormat

        public struct TData
        {
            public string[] dID;
            public uint dLen;
        } // end TData

        public struct TComplex
        {
            public double valos;
            public double kepzetes;
        }

        public static TComplex[] arbmint = new TComplex[256];

        public static int xk = 0;
        public static int yk = 0;
        public static int result = 0;
        public static uint mintadb = 0;
        public static double hossz = 0;

        public static ushort leptet = 0;

        public static byte[] arbit = new byte[255 + 1];
        // az indexnek megfeleő bit-fordított tagok vannak benne
        public static uint[,] eredm = new uint[512 + 1, 128 + 1];

        public static int[] flin = new int[132 + 1];
        public static int[] fmel = new int[132 + 1];
        public static TFbank[] filterbank = new TFbank[23 + 1];
        public static uint[,] sum;
        public static double[] mfccarr;

        public static bool converted = false;
        public static bool filtered = false;
        public static bool opened = false;
        public static bool rec = false;
        public static short[] BackBuf;
        public static string PBack = String.Empty;

        public static bool IsOn = false;
        public static bool MakeRef = false;
        public static bool felv = false;
        public static byte silentcount = 0;
        public static byte reccount = 0;

        public static int FactCount = 0;
        public static int BytesWritten = 0;
        public static int WriteBytes = 0;
        // wav
        public static int zajk = 500;
        public static int recog = 500;
        //public static byte mfccnum = 14;  //15
        public const int n = 256;
        public const double ketpi = 2 * Math.PI;
        public const int kvant = 128;
    } // end WavInit

}
