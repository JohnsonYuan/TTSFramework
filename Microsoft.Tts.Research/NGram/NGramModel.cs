//----------------------------------------------------------------------------
// <copyright file="NGramModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements NGram Model
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider.BaseUtils;

    using GrapId = System.UInt16;
    using ProbabilityInt = System.Int16;
    using ReferenceIndex = System.UInt16;

    /// <summary>
    /// NGram.
    /// </summary>
    public class NGram
    {
        private string _grammar;
        private double _probability;
        private double _backoff;

        /// <summary>
        /// Gets or sets Grammar.
        /// </summary>
        public string Grammar
        {
            get { return _grammar; }
            set { _grammar = value; }
        }

        /// <summary>
        /// Gets or sets Probability.
        /// </summary>
        public double Probability
        {
            get { return _probability; }
            set { _probability = value; }
        }

        /// <summary>
        /// Gets or sets Backoff.
        /// </summary>
        public double Backoff
        {
            get { return _backoff; }
            set { _backoff = value; }
        }
    }

    /// <summary>
    /// Grammar State.
    /// </summary>
    public class GrammarState
    {
        private GrapId _graphId;
        private ProbabilityInt _prob;
        private ProbabilityInt _backoff;
        private ReferenceIndex _referenceIndex;

        /// <summary>
        /// Gets or sets GraphId.
        /// </summary>
        public GrapId GraphId
        {
          get { return _graphId; }
          set { _graphId = value; }
        }

        /// <summary>
        /// Gets or sets Log Probability.
        /// </summary>
        public ProbabilityInt Prob
        {
          get { return _prob; }
          set { _prob = value; }
        }

        /// <summary>
        /// Gets or sets Log Backoff.
        /// </summary>
        public ProbabilityInt Backoff
        {
          get { return _backoff; }
          set { _backoff = value; }
        }

        /// <summary>
        /// Gets or sets Reference Index for higher level gram.
        /// </summary>
        public ReferenceIndex ReferenceIndex
        {
          get { return _referenceIndex; }
          set { _referenceIndex = value; }
        }
    }

    /// <summary>
    /// The complete NGram Model.
    /// </summary>
    public class NGramModel
    {
        /// <summary>
        /// Maximal Gram Number.
        /// </summary>
        public byte MaxGramNumber = 4;

        /// <summary>
        /// Grammar Separator.
        /// </summary>
        public char[] GrammarSeparator = new char[] { ' ' };

        private Language _language;
        private List<char> _symbolList;
        private List<string> _graphemeDictionary;
        private int _grammarCount;
        private int _probabilityAmplifier = 1000;
        private long _maximalGraphemeIndexSize = ((long)1 << (8 * sizeof(GrapId))) - 2;
        private long _maximalGrammarIndexSize = ((long)1 << (8 * sizeof(ReferenceIndex))) - 2;

        private double _minNgramValue = -100.0;

        /// <summary>
        /// The key will be 1, 2, 3, or 4 to indicate 1Gram, 2Gram, 3Gram or 4Gram.
        /// The value is the detail nGram data for each level gram.
        /// For example NGramData[2][grammar] will representate the 2Gram using the grammar.
        /// </summary>
        private Dictionary<int, SortedDictionary<string, NGram>> _nGramData = new Dictionary<int, SortedDictionary<string, NGram>>();
        private byte _maxNgram;

        /// <summary>
        /// Gets Language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
        }

        /// <summary>
        /// Gets Symbol List.
        /// </summary>
        public List<char> SymbolList
        {
            get { return _symbolList; }
        }

        /// <summary>
        /// Gets Grapheme Dictionary.
        /// </summary>
        public List<string> GraphemeDictionary
        {
            get { return _graphemeDictionary; }
        }

        /// <summary>
        /// Gets The Highest level NGram.
        /// </summary>
        public byte MaxNgram
        {
            get { return _maxNgram; }
        }

        /// <summary>
        /// Gets or sets Probability Amplifier.
        /// </summary>
        public int ProbabilityAmplifier
        {
            get { return _probabilityAmplifier; }
            set { _probabilityAmplifier = value; }
        }

        /// <summary>
        /// Gets or sets NGram Data.
        /// </summary>
        public Dictionary<int, SortedDictionary<string, NGram>> NGramData
        {
            get { return _nGramData; }
            set { _nGramData = value; }
        }

        /// <summary>
        /// Load the gram data from file path.
        /// </summary>
        /// <param name="nGramDataFilePath">NGram data file path.</param>
        public void Load(string nGramDataFilePath)
        {
            using (StreamReader xmlStream = new StreamReader(nGramDataFilePath))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlStream);

                XmlNodeList langNodes = doc.SelectNodes("//AllLanguages/Language");
                string langId = langNodes[0].Attributes["ID"].InnerText;
                _language = Localor.StringToLanguage(langId);
                XmlNodeList uniGramNodes = langNodes[0].SelectNodes("Unigram/UniSyllable");
                XmlNodeList biGramNodes = langNodes[0].SelectNodes("Bigram/BiSyllable");
                XmlNodeList triGramNodes = langNodes[0].SelectNodes("Trigram/TriSyllable");
                XmlNodeList fourGramNodes = langNodes[0].SelectNodes("Fourgram/FourSyllable");

                if (uniGramNodes != null)
                {
                    _maxNgram = 1;
                    LoadGramData(1, uniGramNodes);
                    CreateGraphemeAndSymbolList(_nGramData[1]);
                }

                if (biGramNodes != null)
                {
                    _maxNgram = 2;
                    LoadGramData(2, biGramNodes);
                }

                if (uniGramNodes != null)
                {
                    _maxNgram = 3;
                    LoadGramData(3, triGramNodes);
                }

                if (biGramNodes != null)
                {
                    _maxNgram = 4;
                    LoadGramData(4, fourGramNodes);
                }

                Validate();
            }
        }

        /// <summary>
        /// Save the gram data into stream.
        /// </summary>
        /// <param name="stream">Binary stream.</param>
        public void SaveToBinary(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (_grammarCount == 0)
            {
                throw new InvalidDataException("There is no nGram data");
            }

            using (TrieTree graphemeTrieTree = new TrieTree(_graphemeDictionary))
            {
                byte[] graphemeDictData = graphemeTrieTree.GetTrieData();
                int graphemeDictLength = graphemeDictData.Length;
                
                // Keep data alignment as 2 bytes
                if (graphemeDictLength % 2 != 0)
                {
                    graphemeDictLength++;
                }

                // Save NGramData
                GrammarState[] grammarStates = new GrammarState[_grammarCount + 2];
                grammarStates[0] = new GrammarState();
                grammarStates[0].ReferenceIndex = 1;

                Dictionary<string, int> grammarIndex = new Dictionary<string, int>();
                int stateIndex = 1;
                int finalGramStateIndex = 0;
                for (int gram = 1; gram <= _maxNgram; gram++)
                {
                    string lastReferredGrammar = string.Empty;
                    if (gram == _maxNgram)
                    {
                        finalGramStateIndex = stateIndex;
                        grammarStates[stateIndex++] = new GrammarState();
                    }

                    foreach (string grammar in _nGramData[gram].Keys)
                    {
                        string[] graphemes = grammar.Split(GrammarSeparator, StringSplitOptions.RemoveEmptyEntries);

                        // last grapheme
                        string lastGrapheme = graphemes[graphemes.Length - 1];
                        int len = 0;
                        int graphemeId = graphemeTrieTree.FindLongest(lastGrapheme, out len);
                        Debug.Assert(graphemeId != -1);
                        Debug.Assert(!grammarIndex.ContainsKey(grammar));

                        // Save the state index for easily query
                        grammarIndex.Add(grammar, stateIndex);
                        grammarStates[stateIndex] = new GrammarState();
                        grammarStates[stateIndex].GraphId = (GrapId)graphemeId;

                        // Convert the probability into ProbabilityInt type with amplifier
                        if (_nGramData[gram][grammar].Probability * _probabilityAmplifier < ProbabilityInt.MinValue)
                        {
                            grammarStates[stateIndex].Prob = ProbabilityInt.MinValue;
                        }
                        else
                        {
                            grammarStates[stateIndex].Prob = (ProbabilityInt)(_nGramData[gram][grammar].Probability * _probabilityAmplifier);
                        }

                        if (_nGramData[gram][grammar].Backoff * _probabilityAmplifier < short.MinValue)
                        {
                            grammarStates[stateIndex].Backoff = ProbabilityInt.MinValue;
                        }
                        else
                        {
                            grammarStates[stateIndex].Backoff = (ProbabilityInt)(_nGramData[gram][grammar].Backoff * _probabilityAmplifier);
                        }

                        // set the reference index for lower level gram data
                        if (gram != 1)
                        {
                            string referredGrammar = graphemes[0];
                            for (int i = 1; i < graphemes.Length - 1; i++)
                            {
                                referredGrammar = referredGrammar + " " + graphemes[i];
                            }

                            if (!referredGrammar.Equals(lastReferredGrammar, StringComparison.Ordinal))
                            {
                                // Update the reference index for the lower level gram
                                lastReferredGrammar = referredGrammar;
                                Debug.Assert(grammarIndex.ContainsKey(lastReferredGrammar));
                                int referredIndex = grammarIndex[lastReferredGrammar];
                                Debug.Assert(grammarStates[referredIndex] != null);
                                if (gram != _maxNgram)
                                {
                                    grammarStates[referredIndex].ReferenceIndex = (ReferenceIndex)stateIndex;
                                }
                                else
                                {
                                    grammarStates[referredIndex].ReferenceIndex = (ReferenceIndex)(stateIndex - finalGramStateIndex);
                                }
                            }
                        }

                        stateIndex++;
                    }
                }

                // Save the model into binary stream
                BinaryWriter bw = new BinaryWriter(stream);
                {
                    // Write the language ID
                    bw.Write((ushort)_language);

                    // Write the Gram Count
                    bw.Write((ushort)this._maxNgram);

                    // Write the Probability Amplifier
                    bw.Write((int)_probabilityAmplifier);

                    // Write the grammar state number
                    bw.Write((uint)finalGramStateIndex);

                    // Write the Final grammar state number
                    bw.Write((uint)(_grammarCount + 2 - finalGramStateIndex));

                    int headerSize = sizeof(ushort) + sizeof(ushort) + sizeof(int) +
                        sizeof(uint) + sizeof(uint) +
                        sizeof(uint) + sizeof(uint) + sizeof(uint);

                    // Write the offset of Dictionary
                    bw.Write((uint)headerSize);

                    // Write the offset of Grammar State
                    bw.Write((uint)(headerSize + graphemeDictLength));

                    // Write the offset of Final Grammar State
                    bw.Write((uint)(headerSize + graphemeDictLength + (finalGramStateIndex *
                        (sizeof(GrapId) + sizeof(ProbabilityInt) + sizeof(ProbabilityInt) + sizeof(ReferenceIndex)))));

                    // Write the grapheme Trie Dictionary
                    bw.Write(graphemeDictData, 0, graphemeDictData.Length);

                    // Add the data alignment for grapheme Trie Dictionary
                    for (int i = graphemeDictData.Length; i < graphemeDictLength; i++)
                    {
                        bw.Write((byte)0);
                    }

                    // Write the grammar states for low level gram
                    for (int i = 0; i < finalGramStateIndex; i++)
                    {
                        bw.Write(grammarStates[i].GraphId);
                        bw.Write(grammarStates[i].Prob);
                        bw.Write(grammarStates[i].Backoff);
                        bw.Write(grammarStates[i].ReferenceIndex);
                    }

                    // Write the grammar state for final level gram
                    for (int i = finalGramStateIndex; i < _grammarCount + 2; i++)
                    {
                        bw.Write(grammarStates[i].GraphId);
                        bw.Write(grammarStates[i].Prob);
                    }
                }
            }           
        }

        /// <summary>
        /// Decode the gram value for the grapheme list.
        /// </summary>
        /// <param name="graphemeList">GraphemeList.</param>
        /// <returns>NGram value.</returns>
        public double Decode(List<string> graphemeList)
        {
            double ngramValue = 0;
            for (int i = 0; i < graphemeList.Count; i++)
            {
                if (i + 1 < this._maxNgram)
                {
                    ngramValue += GetGramValue(graphemeList, i, i + 1);
                }
                else
                {
                    ngramValue += GetGramValue(graphemeList, i, this._maxNgram);
                }
            }

            return ngramValue;
        }

        /// <summary>
        /// Get the gram value.
        /// </summary>
        /// <param name="graphemeList">Grapheme list.</param>
        /// <param name="curGraphemeIndex">Current grapheme index.</param>
        /// <param name="gramLevel">Gram level.</param>
        /// <returns>Gram value.</returns>
        private double GetGramValue(List<string> graphemeList, int curGraphemeIndex, int gramLevel)
        {
            if (gramLevel < 1)
            {
                throw new ArgumentException("gram should be larger or equal than 1", "gramLevel");
            }

            if (curGraphemeIndex + 1 < gramLevel || curGraphemeIndex >= graphemeList.Count)
            {
                throw new ArgumentException("curNum exceeds the boundary", "curGraphemeIndex");
            }

            if (gramLevel > this._maxNgram)
            {
                throw new ArgumentException("gram level exceeds the maximal value", "gramLevel");
            }

            double ngramValue = 0;

            if (gramLevel == 1)
            {
                if (this._nGramData[1].ContainsKey(graphemeList[curGraphemeIndex]))
                {
                    ngramValue = this._nGramData[1][graphemeList[curGraphemeIndex]].Probability;
                }
                else
                {
                    ngramValue = _minNgramValue;
                }
            }
            else
            {
                string grammar = graphemeList[curGraphemeIndex - gramLevel + 1];
                string lastGrammar = string.Empty;
                for (int i = curGraphemeIndex - gramLevel + 2; i <= curGraphemeIndex; i++)
                {
                    lastGrammar = grammar;
                    grammar = grammar + " " + graphemeList[i];
                }

                if (this._nGramData[gramLevel].ContainsKey(grammar))
                {
                    ngramValue += this._nGramData[gramLevel][grammar].Probability;
                }
                else
                {
                    ngramValue = GetGramValue(graphemeList, curGraphemeIndex, gramLevel - 1);
                    if (this._nGramData[gramLevel - 1].ContainsKey(lastGrammar))
                    {
                        ngramValue += this._nGramData[gramLevel - 1][lastGrammar].Backoff;
                    }
                }
            }

            return ngramValue;
        }

        /// <summary>
        /// Validate the gram data.
        /// </summary>
        private void Validate()
        {
            for (int i = 1; i <= _maxNgram; i++)
            {
                if (_nGramData[i] == null)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Invalid Gram data for {0} level gram", i));
                }
            }

            if (_maxNgram > MaxGramNumber)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Gram number exceed the maximal {0}", MaxGramNumber));
            }

            if (_symbolList == null || _graphemeDictionary == null)
            {
                throw new InvalidDataException("Invalid Gram data: symbol list is empty or dictionary is empty");
            }

            // Verify the each grammar whether have referred lower gram grammar
            int grammarCount = _nGramData[1].Count;
            for (int gram = 2; gram <= _maxNgram; gram++)
            {
                grammarCount += _nGramData[gram].Count;
                foreach (string grammar in _nGramData[gram].Keys)
                {
                    string[] graphemes = grammar.Split(GrammarSeparator, StringSplitOptions.RemoveEmptyEntries);
                    if (graphemes.Length != gram)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Not matched grapheme number {0} in the grammar of {1} level gram", graphemes.Length, gram));
                    }

                    string referrerGrammar = graphemes[0];
                    for (int i = 1; i < graphemes.Length - 1; i++)
                    {
                        referrerGrammar = referrerGrammar + " " + graphemes[i];
                    }

                    if (!_nGramData[gram - 1].ContainsKey(referrerGrammar))
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Could not found lower level gram for grammar \"{0}\"", referrerGrammar));
                    }
                }
            }

            // Verify the grapheme count and grammar count
            if (_graphemeDictionary.Count > _maximalGraphemeIndexSize)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Grapheme count exceeds the maximal {0}", _maximalGraphemeIndexSize));
            }

            if (grammarCount - _nGramData[_maxNgram].Count > _maximalGrammarIndexSize)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Total Grammar count below {0} gram exceeds the maximal {1}: {2}",
                    _maxNgram, _maximalGrammarIndexSize, grammarCount - _nGramData[_maxNgram].Count));
            }

            if (_nGramData[_maxNgram].Count > _maximalGrammarIndexSize)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Total Grammar count for {0} gram exceeds the maximal {1}: {2}",
                    _maxNgram, _maximalGrammarIndexSize, _nGramData[_maxNgram].Count));
            }

            _grammarCount = grammarCount;
        }

        /// <summary>
        /// Create grapheme dictionary and symbol list.
        /// </summary>
        /// <param name="gramData">Gram data.</param>
        private void CreateGraphemeAndSymbolList(SortedDictionary<string, NGram> gramData)
        {
            _symbolList = new List<char>();
            _graphemeDictionary = new List<string>();
            foreach (string grammar in gramData.Keys)
            {
                string[] graphemes = grammar.Split(GrammarSeparator, StringSplitOptions.RemoveEmptyEntries);
                foreach (string grapheme in graphemes)
                {
                    if (!_graphemeDictionary.Contains(grapheme))
                    {
                        _graphemeDictionary.Add(grapheme);
                    }

                    foreach (char symbol in grapheme.ToCharArray())
                    {
                        if (!_symbolList.Contains(symbol))
                        {
                            _symbolList.Add(symbol);
                        }
                    }
                }
            }

            _symbolList.Sort();
            _graphemeDictionary.Sort();
        }

        /// <summary>
        /// Load the gram data.
        /// </summary>
        /// <param name="gram">Gram level.</param>
        /// <param name="gramNodes">Gram nodes.</param>
        private void LoadGramData(int gram, XmlNodeList gramNodes)
        {
            if (gramNodes == null)
            {
                throw new ArgumentNullException("gramNodes");
            }

            if (!_nGramData.ContainsKey(gram))
            {
                _nGramData.Add(gram, new SortedDictionary<string, NGram>(StringComparer.Ordinal));
            }

            for (int i = 0; i < gramNodes.Count; i++)
            {
                XmlNode node = gramNodes[i];

                NGram nGram = new NGram();
                nGram.Grammar = node.Attributes["Syllable"].InnerText;

                double prob;
                if (double.TryParse(node.Attributes["Prob"].InnerText, out prob))
                {
                    nGram.Probability = prob;
                }
                else
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Not valid probability \"{0}\"", node.Attributes["Prob"].InnerText));
                }

                XmlAttribute backoffAttr = node.Attributes["Backoff"];
                if (backoffAttr != null)
                {
                    if (double.TryParse(node.Attributes["Backoff"].InnerText, out prob))
                    {
                        nGram.Backoff = prob;
                    }
                    else
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Not valid Backoff \"{0}\"", node.Attributes["Backoff"].InnerText));
                    }
                }

                if (string.IsNullOrEmpty(nGram.Grammar))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Grammar could not be empty for {0}th nodes in {1} level gram", i + 1, gram));
                }

                if (_nGramData[gram].ContainsKey(nGram.Grammar))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Conflict Grammar definition: \"{0}\"", nGram.Grammar));
                }

                _nGramData[gram].Add(nGram.Grammar, nGram);
            }
        }
    }
}