//----------------------------------------------------------------------------
// <copyright file="DomainExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script Word class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.TTS.ServiceProvider.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Extend service provider domain class.
    /// </summary>
    public class DomainExtension
    {
        public const TtsDomain DefaultDomain = TtsDomain.TTS_DOMAIN_NONE;

        private static Dictionary<string, TtsDomain> _domainNameIdMap;

        static DomainExtension()
        {
            _domainNameIdMap =
                Enum.GetValues(typeof(TtsDomain))
                .Cast<TtsDomain>().ToDictionary(d => Domain.EnumToName(d), d => d);
        }

        /// <summary>
        /// Map from string domain tag to enum.
        /// </summary>
        /// <param name="domain">The string domain.</param>
        /// <returns>The enum domain tag.</returns>
        public static TtsDomain MapToEnum(string domain)
        {
            var ret = DefaultDomain;

            if (!string.IsNullOrEmpty(domain))
            {
                if (!_domainNameIdMap.ContainsKey(domain))
                {
                    throw new NotSupportedException("Unknown domain:" + domain);
                }

                ret = _domainNameIdMap[domain];
            }

            return ret;
        }
    }
}