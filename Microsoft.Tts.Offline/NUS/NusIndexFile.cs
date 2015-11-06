//----------------------------------------------------------------------------
// <copyright file="NusIndexFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements NUS classes
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.NUS
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// NuuCandidate.
    /// </summary>
    public class NuuCandidate
    {
        /// <summary>
        /// Gets or sets ScriptItemId.
        /// </summary>
        public string ScriptItemId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets OffsetWords.
        /// </summary>
        public int OffsetWords
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets LenWords.
        /// </summary>
        public int LenWords
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The pronunciation string of this candidate.
        /// </summary>
        public string PronString
        {
            get;
            set;
        }
    }

    /// <summary>
    /// NuuGroup.
    /// </summary>
    public class NuuGroup
    {
        /// <summary>
        /// Gets or sets Text.
        /// </summary>
        public string Text
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Candidates.
        /// </summary>
        public NuuCandidate[] Candidates
        {
            get;
            set;
        }
    }

    /// <summary>
    /// NusIndexFile.
    /// </summary>
    public class NusIndexFile : XmlDataFile
    {
        private NuuGroup[] _nuuGroups = null;

        /// <summary>
        /// Gets NuuGroups.
        /// </summary>
        public NuuGroup[] NuuGroups
        {
            get { return _nuuGroups; }
        }

        /// <summary>
        /// Gets Microsoft.Tts.Offline.Schema.NusIndex.xsd.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                return XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.NusIndex.xsd");
            }
        }

        /// <summary>
        /// PerformanceLoad.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="contentController">ContentController.</param>
        protected override void PerformanceLoad(System.IO.StreamReader reader, object contentController)
        {
            XmlHelper.Validate(reader.BaseStream, XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.NusIndex.xsd"));
            XDocument xdoc = XDocument.Load(reader);
            var ns = xdoc.Root.Name.Namespace;
            List<NuuGroup> nuuGroups = new List<NuuGroup>();
            xdoc.Root.Descendants(ns + "nuuGroup").ForEach(nuuGroup =>
                {
                    NuuGroup group = new NuuGroup();
                    nuuGroups.Add(group);
                    group.Text = nuuGroup.Element(ns + "text").Value;
                    List<NuuCandidate> candidates = new List<NuuCandidate>();
                    nuuGroup.Element(ns + "nuuList").Elements(ns + "nuu").ForEach(candidate =>
                        {
                            NuuCandidate nuuCandidate = new NuuCandidate();
                            candidates.Add(nuuCandidate);
                            nuuCandidate.ScriptItemId = candidate.Attribute(ns + "sid").Value;
                            nuuCandidate.OffsetWords = int.Parse(candidate.Attribute(ns + "start").Value);
                            nuuCandidate.LenWords = int.Parse(candidate.Attribute(ns + "len").Value);
                        });
                    group.Candidates = candidates.ToArray();
                });

            _nuuGroups = nuuGroups.ToArray();
        }
    }
}