//----------------------------------------------------------------------------
// <copyright file="DictionaryExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements DictionaryExtension class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Extension methods for Dictionary.
    /// </summary>
    public static class DictionaryExtension
    {
        /// <summary>
        /// Get key collection of the Dictionary.
        /// </summary>
        /// <typeparam name="T1">Type of dictionary key.</typeparam>
        /// <typeparam name="T2">Type of dictionary value.</typeparam> 
        /// <param name="sources">The source list.</param>
        /// <returns>Key collection.</returns>
        public static Collection<T1> GetKeyCollection<T1, T2>(this Dictionary<T1, T2> sources)
        {
            Helper.ThrowIfNull(sources);
            Collection<T1> keyCollection = new Collection<T1>();
            sources.Keys.ForEach(key => keyCollection.Add(key));
            return keyCollection;
        }

        /// <summary>
        /// Add item to a dictionary with count.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="dictionary">The source dictionary.</param>
        /// <param name="item">The item to add.</param>
        public static void Add<T>(this IDictionary<T, int> dictionary, T item)
        {
            if (dictionary.ContainsKey(item))
            {
                dictionary[item]++;
            }
            else
            {
                dictionary[item] = 1;
            }
        }
    }
}