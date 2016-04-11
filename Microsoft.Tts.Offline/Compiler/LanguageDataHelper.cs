//----------------------------------------------------------------------------
// <copyright file="LanguageDataHelper.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Language Data Helper
//      Definition of Raw Data Name, Module Data Name and Tool Name
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler.LanguageData
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider.LangData;

    /// <summary>
    /// Raw data name.
    /// </summary>
    public class RawDataName
    {
        /// <summary>
        /// Lexicon Name.
        /// </summary>
        public const string Lexicon = "Lexicon";

        /// <summary>
        /// PhoneSet.
        /// </summary>
        public const string PhoneSet = "PhoneSet";

        /// <summary>
        /// Backend PhoneSet.
        /// </summary>
        public const string BackendPhoneSet = "BackendPhoneSet";

        /// <summary>
        /// PosSet.
        /// </summary>
        public const string PosSet = "PosSet";

        /// <summary>
        /// LexicalAttributeSchema.
        /// </summary>
        public const string LexicalAttributeSchema = "Schema";

        /// <summary>
        /// CharTable.
        /// </summary>
        public const string CharTable = "CharTable";

        /// <summary>
        /// SyllabifyRule.
        /// </summary>
        public const string SyllabifyRule = "SyllabifyRule";

        /// <summary>
        /// PolyphoneRule.
        /// </summary>
        public const string PolyphoneRule = "PolyphoneRule";

        /// <summary>
        /// SentenceDetectRule.
        /// </summary>
        public const string SentenceDetectRule = "SentenceDetectRule";

        /// <summary>
        /// QuotationMarkTable.
        /// </summary>
        public const string QuotationMarkTable = "QuotationMarkTable";

        /// <summary>
        /// ParallelStructTable.
        /// </summary>
        public const string ParallelStructTable = "ParallelStructTable";

        /// <summary>
        /// WordFeatureSuffixTable.
        /// </summary>
        public const string WordFeatureSuffixTable = "WordFeatureSuffixTable";

        /// <summary>
        /// PhoneMappingRule.
        /// </summary>
        public const string PhoneMappingRule = "PhoneMappingRule";

        /// <summary>
        /// Backend PhoneMappingRule.
        /// </summary>
        public const string BackendPhoneMappingRule = "BackendPhoneMappingRule";

        /// <summary>
        /// Frontend-Backend PhoneMappingRule.
        /// </summary>
        public const string FrontendBackendPhoneMappingRule = "FrontendBackendPhoneMappingRule";

        /// <summary>
        /// Mix lingual POS converter.
        /// </summary>
        public const string MixLingualPOSConverterData = "MixLingualPOSConverterData";

        /// <summary>
        /// TnRule.
        /// </summary>
        public const string TnRule = "TnRule";

        /// <summary>
        /// FstNERule.
        /// </summary>
        public const string FstNERule = "FstNERule";

        /// <summary>
        /// CompoundRule.
        /// </summary>
        public const string CompoundRule = "CompoundRule";

        /// <summary>
        /// FrenchLiaisonRule.
        /// </summary>
        public const string FrenchLiaisonRule = "FrenchLiaisonRule";

        /// <summary>
        /// BoundaryPronChangeRule.
        /// </summary>
        public const string BoundaryPronChangeRule = "BoundaryPronChangeRule";

        /// <summary>
        /// PosLexicalRule.
        /// </summary>
        public const string PosLexicalRule = "PosLexicalRule";

        /// <summary>
        /// PosContextualRule.
        /// </summary>
        public const string PosContextualRule = "PosContextualRule";

        /// <summary>
        /// TruncateRule.
        /// </summary>
        public const string TruncateRule = "TruncateRule";

        /// <summary>
        /// PauseLength.
        /// </summary>
        public const string PauseLength = "PauseLength";

        /// <summary>
        /// LtsRuleDataPath.
        /// </summary>
        public const string LtsRuleDataPath = "LtsRuleDataPath";

        /// <summary>
        /// WordBreakerDataPath.
        /// </summary>
        public const string WordBreakerDataPath = "WordBreakerDataPath";

        /// <summary>
        /// PostWordBreaker.
        /// </summary>
        public const string PostWordBreaker = "PostWordBreaker";

        /// <summary>
        /// ChineseTone.
        /// </summary>
        public const string ChineseTone = "ChineseTone";

        /// <summary>
        /// SentenceSeparatorDataPath.
        /// </summary>
        public const string SentenceSeparatorDataPath = "SentenceSeparatorDataPath";

        /// <summary>
        /// LangIdentifierRule.
        /// </summary>
        public const string LangIdentifierRule = "LangIdentifierRule";

        /// <summary>
        /// ForeignLtsCollection.
        /// </summary>
        public const string ForeignLtsCollection = "ForeignLtsCollection";

        /// <summary>
        /// Word Frequency Path.
        /// </summary>
        public const string WordFreqPath = "WordFrequency";

        /// <summary>
        /// Domain Script Folder.
        /// </summary>
        public const string DomainScriptFolder = "DomainScriptFolder";

        /// <summary>
        /// Domain List File.
        /// </summary>
        public const string DomainListFile = "DomainListFile";

        /// <summary>
        /// Extra Domain Lexicon.
        /// </summary>
        public const string ExtraDomainLexicon = "ExtraDomainLexicon";

        /// <summary>
        /// Non Pruned Word List File.
        /// </summary>
        public const string NonPrunedWordListFile = "NonPrunedWordListFile";

        /// <summary>
        /// Regression Lexicon.
        /// </summary>
        public const string RegressionLexicon = "RegressionLexicon";

        /// <summary>
        /// AcronymDisambiguation rule Folder.
        /// </summary>
        public const string AcronymDisambiguation = "AcronymDisambiguation";

        /// <summary>
        /// NEDisambiguation rule Folder.
        /// </summary>
        public const string NEDisambiguation = "NEDisambiguation";

        /// <summary>
        /// Voice Font.
        /// </summary>
        public const string VoiceFont = "VoiceFont";

        /// <summary>
        /// Extra LangData.
        /// </summary>
        public const string ExtraDAT = "ExtraDAT";

        /// <summary>
        /// Polyphony Disambiguation.
        /// </summary>
        public const string PolyphonyModel = "PolyphonyModel";

        /// <summary>
        /// RNN Polyphony Disambiguation.
        /// </summary>
        public const string RNNPolyphonyModel = "RNNPolyphonyModel";

        /// <summary>
        /// Prevents a default instance of the <see cref="RawDataName"/> class from being created.
        /// </summary>
        private RawDataName()
        {
        }
    }

    /// <summary>
    /// Module Data Name.
    /// </summary>
    public class ModuleDataName
    {
        /// <summary>
        /// Lexicon.
        /// </summary>
        public const string Lexicon = RawDataName.Lexicon;

        /// <summary>
        /// PhoneSet.
        /// </summary>
        public const string PhoneSet = RawDataName.PhoneSet;

        /// <summary>
        /// Backend PhoneSet.
        /// </summary>
        public const string BackendPhoneSet = RawDataName.BackendPhoneSet;

        /// <summary>
        /// PosSet.
        /// </summary>
        public const string PosSet = RawDataName.PosSet;

        /// <summary>
        /// CharTable.
        /// </summary>
        public const string CharTable = RawDataName.CharTable;

        /// <summary>
        /// SyllabifyRule.
        /// </summary>
        public const string SyllabifyRule = RawDataName.SyllabifyRule;

        /// <summary>
        /// PolyphoneRule.
        /// </summary>
        public const string PolyphoneRule = "PolyphonyRule";

        /// <summary>
        /// SentenceDetector.
        /// </summary>
        public const string SentenceDetector = "SentenceDetector";

        /// <summary>
        /// Rnnlts model.
        /// </summary>
        public const string RNNLts = "RNNLtsModel";

        /// <summary>
        /// Rnn POS model.
        /// </summary>
        public const string RNNPos = "RNNPosModel";
        
        /// <summary>
        /// QuotationMarkTable.
        /// </summary>
        public const string QuotationMarkTable = RawDataName.QuotationMarkTable;

        /// <summary>
        /// ParallelStructTable.
        /// </summary>
        public const string ParallelStructTable = RawDataName.ParallelStructTable;

        /// <summary>
        /// WordFeatureSuffixTable.
        /// </summary>
        public const string WordFeatureSuffixTable = RawDataName.WordFeatureSuffixTable;

        /// <summary>
        /// FstNE Rule.
        /// </summary>
        public const string FstNERule = RawDataName.FstNERule;

        /// <summary>
        /// LtsRule.
        /// </summary>
        public const string LtsRule = "LtsRule";

        /// <summary>
        /// Phone Meta.
        /// </summary>
        public const string PhoneEventData = "PhoneEventData";

        /// <summary>
        /// PhoneMappingRule.
        /// </summary>
        public const string PhoneMappingRule = RawDataName.PhoneMappingRule;

        /// <summary>
        /// BackendPhoneMappingRule.
        /// </summary>
        public const string BackendPhoneMappingRule = RawDataName.BackendPhoneMappingRule;

        /// <summary>
        /// Frontend-BackendPhoneMappingRule.
        /// </summary>
        public const string FrontendBackendPhoneMappingRule = RawDataName.FrontendBackendPhoneMappingRule;

        /// <summary>
        /// Mix lingual POS converter.
        /// </summary>
        public const string MixLingualPOSConverterData = "MixLingualPOSConverterData";

        /// <summary>
        /// TnRule.
        /// </summary>
        public const string TnRule = RawDataName.TnRule;

        /// <summary>
        /// CompoundRule.
        /// </summary>
        public const string CompoundRule = RawDataName.CompoundRule;

        /// <summary>
        /// FrenchLiaisonRule.
        /// </summary>
        public const string FrenchLiaisonRule = RawDataName.FrenchLiaisonRule;

        /// <summary>
        /// BoundaryPronChangeRule.
        /// </summary>
        public const string BoundaryPronChangeRule = RawDataName.BoundaryPronChangeRule;

        /// <summary>
        /// SentenceSeparator.
        /// </summary>
        public const string SentenceSeparator = "SentenceSeparator";

        /// <summary>
        /// WordBreaker.
        /// </summary>
        public const string WordBreaker = "WordBreaker";

        /// <summary>
        /// PostWordBreaker.
        /// </summary>
        public const string PostWordBreaker = RawDataName.PostWordBreaker;

        /// <summary>
        /// PostWordBreaker.
        /// </summary>
        public const string CRFWordBreaker = "CRFWordBreaker";

        /// <summary>
        /// ChineseTone.
        /// </summary>
        public const string ChineseTone = RawDataName.ChineseTone;

        /// <summary>
        /// PosRule.
        /// </summary>
        public const string PosRule = "PosRule";

        /// <summary>
        /// UnitGenerator.
        /// </summary>
        public const string UnitGenerator = "UnitGenerator";

        /// <summary>
        /// PosTaggerPos.
        /// </summary>
        public const string PosTaggerPos = "PosTaggerPos";

        /// <summary>
        /// LangIdentifierRule.
        /// </summary>
        public const string LangIdentifierRule = RawDataName.LangIdentifierRule;

        /// <summary>
        /// ProsodyModelBR0.
        /// </summary>
        public const string ProsodyModelBR0 = "BR0";

        /// <summary>
        /// ProsodyModelBR2.
        /// </summary>
        public const string ProsodyModelBR2 = "BR2";

        /// <summary>
        /// ProsodyModelACT.
        /// </summary>
        public const string ProsodyModelACT = "ACT";

        /// <summary>
        /// ForeignLtsCollection.
        /// </summary>
        public const string ForeignLtsCollection = RawDataName.ForeignLtsCollection;

        /// <summary>
        /// ForeignLtsCollection.
        /// </summary>
        public const string AcronymDisambiguation = RawDataName.AcronymDisambiguation;

        /// <summary>
        /// NEDisambiguation.
        /// </summary>
        public const string NEDisambiguation = RawDataName.NEDisambiguation;

        /// <summary>
        /// PolyphonyDisambiguation.
        /// </summary>
        public const string PolyphonyModel = RawDataName.PolyphonyModel;

        /// <summary>
        /// RNN PolyphonyDisambiguation.
        /// </summary>
        public const string RNNPolyphonyModel = RawDataName.RNNPolyphonyModel;

        /// <summary>
        /// CRF Sentence type detector model.
        /// </summary>
        public const string CRFSentTypeDetectorModel = "CRFSentTypeDetectorModel";

        /// <summary>
        /// Prevents a default instance of the <see cref="ModuleDataName"/> class from being created..
        /// </summary>
        private ModuleDataName()
        {
        }
    }

    /// <summary>
    /// Data Guid.
    /// </summary>
    public class DataGuid
    {
        #region Fields
        private string _name;
        private string _guid;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="DataGuid"/> class.
        /// </summary>
        /// <param name="name">Data name.</param>
        /// <param name="guid">Data guid.</param>
        public DataGuid(string name, string guid)
        {
            Name = name;
            Guid = guid;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets Name.
        /// </summary>
        public string Name
        {
            get
            { 
                return _name; 
            }

            set 
            { 
                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets Guid.
        /// </summary>
        public string Guid
        {
            get 
            { 
                return _guid;
            }

            set 
            {
                _guid = value; 
            }
        }

        #endregion
    }

    /// <summary>
    /// Class of Each Language Data.
    /// </summary>
    public class LanguageDataHelper
    {
        /// <summary>
        /// Internal Data Guid information.
        /// </summary>
        public static DataGuid[] InternalDataGuid = new DataGuid[]
        {
            new DataGuid(ModuleDataName.SentenceSeparator,   "E67AB014-65F6-4e5e-9C1E-2E3A06E9B212"),
            new DataGuid(ModuleDataName.WordBreaker,         "629AA5C4-4D13-4bb8-BC17-D830F2517726"),
            new DataGuid(ModuleDataName.CRFWordBreaker,  "B15E4E27-AB10-473A-9A43-77108DAD4691"),
            new DataGuid(ModuleDataName.ChineseTone,         "B85C9DA5-FC54-4F19-B222-B038EFBFC108"),
            new DataGuid(ModuleDataName.PostWordBreaker,     "99CC36F3-1480-4BE7-B3CD-3479C7915BC7"),
            new DataGuid(ModuleDataName.PhoneSet,            "29A5584B-5A6F-4d4d-BC81-7B524468FE8C"),
            new DataGuid(ModuleDataName.BackendPhoneSet,     "B3526045-39E7-4539-BEB1-8429C732800A"),
            new DataGuid(ModuleDataName.PolyphoneRule,       "E849E61B-0D76-41ee-BB54-E3250CFD21C3"),
            new DataGuid(ModuleDataName.PosSet,              "370AD112-0D2D-4997-B687-CA76B4DFE4F3"),
            new DataGuid(ModuleDataName.PosRule,             "0CB71848-B746-4fa8-B7E2-4671BCDDB483"),
            new DataGuid(ModuleDataName.RNNPos,              "3235B923-C8B6-47ED-AF97-8F763924DB13"),
            new DataGuid(ModuleDataName.CharTable,           "F6E4F50A-83B8-4754-8CC9-D1F772268BF8"),
            new DataGuid(ModuleDataName.CompoundRule,        "19A6569A-BF1F-4e8d-A13E-F2DAF57DD66B"),
            new DataGuid(ModuleDataName.FstNERule,           "BFC4309D-57C4-4741-B1FF-83295B634B51"),
            new DataGuid(ModuleDataName.LtsRule,             "AC4AEFCF-6D8C-48b1-AF0E-BEB0E640BAE7"),
            new DataGuid(ModuleDataName.RNNLts,              "46EE52BA-2CC3-4330-B7AA-D14BFA521B75"),
            new DataGuid(ModuleDataName.Lexicon,             "7BD71F46-E7B1-4564-ADEE-81354818A303"),
            new DataGuid(ModuleDataName.SentenceDetector,    "00A2359E-C05F-4182-AACC-31BEF50D04EF"),
            new DataGuid(ModuleDataName.QuotationMarkTable,  "B54490E3-050B-4f66-9C01-76BF4E35A648"),
            new DataGuid(ModuleDataName.ParallelStructTable,    "D8951565-F16D-46c6-A2C8-C2107F9EAEA1"),
            new DataGuid(ModuleDataName.WordFeatureSuffixTable,    "5554BA64-7557-436D-9D6F-D5D1D7AFB2D3"),
            new DataGuid(ModuleDataName.PhoneMappingRule,    "388B0327-FDA5-478b-B871-163F689E40A8"),
            new DataGuid(ModuleDataName.BackendPhoneMappingRule,   "718AA21F-6046-4AD9-B44F-FE1CF6F4E131"),
            new DataGuid(ModuleDataName.FrontendBackendPhoneMappingRule,   "FEA3B45B-E901-40F1-8E0B-20E5466AA361"),
            new DataGuid(ModuleDataName.MixLingualPOSConverterData,   "7758AA3C-B01F-459E-8379-3884189AA909"),
            new DataGuid(ModuleDataName.FrenchLiaisonRule,   "5E76C15C-4F92-4892-9964-1FB530954878"),
            new DataGuid(ModuleDataName.BoundaryPronChangeRule, "CDC85643-3D56-4fba-8C72-9ADD1B7E5894"),
            new DataGuid(ModuleDataName.UnitGenerator,       "9D9E8526-B5A4-44d9-AA97-FE02C38D23AD"),
            new DataGuid(ModuleDataName.SyllabifyRule,       "78F6770D-6248-4b38-9D11-C099EFD046B1"),
            new DataGuid(ModuleDataName.TnRule,              "7D5841AB-516F-42c0-A64C-C6E3166668D2"),
            new DataGuid(ModuleDataName.PosTaggerPos,        "F81FD1D1-6FEC-4e0d-8893-F4B94152D5B0"),
            new DataGuid(ModuleDataName.LangIdentifierRule,  "EFDC81F5-3EB0-4db3-9EFA-C8EC49B1C265"),
            new DataGuid(ModuleDataName.ProsodyModelBR0,  "01BE6345-BBB6-431E-BD4E-919D1D7B13AA"),
            new DataGuid(ModuleDataName.ProsodyModelBR2,  "5E699FE4-244C-49FB-A8FF-51DAB144C049"),
            new DataGuid(ModuleDataName.ProsodyModelACT,  "D38D494E-0BE1-4F39-8316-D29ACDF82E4D"),
            new DataGuid(ModuleDataName.ForeignLtsCollection,  "6F4AC239-EA18-428d-8370-C386CFB694DC"),
            new DataGuid(ModuleDataName.AcronymDisambiguation, "CEA1BE6F-CDAA-4e01-BDEC-49AB604D334E"),
            new DataGuid(ModuleDataName.NEDisambiguation,      "611DDB18-4B46-4694-B28F-9ED461D560D7"),
            new DataGuid(ModuleDataName.PhoneEventData,      "9ABDA282-9734-48C8-BA24-F6B55F7BE721"),
            new DataGuid(ModuleDataName.PolyphonyModel,      "D49F77B9-8982-4860-9D5D-55919CF4F54E"),
            new DataGuid(ModuleDataName.RNNPolyphonyModel,   "6DE01F86-0DA8-4A23-830B-3730F0325198"),
            new DataGuid(ModuleDataName.CRFSentTypeDetectorModel, "3292D97F-C52C-4143-AA89-DF847F5C4406")
        };

        /// <summary>
        /// Get the reserved guid for data name.
        /// </summary>
        /// <param name="name">Data name.</param>
        /// <returns>Guid string.</returns>
        public static string GetReservedGuid(string name)
        {
            string guid = null;
            
            for (int guidIndex = 0; guidIndex < InternalDataGuid.Length; guidIndex++)
            {
                if (InternalDataGuid[guidIndex].Name.Equals(name))
                {
                    guid = InternalDataGuid[guidIndex].Guid;
                    break;
                }
            }

            return guid;
        }

        /// <summary>
        /// Get the reserved data name according to the guid.
        /// </summary>
        /// <param name="guidValue">Guid string.</param>
        /// <returns>Data name.</returns>
        public static string GetReservedDataName(string guidValue)
        {
            string name = null;
            
            for (int guidIndex = 0; guidIndex < InternalDataGuid.Length; guidIndex++)
            {
                if (InternalDataGuid[guidIndex].Guid.Equals(guidValue, StringComparison.OrdinalIgnoreCase))
                {
                    name = InternalDataGuid[guidIndex].Name;
                    break;
                }
            }

            return name;
        }

        /// <summary>
        /// Replace module binary in dat file.
        /// </summary>
        /// <param name="datPath">Dat file path.</param>
        /// <param name="binPath">Binary file path.</param>
        /// <param name="module">Module name.</param>
        public static void ReplaceBinaryFile(string datPath, string binPath, string module)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] byteDatas = File.ReadAllBytes(binPath);
                stream.Write(byteDatas, 0, byteDatas.Length);
                ReplaceBinaryFile(datPath, stream, module);
            }
        }

        /// <summary>
        /// Replace module binary in dat file.
        /// </summary>
        /// <param name="dataPath">Dat file path.</param>
        /// <param name="binStream">Binary file stream.</param>
        /// <param name="module">Module name defined in ModuleDataName class.</param>
        public static void ReplaceBinaryFile(string dataPath, MemoryStream binStream, string module)
        {
            using (LangDataFile dataFile = new LangDataFile())
            {
                string dataBakPath = Helper.NeutralFormat("{0}.bak", dataPath);
                File.Delete(dataBakPath);
                File.Move(dataPath, dataBakPath);
                dataFile.Load(dataBakPath);

                // get guid of module
                string guidStr = GetReservedGuid(module);
                Guid guid = new Guid(guidStr);

                // replace binary file
                LangDataObject tnObject = dataFile.GetDataObject(guid);
                tnObject.Data = binStream.ToArray();
                dataFile.AddDataObject(tnObject, true);

                SortLangData(dataFile);

                dataFile.Save(dataPath);
            }
        }

        /// <summary>
        /// Sort Langugae data objects by module data name.
        /// </summary>
        /// <param name="langData">Language data file.</param>
        private static void SortLangData(LangDataFile langData)
        {
            ArrayList sortedDataObjects = new ArrayList();
            foreach (var data in langData.DataObjects)
            {
                string moduleDataName = GetReservedDataName(data.Token.ToString());
                sortedDataObjects.Add(new KeyValuePair<string, LangDataObject>(moduleDataName, data));
            }

            sortedDataObjects.Sort(new CompareLangDataObject());

            foreach (KeyValuePair<string, LangDataObject> obj in sortedDataObjects)
            {
                langData.AddDataObject(obj.Value, true);
            }
        }
    }

    /// <summary>
    /// Tool Name.
    /// </summary>
    internal class ToolName
    {
        /// <summary>
        /// Ruler compiler.
        /// </summary>
        public const string RuleCompiler = "polycomp.exe";

        /// <summary>
        /// Overdue bldvendor.
        /// </summary>
        public const string BldVendor1 = "bldvendor.exe";

        /// <summary>
        /// Bldvendor.
        /// </summary>
        public const string BldVendor2 = "bldVendorV2.exe";

        /// <summary>
        /// Pos Rule Compiler.
        /// </summary>
        public const string PosRuleCompiler = "rule_text2bin_U.exe";

        /// <summary>
        /// TNML compiler.
        /// </summary>
        public const string TnmlCompiler = "CompTNML.exe";

        /// <summary>
        /// FstNE compiler.
        /// </summary>
        public const string FstNECompiler = "CompFstNE.exe";

        /// <summary>
        /// LTS Rule Compiler.
        /// </summary>
        public const string LtsCompiler = "ltscomp.exe";

        /// <summary>
        /// Prevents a default instance of the <see cref="ToolName"/> class from being created.
        /// </summary>
        private ToolName()
        {
        }
    }
}
