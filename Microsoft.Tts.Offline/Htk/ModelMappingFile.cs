//----------------------------------------------------------------------------
// <copyright file="ModelMappingFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements model mapping file related classes
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Imiplementation of class ModelMappingFile.
    /// </summary>
    public class ModelMappingFile
    {   
        /// <summary>
        /// ModelSet Number Tag.
        /// </summary>
        public const string ModelSetNumTag = @"<ModelSetNumber>";

        /// <summary>
        /// ModelSet Name.
        /// </summary>
        public const string ModelSetNameTag = @"<ModelSetName>";

        private string _fileName;
        private Collection<ModelMappingSet> _modelMappingSets = new Collection<ModelMappingSet>();
        private Dictionary<string, ulong> _modelKeys = new Dictionary<string, ulong>();

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelMappingFile"/> class.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        public ModelMappingFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            Load(fileName);

            _fileName = fileName;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets File name.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// Gets Model keys. From model name to model key.
        /// </summary>
        public Dictionary<string, ulong> ModelKeys
        {
            get { return _modelKeys; }
        }

        #endregion

        #region public operations

        /// <summary>
        /// Get model mapping set .
        /// </summary>
        /// <param name="type">The hmm model type.</param>
        /// <returns>Model mapping set.</returns>
        public ModelMappingSet GetModelMappingSet(HmmModelType type)
        {
            ModelMappingSet model = null;

            foreach (ModelMappingSet set in _modelMappingSets)
            {
                if (set.ModelType == type)
                {
                    model = set;
                    break;
                }
            }

            return model;
        }

        #endregion

        #region private operations

        /// <summary>
        /// Load wave segments strings.
        /// </summary>
        /// <param name="sr">TextReader to be read.</param>
        /// <param name="waveSegsStrings">Wave segments strings.</param>
        /// <param name="waveSegsInfoNum">Wave file number.</param>
        private static void LoadWaveSegsStrings(TextReader sr, Collection<string> waveSegsStrings, uint waveSegsInfoNum)
        {
            Debug.Assert(sr != null);
            Debug.Assert(waveSegsStrings != null);

            for (uint i = 0; i < waveSegsInfoNum; ++i)
            {
                string segsLine = sr.ReadLine();
                Debug.Assert(segsLine != null);
                waveSegsStrings.Add(segsLine);
            }
        }

        /// <summary>
        /// Load model mappings .
        /// </summary>
        /// <param name="sr">TextReader.</param>
        /// <param name="mappings">Mappings read.</param>
        /// <param name="modelNum">Model number.</param>
        private static void LoadModelMappings(TextReader sr, Dictionary<ulong, ModelMapping> mappings, long modelNum)
        {
            Debug.Assert(sr != null);
            Debug.Assert(mappings != null);

            if (modelNum > 0)
            {
                string modelKeyLine;
                long modelsRead = 0;
                while ((modelKeyLine = sr.ReadLine()) != null)
                {
                    string[] items = Regex.Split(modelKeyLine, @"\s+");
                    if (items.Length != 3 || !modelKeyLine.StartsWith(@"~", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                            "model key-name line is invalid: {0}",
                            modelKeyLine));
                    }

                    ModelMapping mapping = new ModelMapping();
                    mapping.ModelName = items[1];
                    string key = items[0].Substring(1, items[0].Length - 1);
                    uint waveSegsInfoNum;
                    try
                    {
                        mapping.ModelKey = ulong.Parse(key, CultureInfo.InvariantCulture);
                        waveSegsInfoNum = uint.Parse(items[2], CultureInfo.InvariantCulture);
                    }
                    catch (FormatException e)
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                            "Model key string \"{0}\"is invalid: {1}",
                            modelKeyLine,
                            e.Message));
                    }

                    LoadWaveSegsStrings(sr, mapping.WaveSegsStrings, waveSegsInfoNum);
                    mappings.Add(mapping.ModelKey, mapping);

                    modelsRead++;
                    if (modelsRead == modelNum)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Load debbuging mapping file.
        /// </summary>
        /// <param name="fileName">File name.</param>
        private void Load(string fileName)
        {
            using (StreamReader sr = new StreamReader(fileName, Encoding.ASCII))
            {
                // skip the first line
                sr.ReadLine();

                string modelSetNameLine;
                while ((modelSetNameLine = sr.ReadLine()) != null)
                {
                    string[] items = Regex.Split(modelSetNameLine, @"\s+");
                    if (items.Length != 3 || !items[0].Equals(ModelSetNameTag))
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, 
                            "model set name line is invalid: {0}", modelSetNameLine));
                    }

                    ModelMappingSet model = new ModelMappingSet();
                    long modelNum;
                    try
                    {
                        model.ModelType = (HmmModelType)Enum.Parse(typeof(HmmModelType), items[1], true);
                        modelNum = long.Parse(items[2], CultureInfo.InvariantCulture);
                    }
                    catch (FormatException e)
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                        "ModelSet name string \"{0}\"is invalid: {1}", modelSetNameLine, e.Message));
                    }

                    LoadModelMappings(sr, model.ModelMappings, modelNum);
                    _modelMappingSets.Add(model);

                    foreach (KeyValuePair<ulong, ModelMapping> pair in model.ModelMappings)
                    {
                        _modelKeys.Add(pair.Value.ModelName, pair.Key);
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Implementation of class ModelMappingSet.
    /// </summary>
    public class ModelMappingSet
    {
        private HmmModelType _type = HmmModelType.Invalid;
        private Dictionary<ulong, ModelMapping> _modelMappings = new Dictionary<ulong, ModelMapping>();

        #region properties

        /// <summary>
        /// Gets or sets Model type.
        /// </summary>
        public HmmModelType ModelType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets Model mappings.
        /// </summary>
        public Dictionary<ulong, ModelMapping> ModelMappings
        {
            get { return _modelMappings; }
        }

        #endregion
    }

    /// <summary>
    /// Implementation of class ModelMapping.
    /// </summary>
    public class ModelMapping
    {
        private ulong _modelKey;
        private string _modelName;
        private Collection<WaveSegsInfo> _waveSegsInfos = new Collection<WaveSegsInfo>();
        private Collection<string> _waveSegsStrings = new Collection<string>();
        private bool _parsed;

        #region properties

        /// <summary>
        /// Gets or sets The key of the model.
        /// </summary>
        [CLSCompliantAttribute(false)]
        public ulong ModelKey
        {
            get { return _modelKey; }
            set { _modelKey = value; }
        }

        /// <summary>
        /// Gets or sets Model name.
        /// </summary>
        public string ModelName
        {
            get
            {
                return _modelName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _modelName = value;
            }
        }

        /// <summary>
        /// Gets The wave segs infomation.
        /// </summary>
        public Collection<WaveSegsInfo> WaveSegsInfos
        {
            get
            {
                if (!_parsed)
                {
                    ParseStringsToSegs();
                }

                return _waveSegsInfos;
            }
        }

        /// <summary>
        /// Gets The wave segs strings.
        /// </summary>
        public Collection<string> WaveSegsStrings
        {
            get { return _waveSegsStrings; }
        }

        #endregion

        #region private operations

        /// <summary>
        /// Parse strings into segments.
        /// </summary>
        private void ParseStringsToSegs()
        {
            _waveSegsInfos.Clear();
            foreach (string segString in _waveSegsStrings)
            {
                string[] items = Regex.Split(segString, @"\s+");

                if (items.Length < 3 || items.Length % 2 != 1)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                        "Segment string is invalid: {0}", segString));
                }

                WaveSegsInfo segsInfo = new WaveSegsInfo();
                segsInfo.FileName = items[0];
                for (int j = 0; j < items.Length / 2; ++j)
                {
                    WaveSeg seg = new WaveSeg();
                    try
                    {
                        seg.StartTime = double.Parse(items[(2 * j) + 1], CultureInfo.InvariantCulture);
                        seg.Length = double.Parse(items[(2 * j) + 2], CultureInfo.InvariantCulture);
                    }
                    catch (FormatException e)
                    {
                        throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                        "Segment string \"{0}\"is invalid: {1}", segString, e.Message));
                    }

                    segsInfo.WaveSegs.Add(seg);
                }

                _waveSegsInfos.Add(segsInfo);
            }

            _parsed = true;
        }

        #endregion
    }

    /// <summary>
    /// Class WaveSegsInfo.
    /// </summary>
    public class WaveSegsInfo
    {
        private string _fileName;
        private Collection<WaveSeg> _waveSegs = new Collection<WaveSeg>();

        #region properties

        /// <summary>
        /// Gets or sets File name.
        /// </summary>
        public string FileName
        {
            get
            {
                return _fileName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _fileName = value;
            }
        }

        /// <summary>
        /// Gets Wave segments.
        /// </summary>
        public Collection<WaveSeg> WaveSegs
        {
            get { return _waveSegs; }
        }

        /// <summary>
        /// Gets String description.
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", FileName);
                foreach (WaveSeg seg in WaveSegs)
                {
                    double endTime = -1;

                    // seg.Length < 0 means that the duration is to the end of wave file
                    if (!(seg.Length < 0))
                    {
                        endTime = seg.StartTime + seg.Length;
                    }

                    sb.AppendFormat(CultureInfo.InvariantCulture, "({0:f3}, {1:f3})", seg.StartTime, endTime);
                }

                return sb.ToString();
            }
        }

        #endregion
    }

    /// <summary>
    /// Class WaveSeg.
    /// </summary>
    public class WaveSeg
    {
        #region Fields

        private double _startTime;
        private double _length;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets The start time of this wave segment.
        /// </summary>
        public double StartTime
        {
            get
            {
                return _startTime;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", @"Start time should NOT be negative!");
                }

                _startTime = value;
            }
        }

        /// <summary>
        /// Gets or sets The time length of this wave segment.
        /// </summary>
        public double Length
        {
            get
            {
                return _length;
            }

            set
            {
                // use -1 to indicate that the duration is to the end of wave file
                if (value < 0)
                {
                    _length = -1;
                }
                else
                {
                    _length = value;
                }
            }
        }

        #endregion
    }
}