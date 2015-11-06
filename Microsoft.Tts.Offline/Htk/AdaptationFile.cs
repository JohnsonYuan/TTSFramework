//----------------------------------------------------------------------------
// <copyright file="AdaptationFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements several classes related with loading files used in voice 
//     adaptation. For example, base class files, transform matrix files, etc.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Utility;

    /// <summary>
    /// Voice adaptation transform type.
    /// </summary>
    public enum AdaptationTransformType
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        UNDEFINED = 0,

        /// <summary>
        /// Cmp.
        /// </summary>
        CMP = 1,

        /// <summary>
        /// State duration.
        /// </summary>
        StateDuration = 2,

        /// <summary>
        /// Phone duration.
        /// </summary>
        PhoneDuration = 3,
    }

    /// <summary>
    /// Voice adaptation transform type.
    /// </summary>
    public enum TransformType
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        UNDEFINED = 0,

        /// <summary>
        /// Mean.
        /// </summary>
        MEAN = 1,

        /// <summary>
        /// Variance.
        /// </summary>
        VARIANCE = 2,
    }

    /// <summary>
    /// The base class definition file used in voice adaptation process.
    /// </summary>
    public class HtsRegressionBaseFile
    {
        private Dictionary<int, Collection<ModelItem>> _classDefinition = new Dictionary<int, Collection<ModelItem>>();

        #region properties

        /// <summary>
        /// Gets or sets the stream count.
        /// </summary>
        public int StreamCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the width of each stream.
        /// </summary>
        public int[] StreamWidths
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the class count.
        /// </summary>
        public int ClassCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the base class definition. Key = class ID. Value = list of base class definition.
        /// </summary>
        public Dictionary<int, Collection<ModelItem>> ClassDef
        {
            get
            {
                return _classDefinition;
            }
        }

        #endregion

        /// <summary>
        /// Loads base class file. The file path is "..\va.cmpRegTree\AdaptationRegression.base".
        /// </summary>
        /// <param name="filePath">The given file name.</param>
        public void Load(string filePath)
        {
            Helper.ThrowIfFileNotExist(filePath);

            _classDefinition.Clear();
            using (StreamReader sr = new StreamReader(filePath))
            {
                Regex streamInfoPattern = new Regex(@"<STREAMINFO>\s+(\d+)\s+(.+)");
                Regex numClassPattern = new Regex(@"<NUMCLASSES>\s+(\d+)");
                Regex classPattern = new Regex(@"<CLASS>\s+(\d+)\s+{(\S+)}");

                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = streamInfoPattern.Match(line);
                    if (match.Success)
                    {
                        StreamCount = int.Parse(match.Groups[1].Value);
                        string[] widths = match.Groups[2].Value.Split(' ');
                        StreamWidths = new int[widths.Length];
                        for (int index = 0; index < widths.Length; ++index)
                        {
                            StreamWidths[index] = int.Parse(widths[index]);
                        }

                        continue;
                    }

                    match = numClassPattern.Match(line);
                    if (match.Success)
                    {
                        ClassCount = int.Parse(match.Groups[1].Value);
                        continue;
                    }

                    match = classPattern.Match(line);
                    if (match.Success)
                    {
                        int classIndex = int.Parse(match.Groups[1].Value);
                        string[] items = match.Groups[2].Value.Split(',');
                        Collection<ModelItem> classItems = new Collection<ModelItem>();
                        foreach (string item in items)
                        {
                            ModelItem classItem = new ModelItem(item);
                            classItems.Add(classItem);
                        }

                        _classDefinition.Add(classIndex, classItems);
                    }
                }
            }
        }

        /// <summary>
        /// The linear transform matrixes.
        /// </summary>
        public class ModelItem
        {
            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="ModelItem"/> class.
            /// </summary>
            /// <param name="baseItem"> The model item string. </param>
            public ModelItem(string baseItem)
            {
                Parse(baseItem);
            }

            #endregion

            #region properties

            /// <summary>
            /// Gets or sets the name of model.
            /// </summary>
            public string Name
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the index of state.
            /// </summary>
            public int StateIndex
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the index of stream.
            /// </summary>
            public int StreamIndex
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the index of mixture.
            /// </summary>
            public int MixtureIndex
            {
                get;
                set;
            }

            #endregion

            /// <summary>
            /// Parse model item string.
            /// </summary>
            /// <param name="baseItem"> The model item string. </param>
            public void Parse(string baseItem)
            {
                Regex cmpPattern = new Regex(@"(\S+)\.state\[(\d+)]\.stream\[(\d+)]\.mix\[(\d+)]");
                Regex durPattern = new Regex(@"(\S+)\.state\[(\d+)]\.mix\[(\d+)]");
                Match cmpMatch = cmpPattern.Match(baseItem);
                Match durMatch = durPattern.Match(baseItem);
                if (cmpMatch.Success)
                {
                    Name = cmpMatch.Groups[1].Value;
                    StateIndex = int.Parse(cmpMatch.Groups[2].Value);
                    StreamIndex = int.Parse(cmpMatch.Groups[3].Value);
                    MixtureIndex = int.Parse(cmpMatch.Groups[4].Value);
                }
                else if (durMatch.Success)
                {
                    Name = durMatch.Groups[1].Value;
                    StateIndex = int.Parse(durMatch.Groups[2].Value);
                    StreamIndex = 0;
                    MixtureIndex = int.Parse(durMatch.Groups[3].Value);
                }
            }
        }
    }

    /// <summary>
    /// The transform matrix file used in voice adaptation process.
    /// </summary>
    public class HtsTransformFile
    {
        private Dictionary<int, Collection<int>> _meanFormMapping = new Dictionary<int, Collection<int>>();
        private Dictionary<int, Collection<int>> _varFormMapping = new Dictionary<int, Collection<int>>();
        private Dictionary<int, LinXForm> _meanForms = new Dictionary<int, LinXForm>();
        private Dictionary<int, LinXForm> _varForms = new Dictionary<int, LinXForm>();

        #region properties

        /// <summary>
        /// Gets the mean transform mapping. 
        /// Key = mean transform matrix ID. Value = base class indexes that shared the same transform matrix.
        /// </summary>
        public Dictionary<int, Collection<int>> MeanFormMapping
        {
            get
            {
                return _meanFormMapping;
            }
        }

        /// <summary>
        /// Gets the variance transform mapping. 
        /// Key = variance transform matrix ID. Value = base classes that shared the same transform matrix.
        /// In current adaptation process, the "MeanFormMapping" is exactly same with "VarFormMapping".
        /// </summary>
        public Dictionary<int, Collection<int>> VarFormMapping
        {
            get
            {
                return _varFormMapping;
            }
        }

        /// <summary>
        /// Gets the mean transforms. 
        /// </summary>
        public Dictionary<int, LinXForm> MeanForms
        {
            get
            {
                return _meanForms;
            }
        }

        /// <summary>
        /// Gets the variance transforms. 
        /// </summary>
        public Dictionary<int, LinXForm> VarForms
        {
            get
            {
                return _varForms;
            }
        }

        #endregion

        /// <summary>
        /// Loads the transform matrix file.
        /// </summary>
        /// <param name="filePath">The given file name.</param>
        public void Load(string filePath)
        {
            Helper.ThrowIfFileNotExist(filePath);

            _meanFormMapping.Clear();
            _varFormMapping.Clear();
            _meanForms.Clear();
            _varForms.Clear();

            using (StreamReader sr = new StreamReader(filePath))
            {
                string varLine = "<XFORMKIND>MLLRVAR";
                Regex classXFormPattern = new Regex(@"<CLASSXFORM>\s+(\d+)\s+(\d+)");
                Regex numXFormPattern = new Regex(@"<NUMXFORMS>\s+(\d+)");
                bool isMLLRVar = false;

                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == varLine)
                    {
                        isMLLRVar = true;
                        continue;
                    }

                    Match match = numXFormPattern.Match(line);
                    if (match.Success)
                    {
                        int numberOfTransform = int.Parse(match.Groups[1].Value);
                        for (int i = 0; i < numberOfTransform; ++i)
                        {
                            LinXForm xForm = LoadLinearTransform(sr);
                            if (!isMLLRVar)
                            {
                                xForm.Type = TransformType.MEAN;
                                MeanForms.Add(xForm.ID, xForm);
                            }
                            else
                            {
                                xForm.Type = TransformType.VARIANCE;
                                VarForms.Add(xForm.ID, xForm);
                            }
                        }

                        continue;
                    }

                    match = classXFormPattern.Match(line);
                    if (match.Success)
                    {
                        int classIndex = int.Parse(match.Groups[1].Value);
                        int transformIndex = int.Parse(match.Groups[2].Value);

                        if (!isMLLRVar)
                        {
                            if (!_meanFormMapping.ContainsKey(transformIndex))
                            {
                                _meanFormMapping.Add(transformIndex, new Collection<int>());
                            }

                            _meanFormMapping[transformIndex].Add(classIndex);
                        }
                        else
                        {
                            if (!_varFormMapping.ContainsKey(transformIndex))
                            {
                                _varFormMapping.Add(transformIndex, new Collection<int>());
                            }

                            _varFormMapping[transformIndex].Add(classIndex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads one linear transform from the stream.
        /// </summary>
        /// <param name="sr">Stream reader that contains the transform matrix.</param>
        /// <returns>The loaded linear transform.</returns>
        private LinXForm LoadLinearTransform(StreamReader sr)
        {
            LinXForm xform = new LinXForm();

            Regex idPattern = new Regex(@"<LINXFORM>\s+(\d+)");
            Regex vecSizePattern = new Regex(@"<VECSIZE>\s+(\d+)");
            Regex biasPattern = new Regex(@"<BIAS>\s+(\d+)");
            string offsetTag = "<OFFSET>", blockInfoTag = "<BLOCKINFO>";

            // Process "<LINXFORM> "
            string line = sr.ReadLine();
            Match match = idPattern.Match(line);
            Debug.Assert(match.Success);
            xform.ID = int.Parse(match.Groups[1].Value);

            // Process "<VECSIZE> "
            line = sr.ReadLine();
            match = vecSizePattern.Match(line);
            Debug.Assert(match.Success);
            xform.VecSize = int.Parse(match.Groups[1].Value);

            // Process "<OFFSET>" in mean form or "<LOGDET>" in var form
            line = sr.ReadLine();
            if (line == offsetTag)
            {
                // Process "<BIAS>"
                line = sr.ReadLine();
                match = biasPattern.Match(line);
                Debug.Assert(match.Success);
                xform.Bias = new float[int.Parse(match.Groups[1].Value)];

                line = sr.ReadLine();
                string[] sArr = line.Trim().Split(new char[] { ' ' });
                Debug.Assert(xform.Bias.Length == sArr.Length);
                for (int i = 0; i < xform.Bias.Length; ++i)
                {
                    xform.Bias[i] = float.Parse(sArr[i]);
                }
            }

            while (!line.StartsWith(blockInfoTag))
            {
                line = sr.ReadLine();
            }

            // Process "<BLOCKINFO> "
            string[] strArr = line.Split(new char[] { ' ' });
            Debug.Assert(strArr[0] == blockInfoTag);
            int numBlock = int.Parse(strArr[1]);
            for (int i = 0; i < numBlock; ++i)
            {
                int blockSize = int.Parse(strArr[i + 2]);
                float[,] matrix = new float[blockSize, blockSize];
                xform.Blocks.Add(matrix);

                sr.ReadLine();  // Skip "<BLOCK>"
                sr.ReadLine();  // Skip "<XFORM>"

                // Loads transform matrix value.
                for (int j = 0; j < blockSize; ++j)
                {
                    line = sr.ReadLine();
                    string[] sArr = line.Trim().Split(new char[] { ' ' });
                    Debug.Assert(blockSize == sArr.Length);
                    for (int k = 0; k < blockSize; ++k)
                    {
                        matrix[j, k] = float.Parse(sArr[k]);
                    }
                }
            }

            return xform;
        }
    }

    /// <summary>
    /// The linear transform matrixes.
    /// </summary>
    public class LinXForm
    {
        #region Private fields
        private List<float[,]> _blocks = new List<float[,]>();
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="LinXForm"/> class.
        /// </summary>
        public LinXForm()
        {
            Type = TransformType.UNDEFINED;
        }
        #endregion

        /// <summary>
        /// Gets or sets the type of current linear transform. 
        /// </summary>
        public TransformType Type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ID of current linear transform. 
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the vector size of current linear transform. 
        /// </summary>
        public int VecSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the bias array of current linear transform. 
        /// </summary>
        public float[] Bias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the transform matrix values of current linear transform.
        /// </summary>
        public List<float[,]> Blocks
        {
            get
            {
                return _blocks;
            }
        }
    }

    /// <summary>
    /// The mapping relationship between transform matrix and HMM models, used for voice adaptation debug and investigation.
    /// </summary>
    public class AdaptationMapping
    {
        private Dictionary<int, Collection<string>> _transform2ModelMapping = new Dictionary<int, Collection<string>>();
        private Dictionary<string, int> _model2TransformMapping = null;

        /// <summary>
        /// Gets The mapping relationship from transform matrix index to HMM models.
        /// </summary>
        public Dictionary<int, Collection<string>> Transform2ModelMapping
        {
            get { return _transform2ModelMapping; }
        }

        /// <summary>
        /// Gets the transform matrix index from the model name.
        /// </summary>
        /// <param name="modelName">The model name.</param>
        /// <returns>The transform matrix index.</returns>
        public int GetTransformIndexFromModelName(string modelName)
        {
            if (_model2TransformMapping == null)
            {
                _model2TransformMapping = new Dictionary<string, int>();
                foreach (KeyValuePair<int, Collection<string>> pair in Transform2ModelMapping)
                {
                    if (pair.Key == 0)
                    {
                        // The transform matrix ID 0 is meaningless
                        continue;
                    }

                    foreach (string model in pair.Value)
                    {
                        Debug.Assert(!_model2TransformMapping.ContainsKey(model));
                        _model2TransformMapping.Add(model, pair.Key);
                    }
                }
            }

            int transformIndex  = -1;
            if (_model2TransformMapping.ContainsKey(modelName))
            {
                transformIndex = _model2TransformMapping[modelName];
            }

            return transformIndex;
        }
    }

    /// <summary>
    /// The transform matrix and stream mapping file used for voice adaptation debugging.
    /// </summary>
    public class AdaptationMappingFile
    {
        private Dictionary<AdaptationTransformType, AdaptationMapping> _adaptationMappings = 
            new Dictionary<AdaptationTransformType, AdaptationMapping>();

        /// <summary>
        /// Gets The adaptation mapping(Key = feature discription. Value = transform and stream mapping).
        /// </summary>
        public Dictionary<AdaptationTransformType, AdaptationMapping> AdaptationMappings
        {
            get
            {
                return _adaptationMappings;
            }
        }

        /// <summary>
        /// Creates mapping relationship between transform matrix and shared streams.
        /// </summary>
        /// <param name="mmfFile">The HMM model file, including unseen model.</param>
        /// <param name="regressionBaseFile">The regression base class definition file.</param>
        /// <param name="xformFile">The transform matrix file.</param>
        /// <returns>The mapping relationship between transform matrix and shared streams.</returns>
        public static AdaptationMapping CreateXformStreamMapping(string mmfFile, string regressionBaseFile, string xformFile)
        {
            Dictionary<string, string[,]> hmmStateMapping = HmmReader.GetHmmAndStateMapping(mmfFile);

            HtsRegressionBaseFile baseClassFile = new HtsRegressionBaseFile();
            baseClassFile.Load(regressionBaseFile);

            HtsTransformFile transformFile = new HtsTransformFile();
            transformFile.Load(xformFile);

            AdaptationMapping adaptationMapping = new AdaptationMapping();
            foreach (KeyValuePair<int, Collection<int>> pair in transformFile.MeanFormMapping)
            {
                int xFormIndex = pair.Key;
                Debug.Assert(!adaptationMapping.Transform2ModelMapping.ContainsKey(xFormIndex));
                adaptationMapping.Transform2ModelMapping.Add(xFormIndex, new Collection<string>());

                foreach (int baseClassIndex in pair.Value)
                {
                    foreach (var baseItem in baseClassFile.ClassDef[baseClassIndex])
                    {
                        string[,] streamList = hmmStateMapping[baseItem.Name];
                        string streamName = streamList[0, 0]; // for duration model
                        if (baseItem.StreamIndex > 0)
                        {
                            streamName = streamList[baseItem.StateIndex - 2, baseItem.StreamIndex - 1];
                        }

                        if (!adaptationMapping.Transform2ModelMapping[xFormIndex].Contains(streamName))
                        {
                            adaptationMapping.Transform2ModelMapping[xFormIndex].Add(streamName);
                        }
                    }
                }
            }

            return adaptationMapping;
        }

        /// <summary>
        /// Saves adaptation mapping file.
        /// </summary>
        /// <param name="filePath">The given file name.</param>
        public void Save(string filePath)
        {
            Helper.ThrowIfNull(filePath);

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                foreach (KeyValuePair<AdaptationTransformType, AdaptationMapping> pair in _adaptationMappings)
                {
                    AdaptationMapping adaptationMapping = pair.Value;
                    string typeName = pair.Key.ToString();

                    sw.WriteLine("{0}\t{1}", typeName, adaptationMapping.Transform2ModelMapping.Count);
                    List<int> keyList = new List<int>(adaptationMapping.Transform2ModelMapping.Keys);
                    keyList.Sort();    // for the ease of check.
                    foreach (int transformIndex in keyList)
                    {
                        Collection<string> sharedStreams = adaptationMapping.Transform2ModelMapping[transformIndex];
                        sw.Write("{0}\t{1}\t", transformIndex, sharedStreams.Count);
                        foreach (string stream in sharedStreams)
                        {
                            sw.Write("{0}\t", stream);
                        }

                        sw.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// Loads adaptation mapping file.
        /// </summary>
        /// <param name="filePath">The given file name.</param>
        public void Load(string filePath)
        {
            Helper.ThrowIfFileNotExist(filePath);

            _adaptationMappings.Clear();
            Append(filePath);
        }

        /// <summary>
        /// Append adaptation mapping file.
        /// </summary>
        /// <param name="filePath">The given file name.</param>
        public void Append(string filePath)
        {
            Helper.ThrowIfFileNotExist(filePath);

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    AdaptationMapping oneMapping = new AdaptationMapping();

                    string[] items = line.Split(new char[] { '\t', ' ' });
                    AdaptationTransformType type = (AdaptationTransformType)Enum.Parse(typeof(AdaptationTransformType), items[0], true);
                    int transformNum = int.Parse(items[1]);
                    
                    for (int i = 0; i < transformNum; ++i)
                    {
                        line = sr.ReadLine();
                        items = line.Split(new char[] { '\t', ' ' });

                        Collection<string> modelCollection = new Collection<string>();
                        int transformIndex = int.Parse(items[0]);
                        int modelNum = int.Parse(items[1]);
                        for (int j = 0; j < modelNum; ++j)
                        {
                            modelCollection.Add(items[j + 2]);
                        }

                        oneMapping.Transform2ModelMapping.Add(transformIndex, modelCollection);
                    }

                    _adaptationMappings.Add(type, oneMapping);
                }
            }
        }
    }
}
