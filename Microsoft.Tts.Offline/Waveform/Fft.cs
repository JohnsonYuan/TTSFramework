//----------------------------------------------------------------------------
// <copyright file="Fft.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements FFT
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    
    /// <summary>
    /// Complex.
    /// </summary>
    public struct Complex
    {
        #region Fields

        /// <summary>
        /// Real.
        /// </summary>
        public float Real;

        /// <summary>
        /// Imaginary.
        /// </summary>
        public float Imaginary;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Complex"/> struct.
        /// </summary>
        /// <param name="real">Real part of this complex.</param>
        /// <param name="imaginary">Imaginary part of this complex.</param>
        public Complex(double real, double imaginary)
        {
            Real = (float)real;
            Imaginary = (float)imaginary;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Complex"/> struct.
        /// </summary>
        /// <param name="real">Real part of this complex.</param>
        /// <param name="imaginary">Imaginary part of this complex.</param>
        public Complex(float real, float imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Operator +.
        /// </summary>
        /// <param name="x">Left operand.</param>
        /// <param name="y">Right operand.</param>
        /// <returns>Result value.</returns>
        public static Complex operator +(Complex x, Complex y)
        {
            return new Complex(x.Real + y.Real, x.Imaginary + y.Imaginary);
        }

        /// <summary>
        /// Operator -.
        /// </summary>
        /// <param name="x">Left operand.</param>
        /// <param name="y">Right operand.</param>
        /// <returns>Result value.</returns>
        public static Complex operator -(Complex x, Complex y)
        {
            return new Complex(x.Real - y.Real, x.Imaginary - y.Imaginary);
        }

        /// <summary>
        /// Add.
        /// </summary>
        /// <param name="x">Left operand.</param>
        /// <param name="y">Right operand.</param>
        /// <returns>Result value.</returns>
        public static Complex Add(Complex x, Complex y)
        {
            return x + y;
        }

        /// <summary>
        /// Subtract.
        /// </summary>
        /// <param name="x">Left operand.</param>
        /// <param name="y">Right operand.</param>
        /// <returns>Result value.</returns>
        public static Complex Subtract(Complex x, Complex y)
        {
            return x - y;
        }

        /// <summary>
        /// Multiply.
        /// </summary>
        /// <param name="x">Left operand.</param>
        /// <param name="y">Right operand.</param>
        /// <returns>Result value.</returns>
        public static Complex Multiply(Complex x, Complex y)
        {
            return x * y;
        }

        /// <summary>
        /// Operator *.
        /// </summary>
        /// <param name="x">Left operand.</param>
        /// <param name="y">Right operand.</param>
        /// <returns>Result value.</returns>
        public static Complex operator *(Complex x, Complex y)
        {
            Complex result = new Complex();
            result.Real = (x.Real * y.Real) - (x.Imaginary * y.Imaginary);
            result.Imaginary = (x.Real * y.Imaginary) + (y.Real * x.Imaginary);
            return result;
        }

        /// <summary>
        /// Operator *.
        /// </summary>
        /// <param name="r">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>Result value.</returns>
        public static Complex operator *(float r, Complex b)
        {
            return new Complex(b.Real * r, b.Imaginary * r);
        }

        /// <summary>
        /// Operator /.
        /// </summary>
        /// <param name="b">Left operand.</param>
        /// <param name="r">Right operand.</param>      
        /// <returns>Result value.</returns>
        public static Complex operator /(Complex b, float r)
        {
            return new Complex(b.Real / r, b.Imaginary / r);
        }

        /// <summary>
        /// Operator Divide.
        /// </summary>
        /// <param name="b">Left operand.</param>
        /// <param name="r">Right operand.</param>      
        /// <returns>Result value.</returns>
        public static Complex Divide(Complex b, float r)
        {
            return new Complex(b.Real / r, b.Imaginary / r);
        }

        /// <summary>
        /// Operator ==.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool operator ==(Complex left, Complex right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Operator !=.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if not equal, otherwise false.</returns>
        public static bool operator !=(Complex left, Complex right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Conjugate the complex.
        /// </summary>
        /// <param name="complex">Input complex.</param>
        public static void Conjugate(ref Complex complex)
        {
            complex.Imaginary *= -1;
        }

        /// <summary>
        /// Conjugate the complex array.
        /// </summary>
        /// <param name="complexes">Input complex array.</param>
        public static void Conjugate(ref Complex[] complexes)
        {
            for (int i = 0; i < complexes.Length; i++)
            {
                Conjugate(ref complexes[i]);
            }
        }

        /// <summary>
        /// Convert a short array to a complex array.
        /// </summary>
        /// <param name="shortArray">Input value array.</param>
        /// <param name="complexArray">Complex array.</param>
        /// <param name="real">Give short value to real or imaginary of the complex.</param>
        public static void Short2Complex(short[] shortArray, ref Complex[] complexArray, bool real)
        {
            System.Diagnostics.Debug.Assert(shortArray.Length == complexArray.Length, "The size of array input and output are not match");
            if (shortArray.Length != complexArray.Length)
            {
                throw new GeneralException("CrossCorrlation.Short2Complex: The size of array input and output are not match. ");
            }

            for (int i = 0; i < shortArray.Length; i++)
            {
                if (real)
                {
                    complexArray[i].Imaginary = 0;
                    complexArray[i].Real = shortArray[i];
                }
                else
                {
                    complexArray[i].Imaginary = shortArray[i];
                    complexArray[i].Real = 0;
                }
            }
        }

        /// <summary>
        /// Mode.
        /// </summary>
        /// <returns>Mode of complex.</returns>
        public double Mode()
        {
            return Math.Sqrt((Real * Real) + (Imaginary * Imaginary));
        }
        #endregion

        #region Override object methods

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            string rstring = null;
            if (Real != 0)
            {
                rstring = Real.ToString("0.##", CultureInfo.InvariantCulture);
            }
            
            string istring = null;
            if (Imaginary > 0)
            {
                istring = "+" + Imaginary.ToString("0.##", CultureInfo.InvariantCulture) + "i";
            }
            else if (Imaginary < 0)
            {
                istring = Imaginary.ToString("0.##", CultureInfo.InvariantCulture) + "i";
            }

            if (string.IsNullOrEmpty(istring) && string.IsNullOrEmpty(rstring))
            {
                rstring = "0";
            }

            return rstring + istring;
        }

        /// <summary>
        /// Equal.
        /// </summary>
        /// <param name="obj">Other object to compare with.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Complex))
            {
                return false;
            }

            Complex other = (Complex)obj;
            return Real == other.Real && Imaginary == other.Imaginary;
        }

        /// <summary>
        /// Get hash code for this object.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Fast Fourier Transfer.
    /// </summary>
    public static class Fft
    {
        #region Static fields

        private static int _fftWidth = 128;
        private static int _windowSize = 128;
        private static int _windowIncrement = 16;

        private static float[] _hamWindow;
        private static Complex[] _fftBuffer = new Complex[FftWidth * 2];

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets FFT width.
        /// </summary>
        public static int FftWidth
        {
            get
            {
                return _fftWidth;
            }

            set
            {
                _fftWidth = value;
                _fftBuffer = new Complex[_fftWidth * 2];
            }
        }

        /// <summary>
        /// Gets or sets Window size.
        /// </summary>
        public static int WindowSize
        {
            get { return _windowSize; }
            set { _windowSize = value; }
        }

        /// <summary>
        /// Gets or sets Window increment step length.
        /// </summary>
        public static int WindowIncrement
        {
            get { return _windowIncrement; }
            set { _windowIncrement = value; }
        }

        #endregion

        #region Fast Fourier Transfer

        /// <summary>
        /// Transfer.
        /// </summary>
        /// <param name="samples">Input value array.</param>
        /// <returns>Output values array.</returns>
        public static float[][] Transfer(short[] samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException("samples");
            }

            float[][] spectrograms = null;

            short[] block = new short[FftWidth];

            if (FftWidth > WindowSize)
            {
                throw new InvalidDataException("WindowSize is short than FftWinth");
            }

            int windowNumber = (int)Math.Ceiling((float)samples.Length / WindowIncrement);

            spectrograms = new float[windowNumber][];
            for (int i = 0; i < windowNumber; ++i)
            {
                Buffer.SetByte(block, 0, 0);
                int sample2copy = Math.Min(samples.Length - (i * WindowIncrement), WindowSize);
                Buffer.BlockCopy(samples, i * WindowIncrement * 2, block, 0, sample2copy * 2);

                float[] data = new float[FftWidth];
                Transfer(false, block, ref data);
                spectrograms[i] = data;
            }

            return spectrograms;
        }

        /// <summary>
        /// Transfer.
        /// </summary>
        /// <param name="invert">Do invert transfering or not.</param>
        /// <param name="complexs">Complex array.</param>
        public static void Transfer(bool invert, ref Complex[] complexs)
        {
            complexs = FFT(complexs, invert);
            if (complexs.Length == 0)
            {
                throw new InvalidDataException("the complex array is empty.");
            }

            if (invert)
            {
                for (int i = 0; i < complexs.Length; i++)
                {
                    complexs[i] /= complexs.Length;
                }
            }
        }

        /// <summary>
        /// Transfer.
        /// </summary>
        /// <param name="invert">Do invert transfering or not.</param>
        /// <param name="samples">Input value array.</param>
        /// <param name="outputValues">Output value array.</param>
        private static void Transfer(bool invert, short[] samples, ref float[] outputValues)
        {
            if (samples.Length < FftWidth)
            {
                throw new InvalidDataException("the length of samples is short than FftWinth");
            }

            for (int i = 0; i < samples.Length; i++)
            {
                _fftBuffer[i].Imaginary = samples[i];
                _fftBuffer[i].Real = 0;
            }

            Ham(ref _fftBuffer);
            Transfer(invert, ref _fftBuffer);

            for (int i = 0; i < outputValues.Length; i++)
            {
                outputValues[i] = (float)_fftBuffer[i].Mode();
            }
        }

        /// <summary>
        /// Do FFT recursive.
        /// </summary>
        /// <param name="input">Complex array.</param>
        /// <param name="invert">Do invert transfering or not.</param>
        /// <returns>The FFT result complex array.</returns>
        private static Complex[] FFT(Complex[] input, bool invert)
        {
            if (input.Length == 1)
            {
                return new Complex[] { input[0] };
            }

            int length = input.Length;
            int half = length / 2;

            Complex[] evens = new Complex[half];
            for (int i = 0; i < half; i++)
            {
                evens[i] = input[2 * i];
            }

            Complex[] evenResult = FFT(evens, invert);
            Complex[] odds = evens;

            for (int i = 0; i < half; i++)
            {
                odds[i] = input[(2 * i) + 1];
            }

            Complex[] oddResult = FFT(odds, invert);

            Complex[] result = new Complex[length];
            float fac = (float)(-2.0 * Math.PI / length);
            if (invert)
            {
                fac = -fac;
            }

            double sinFac = Math.Sin(fac);
            double cosFac = Math.Cos(fac);
            double cosBase = 1.0;
            double sinBase = 0;
            for (int k = 0; k < half; k++)
            {
                Complex oddPart = oddResult[k] * new Complex(cosBase, sinBase);
                result[k] = evenResult[k] + oddPart;
                result[k + half] = evenResult[k] - oddPart;
                double sinTemp = (sinBase * cosFac) + (cosBase * sinFac);
                double cosTemp = (cosBase * cosFac) - (sinBase * sinFac);
                sinBase = sinTemp;
                cosBase = cosTemp;
            }

            return result;
        }

        #endregion

        #region Hamming window function

        /// <summary>
        /// Generate precomputed Hamming window function.
        /// </summary>
        /// <param name="frameSize">Frame size.</param>
        private static void GenHamWindow(int frameSize)
        {
            if (_hamWindow == null || _hamWindow.Length < frameSize)
            {
                _hamWindow = new float[frameSize];
            }

            float fac = (float)(2.0 * Math.PI / (frameSize - 1));

            for (int i = 0; i < frameSize; i++)
            {
                _hamWindow[i] = (float)(0.54 - (0.46 * Math.Cos(fac * i)));
            }
        }

        /// <summary>
        /// Apply Hamming Window to Speech frame s.
        /// </summary>
        /// <param name="complex">Complex array.</param>
        private static void Ham(ref Complex[] complex)
        {
            if (_hamWindow == null || complex.Length != _hamWindow.Length)
            {
                GenHamWindow(complex.Length);
            }

            for (int i = 0; i < complex.Length; i++)
            {
                complex[i].Real *= _hamWindow[i];
            }
        }

        #endregion
    }
}