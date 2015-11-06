//----------------------------------------------------------------------------
// <copyright file="Feature.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Feature
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Cart
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    /// <summary>
    /// Feature.
    /// </summary>
    public class Feature
    {
        #region Fields

        private int _index;
        private int _metaFeatureIndex;

        private Collection<int> _values = new Collection<int>();
        private MetaCart _metaCart;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Feature"/> class.
        /// </summary>
        /// <param name="metaCart">CART metadata.</param>
        public Feature(MetaCart metaCart)
        {
            if (metaCart == null)
            {
                throw new ArgumentNullException("metaCart");
            }

            _metaCart = metaCart;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets MetaCart.
        /// </summary>
        public MetaCart MetaCart
        {
            get { return _metaCart; }
        }

        /// <summary>
        /// Gets or sets Index.
        /// </summary>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        /// <summary>
        /// Gets or sets Index in metadata.
        /// </summary>
        public int MetaFeatureIndex
        {
            get { return _metaFeatureIndex; }
            set { _metaFeatureIndex = value; }
        }

        /// <summary>
        /// Gets Values or scope.
        /// </summary>
        public Collection<int> Values
        {
            get { return _values; }
        }

        #endregion

        #region Presentation

        /// <summary>
        /// Gets description.
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder ret = new StringBuilder();
                MetaFeature mf = MetaCart.IndexedMetaFeatures[MetaFeatureIndex];

                ret.Append(mf.Name);
                ret.Append(":");
                for (int i = 0; i < Values.Count; i++)
                {
                    if (i == Values.Count - 1)
                    {
                        ret.Append(mf.Values[Values[i]]);
                    }
                    else
                    {
                        ret.Append(mf.Values[Values[i]]);
                        ret.Append(",");
                    }
                }

                return ret.ToString();
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Evaluate whether a feature is satisfied with this feature.
        /// </summary>
        /// <param name="feature">Feature.</param>
        /// <returns>True if feature is satisfied with this instance, otherwise false.</returns>
        public bool Test(TtsUnitFeature feature)
        {
            if (feature == null)
            {
                throw new ArgumentNullException("feature");
            }

            int target = feature[MetaFeatureIndex];
            foreach (int v in Values)
            {
                if (v == target)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}