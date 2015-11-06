//----------------------------------------------------------------------------
// <copyright file="DataConfiguration.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Data configuration
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Lexicon Data.
    /// </summary>
    internal class LexiconData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconData"/> class.
        /// </summary>
        public LexiconData()
            : base(RawDataName.Lexicon)
        {
            RelativePath = "Lexicon\\Lexicon\\Lexicon.xml";
        }

        /// <summary>
        /// Load Lexicon Data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Lexicon Data object.</returns>
        internal override object LoadDataObject(ErrorSet errorSet)
        {
            Lexicon lexicon = new Lexicon(this.Language);
            Lexicon.ContentControler lexiconControler = new Lexicon.ContentControler();
            lexiconControler.IsCaseSensitive = true;
            lexicon.Load(this.Path, lexiconControler);
            return lexicon;
        }
    }

    /// <summary>
    /// Lexicon Data.
    /// </summary>
    internal class ExtraDomainLexiconData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtraDomainLexiconData"/> class.
        /// </summary>
        public ExtraDomainLexiconData()
            : base(RawDataName.ExtraDomainLexicon)
        {
            RelativePath = "Lexicon\\Lexicon\\ExtraDomainLexicon.xml";
        }

        /// <summary>
        /// Load Lexicon Data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Lexicon Data object.</returns>
        internal override object LoadDataObject(ErrorSet errorSet)
        {
            Lexicon lexicon = new Lexicon(this.Language);
            Lexicon.ContentControler lexiconControler = new Lexicon.ContentControler();
            lexiconControler.IsCaseSensitive = true;
            lexicon.Load(this.Path, lexiconControler);
            return lexicon;
        }
    }

    /// <summary>
    /// Lexicon Data.
    /// </summary>
    internal class RegressionLexiconData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegressionLexiconData"/> class.
        /// </summary>
        public RegressionLexiconData()
            : base(RawDataName.RegressionLexicon)
        {
            RelativePath = "Lexicon\\Lexicon\\RegressionLexicon.xml";
        }

        /// <summary>
        /// Load Lexicon Data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Lexicon Data object.</returns>
        internal override object LoadDataObject(ErrorSet errorSet)
        {
            Lexicon lexicon = new Lexicon(this.Language);
            Lexicon.ContentControler lexiconControler = new Lexicon.ContentControler();
            lexiconControler.IsCaseSensitive = true;
            lexicon.Load(this.Path, lexiconControler);
            return lexicon;
        }
    }

    /// <summary>
    /// Lexical Attribute Schema Data.
    /// </summary>
    internal class SchemaData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaData"/> class.
        /// </summary>
        public SchemaData()
            : base(RawDataName.LexicalAttributeSchema)
        {
            RelativePath = "Lexicon\\Lexicon\\schema.xml";
        }

        /// <summary>
        /// Load Lexicon Attribute Schema Data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Lexicon Attribute Schema Data object.</returns>
        internal override object LoadDataObject(ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            LexicalAttributeSchema schema = new LexicalAttributeSchema();
            schema.Load(this.Path);
            schema.Validate();
            errorSet.Merge(schema.ErrorSet);
            if (schema.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                schema = null;
            }

            return schema;
        }
    }

    /// <summary>
    /// POS set Data.
    /// </summary>
    internal class PosSetData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PosSetData"/> class.
        /// </summary>
        public PosSetData()
            : base(RawDataName.PosSet)
        {
            RelativePath = "Lexicon\\Lexicon\\postable.xml";
        }

        /// <summary>
        /// Load POS Set Data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>POS Set Data object.</returns>
        internal override object LoadDataObject(ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            TtsPosSet posSet = new TtsPosSet();
            posSet.Load(this.Path);
            return posSet;
        }
    }

    /// <summary>
    /// Phone set Data.
    /// </summary>
    internal class PhoneSetData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneSetData"/> class.
        /// </summary>
        public PhoneSetData()
            : base(RawDataName.PhoneSet)
        {
            RelativePath = "Lexicon\\Lexicon\\phoneset.xml";
        }

        /// <summary>
        /// Load Phone set Data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Phone set Data object.</returns>
        internal override object LoadDataObject(ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            TtsPhoneSet phoneSet = new TtsPhoneSet();
            phoneSet.Load(this.Path);
            phoneSet.Validate();
            errorSet.Merge(phoneSet.ErrorSet);
            if (phoneSet.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                phoneSet = null;
            }

            return phoneSet;
        }
    }

    /// <summary>
    /// Backend Phone set Data.
    /// </summary>
    internal class BackendPhoneSetData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackendPhoneSetData"/> class.
        /// </summary>
        public BackendPhoneSetData()
            : base(RawDataName.BackendPhoneSet)
        {
            RelativePath = "Lexicon\\Lexicon\\Backendphoneset.xml";
        }

        /// <summary>
        /// Load Phone set Data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Phone set Data object.</returns>
        internal override object LoadDataObject(ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            TtsPhoneSet phoneSet = new TtsPhoneSet();
            phoneSet.Load(this.Path);
            phoneSet.Validate();
            errorSet.Merge(phoneSet.ErrorSet);
            if (phoneSet.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                phoneSet = null;
            }

            return phoneSet;
        }
    }

    /// <summary>
    /// Char table Data.
    /// </summary>
    internal class CharTableData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CharTableData"/> class.
        /// </summary>
        public CharTableData()
            : base(RawDataName.CharTable)
        {
            RelativePath = "TAData\\Misc\\chartable.xml";
        }

        /// <summary>
        /// Load Char table Data object.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Char table Data object.</returns>
        internal override object LoadDataObject(ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            CharTable charTable = new CharTable();
            charTable.Load(Path);
            errorSet.Merge(charTable.ErrorSet);
            return charTable;
        }
    }

    /// <summary>
    /// Syllabify rule Data.
    /// </summary>
    internal class SyllabifyRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyllabifyRuleData"/> class.
        /// </summary>
        public SyllabifyRuleData()
            : base(RawDataName.SyllabifyRule)
        {
            RelativePath = "TAData\\SyllabifyRules.xml";
        }
    }

    /// <summary>
    /// Truncate rule Data.
    /// </summary>
    internal class TruncateRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TruncateRuleData"/> class.
        /// </summary>
        public TruncateRuleData()
            : base(RawDataName.TruncateRule)
        {
            RelativePath = "TAData\\TruncateRules.xml";
        }
    }

    /// <summary>
    /// Pause length Data.
    /// </summary>
    internal class PauseLengthData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PauseLengthData"/> class.
        /// </summary>
        public PauseLengthData()
            : base(RawDataName.PauseLength)
        {
            RelativePath = "TAData\\Misc\\PauseLength.xml";
        }
    }

    /// <summary>
    /// Polyphone rule Data.
    /// </summary>
    internal class PolyphoneRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolyphoneRuleData"/> class.
        /// </summary>
        public PolyphoneRuleData()
            : base(RawDataName.PolyphoneRule)
        {
            RelativePath = "Rules\\PolyRule\\polyrule.txt";
        }
    }

    /// <summary>
    /// BoundaryPronChange Rule Data.
    /// </summary>
    internal class BoundaryPronChangeRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundaryPronChangeRuleData"/> class.
        /// </summary>
        public BoundaryPronChangeRuleData()
            : base(RawDataName.BoundaryPronChangeRule)
        {
            RelativePath = "Rules\\BoundaryPronChangeRule\\BoundaryPronChangeRule.txt";
        }
    }

    /// <summary>
    /// Sentence detect rule data.
    /// </summary>
    internal class SentenceDetectData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceDetectData"/> class.
        /// </summary>
        public SentenceDetectData()
            : base(RawDataName.SentenceDetectRule)
        {
            RelativePath = "Rules\\SentDetectRule\\SentDetectRule.txt";
        }
    }

    /// <summary>
    /// Sentence detect rule data.
    /// </summary>
    internal class QuotationDetectorData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuotationDetectorData"/> class.
        /// </summary>
        public QuotationDetectorData()
            : base(RawDataName.QuotationMarkTable)
        {
            RelativePath = "TAData\\Misc\\QuotationMarkTable.xml";
        }
    }

    /// <summary>
    /// Sentence detect rule data.
    /// </summary>
    internal class ParallelStructDetectorData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelStructDetectorData"/> class.
        /// </summary>
        public ParallelStructDetectorData()
            : base(RawDataName.ParallelStructTable)
        {
            RelativePath = "TAData\\Misc\\ParallelStructTable.xml";
        }
    }

    /// <summary>
    /// Sentence detect rule data.
    /// </summary>
    internal class TextRegularizerData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextRegularizerData"/> class.
        /// </summary>
        public TextRegularizerData()
            : base(RawDataName.WordFeatureSuffixTable)
        {
            RelativePath = "TAData\\Misc\\WordFeatureSuffixTable.xml";
        }
    }

    /// <summary>
    /// LTS rule Data.
    /// </summary>
    internal class LtsRuleDataPath : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LtsRuleDataPath"/> class.
        /// </summary>
        public LtsRuleDataPath()
            : base(RawDataName.LtsRuleDataPath)
        {
            RelativePath = Helper.NeutralFormat("Lexicon\\lts");
        }
    }

    /// <summary>
    /// SSML phone mapping Data.
    /// </summary>
    internal class PhoneMappingRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneMappingRuleData"/> class.
        /// </summary>
        public PhoneMappingRuleData()
            : base(RawDataName.PhoneMappingRule)
        {
            RelativePath = "Rules\\PhoneMappingRule\\tnml_PhoneMapping.xml";
        }
    }

    /// <summary>
    /// Backend SSML phone mapping Data.
    /// </summary>
    internal class BackendPhoneMappingRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackendPhoneMappingRuleData"/> class.
        /// </summary>
        public BackendPhoneMappingRuleData()
            : base(RawDataName.BackendPhoneMappingRule)
        {
            RelativePath = "Rules\\PhoneMappingRule\\tnml_BackendPhoneMapping.xml";
        }
    }

    /// <summary>
    /// Froneend-Backend phone mapping Data.
    /// </summary>
    internal class FrontendBackendPhoneMappingRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrontendBackendPhoneMappingRuleData"/> class.
        /// </summary>
        public FrontendBackendPhoneMappingRuleData()
            : base(RawDataName.FrontendBackendPhoneMappingRule)
        {
            RelativePath = "Rules\\PhoneMappingRule\\tnml_FrontendBackendPhoneMapping.xml";
        }
    }

    /// <summary>
    /// Mix lingual pos converer data.
    /// </summary>
    internal class MixLingualPOSConverterData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MixLingualPOSConverterData"/> class.
        /// </summary>
        public MixLingualPOSConverterData()
            : base(RawDataName.MixLingualPOSConverterData)
        {
            RelativePath = "Rules\\POS\\tnml_POSConverter.xml";
        }
    }

    /// <summary>
    /// Word Breaker data Path.
    /// </summary>
    internal class WordBreakerDataPath : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WordBreakerDataPath"/> class.
        /// </summary>
        public WordBreakerDataPath()
            : base(RawDataName.WordBreakerDataPath)
        {
            RelativePath = "TAData";
        }
    }

    /// <summary>
    /// Post Word Breaker data.
    /// </summary>
    internal class PostWordBreakerData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostWordBreakerData"/> class.
        /// </summary>
        public PostWordBreakerData()
            : base(RawDataName.PostWordBreaker)
        {
            RelativePath = "TAData\\postwordbreaker.txt";
        }
    }

    /// <summary>
    /// Neutral Pattern List Data.
    /// </summary>
    internal class ChineseToneData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChineseToneData"/> class.
        /// </summary>
        public ChineseToneData()
            : base(RawDataName.ChineseTone)
        {
            RelativePath = "TAData\\Lexicon\\chinesetone.txt";
        }
    }

    /// <summary>
    /// Sentence Separator data Path.
    /// </summary>
    internal class SentenceSeparatorDataPath : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceSeparatorDataPath"/> class.
        /// </summary>
        public SentenceSeparatorDataPath()
            : base(RawDataName.SentenceSeparatorDataPath)
        {
            RelativePath = "TAData";
        }
    }

    /// <summary>
    /// POS Lexical Rule Data.
    /// </summary>
    internal class PosLexicalRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PosLexicalRuleData"/> class.
        /// </summary>
        public PosLexicalRuleData()
            : base(RawDataName.PosLexicalRule)
        {
            RelativePath = Helper.NeutralFormat("Rules\\PostaggerRule\\{0}_lexical_rule", 
                Localor.LanguageToString(Language));
        }

        /// <summary>
        /// Set the language .
        /// </summary>
        /// <param name="language">Language.</param>
        public override void SetLanguage(Language language)
        {
            base.SetLanguage(language);
            if (!string.IsNullOrEmpty(this.RelativePath))
            {
                RelativePath = Helper.NeutralFormat("Rules\\PostaggerRule\\{0}_lexical_rule",
                    Localor.LanguageToString(language));
            }
        }
    }

    /// <summary>
    /// POS Contectual Rule Data.
    /// </summary>
    internal class PosContextualRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PosContextualRuleData"/> class.
        /// </summary>
        public PosContextualRuleData()
            : base(RawDataName.PosContextualRule)
        {
            RelativePath = Helper.NeutralFormat("Rules\\PostaggerRule\\{0}_context_rule", 
                Localor.LanguageToString(Language));
        }

        /// <summary>
        /// Set the language .
        /// </summary>
        /// <param name="language">Language.</param>
        public override void SetLanguage(Language language)
        {
            base.SetLanguage(language);
            if (!string.IsNullOrEmpty(this.RelativePath))
            {
                RelativePath = Helper.NeutralFormat("Rules\\PostaggerRule\\{0}_context_rule",
                   Localor.LanguageToString(language));
            }
        }
    }

    /// <summary>
    /// TN Rule Data.
    /// </summary>
    internal class TnRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TnRuleData"/> class.
        /// </summary>
        public TnRuleData()
            : base(RawDataName.TnRule)
        {
            RelativePath = Helper.NeutralFormat("Rules\\TnRule\\tn{0}.xml", (short)Language);
        }

        /// <summary>
        /// Set the language .
        /// </summary>
        /// <param name="language">Language.</param>
        public override void SetLanguage(Language language)
        {
            base.SetLanguage(language);
            if (!string.IsNullOrEmpty(this.RelativePath))
            {
                RelativePath = Helper.NeutralFormat("Rules\\TnRule\\tn{0}.xml", (short)language);
            }
        }
    }

    /// <summary>
    /// FstNE Rule Data
    /// Currently it use the same raw data as TnRuleData.
    /// </summary>
    internal class FstNERuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FstNERuleData"/> class.
        /// </summary>
        public FstNERuleData()
            : base(RawDataName.FstNERule)
        {
            RelativePath = Helper.NeutralFormat("Rules\\TnRule\\tn{0}.xml", (short)Language);
        }

        /// <summary>
        /// Set the language. 
        /// </summary>
        /// <param name="language">Language.</param>
        public override void SetLanguage(Language language)
        {
            base.SetLanguage(language);
            if (!string.IsNullOrEmpty(this.RelativePath))
            {
                RelativePath = Helper.NeutralFormat("Rules\\TnRule\\tn{0}.xml", (short)language);
            }
        }
    }

    /// <summary>
    /// Compound Rule Data.
    /// </summary>
    internal class CompoundRuleData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundRuleData"/> class.
        /// </summary>
        public CompoundRuleData()
            : base(RawDataName.CompoundRule)
        {
            RelativePath = Helper.NeutralFormat("TAData\\Compound.xml");
        }
    }

    /// <summary>
    /// Compound Rule Data.
    /// </summary>
    internal class ForeignLtsCollection : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignLtsCollection"/> class.
        /// </summary>
        public ForeignLtsCollection()
            : base(RawDataName.ForeignLtsCollection)
        {
            RelativePath = string.Empty;
        }
    }

    /// <summary>
    /// Word Frequency Data.
    /// </summary>
    internal class WordFrequencyData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WordFrequencyData"/> class.
        /// </summary>
        public WordFrequencyData() : base(RawDataName.WordFreqPath)
        {
            RelativePath = Helper.NeutralFormat(@"Language\Corpus\CorpusLexiconAnalysis\Data\WordStatistics\LexiconCoverage.KnownWord.xml");
        }
    }

    /// <summary>
    /// Domain List File Data.
    /// </summary>
    internal class DomainListFileData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainListFileData"/> class.
        /// </summary>
        public DomainListFileData()
            : base(RawDataName.DomainListFile)
        {
            RelativePath = Helper.NeutralFormat(@"Language\TAData\Lexicon\WinMo\DomainList.txt");
        }
    }

    /// <summary>
    /// Domain Script Folder Data.
    /// </summary>
    internal class DomainScriptFolderData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainScriptFolderData"/> class.
        /// </summary>
        public DomainScriptFolderData()
            : base(RawDataName.DomainScriptFolder)
        {
            RelativePath = Helper.NeutralFormat(@"Language\Corpus\Domain");
        }
    }

    /// <summary>
    /// Non Pruned Word List File Data.
    /// </summary>
    internal class NonPrunedWordListFileData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonPrunedWordListFileData"/> class.
        /// </summary>
        public NonPrunedWordListFileData()
            : base(RawDataName.NonPrunedWordListFile)
        {
            RelativePath = Helper.NeutralFormat(@"Language\TAData\Lexicon\WinMo\nonNativeWordList.txt");
        }
    }

    /// <summary>
    /// AcronymDisambiguation data.
    /// </summary>
    internal class AcronymDisambiguationData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AcronymDisambiguationData"/> class.
        /// </summary>
        public AcronymDisambiguationData()
            : base(RawDataName.AcronymDisambiguation)
        {
            RelativePath = Helper.NeutralFormat(@"Rules\AcronymDisambiguation");
        }
    }

    /// <summary>
    /// NEDisambiguation data.
    /// </summary>
    internal class NEDisambiguationData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NEDisambiguationData"/> class.
        /// </summary>
        public NEDisambiguationData()
            : base(RawDataName.NEDisambiguation)
        {
            RelativePath = Helper.NeutralFormat(@"Rules\NEDisambiguation");
        }
    }

    /// <summary>
    /// Voice Font.
    /// </summary>
    internal class VoiceFontData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceFontData"/> class.
        /// </summary>
        public VoiceFontData()
            : base(RawDataName.VoiceFont)
        {
            RelativePath = string.Empty;
        }
    }

    /// <summary>
    /// Extra LangData.
    /// </summary>
    internal class ExtraDATData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtraDATData"/> class.
        /// </summary>
        public ExtraDATData()
            : base(RawDataName.ExtraDAT)
        {
            RelativePath = string.Empty;
        }
    }

    /// <summary>
    /// PolyphonyModelData.
    /// </summary>
    internal class PolyphonyModelData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolyphonyModelData"/> class.
        /// </summary>
        public PolyphonyModelData()
            : base(RawDataName.PolyphonyModel)
        {
            RelativePath = Helper.NeutralFormat(@"Rules\PolyphonyModel");
        }
    }

    /// <summary>
    /// RNN PolyphonyModelData.
    /// </summary>
    internal class RNNPolyphonyModelData : DataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RNNPolyphonyModelData"/> class.
        /// </summary>
        public RNNPolyphonyModelData()
            : base(RawDataName.RNNPolyphonyModel)
        {
            RelativePath = Helper.NeutralFormat(@"Rules\RNNPolyphonyModel");
        }
    }
}