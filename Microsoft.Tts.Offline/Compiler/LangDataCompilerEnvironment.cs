//----------------------------------------------------------------------------
// <copyright file="LangDataCompilerEnvironment.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      LangDataCompiler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.FlowEngine;

    /// <summary>
    /// LangDataCompiler Environment.
    /// </summary>
    public class LangDataCompilerEnvironment : FlowHandler
    {
        private DataCompiler _compiler;
        private Lexicon _lexicon = null;
        private Lexicon _caseInsensitiveLexicon = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LangDataCompilerEnvironment"/> class.
        /// </summary>
        /// <param name="name">The name string.</param>
        public LangDataCompilerEnvironment(string name) : base(name)
        {
            Description = "Language Data Compiler Environment";
        }

        /// <summary>
        /// Sets Compiler.
        /// </summary>
        public DataCompiler InCompiler
        {
            set 
            {
                _compiler = value; 
            }
        }

        /// <summary>
        /// Gets or sets Compiler Lexicon.
        /// </summary>
        public Lexicon InSetCompilerLexicon { get; set; }

        /// <summary>
        /// Gets or sets Out Lexicon Path.
        /// </summary>
        public string InSetOutLexiconPath { get; set; }

        /// <summary>
        /// Gets Output Compiler Lexicon.
        /// </summary>
        public Lexicon OutCompilerLexicon
        {
            get 
            {
                return InSetCompilerLexicon; 
            }
        }

        /// <summary>
        /// Gets Output Compiler.
        /// </summary>
        public DataCompiler OutCompiler
        {
            get 
            { 
                return _compiler;
            }
        }

        /// <summary>
        /// Gets Output Lexicon.
        /// </summary>
        public Lexicon OutLexicon
        {
            get
            {
                if (_lexicon == null && _compiler != null)
                {
                    ErrorSet errorSet = new ErrorSet();
                    _lexicon = _compiler.GetObject(RawDataName.Lexicon, errorSet) as Lexicon;
                }

                return _lexicon;
            }
        }

        /// <summary>
        /// Gets Case insensitive lexicon.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public Lexicon OutCaseInsensitiveLexicon
        {
            get
            {
                if (_caseInsensitiveLexicon == null && OutLexicon != null)
                {
                    // Change to case insensitive lexicon
                    MemoryStream lexiconStream = new MemoryStream();
                    using (XmlWriter xmlWriter = XmlWriter.Create(lexiconStream))
                    {
                        Lexicon.ContentControler lexiconControler = new Lexicon.ContentControler();
                        lexiconControler.IsCaseSensitive = true;
                        OutLexicon.Save(xmlWriter, lexiconControler);
                    }

                    lexiconStream.Seek(0, SeekOrigin.Begin);
                    _caseInsensitiveLexicon = new Lexicon();
                    using (StreamReader sr = new StreamReader(lexiconStream))
                    {
                        _caseInsensitiveLexicon.Load(sr);
                    }
                }

                return _caseInsensitiveLexicon;
            }
        }

        /// <summary>
        /// Gets Output Word Frequency File Path.
        /// </summary>
        public string OutWordFreqPath
        {
            get
            {
                if (_compiler != null)
                {
                    return _compiler.GetDataPath(RawDataName.WordFreqPath);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Output Phone Set.
        /// </summary>
        public TtsPhoneSet OutPhoneSet
        {
            get
            {
                if (_compiler != null)
                {
                    ErrorSet errors = new ErrorSet();
                    return _compiler.GetObject(RawDataName.PhoneSet, errors) as TtsPhoneSet;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets Output Attrib Schema.
        /// </summary>
        public LexicalAttributeSchema OutAttribSchema
        {
            get
            {
                if (_compiler != null)
                {
                    ErrorSet errors = new ErrorSet();
                    return _compiler.GetObject(RawDataName.LexicalAttributeSchema, errors) as LexicalAttributeSchema;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets Output Domain Script Folder.
        /// </summary>
        public string OutDomainScriptFolder
        {
            get
            {
                if (_compiler != null)
                {
                    return _compiler.GetDataPath(RawDataName.DomainScriptFolder);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Output Domain List File.
        /// </summary>
        public string OutDomainListFile
        {
            get
            {
                if (_compiler != null)
                {
                    return _compiler.GetDataPath(RawDataName.DomainListFile);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Output Extra Domain Lexicon.
        /// </summary>
        public Lexicon OutExtraDomainLexicon
        {
            get
            {
                if (_compiler != null)
                {
                    ErrorSet errors = new ErrorSet();
                    return _compiler.GetObject(RawDataName.ExtraDomainLexicon, errors) as Lexicon;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets Output Regression Lexicon.
        /// </summary>
        public Lexicon OutRegressionLexicon
        {
            get
            {
                if (_compiler != null)
                {
                    ErrorSet errors = new ErrorSet();
                    return _compiler.GetObject(RawDataName.RegressionLexicon, errors) as Lexicon;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets Output Not Pruned Word List File.
        /// </summary>
        public string OutNotPrunedWordListFile
        {
            get
            {
                if (_compiler != null)
                {
                    return _compiler.GetDataPath(RawDataName.NonPrunedWordListFile);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Output Voice Font Path.
        /// </summary>
        public string OutVoiceFont
        {
            get
            {
                if (_compiler != null)
                {
                    return _compiler.GetDataPath(RawDataName.VoiceFont);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Output Extra LangData path.
        /// </summary>
        public string OutExtraDAT
        {
            get
            {
                if (_compiler != null)
                {
                    return _compiler.GetDataPath(RawDataName.ExtraDAT);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Output Tn Rule File.
        /// </summary>
        public string OutTnRuleFile
        {
            get
            {
                if (_compiler != null)
                {
                    return _compiler.GetDataPath(RawDataName.TnRule);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Output Chartable File.
        /// </summary>
        public string OutCharTableFile
        {
            get
            {
                if (_compiler != null)
                {
                    return _compiler.GetDataPath(RawDataName.CharTable);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Working Directory.
        /// </summary>
        public string OutWorkingDirectory
        {
            get 
            { 
                return InWorkingDirectory; 
            }
        }

        /// <summary>
        /// The abstract method ValidateArguments() will be called to perform the validation
        /// For the inputs of this handler.
        /// This method won't be called if this handler is disabled.
        /// </summary>
        protected override void ValidateArguments()
        {
            if (_compiler == null)
            {
                throw new ArgumentException("Compiler should be assigned before used");
            }
        }

        /// <summary>
        /// The abstract method Execute() will be called to perform the action of this handler.
        /// This method won't be called if this handler is disabled.
        /// </summary>
        protected override void Execute()
        {
            if (this.InSetCompilerLexicon != null && !string.IsNullOrEmpty(InSetOutLexiconPath))
            {
                InSetCompilerLexicon.Save(GetOutPathUnderResultDirectory(InSetOutLexiconPath));
            }
        }

        /// <summary>
        /// The abstract method ValidateResults() will be used to fill result generated by this handler
        /// After Execute().
        /// This method will be called whenever this handler is disabled or not.
        /// </summary>
        /// <param name="enable">Indicator to whether flow is enabled.</param>
        protected override void ValidateResults(bool enable)
        {
        }
    }
}