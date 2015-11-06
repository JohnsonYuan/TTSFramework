//----------------------------------------------------------------------------
// <copyright file="MemoryMatrixExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     This module implements MemoryMatrix extension
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using Microsoft.Tts.Offline.Utility;

    public static class MemoryMatrixExtension
    {
        /// <summary>
        /// Convert MemoryMatrix to arrays.
        /// </summary>
        /// <typeparam name="T">Type of the element.</typeparam>
        /// <param name="memoryMatrix">Memory matrix.</param>
        /// <returns>2-dimensioal array.</returns>
        public static T[,] ToArray<T>(this MemoryMatrix<T> memoryMatrix) where T : struct
        {
            T[,] array = new T[memoryMatrix.Row, memoryMatrix.Column];
            for (int i = 0; i < memoryMatrix.Row; ++i)
            {
                for (int j = 0; j < memoryMatrix.Column; ++j)
                {
                    array[i, j] = memoryMatrix[i][j];
                }
            }

            return array;
        }

        /// <summary>
        /// Verify whether dimensions MemoryMatrix are equal to that of the array.
        /// </summary>
        /// <typeparam name="T">Type of the element.</typeparam>
        /// <param name="memoryMatrix">Memory matrix.</param>
        /// <param name="array">2-dimensioal array.</param>
        /// <returns>Matches or not.</returns>
        public static bool DimensionIsMatched<T>(this MemoryMatrix<T> memoryMatrix, T[,] array) where T : struct
        {
            bool result;

            if (array.GetLength(0) == memoryMatrix.Row &&
                array.GetLength(1) == memoryMatrix.Column)
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Copy from array to MemoryMatrix.
        /// </summary>
        /// <typeparam name="T">Type of the element.</typeparam>
        /// <param name="memoryMatrix">Memory matrix.</param>
        /// <param name="array">2-dimensioal array.</param>
        public static void CopyFrom<T>(this MemoryMatrix<T> memoryMatrix, T[,] array) where T : struct
        {
            if (!memoryMatrix.DimensionIsMatched(array))
            {
                string message = Helper.NeutralFormat("Dimensions of array[{0}, {1}] " +
                    "and MemoryMatrix [{2}][{3}] don't match!", array.GetLength(0), array.GetLength(1),
                    memoryMatrix.Row, memoryMatrix.Column);
                throw new ArgumentException(message);
            }

            for (int i = 0; i < array.GetLength(0); ++i)
            {
                for (int j = 0; j < array.GetLength(1); ++j)
                {
                    memoryMatrix[i][j] = array[i, j];
                }
            }
        }

        /// <summary>
        /// Duplicate the matrix.
        /// </summary>
        /// <typeparam name="T">T.</typeparam>
        /// <param name="memoryMatrix">MemoryMatrix.</param>
        /// <returns>Ret.</returns>
        public static MemoryMatrix<T> Duplicate<T>(this MemoryMatrix<T> memoryMatrix) where T : struct
        {
            MemoryMatrix<T> ret = new MemoryMatrix<T>(memoryMatrix.Row, memoryMatrix.Column);
            ret.CopyFrom(memoryMatrix.ToArray());
            return ret;
        }

        /// <summary>
        /// Set MemoryMatrix values to those in source array.
        /// </summary>
        /// <typeparam name="T">Type of the element.</typeparam>
        /// <param name="memoryMatrix">Memory matrix.</param>
        /// <param name="source">2-dimensioal array.</param>
        /// <param name="offset">The raw index offset.</param>
        public static void SetValues<T>(this MemoryMatrix<T> memoryMatrix, T[,] source, int offset) where T : struct
        {
            if (memoryMatrix.Row + offset > source.GetLength(0))
            {
                string message = Helper.NeutralFormat("Dimension of source[{0},] " +
                    "and MemoryMatrix [{1}][] don't match!", source.GetLength(0), memoryMatrix.Row);
                throw new ArgumentException(message);
            }

            if (memoryMatrix.Column != source.GetLength(1))
            {
                string message = Helper.NeutralFormat("Dimension of source[, {0}] " +
                    "and MemoryMatrix [][{1}] don't match!", source.GetLength(1), memoryMatrix.Column);
                throw new ArgumentException(message);
            }

            for (int i = 0; i < memoryMatrix.Row; ++i)
            {
                for (int j = 0; j < memoryMatrix.Column; ++j)
                {
                    memoryMatrix[i][j] = source[i + offset, j];
                }
            }
        }
    }
}