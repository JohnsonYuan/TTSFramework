//----------------------------------------------------------------------------
// <copyright file="FrontendSetting.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      FrontendSetting
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Tts.Offline.Config;

    /// <summary>
    /// This is a.
    /// </summary>
    public class FrontendSetting
    {
        #region Const Members
        private const string Domain = "Domain";
        private const string FileName = "FileName";
        private const string GUID = "GUID";
        #endregion

        #region Field Members
        /// <summary>
        /// Key: Domain name, Value: Domain DAT file name.
        /// </summary>
        private IDictionary<string, string> _domainMembers = new Dictionary<string, string>();

        /// <summary>
        /// Sayas mapping.
        /// </summary>
        private IDictionary<string, string> _sayAsMapping = new Dictionary<string, string>();

        /// <summary>
        /// Disabled Datas.
        /// </summary>
        private Guid[] _disabledDatas;

        #endregion

        #region Constructor Members

        /// <summary>
        /// Initializes a new instance of the <see cref="FrontendSetting" /> class.
        /// </summary>
        /// <param name="path">Setting path.</param>
        public FrontendSetting(string path)
        {
            IniFile file = new IniFile();
            file.Load(path);
            if (file.Sections.ContainsKey(Domain))
            {
                IList<KeyValuePair<string, string>> domains = file.Sections[Domain];
                if (domains[0].Key != "Number")
                {
                    throw new ArgumentException("The first line in domain section should be: Number = <num>.");
                }

                int number = int.Parse(domains[0].Value);
                foreach (KeyValuePair<string, string> pair in domains.Where((p) => p.Key.StartsWith(Domain)))
                {
                    int loc = int.Parse(pair.Key.Substring(Domain.Length));
                    if (loc < 0 || loc >= number)
                    {
                        throw new IndexOutOfRangeException(string.Format("Domain{0} should be >= Domain0 and < Domain{1}", loc, number));
                    }

                    string fileName = domains.First((p) => p.Key == FileName + loc).Value;
                    _domainMembers.Add(pair.Value, fileName);
                }
            }

            if (file.Sections.ContainsKey("SayAsMapping"))
            {
                foreach (KeyValuePair<string, string> pair in file.Sections["SayAsMapping"])
                {
                    _sayAsMapping.Add(pair.Key, pair.Value);
                }
            }

            if (file.Sections.ContainsKey("DisabledData"))
            {
                IList<KeyValuePair<string, string>> disabledDatas = file.Sections["DisabledData"];
                if (disabledDatas[0].Key != "Number")
                {
                    throw new ArgumentException("The first line in DisabledData section should be: Number = <num>.");
                }

                int number = int.Parse(disabledDatas[0].Value);
                _disabledDatas = new Guid[number];
                foreach (KeyValuePair<string, string> pair in disabledDatas.Where((p) => p.Key.StartsWith(GUID)))
                {
                    int loc = int.Parse(pair.Key.Substring(GUID.Length));
                    _disabledDatas[loc] = Guid.Parse(pair.Value);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Domain members.
        /// Key: Domain name, Value: Domain DAT file name.
        /// </summary>
        public IDictionary<string, string> DomainMembers
        {
            get { return _domainMembers; }
        }

        /// <summary>
        /// Gets Sayas mapping.
        /// </summary>
        public IDictionary<string, string> SayAsMapping
        {
            get { return _sayAsMapping; }
        }

        /// <summary>
        /// Gets Disabled datas.
        /// </summary>
        public ICollection<Guid> DisabledDatas
        {
            get { return _disabledDatas; }
        }
        #endregion
    }
}
