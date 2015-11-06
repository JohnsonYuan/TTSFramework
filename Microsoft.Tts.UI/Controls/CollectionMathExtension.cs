//----------------------------------------------------------------------------
// <copyright file="CollectionMathExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     definition of abstract class ViewDataBase
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Reflection;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Extensions for collection.
    /// </summary>
    public static class CollectionMatchExtension
    {
        public delegate bool IsSkipCallback(double value);

        /// <summary>
        /// Do liner transform between 2 samples.
        /// </summary>
        /// <param name="collection">Collection.</param>
        /// <param name="index1">Index1.</param>
        /// <param name="index2">Index2.</param>
        /// <param name="skipCallback">SkipCallback.</param>
        public static void LinerTransform(this Collection<double> collection, int index1, int index2, IsSkipCallback skipCallback)
        {
            if (index1 > index2)
            {
                int n = index2;
                index2 = index1;
                index1 = n;
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (index2 > index1 + 1 &&
                !double.IsNaN(collection[index1]) &&
                !double.IsNaN(collection[index2]))
            {
                double k = (double)(collection[index2] - collection[index1]) / (index2 - index1);
                double n = (double)collection[index1];
                while (index1++ < index2)
                {
                    n += k;
                    if (skipCallback == null || !skipCallback(collection[index1]))
                    {
                        collection[index1] = n;
                    }
                }
            }
        }

        /// <summary>
        /// Do liner transform between 2 samples.
        /// </summary>
        /// <param name="collection">Collection.</param>
        /// <param name="index1">Index1.</param>
        /// <param name="index2">Index2.</param>
        public static void LinerTransform(this Collection<double> collection, int index1, int index2)
        {
            LinerTransform(collection, index1, index2, null);
        }

        /// <summary>
        /// Do resample on specific location.
        /// </summary>
        /// <param name="collection">Collection.</param>
        /// <param name="ratio">Ratio.</param>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        /// <returns>Return collection.</returns>
        public static Collection<double> Resample(
            this Collection<double> collection, double ratio, int start, int end)
        {
            if (ratio < 0 || start > end)
            {
                throw new ArgumentException("Resample parameters are invalid.");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            Collection<double> ret = new Collection<double>();
            if (ratio > 0)
            {
                int nLen = (int)Math.Round((end - start) * ratio);
                ratio = 1 / ratio;
                int newStart = 0;
                int nIndex = start;
                double dIndex = start;
                for (int i = 0; i < nLen; i++)
                {
                    ret.Add(collection[nIndex]);
                    if (nIndex != start)
                    {
                        if (i > newStart + 1)
                        {
                            ret.LinerTransform(newStart, i);
                        }

                        newStart = i;
                        start = nIndex;
                    }

                    dIndex += ratio;
                    nIndex = (int)Math.Round(dIndex);
                }
            }

            return ret;
        }

        /// <summary>
        /// Add items.
        /// </summary>
        /// <typeparam name="T">T.</typeparam>
        /// <param name="collection">Collection.</param>
        /// <param name="items">Items.</param>
        public static void Add<T>(this Collection<T> collection, Collection<T> items)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            foreach (T item in items)
            {
                collection.Add(item);
            }
        }

        /// <summary>
        /// Clear target and copy to target.
        /// </summary>
        /// <typeparam name="T">T.</typeparam>
        /// <param name="collection">Collection.</param>
        /// <param name="target">Target.</param>
        public static void DuplicateTo<T>(this Collection<T> collection, Collection<T> target)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.Clear();
            target.Add(collection);
        }

        /// <summary>
        /// Do log based on E on the collection.
        /// </summary>
        /// <param name="collection">Collection.</param>
        public static void LogE(this Collection<double> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            for (int i = 0; i < collection.Count; i++)
            {
                collection[i] = Math.Log(collection[i], Math.E);
            }
        }

        /// <summary>
        /// Do pow based on E on the collection.
        /// </summary>
        /// <param name="collection">Collection.</param>
        public static void PowE(this Collection<double> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            for (int i = 0; i < collection.Count; i++)
            {
                collection[i] = Math.Pow(Math.E, collection[i]);
            }
        }

        /// <summary>
        /// Replace the old value with new value.
        /// </summary>
        /// <param name="collection">Collection.</param>
        /// <param name="oldValue">OldValue.</param>
        /// <param name="newValue">NewValue.</param>
        public static void Replace(this Collection<double> collection, double oldValue, double newValue)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i] == oldValue)
                {
                    collection[i] = newValue;
                }
            }
        }
    }
}