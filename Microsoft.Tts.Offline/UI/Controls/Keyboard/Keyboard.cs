//----------------------------------------------------------------------------
// <copyright file="Keyboard.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements IPA keyboard
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// The Keyboard model.
    /// </summary>
    public partial class Keyboard : UserControl
    {
        #region fields

        private const string DefaultKeyboardFont = "Arial Unicode MS";
        private string _font;
        private SortedDictionary<int, KeyboardInfo> _phoneList;
        private KeyboardConfig _config;
        private Button _selectedButton;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Keyboard"/> class.
        /// </summary>
        public Keyboard()
        {
            InitializeComponent();
        }

        #region events

        /// <summary>
        /// Keyboard event.
        /// </summary>
        public event EventHandler<KeyboardEventArgs> KeyboardEvent = delegate { };

        /// <summary>
        /// Keyboard speak event.
        /// </summary>
        public event EventHandler<KeyboardSpeakEventArgs> KeyboardSpeakEvent = delegate { };

        #endregion

        #region properties

        /// <summary>
        /// Gets Keyboard config.
        /// </summary>
        public KeyboardConfig KeyboardConfig
        {
            get
            {
                return _config;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="language">Language.</param>
        public void Initialization(string language)
        {
            _config = new KeyboardConfig(language);
            InitializeKeyboard();
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="filePath">Config file path.</param>
        public void Initialization(string language, string filePath)
        {
            _config = new KeyboardConfig(language, filePath);
            InitializeKeyboard();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize keyboard.
        /// </summary>
        private void InitializeKeyboard()
        {
            _phoneList = _config.Phones;
            _font = string.Empty;
            using (InstalledFontCollection insFont = new InstalledFontCollection())
            {
                foreach (FontFamily family in insFont.Families)
                {
                    if (family.IsStyleAvailable(FontStyle.Regular))
                    {
                        if (family.Name == _config.Font)
                        {
                            _font = _config.Font;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(_font))
                {
                    _font = DefaultKeyboardFont;
                }

                labelPhoneInfo.Font = GenerateFont();
                GenerateButton();
                labelPhoneInfo.Text = string.Empty;
                panelInformation.Controls.Clear();
            }  
        }

        /// <summary>
        /// Generate keyboard buttons.
        /// </summary>
        private void GenerateButton()
        {
            const int LocationX = 3;
            const int LocationY = 3;
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel2.Controls.Clear();
            tableLayoutPanel3.Controls.Clear();
            for (int index = 0; index < _phoneList.Keys.Count; index++)
            {
                using (Button keyboardButton = new Button())
                {
                    keyboardButton.Font = GenerateFont();
                    keyboardButton.Name = string.Format(CultureInfo.InvariantCulture,
                        "button{0}", index);
                    keyboardButton.Dock = DockStyle.Fill;
                    keyboardButton.TabIndex = index;
                    keyboardButton.TabStop = false;
                    keyboardButton.Click += new EventHandler(KeyboardButton_Click);
                    keyboardButton.Location = new System.Drawing.Point(LocationX, LocationY);
                    if (_phoneList.ContainsKey(index))
                    {
                        keyboardButton.Text = _phoneList[index].DisplayedPhone;
                        switch (_phoneList[index].PhoneType)
                        {
                            case KeyboardInfo.PhoneTypeEnums.Consonant:
                                keyboardButton.MouseHover += new EventHandler(ShowPhoneInfo);
                                tableLayoutPanel1.Controls.Add(keyboardButton);
                                break;

                            case KeyboardInfo.PhoneTypeEnums.Vowel:
                                keyboardButton.MouseHover += new EventHandler(ShowPhoneInfo);
                                tableLayoutPanel2.Controls.Add(keyboardButton);
                                break;

                            case KeyboardInfo.PhoneTypeEnums.Symbol:
                                keyboardButton.MouseHover += new EventHandler(ShowSymbolInfo);
                                tableLayoutPanel3.Controls.Add(keyboardButton);
                                break;

                            default:
                                throw new ArgumentException("Not supported");
                        }
                    }
                    else
                    {
                        throw new InvalidDataException("Cannot generate button. The button's information does not exist");
                    }
                }              
            }
        }

        /// <summary>
        /// The KeyboardButton_Click method.
        /// </summary>
        /// <param name="sender">The clicked keyboard button.</param>
        /// <param name="e">Empty EventArgs.</param>
        private void KeyboardButton_Click(object sender, EventArgs e)
        {
            if (_selectedButton != null)
            {
                _selectedButton.UseVisualStyleBackColor = true;
            }

            Button button = sender as Button;
            if (_phoneList.ContainsKey(button.TabIndex))
            {
                KeyboardEventArgs eventArgs = new KeyboardEventArgs(_phoneList[button.TabIndex].TtsPhone,
                                                                    _phoneList[button.TabIndex].IpaPhone,
                                                                    _phoneList[button.TabIndex].DisplayedPhone);
                KeyboardEvent(this, eventArgs);
            }

            button.BackColor = Color.Azure;
            _selectedButton = button;
        }

        /// <summary>
        /// Generate font function.
        /// </summary>
        /// <returns>Font.</returns>
        private Font GenerateFont()
        {
            return new System.Drawing.Font(_font, 8.25F, System.Drawing.FontStyle.Regular,
                                           System.Drawing.GraphicsUnit.Point, (byte)0);
        }

        /// <summary>
        /// Show example words.
        /// </summary>
        /// <param name="sender">The current mouse hovering button.</param>
        /// <param name="e">Empty EventArgs.</param>
        private void ShowPhoneInfo(object sender, EventArgs e)
        {
            const int IncrementY = 32;
            const int WordLabelX = 200;
            const int WordLabelY = 30;
            const int PronLabelX = 130;
            const int PronLabelY = 30;
            const int SoundButtonX = 40;
            const int SoundButtonY = 40;
            int locationX = panelInformation.Location.X;
            int locationY = panelInformation.Location.Y - 80;
            labelPhone.Text = "Phone:";
            labelExampleWord.Text = "Example word:";

            Button button = sender as Button;
            KeyboardPhone keyboardPhone = _phoneList[button.TabIndex] as KeyboardPhone;
            Debug.Assert(keyboardPhone != null);
            labelPhoneInfo.Text = keyboardPhone.DisplayedPhone;
            ReadOnlyCollection<KeyboardPhone.WordInformation> exampleWords =
                keyboardPhone.ExampleWords;

            panelInformation.Controls.Clear();
            for (int i = 0; i < exampleWords.Count; ++i)
            {
                Label wordLabel = new Label();
                wordLabel.Font = GenerateFont();
                wordLabel.Text = exampleWords[i].Grapheme;
                wordLabel.Size = new System.Drawing.Size(WordLabelX, WordLabelY);
                wordLabel.Location = new System.Drawing.Point(locationX, locationY);
                panelInformation.Controls.Add(wordLabel);
                locationY += IncrementY;
                using (Label pronLabel = new Label())
                {
                    pronLabel.Font = GenerateFont();
                    string displayedPron = _config.ReplaceUndisplayableSymbol(exampleWords[i].IpaPronunciation);
                    string pron = exampleWords[i].IpaPronunciation;
                    pronLabel.Text = "/ " + displayedPron + " /";
                    pronLabel.Size = new System.Drawing.Size(PronLabelX, PronLabelY);
                    pronLabel.Location = new System.Drawing.Point(locationX, locationY);
                    panelInformation.Controls.Add(pronLabel);
                    locationX += PronLabelX;
                    using (Button soundButton = new Button())
                    {
                        soundButton.Size = new System.Drawing.Size(SoundButtonX, SoundButtonY);
                        soundButton.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
                        soundButton.Location = new System.Drawing.Point(locationX, locationY);
                        soundButton.Image = (Image)global::Microsoft.Tts.Offline.Properties.Resources.ResourceManager.GetObject("speaker");
                        soundButton.TabStop = false;
                        soundButton.Click += delegate { KeyboardSpeakEvent(this, new KeyboardSpeakEventArgs(wordLabel.Text, pron)); };
                        panelInformation.Controls.Add(soundButton);
                        locationX = panelInformation.Location.X;
                        locationY += IncrementY;
                    }
                }          
            }
        }

        /// <summary>
        /// Show symbol's information.
        /// </summary>
        /// <param name="sender">The current focused symbol button.</param>
        /// <param name="e">Empty EventArgs.</param>
        private void ShowSymbolInfo(object sender, EventArgs e)
        {
            const int KeyboardLabelX = 160;
            const int KeyboardLabelY = 30;
            int locationX = panelInformation.Location.X;
            int locationY = panelInformation.Location.Y - 80;
            labelPhone.Text = "Symbol:";
            labelExampleWord.Text = "Function:";

            Button button = sender as Button;
            KeyboardSymbol keyboardSymbol = _phoneList[button.TabIndex] as KeyboardSymbol;
            Debug.Assert(keyboardSymbol != null);
            labelPhoneInfo.Text = keyboardSymbol.DisplayedPhone;
            panelInformation.Controls.Clear();
            using (Label keyboardLabel = new Label())
            {
                keyboardLabel.Font = GenerateFont();
                keyboardLabel.Text = keyboardSymbol.Description;
                keyboardLabel.Size = new System.Drawing.Size(KeyboardLabelX, KeyboardLabelY);
                keyboardLabel.Location = new System.Drawing.Point(locationX, locationY);
                panelInformation.Controls.Add(keyboardLabel);
            }
        }

        #endregion
    }

    /// <summary>
    /// The KeyboardConfig class.
    /// </summary>
    public class KeyboardConfig
    {
        #region fields

        private static XmlSchema _schema;
        private string _font = string.Empty;
        private string _originalRegex;
        private string _displayedRegex;
        private SortedDictionary<string, KeyboardInfo> _ttsPhoneHashTable =
            new SortedDictionary<string, KeyboardInfo>(StringComparer.Ordinal);

        private SortedDictionary<string, KeyboardInfo> _ipaPhoneHashTable =
            new SortedDictionary<string, KeyboardInfo>(StringComparer.Ordinal);

        private SortedDictionary<string, KeyboardInfo> _displayedPhoneHashTable =
            new SortedDictionary<string, KeyboardInfo>(StringComparer.Ordinal);

        private XmlDocument _dom = new XmlDocument();
        private SortedDictionary<int, KeyboardInfo> _keyboardPhoneList =
            new SortedDictionary<int, KeyboardInfo>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardConfig"/> class.
        /// Load data from internal xml.
        /// </summary>
        /// <param name="language">User selected language, such as "en-US".</param>
        public KeyboardConfig(string language)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream xmlStream = Stream.Null;
            string xmlResource = Helper.NeutralFormat("{0}.Controls.Keyboard.Keyboard_{1}.xml", asm.GetName().Name, language);
            xmlStream = asm.GetManifestResourceStream(xmlResource);
            try
            {
                XmlHelper.Validate(xmlStream, Schema);
                _dom.Load(xmlStream);
            }
            catch
            {
                throw new InvalidDataException("Inner xml file has error format");
            }

            LoadData(language);
            GenerateHashTable();
            GenerateRegex();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardConfig"/> class.
        /// Load data from given xml. If the given xml is not existed, then throw a exception.
        /// </summary>
        /// <param name="language">User selected language, such as "en-US".</param>
        /// <param name="filePath">The user given keyboard xml file path.</param>
        public KeyboardConfig(string language, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), filePath);
            }
            else
            {
                try
                {
                    XmlHelper.Validate(filePath, Schema);
                    _dom.Load(filePath);
                }
                catch
                {
                    throw new InvalidDataException("The given xml file has error format");
                }
            }

            LoadData(language);
            GenerateHashTable();
            GenerateRegex();
        }

        #region enums

        /// <summary>
        /// Phone style enum.
        /// </summary>
        public enum PhoneStyle
        {
            /// <summary>
            /// Tts phone string.
            /// </summary>
            TtsPhone,

            /// <summary>
            /// Ipa phone string.
            /// </summary>
            IpaPhone,

            /// <summary>
            /// Displayed phone string.
            /// </summary>
            DisplayedPhone
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets Supported languages.
        /// </summary>
        public static ReadOnlyCollection<string> SupportedLanguages
        {
            get
            {
                List<string> supportedLanguages = new List<string>();
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream xmlStream = Stream.Null;
                Array langs = Enum.GetValues(typeof(Language));
                for (int i = 0; i < langs.Length; i++)
                {
                    string langName = Localor.LanguageToString((Language)langs.GetValue(i));
                    string xmlResource = string.Format(CultureInfo.InvariantCulture,
                        "{0}.Controls.Keyboard.Keyboard_{1}.xml", asm.GetName().Name, langName);
                    xmlStream = asm.GetManifestResourceStream(xmlResource);
                    if (xmlStream != null)
                    {
                        supportedLanguages.Add(langName);
                    }
                }

                return new ReadOnlyCollection<string>(supportedLanguages);
            }
        }

        /// <summary>
        /// Gets Keyboard schema.
        /// </summary>
        public static XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.Keyboard.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets Keyboard phone list.
        /// </summary>
        public SortedDictionary<int, KeyboardInfo> Phones
        {
            get
            {
                return _keyboardPhoneList;
            }
        }

        /// <summary>
        /// Gets Keyboard font.
        /// </summary>
        public string Font
        {
            get
            {
                return _font;
            }
        }

        /// <summary>
        /// Gets Supported Consonants.
        /// </summary>
        public ReadOnlyCollection<string> SupportedConsonants
        {
            get
            {
                List<string> consonantList = new List<string>();
                if (_keyboardPhoneList != null)
                {
                    foreach (KeyboardInfo keyboardInfo in _keyboardPhoneList.Values)
                    {
                        if (keyboardInfo.PhoneType == KeyboardInfo.PhoneTypeEnums.Consonant)
                        {
                            consonantList.Add(keyboardInfo.IpaPhone);
                        }
                    }
                }

                return new ReadOnlyCollection<string>(consonantList);
            }
        }

        /// <summary>
        /// Gets Supported vowels.
        /// </summary>
        public ReadOnlyCollection<string> SupportedVowels
        {
            get
            {
                List<string> vowelList = new List<string>();
                if (_keyboardPhoneList != null)
                {
                    foreach (KeyboardInfo keyboardInfo in _keyboardPhoneList.Values)
                    {
                        if (keyboardInfo.PhoneType == KeyboardInfo.PhoneTypeEnums.Vowel)
                        {
                            vowelList.Add(keyboardInfo.IpaPhone);
                        }
                    }
                }

                return new ReadOnlyCollection<string>(vowelList);
            }
        }

        /// <summary>
        /// Gets Supported symbols.
        /// </summary>
        public ReadOnlyCollection<string> SupportedSymbols
        {
            get
            {
                List<string> symbolList = new List<string>();
                if (_keyboardPhoneList != null)
                {
                    foreach (KeyboardInfo keyboardInfo in _keyboardPhoneList.Values)
                    {
                        if (keyboardInfo.PhoneType == KeyboardInfo.PhoneTypeEnums.Symbol)
                        {
                            symbolList.Add(keyboardInfo.IpaPhone);
                        }
                    }
                }

                return new ReadOnlyCollection<string>(symbolList);
            }
        }

        #endregion

        #region public method

        /// <summary>
        /// Is keyboard supported correct language?.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <returns>Bool.</returns>
        public static bool IsSupported(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException(language);
            }

            bool supported = false;
            ReadOnlyCollection<string> langs = SupportedLanguages;
            foreach (string lang in langs)
            {
                if (language == lang)
                {
                    supported = true;
                    break;
                }
            }

            return supported;
        }

        /// <summary>
        /// Replace symbols that can not be displayed in control text.
        /// </summary>
        /// <param name="text">Button text or label text that need to be changed.</param>
        /// <returns>New generate string.</returns>
        public string ReplaceUndisplayableSymbol(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(text);
            }

            Match match = Regex.Match(text, _originalRegex);
            if (match.Success)
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int index = 0; index < match.Groups[1].Captures.Count; index++)
                {
                    string key = match.Groups[1].Captures[index].Value;
                    if (key == " " || key == "/")
                    {
                        stringBuilder.Append(key);
                    }

                    if (_ipaPhoneHashTable.ContainsKey(key))
                    {
                        stringBuilder.Append(_ipaPhoneHashTable[key].DisplayedPhone);
                    }
                }

                return stringBuilder.ToString();
            }
            else
            {
                throw new ArgumentException("Error ipa phone");
            }
        }

        /// <summary>
        /// Convert IPA phone to its original format.
        /// </summary>
        /// <param name="text">Original IPA phone.</param>
        /// <returns>IPA phone with its original format.</returns>
        public string ConvertToOriginalPhone(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(text);
            }

            Match match = Regex.Match(text, _displayedRegex);
            if (match.Success)
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int index = 0; index < match.Groups[1].Captures.Count; index++)
                {
                    string key = match.Groups[1].Captures[index].Value;
                    if (key == " " || key == "/")
                    {
                        stringBuilder.Append(key);
                    }

                    if (_displayedPhoneHashTable.ContainsKey(key))
                    {
                        stringBuilder.Append(_displayedPhoneHashTable[key].IpaPhone);
                    }
                }

                return stringBuilder.ToString();
            }
            else
            {
                throw new ArgumentException("Error displayed phone");
            }
        }

        /// <summary>
        /// Get given phone's all information.
        /// </summary>
        /// <param name="phone">TTS phone or IPA phone or displayed phone.</param>
        /// <param name="phoneStyle">Phone style.</param>
        /// <returns>KeyboardInfo structure.</returns>
        public KeyboardInfo GetPhoneInfo(string phone, PhoneStyle phoneStyle)
        {
            if (string.IsNullOrEmpty(phone))
            {
                throw new ArgumentException("Phone");
            }

            KeyboardInfo keyboardInfo = null;
            if (phoneStyle == PhoneStyle.IpaPhone && _ipaPhoneHashTable.ContainsKey(phone))
            {
                keyboardInfo = _ipaPhoneHashTable[phone];
            }
            else if (phoneStyle == PhoneStyle.TtsPhone && _ttsPhoneHashTable.ContainsKey(phone))
            {
                keyboardInfo = _ttsPhoneHashTable[phone];
            }
            else if (phoneStyle == PhoneStyle.DisplayedPhone && _displayedPhoneHashTable.ContainsKey(phone))
            {
                keyboardInfo = _displayedPhoneHashTable[phone];
            }
            else
            {
                string message = Helper.NeutralFormat("Can not find PhoneInfo of {0} in keyboard.", phone);
                throw new ArgumentOutOfRangeException(message);
            }

            return keyboardInfo;
        }

        /// <summary>
        /// Split IPA phone string to IPA phone collection.
        /// </summary>
        /// <param name="ipaPhoneText">IPA phone string.</param>
        /// <returns>IPA phone collection.</returns>
        public ReadOnlyCollection<string> GetIPAPhones(string ipaPhoneText)
        {
            if (string.IsNullOrEmpty(ipaPhoneText))
            {
                throw new ArgumentNullException(ipaPhoneText);
            }

            const int MaxLengthOfIPAPhone = 5;
            List<string> ipaPhones = new List<string>();
            string phoneText = ipaPhoneText.Replace(" ", string.Empty);
            int index = 0;
            while (index < phoneText.Length)
            {
                int i = Math.Min(MaxLengthOfIPAPhone, phoneText.Length - index);
                for (; i > 1; i--)
                {
                    string subText = phoneText.Substring(index, i);
                    if (FindKeyboardPhone(subText, PhoneStyle.IpaPhone) != null)
                    {
                        ipaPhones.Add(subText);
                        index += i;
                        break;
                    }
                }

                if (i == 1)
                {
                    ipaPhones.Add(phoneText.Substring(index, 1));
                    ++index;
                }
            }

            return new ReadOnlyCollection<string>(ipaPhones);
        }

        #endregion

        /// <summary>
        /// Load data for xml and save it to corresponding class.
        /// </summary>
        /// <param name="language">The language.</param>
        private void LoadData(string language)
        {
            Debug.Assert(_dom != null);

            int index = 0;
            bool result;
            XmlNodeList xmlNodeList;
            XmlNodeList exampleWordsNodeList;
            XmlNode xmlNode;
            XmlAttribute xmlAttri;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(_dom.NameTable);
            nsmgr.AddNamespace("tts", Schema.TargetNamespace);
            xmlAttri = _dom.DocumentElement.Attributes["lang"];
            if (xmlAttri.InnerText != language)
            {
                throw new InvalidDataException("The given language is incorrect");
            }

            xmlNode = _dom.DocumentElement.SelectSingleNode(@"tts:font", nsmgr);
            xmlAttri = xmlNode.Attributes["name"];
            _font = xmlAttri.InnerText;
            xmlNodeList = _dom.DocumentElement.SelectNodes(@"tts:key", nsmgr);
            foreach (XmlElement currentNode in xmlNodeList)
            {
                if (currentNode.HasAttribute("vowel"))
                {
                    KeyboardPhone currentPhone = new KeyboardPhone();
                    xmlAttri = currentNode.Attributes["ttsPhone"];
                    currentPhone.TtsPhone = xmlAttri.InnerText;
                    xmlAttri = currentNode.Attributes["ipaPhone"];
                    currentPhone.IpaPhone = xmlAttri.InnerText;
                    xmlAttri = currentNode.Attributes["vowel"];
                    if (bool.TryParse(xmlAttri.InnerText, out result))
                    {
                        if (result == true)
                        {
                            currentPhone.PhoneType = KeyboardInfo.PhoneTypeEnums.Vowel;
                        }
                        else
                        {
                            currentPhone.PhoneType = KeyboardInfo.PhoneTypeEnums.Consonant;
                        }
                    }
                    else
                    {
                        throw new InvalidDataException("Vowel format is not correct");
                    }

                    xmlAttri = currentNode.Attributes["displayedPhone"];
                    currentPhone.DisplayedPhone = xmlAttri.InnerText;
                    exampleWordsNodeList = currentNode.ChildNodes;
                    KeyboardPhone.WordInformation[] exampleWords =
                        new KeyboardPhone.WordInformation[exampleWordsNodeList.Count];
                    for (int i = 0; i < exampleWordsNodeList.Count; i++)
                    {
                        exampleWords[i].Grapheme =
                            exampleWordsNodeList[i].Attributes["grapheme"].InnerText;
                        exampleWords[i].IpaPronunciation =
                            exampleWordsNodeList[i].Attributes["ipaPronunciation"].InnerText;
                    }

                    currentPhone.SetExampleWords(exampleWords);
                    _keyboardPhoneList[index] = currentPhone;
                }
                else
                {
                    KeyboardSymbol currentSymbolInfo = new KeyboardSymbol();
                    xmlAttri = currentNode.Attributes["ttsPhone"];
                    currentSymbolInfo.TtsPhone = xmlAttri.InnerText;
                    xmlAttri = currentNode.Attributes["ipaPhone"];
                    currentSymbolInfo.IpaPhone = xmlAttri.InnerText;
                    xmlAttri = currentNode.Attributes["displayedPhone"];
                    currentSymbolInfo.DisplayedPhone = xmlAttri.InnerText;
                    currentSymbolInfo.PhoneType = KeyboardInfo.PhoneTypeEnums.Symbol;
                    xmlNode = currentNode.FirstChild;
                    xmlAttri = xmlNode.Attributes["description"];
                    currentSymbolInfo.Description = xmlAttri.InnerText;
                    _keyboardPhoneList[index] = currentSymbolInfo;
                }

                index++;
            }
        }

        /// <summary>
        /// Generate regexes for ReplaceUndisplayableSymbol and ConvertToOriginalPhone functions.
        /// </summary>
        private void GenerateRegex()
        {
            StringBuilder originalRegex = new StringBuilder();
            StringBuilder displayedRegex = new StringBuilder();
            originalRegex.Append("^( |/|");
            displayedRegex.Append("^( |/|");
            foreach (string ipaPhone in _ipaPhoneHashTable.Keys)
            {
                originalRegex.Insert(2, ipaPhone + "|");
                displayedRegex.Insert(2, _ipaPhoneHashTable[ipaPhone].DisplayedPhone + "|");
            }

            originalRegex.Remove(originalRegex.Length - 1, 1);
            displayedRegex.Remove(displayedRegex.Length - 1, 1);
            originalRegex.Append(")*$");
            displayedRegex.Append(")*$");
            _originalRegex = originalRegex.ToString();
            _displayedRegex = displayedRegex.ToString();
        }

        /// <summary>
        /// Generate hashtable for getting phone information structure by given tts, ipa or displayed phone.
        /// </summary>
        private void GenerateHashTable()
        {
            foreach (int key in _keyboardPhoneList.Keys)
            {
                KeyboardInfo keyboardInfo = _keyboardPhoneList[key];
                if (!_ipaPhoneHashTable.ContainsKey(keyboardInfo.IpaPhone))
                {
                    _ipaPhoneHashTable[keyboardInfo.IpaPhone] = keyboardInfo;
                }
                else
                {
                    throw new ArgumentException("Conflict in IPA phone mapping.");
                }

                if (!_ttsPhoneHashTable.ContainsKey(keyboardInfo.TtsPhone))
                {
                    _ttsPhoneHashTable[keyboardInfo.TtsPhone] = keyboardInfo;
                }
                else
                {
                    throw new ArgumentException("Conflict in TTS phone mapping.");
                }

                if (!_displayedPhoneHashTable.ContainsKey(keyboardInfo.DisplayedPhone))
                {
                    _displayedPhoneHashTable[keyboardInfo.DisplayedPhone] = keyboardInfo;
                }
                else
                {
                    throw new ArgumentException("Conflict in displayed phone mapping.");
                }
            }
        }

        /// <summary>
        /// Find keyboard phone.
        /// </summary>
        /// <param name="phoneText">Phone string.</param>
        /// <param name="phoneStyle">Phone style.</param>
        /// <returns>Keyboard phone.</returns>
        private KeyboardPhone FindKeyboardPhone(string phoneText, PhoneStyle phoneStyle)
        {
            Debug.Assert(!string.IsNullOrEmpty(phoneText));

            KeyboardInfo foundPhone = null;
            if (_keyboardPhoneList != null)
            {
                foreach (int key in _keyboardPhoneList.Keys)
                {
                    KeyboardInfo phone = _keyboardPhoneList[key];
                    string phoneLabel;
                    if (phoneStyle == PhoneStyle.IpaPhone)
                    {
                        phoneLabel = phone.IpaPhone;
                    }
                    else if (phoneStyle == PhoneStyle.TtsPhone)
                    {
                        phoneLabel = phone.TtsPhone;
                    }
                    else
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Invalid phone type [{0}]", phoneStyle);
                        throw new NotSupportedException(message);
                    }

                    if (string.CompareOrdinal(phoneLabel, phoneText) == 0)
                    {
                        foundPhone = phone;
                        break;
                    }
                }
            }

            return foundPhone as KeyboardPhone;
        }
    }

    /// <summary>
    /// The base class for the KeyboardPhone class and KeyboardSymbol class.
    /// </summary>
    public class KeyboardInfo
    {
        #region fields

        private string _ttsPhone;
        private string _ipaPhone;
        private string _displayedPhone;
        private PhoneTypeEnums _phoneType;

        #endregion

        #region enums

        /// <summary>
        /// Phone type enums.
        /// </summary>
        public enum PhoneTypeEnums
        {
            /// <summary>
            /// Default phone type.
            /// </summary>
            None,

            /// <summary>
            /// Consonant phone type.
            /// </summary>
            Consonant,

            /// <summary>
            /// Vowel phone type.
            /// </summary>
            Vowel,

            /// <summary>
            /// Symbol phone type.
            /// </summary>
            Symbol
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets TTS phone.
        /// </summary>
        public string TtsPhone
        {
            get
            {
                return _ttsPhone;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _ttsPhone = value;
            }
        }

        /// <summary>
        /// Gets or sets IPA phone.
        /// </summary>
        public string IpaPhone
        {
            get
            {
                return _ipaPhone;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _ipaPhone = value;
            }
        }

        /// <summary>
        /// Gets or sets Displayed phone.
        /// </summary>
        public string DisplayedPhone
        {
            get
            {
                return _displayedPhone;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _displayedPhone = value;
            }
        }

        /// <summary>
        /// Gets or sets PhoneType enum.
        /// </summary>
        public PhoneTypeEnums PhoneType
        {
            get
            {
                return _phoneType;
            }

            set
            {
                try
                {
                    _phoneType = value;
                }
                catch
                {
                    throw new ArgumentException("PhoneType");
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// The KeyboardPhone class defined for saving keyboard button information.
    /// </summary>
    public class KeyboardPhone : KeyboardInfo
    {
        #region fields

        private WordInformation[] _exampleWords;

        #endregion

        #region properties

        /// <summary>
        /// Gets Example words.
        /// </summary>
        public ReadOnlyCollection<WordInformation> ExampleWords
        {
            get
            {
                return new ReadOnlyCollection<WordInformation>(_exampleWords);
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Set example words.
        /// </summary>
        /// <param name="exampleWords">Example words.</param>
        public void SetExampleWords(WordInformation[] exampleWords)
        {
            if (exampleWords == null)
            {
                throw new ArgumentNullException("exampleWords");
            }

            _exampleWords = exampleWords;
        }

        #endregion

        #region word information

        /// <summary>
        /// Word information.
        /// </summary>
        public struct WordInformation
        {
            /// <summary>
            /// Word grapheme.
            /// </summary>
            public string Grapheme;

            /// <summary>
            /// IPA pronunciation.
            /// </summary>
            public string IpaPronunciation;
        }

        #endregion
    }

    /// <summary>
    /// The KeyboardSymbol class defined for saving keyboard symbol data.
    /// </summary>
    public class KeyboardSymbol : KeyboardInfo
    {
        #region fields

        private string _description;

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets Description.
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _description = value;
            }
        }

        #endregion
    }

    /// <summary>
    /// The keyboard event arguments.
    /// </summary>
    public class KeyboardEventArgs : EventArgs
    {
        #region fields

        private string _ttsPhone;
        private string _ipaPhone;
        private string _displayedPhone;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardEventArgs"/> class.
        /// </summary>
        /// <param name="ttsPhone">TTS phone.</param>
        /// <param name="ipaPhone">IPA phone.</param>
        /// <param name="displayedPhone">Displayed phone.</param>
        public KeyboardEventArgs(string ttsPhone, string ipaPhone, string displayedPhone)
        {
            _ttsPhone = ttsPhone;
            _ipaPhone = ipaPhone;
            _displayedPhone = displayedPhone;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets TTS phone.
        /// </summary>
        public string TtsPhone
        {
            get
            {
                return _ttsPhone;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _ttsPhone = value;
            }
        }

        /// <summary>
        /// Gets or sets IPA phone.
        /// </summary>
        public string IpaPhone
        {
            get
            {
                return _ipaPhone;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _ipaPhone = value;
            }
        }

        /// <summary>
        /// Gets or sets Displayed phone.
        /// </summary>
        public string DisplayedPhone
        {
            get
            {
                return _displayedPhone;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _displayedPhone = value;
            }
        }

        #endregion
    }

    /// <summary>
    /// The keyboard event arguments.
    /// </summary>
    public class KeyboardSpeakEventArgs : EventArgs
    {
        #region fields

        private string _exampleWord;
        private string _pronunciation;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardSpeakEventArgs"/> class.
        /// </summary>
        /// <param name="exampleWord">Word grapheme.</param>
        /// <param name="pron">Word pronunciation.</param>
        public KeyboardSpeakEventArgs(string exampleWord, string pron)
        {
            _exampleWord = exampleWord;
            _pronunciation = pron;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets Example word grapheme.
        /// </summary>
        public string ExampleWord
        {
            get
            {
                return _exampleWord;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _exampleWord = value;
            }
        }

        /// <summary>
        /// Gets or sets Pronunciation.
        /// </summary>
        public string Pronunciation
        {
            get
            {
                return _pronunciation;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _pronunciation = value;
            }
        }

        #endregion
    }
}