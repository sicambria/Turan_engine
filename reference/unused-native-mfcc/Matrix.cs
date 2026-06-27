using System;
using System.Collections.Generic;
using System.Text;

namespace Felismero_motor
{
    /// <summary>
    /// Minimal JAMA-style dense double matrix.
    ///
    /// Provenance: a reduced port of the public-domain JAMA <c>Matrix</c> /
    /// CoMIRVA <c>comirva.audio.util.math.Matrix</c> used by Klaus Seyerlehner's
    /// CoMIRVA MFCC extractor. Only the members actually called by <c>MFCC</c>
    /// (see mfcc.cs) are implemented.
    ///
    /// UNUSED - not part of any build. See README.md in this folder.
    /// Code-review-verified, NOT compiler-verified.
    /// </summary>
    class Matrix
    {
        /// <summary>Array for internal storage of elements, [row][column].</summary>
        private double[][] A;

        /// <summary>Row dimension.</summary>
        private int m;

        /// <summary>Column dimension.</summary>
        private int n;

        /// <summary>
        /// Construct a matrix from a jagged 2-D array, quickly, without checking
        /// that all rows have the declared length.
        /// </summary>
        /// <param name="a">Jagged array of doubles, [row][column].</param>
        /// <param name="m">Number of rows.</param>
        /// <param name="n">Number of columns.</param>
        public Matrix(double[][] a, int m, int n)
        {
            this.A = a;
            this.m = m;
            this.n = n;
        }

        /// <summary>
        /// Construct a matrix from a one-dimensional, column-packed array.
        /// </summary>
        /// <param name="vals">One-dimensional array of doubles, packed by columns.</param>
        /// <param name="m">Number of rows.</param>
        public Matrix(double[] vals, int m)
        {
            this.m = m;
            this.n = (m != 0 ? vals.Length / m : 0);

            if (m * this.n != vals.Length)
                throw new ArgumentException("Array length must be a multiple of m.");

            A = new double[m][];
            for (int i = 0; i < m; i++)
            {
                A[i] = new double[this.n];
                for (int j = 0; j < this.n; j++)
                    A[i][j] = vals[i + j * m];
            }
        }

        /// <summary>
        /// Construct an m-by-n matrix of zeros.
        /// </summary>
        /// <param name="m">Number of rows.</param>
        /// <param name="n">Number of columns.</param>
        public Matrix(int m, int n)
        {
            this.m = m;
            this.n = n;
            A = new double[m][];
            for (int i = 0; i < m; i++)
                A[i] = new double[n];
        }

        /// <summary>Set a single element.</summary>
        public void set(int i, int j, double v)
        {
            A[i][j] = v;
        }

        /// <summary>Get the row dimension.</summary>
        public int getRowDimension()
        {
            return m;
        }

        /// <summary>Get the column dimension.</summary>
        public int getColumnDimension()
        {
            return n;
        }

        /// <summary>
        /// Get a submatrix spanning rows i0..i1 and columns j0..j1, inclusive.
        /// </summary>
        public Matrix getMatrix(int i0, int i1, int j0, int j1)
        {
            Matrix sub = new Matrix(i1 - i0 + 1, j1 - j0 + 1);
            double[][] B = sub.A;
            try
            {
                for (int i = i0; i <= i1; i++)
                    for (int j = j0; j <= j1; j++)
                        B[i - i0][j - j0] = A[i][j];
            }
            catch (IndexOutOfRangeException e)
            {
                throw new IndexOutOfRangeException("Submatrix indices out of range. " + e.Message);
            }
            return sub;
        }

        /// <summary>
        /// Linear algebraic matrix multiplication, this * b.
        /// </summary>
        public Matrix times(Matrix b)
        {
            if (b.m != n)
                throw new ArgumentException("Matrix inner dimensions must agree.");

            Matrix result = new Matrix(m, b.n);
            double[][] C = result.A;
            double[] Bcolj = new double[n];
            for (int j = 0; j < b.n; j++)
            {
                for (int k = 0; k < n; k++)
                    Bcolj[k] = b.A[k][j];

                for (int i = 0; i < m; i++)
                {
                    double[] Arowi = A[i];
                    double s = 0;
                    for (int k = 0; k < n; k++)
                        s += Arowi[k] * Bcolj[k];
                    C[i][j] = s;
                }
            }
            return result;
        }

        /// <summary>
        /// Multiply every element by a scalar in place: A = s * A.
        /// </summary>
        public void timesEquals(double s)
        {
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    A[i][j] = s * A[i][j];
        }

        /// <summary>
        /// Replace every element by its natural logarithm, in place.
        /// </summary>
        public void logEquals()
        {
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    A[i][j] = Math.Log(A[i][j]);
        }

        /// <summary>
        /// Clamp every element up to a lower boundary, in place:
        /// if A[i][j] &lt; v then A[i][j] = v.
        /// </summary>
        public void thrunkAtLowerBoundary(double v)
        {
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                {
                    if (A[i][j] < v)
                        A[i][j] = v;
                }
        }

        /// <summary>
        /// Make a one-dimensional column-packed copy of the internal array.
        /// </summary>
        public double[] getColumnPackedCopy()
        {
            double[] vals = new double[m * n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    vals[i + j * m] = A[i][j];
            return vals;
        }
    }
}
