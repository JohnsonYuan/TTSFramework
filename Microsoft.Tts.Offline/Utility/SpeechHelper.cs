//----------------------------------------------------------------------------
// <copyright file="SpeechHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements speech environment helper funtions
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.Win32;

    /// <summary>
    /// SpeechHelper.
    /// </summary>
    [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
    public static class SpeechHelper
    {
        #region Public const fields

        /// <summary>
        /// The root location of the voice tokens in the registry.
        /// </summary>
        private const string TokensRootPath = @"SOFTWARE\Microsoft\Speech\Voices\Tokens";

        /// <summary>
        /// The root location of the WoW voice tokens in the registry.
        /// </summary>
        private const string TokensWowRootPath = @"SOFTWARE\Wow6432Node\Microsoft\Speech\Voices\Tokens";

        /// <summary>
        /// CLSID of the TTS 30 engine.
        /// </summary>
        private const string Tts30Clsid = "{a12bdfa1-c3a1-48ea-8e3f-27945e16cf7e}";

        #endregion

        #region Fields

        private static readonly Guid _mulanGuid = new Guid("{F51C7B23-6566-424C-94CF-2C4F83EE96FF}");

        // bug pointed out by v-minhu, which can't detect the right version number after 
        // install new sapi in XP box with original sapi
        // private const string sapiclsid = "{0655E396-25D0-11D3-9C26-00C04F8EF87C}";
        // change to SpVoice
        private static readonly Guid _sapiGuid = new Guid("{96749377-3391-11D2-9EE3-00C04F797396}");

        private static object _sapi53Installed;
        private static string _sapiVersion;
        private static string _mulanVersion;
        private static string _systemVersion;

        #endregion

        #region Public static properties

        /// <summary>
        /// Gets SAPI 6 version string.
        /// </summary>
        public static string Sapi6Version
        {
            get
            {
                string basepath = Environment.GetEnvironmentVariable("SystemRoot") + @"\winsxs";
                if (string.IsNullOrEmpty(basepath) || !Directory.Exists(basepath))
                {
                    // error
                    return null;
                }

                string[] subDirPaths = Directory.GetDirectories(basepath);
                int maxbuildversion = -1;
                string versionstring = null;
                foreach (string dir in subDirPaths)
                {
                    // x86_Microsoft.Speech.SAPI_6595b64144ccf1df_6.0.6301.0_x-ww_28060daa
                    if (!string.IsNullOrEmpty(dir) ||
                        dir.IndexOf("Microsoft.Speech.SAPI_6595b64144ccf1df_6", StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    Regex rg = new Regex(@"(\d+)\.(\d+).(\d+)\.(\d+)");
                    Match build = rg.Match(dir);
                    if (build.Success)
                    {
                        int buildver = int.Parse(build.Groups[3].Value, CultureInfo.InvariantCulture);
                        if (buildver > maxbuildversion)
                        {
                            maxbuildversion = buildver;
                            versionstring = build.Groups[1].Value
                                + "." + build.Groups[2].Value
                                + "." + build.Groups[3].Value
                                + "." + build.Groups[4].Value;
                        }
                    }
                    else
                    {
                        // skip
                    }
                }

                if (maxbuildversion >= 0)
                {
                    return versionstring;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets Collect the OS version information used by current Application.
        /// </summary>
        public static string OperatingSystemVersion
        {
            get
            {
                // if cache exists, return it;
                if (_systemVersion != null)
                {
                    return _systemVersion;
                }

                StringBuilder ver = new StringBuilder();
                string verKeyPath = @"Software\Microsoft\Windows NT\CurrentVersion";

                System.OperatingSystem info = System.Environment.OSVersion;
                ver.Append(info.Platform);
                ver.Append("(");
                ver.Append(info.Version.ToString());
                ver.Append(")");

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(verKeyPath))
                {
                    if (key == null)
                    {
                        return null;
                    }

                    ver.Append(" ");
                    ver.Append((string)key.GetValue("BuildLab"));
                    ver.Append("(");
                    ver.Append((string)key.GetValue("CSDVersion"));
                    ver.Append(")");
                }

                return _systemVersion = ver.ToString();
            }
        }

        /// <summary>
        /// Gets Try to collect the Mulan version information used by current Application.
        /// </summary>
        public static string MulanVersion
        {
            get
            {
                // if cache exists, return it
                if (!string.IsNullOrEmpty(_mulanVersion))
                {
                    return _mulanVersion;
                }

                StringBuilder versionString;
                string dllFilePath = null;
                FileVersionInfo fv = null;

                try
                {
                    // Bug raised by v-minhu: Should handle with manifest modification
                    // Handle this by finding actual module loaded by this application

                    // try to check the version of module actually loaded
                    ProcessModule m = FindModule("MulanEngine.dll");
                    if (m == null)
                    {
                        m = FindModule("MULANE~1.DLL");
                    }

                    if (m == null)
                    {
                        // after name update
                        m = FindModule("MSTTSEngine.dll");
                    }

                    if (m == null)
                    {
                        // after name update
                        m = FindModule("MSTTSE~1.DLL");
                    }

                    if (m != null)
                    {
                        dllFilePath = m.FileName;
                        fv = m.FileVersionInfo;
                    }
                    else
                    {
                        // get the dll version from register setting
                        dllFilePath = SpeechHelper.GetFullPathOfCom(_mulanGuid);
                        if (string.IsNullOrEmpty(dllFilePath) || !File.Exists(dllFilePath))
                        {
                            return null;
                        }

                        fv = FileVersionInfo.GetVersionInfo(dllFilePath);
                    }

                    versionString = new StringBuilder(fv.FileVersion);
                    versionString.Append(" " + Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));

                    if (SearchInFile("chk", dllFilePath))
                    {
                        versionString.Append(".chk");
                    }
                    else if (SearchInFile("fre", dllFilePath))
                    {
                        versionString.Append(".fre");
                    }
                }
                catch (ComNotFoundException)
                {
                    // System.Diagnostics.Trace.WriteLine(e.Message + "\r\n" + e.StackTrace);
                    return null;
                }

                return _mulanVersion = versionString.ToString();
            }
        }

        /// <summary>
        /// Gets Try to collect the SAPI version information used by current Application.
        /// </summary>
        public static string SapiVersion
        {
            get
            {
                // if cache exists, return it;
                if (!string.IsNullOrEmpty(_sapiVersion))
                {
                    return _sapiVersion;
                }

                StringBuilder versionString = null;
                string dllFilePath = null;
                FileVersionInfo fv = null;

                try
                {
                    // try to check the version of module actually loaded
                    ProcessModule m = FindModule("sapi.dll");
                    if (m != null)
                    {
                        dllFilePath = m.FileName;
                        fv = m.FileVersionInfo;
                    }
                    else
                    {
                        // get the dll version from register setting
                        dllFilePath = SpeechHelper.GetFullPathOfCom(_sapiGuid);
                        if (!File.Exists(dllFilePath))
                        {
                            return null;
                        }

                        fv = FileVersionInfo.GetVersionInfo(dllFilePath);
                    }

                    versionString = new StringBuilder(fv.FileVersion);
                    versionString.Append(" " + Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));

                    // check whether free or checked built
                    if (SearchInFile("chk", dllFilePath))
                    {
                        versionString.Append(".chk");
                    }
                    else if (SearchInFile("fre", dllFilePath))
                    {
                        versionString.Append(".fre");
                    }
                }
                catch (ComNotFoundException)
                {
                    // System.Diagnostics.Trace.WriteLine(e.Message + "\r\n" + e.StackTrace);
                    return null;
                }

                // cache it
                return _sapiVersion = versionString.ToString();
            }
        }

        /// <summary>
        /// Test whether the TTS voice token exists or not.
        /// </summary>
        /// <param name="tokenId">Voice token id.</param>
        /// <returns>True if exist, otherwise false.</returns>
        public static bool IsVoiceTokenIdExist(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                throw new ArgumentNullException("tokenId");
            }

            string tokenPath = FindTokenPath(tokenId);

            return !string.IsNullOrEmpty(tokenPath);
        }

        #endregion

        #region Public static operations

        /// <summary>
        /// Find the default voice token used in OS.
        /// </summary>
        /// <returns>Voice token.</returns>
        public static string FindDefaultVoice()
        {
            string orgVoiceToken = null;
            using (RegistryKey regkey =
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Speech\Voices", true))
            {
                if (regkey == null)
                {
                    return null;
                }

                string keyvalue = (string)regkey.GetValue("DefaultTokenId");
                if (string.IsNullOrEmpty(keyvalue))
                {
                    return null;
                }

                int tokenStart = keyvalue.LastIndexOf(@"\", StringComparison.Ordinal);
                if (tokenStart < 0)
                {
                    return null;
                }

                orgVoiceToken = keyvalue.Substring(tokenStart + 1);
            }

            return orgVoiceToken;
        }

        /// <summary>
        /// Get voice tolen id according to voice name.
        /// </summary>
        /// <param name="name">Voice name.</param>
        /// <returns>Voice token id.</returns>
        public static string GetVoiceTokenId(string name)
        {
            using (RegistryKey regkey = Registry.LocalMachine.OpenSubKey(SpeechHelper.TokensRootPath, false))
            {
                if (regkey == null)
                {
                    return null;
                }

                foreach (string subkeyname in regkey.GetSubKeyNames())
                {
                    RegistryKey subkey = regkey.OpenSubKey(subkeyname);
                    if (regkey != null)
                    {
                        string defName = (string)subkey.GetValue(string.Empty);
                        if (!string.IsNullOrEmpty(defName) && defName.IndexOf(name, StringComparison.Ordinal) != -1)
                        {
                            return subkeyname;
                        }
                    }

                    string voicename = (string)subkey.GetValue("VoiceName");
                    if (!string.IsNullOrEmpty(voicename) && voicename.IndexOf(name, StringComparison.Ordinal) != -1)
                    {
                        return subkeyname;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get file path of the COM class.
        /// </summary>
        /// <param name="clsid">Class if of COM object.</param>
        /// <returns>File path.</returns>
        public static string GetFullPathOfCom(Guid clsid)
        {
            string comPath = null;
            string keyPath = @"CLSID\{" + clsid.ToString() + @"}\InprocServer32";

            // Get the filepath of MulanEngine.dll from the register
            using (RegistryKey comKey = Registry.ClassesRoot.OpenSubKey(keyPath))
            {
                if (comKey == null)
                {
                    return null;
                }

                comPath = (string)comKey.GetValue(null);
            }

            if (!string.IsNullOrEmpty(comPath))
            {
                // formalize path, Mulan sometime registered MulanEngine with \"dll-path\", found by ditang
                // such as "\"C:\\PROGRA~1\\COMMON~1\\SPEECH~1\\MICROS~1\\MulanTTS\\MULANE~1.DLL\""
                comPath = comPath.Replace("\"", string.Empty);
            }

            return comPath;
        }

        /// <summary>
        /// Detect whether Mulan is installed
        ///     first, check if there Anna or Lili is in the voice list
        ///     second, check whether COM object is installed properly.
        /// </summary>
        /// <returns>True if installed, otherwise false.</returns>
        public static bool IsMulanInstalled()
        {
            bool detected = false;
            using (RegistryKey tokensKey = Registry.LocalMachine.OpenSubKey(SpeechHelper.TokensRootPath))
            {
                if (tokensKey == null)
                {
                    return false;
                }

                foreach (string subname in tokensKey.GetSubKeyNames())
                {
                    string defaultvalue = (string)tokensKey.OpenSubKey(subname).GetValue(null);
                    if (string.IsNullOrEmpty(defaultvalue))
                    {
                        continue;
                    }

                    if (defaultvalue.IndexOf("Anna", StringComparison.Ordinal) != -1
                        || defaultvalue.IndexOf("Lili", StringComparison.Ordinal) != -1)
                    {
                        detected = true;
                        break;
                    }
                }
            }

            if (!detected)
            {
                return false;
            }

            // double check whether COM object is installed
            try
            {
                string dllFilePath = SpeechHelper.GetFullPathOfCom(_mulanGuid);
                if (!string.IsNullOrEmpty(dllFilePath) && File.Exists(dllFilePath))
                {
                    detected = true;
                }
            }
            catch (ComNotFoundException)
            {
                detected = false;
            }

            // TODO: debug
            // return false;
            return detected;
        }

        /// <summary>
        /// Detect whether SAPI 5.3 installed.
        /// </summary>
        /// <returns>True if SAPI 5.3 installed, otherwise false.</returns>
        public static bool IsSapi53Installed()
        {
            string sapiversion = SapiVersion;
            if (_sapi53Installed != null)
            {
                return (bool)_sapi53Installed;
            }

            if (!string.IsNullOrEmpty(sapiversion) &&
                sapiversion.StartsWith("5.3", StringComparison.Ordinal))
            {
                _sapi53Installed = true;
            }
            else
            {
                _sapi53Installed = false;
                return false;
            }

            return (bool)_sapi53Installed;
        }

        /// <summary>
        /// Find the processmodule according to module name.
        /// </summary>
        /// <param name="moduleName">Module name.</param>
        /// <returns>ProcessModule.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static ProcessModule FindModule(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new ArgumentNullException("moduleName");
            }

            Process process = Process.GetCurrentProcess();
            foreach (ProcessModule module in process.Modules)
            {
                if (string.Compare(module.ModuleName, moduleName, true, CultureInfo.CurrentCulture) == 0)
                {
                    return module;
                }
            }

            return null;
        }

        /// <summary>
        /// Search a string in a file.
        /// </summary>
        /// <param name="search">String to search for.</param>
        /// <param name="filePath">File to search.</param>
        /// <returns>True if found, otherwise false.</returns>
        public static bool SearchInFile(string search, string filePath)
        {
            // Verification
            if (string.IsNullOrEmpty(search))
            {
                throw new ArgumentNullException("search");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            bool exist = false;
            try
            {
                FileStream fs =
                    new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    using (TextReader tr = new StreamReader(fs))
                    {
                        fs = null;
                        string data = tr.ReadToEnd();
                        if (!string.IsNullOrEmpty(data))
                        {
                            exist = data.IndexOf(search, StringComparison.Ordinal) != -1;
                        }
                    }
                }
                finally
                {
                    if (null != fs)
                    {
                        fs.Dispose();
                    }
                }
            }
            catch (IOException)
            {
                exist = false;
            }

            return exist;
        }

        #endregion

        #region TTS03 engine

        /// <summary>
        /// Find tokens for the language.
        /// </summary>
        /// <param name="language">Language of the token.</param>
        /// <returns>All token paht of the language.</returns>
        public static string[] FindTokens(Language language)
        {
            string[] tokens;
            using (RegistryKey token = Registry.LocalMachine.OpenSubKey(SpeechHelper.TokensRootPath))
            {
                tokens = FindTts30Tokens(language, token);
            }

            if (tokens == null &&
                Helper.Is64BitMachine())
            {
                using (RegistryKey token = Registry.LocalMachine.OpenSubKey(SpeechHelper.TokensWowRootPath))
                {
                    tokens = FindTts30Tokens(language, token);
                }
            }

            return tokens;
        }

        /// <summary>
        /// Find token name from token registry path.
        /// </summary>
        /// <param name="tokenId">Token registry path.</param>
        /// <returns>Token name.</returns>
        public static string FindTokenName(string tokenId)
        {
            string tokenName = string.Empty;

            string tokenPath = FindTokenPath(tokenId);

            using (RegistryKey subToken = Registry.LocalMachine.OpenSubKey(tokenPath))
            {
                string id = (string)subToken.GetValue("CLSID");
                if (Tts30Clsid != id)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Tts30 Clsid should be [{0}], " +
                        "acutal is [{1}] at [{2}]",
                        Tts30Clsid, id, tokenId + @"\" + "CLSID"));
                }

                using (RegistryKey attri = subToken.OpenSubKey("Attributes"))
                {
                    string version = (string)attri.GetValue("Version");
                    if (version != "3.0")
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Tts30 version should be [3.0], " +
                            "acutal is [{0}] at [{1}]",
                            version, id, tokenId + @"\Attributes\Version"));
                    }

                    string vendor = (string)attri.GetValue("Vendor");
                    if (vendor != "Microsoft")
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Tts30 version should be [Microsoft], " +
                            "acutal is [{0}] at [{1}]",
                            vendor, id, tokenId + @"\Attributes\Vendor"));
                    }

                    tokenName = (string)attri.GetValue("Name");
                }
            }

            return tokenName;
        }

        /// <summary>
        /// Find the voice token id for given language for TTS 3.0 Engine.
        /// </summary>
        /// <param name="language">Language of the voice token to find.</param>
        /// <param name="voiceName">Voice name of the token to find.</param>
        /// <returns>Token registry path.</returns>
        public static string FindTts30TokenId(Language language, string voiceName)
        {
            string tokenId = null;
            using (RegistryKey token = Registry.LocalMachine.OpenSubKey(SpeechHelper.TokensRootPath))
            {
                tokenId = FindTts30TokenId(language, voiceName, token);
            }

            if (string.IsNullOrEmpty(tokenId) &&
                Helper.Is64BitMachine())
            {
                using (RegistryKey token = Registry.LocalMachine.OpenSubKey(SpeechHelper.TokensWowRootPath))
                {
                    tokenId = FindTts30TokenId(language, voiceName, token);
                }
            }

            return tokenId;
        }

        /// <summary>
        /// Check whether TTS 3.0 local handler exists.
        /// </summary>
        /// <param name="language">Language of the engine to be checked.</param>
        public static void CheckTts30LocalHandler(Language language)
        {
            CheckTts30Engine();
            string enginePath = GetFullPathOfCom(new Guid(Tts30Clsid));
            string localHandlerPath = Path.Combine(Path.GetDirectoryName(enginePath),
                @"LocaleHandler\MSTTSLoc" + language.ToString() + ".dll");
            if (!File.Exists(localHandlerPath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Registered TTS 3.0 engine local handler [{0}] not found at: {1}. Please reinstall debug version of TTS 3.0 engine.",
                    Localor.LanguageToString(language), localHandlerPath);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Check whether TTS 3.0 engine exists.
        /// </summary>
        public static void CheckTts30Engine()
        {
            string enginePath = GetFullPathOfCom(new Guid(Tts30Clsid));
            if (string.IsNullOrEmpty(enginePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Could not found TTS 3.0 engine with CLSID: {0}. Please install debug version of TTS 3.0 engine.",
                    Tts30Clsid);
                throw new InvalidOperationException(message);
            }

            if (!File.Exists(enginePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Registered TTS 3.0 engine not found at: {0}. Please reinstall debug version of TTS 3.0 engine.",
                    enginePath);
                throw new InvalidOperationException(message);
            }

            if (!Microsoft.Tts.Offline.Utility.SpeechHelper.SearchInFile("chk", enginePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "TTS 3.0 engine [{0}] must be debug version to work with FontDebugger. Please reinstall debug version of TTS 3.0 engine.",
                    enginePath);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Setup engine environment.
        /// </summary>
        /// <param name="node">XmlNode with configuration data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static void SetupEngine(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            XmlNodeList nodes = node.SelectNodes("file");
            if (nodes == null)
            {
                return;
            }

            foreach (XmlNode subNode in nodes)
            {
                XmlElement ele = (XmlElement)subNode;

                string type = ele.GetAttribute("type");
                string filePath = ele.GetAttribute("path");
                if (!File.Exists(filePath))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException),
                        filePath);
                }

                switch (type)
                {
                    case "dll":
                        CommandLine.SuccessRunCommand(Helper.FindRegsvr32Tool(),
                            " /s \"" + filePath + "\"", Environment.CurrentDirectory);
                        break;
                    case "registry":
                        CommandLine.SuccessRunCommand(Helper.FindRegEditTool(),
                            " /s /c \"" + filePath + "\"", Environment.CurrentDirectory);
                        break;
                    default:
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Only dll or registry is supported to setup. But the type [{0}] is found.",
                            type);
                        throw new NotSupportedException(message);
                }
            }
        }

        /// <summary>
        /// Set default voice for Windows.
        /// </summary>
        /// <param name="tokenId">Voice token id.</param>
        /// <returns>Original defaukt voice token id.</returns>
        public static string SetDefaultVoice(string tokenId)
        {
            string originalTokenId = null;

            using (RegistryKey voices =
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Speech\Voices"))
            {
                if (voices == null)
                {
                    throw new InvalidDataException(@"Can not open or create " +
                        @"CurrentUser key [Software\Microsoft\Speech\Voices] for write");
                }

                originalTokenId = (string)voices.GetValue("DefaultTokenId");

                string tokenPath = FindTokenPath(tokenId);
                if (tokenPath == null)
                {
                    throw new InvalidDataException(
                        "\nVoice token \"" + tokenId + "\" doesn't exist in:\n" +
                        @"HKEY_LOCAL_MACHINE\" + TokensRootPath + "\n" +
                        @"HKEY_LOCAL_MACHINE\" + TokensWowRootPath + "\n\n" +
                        @"Please check your config file, and use these existing voice tokens.");
                }

                tokenPath = Path.Combine(Registry.LocalMachine.Name, tokenPath);
                voices.SetValue("DefaultTokenId", tokenPath);
            }

            return Path.GetFileName(originalTokenId);
        }

        /// <summary>
        /// Set voice path.
        /// </summary>
        /// <param name="path">Voice font path.</param>
        /// <param name="tokenId">Voice token id.</param>
        public static void SetVoicePath(string path, string tokenId)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            string tokenPath = FindTokenPath(tokenId);
            using (RegistryKey token = Registry.LocalMachine.OpenSubKey(tokenPath, true))
            {
                if (token == null)
                {
                    throw new ArgumentNullException(
                        "Can not open localmachine key [" + tokenPath + "] for write");
                }

                token.SetValue("VoicePath", path);
            }
        }

        /// <summary>
        /// Find a registry path of a specified voice token id.
        /// </summary>
        /// <param name="tokenId">Voice token id.</param>
        /// <returns>Voice token path.</returns>
        public static string FindTokenPath(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                throw new ArgumentNullException("tokenId");
            }

            string tokenPath = Path.Combine(TokensRootPath, tokenId);
            using (RegistryKey token = Registry.LocalMachine.OpenSubKey(tokenPath))
            {
                if (token != null)
                {
                    return tokenPath;
                }
            }

            // search in WoW
            tokenPath = Path.Combine(TokensWowRootPath, tokenId);
            using (RegistryKey token = Registry.LocalMachine.OpenSubKey(tokenPath))
            {
                if (token != null)
                {
                    return tokenPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Set Tts DebugObj to a specified token.
        /// </summary>
        /// <param name="tokenId">Voice token id.</param>
        /// <param name="clsid">Class id of debugger.</param>
        /// <param name="enabled">Enabling flag.</param>
        public static void SetTtsDebug(string tokenId, string clsid, bool enabled)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                throw new ArgumentNullException("tokenId");
            }

            string tokenPath = FindTokenPath(tokenId);
            using (RegistryKey tokenKey = Registry.LocalMachine.OpenSubKey(tokenPath, true))
            {
                if (tokenKey == null)
                {
                    string fullPath = Path.Combine(Registry.LocalMachine.Name, tokenPath);
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Can not {0} TTS debugging logger for voice token {1}, since it does not exist in the registry at [{2}].",
                        enabled ? "set" : "unset", tokenId, fullPath);
                    throw new InvalidDataException(message);
                }

                if (enabled)
                {
                    if (string.IsNullOrEmpty(clsid))
                    {
                        throw new ArgumentNullException("clsid");
                    }

                    tokenKey.SetValue("DebugObj", clsid);
                }
                else
                {
                    // remove debug obj
                    tokenKey.DeleteValue("DebugObj");
                }
            }
        }

        #endregion

        #region TTS log

        /// <summary>
        /// Remove clsid registry for given clsid string.
        /// </summary>
        /// <param name="clsid">CLSID to remove from the registry key.</param>
        public static void RemoveComRegistry(string clsid)
        {
            using (RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(@"CLSID", true))
            using (RegistryKey subKey = regKey.OpenSubKey(clsid))
            {
                if (subKey != null)
                {
                    regKey.DeleteSubKeyTree(clsid);
                }
            }
        }

        /// <summary>
        /// Registry assmebly as COM object.
        /// </summary>
        /// <param name="assemblyPath">Location of assembly file to registry as COM assembly.</param>
        /// <param name="regAsmPath">Location of RegAsm.exe.</param>
        /// <param name="gacUtilPath">Location of GacUtil.exe.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static void RegistryManagedCom(string assemblyPath, string regAsmPath, string gacUtilPath)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException("assemblyPath");
            }

            if (string.IsNullOrEmpty(regAsmPath))
            {
                throw new ArgumentNullException("regAsmPath");
            }

            if (!File.Exists(regAsmPath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    regAsmPath);
            }

            if (string.IsNullOrEmpty(gacUtilPath))
            {
                throw new ArgumentNullException("gacUtilPath");
            }

            if (!File.Exists(gacUtilPath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    gacUtilPath);
            }

            if (!File.Exists(assemblyPath))
            {
                string appDir = Path.GetDirectoryName(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                assemblyPath = Path.Combine(appDir, Path.GetFileName(assemblyPath));
                if (!File.Exists(assemblyPath))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException), assemblyPath);
                }
            }

            // Remove other versions of the assembly
            UnregistryManagedCom(assemblyPath, regAsmPath, gacUtilPath);

            // Registry the .NET dll as COM object
            CommandLine.RunCommand(regAsmPath,
                "\"" + assemblyPath + "\"", false, true, Environment.CurrentDirectory);

            CommandLine.RunCommand(gacUtilPath,
                " -i \"" + assemblyPath + "\"", false, true, Environment.CurrentDirectory);
        }

        /// <summary>
        /// Registry assmebly as COM object.
        /// </summary>
        /// <param name="assemblyPath">Location of assembly file to registry as COM assembly.</param>
        /// <param name="regAsmPath">Location of RegAsm.exe.</param>
        /// <param name="gacUtilPath">Location of GacUtil.exe.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static void UnregistryManagedCom(string assemblyPath, string regAsmPath, string gacUtilPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException("assemblyPath");
            }

            if (string.IsNullOrEmpty(regAsmPath))
            {
                throw new ArgumentNullException("regAsmPath");
            }

            if (string.IsNullOrEmpty(gacUtilPath))
            {
                throw new ArgumentNullException("gacUtilPath");
            }

            if (!File.Exists(assemblyPath))
            {
                string appDir = Path.GetDirectoryName(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                assemblyPath = Path.Combine(appDir, Path.GetFileName(assemblyPath));
                if (!File.Exists(assemblyPath))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException), assemblyPath);
                }
            }

            // Registry the .NET dll as COM object
            if (!File.Exists(regAsmPath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    regAsmPath);
            }

            CommandLine.RunCommand(regAsmPath,
                " /unregister \"" + assemblyPath + "\"", false, true, Environment.CurrentDirectory);

            if (!File.Exists(gacUtilPath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    gacUtilPath);
            }

            // Remove other versions of the assembly
            CommandLine.RunCommand(gacUtilPath,
                " /u \"" + Path.GetFileName(assemblyPath) + "\"", false, true, Environment.CurrentDirectory);
        }

        #endregion

        #region Language
        /// <summary>
        /// Check the language whether is Latin language.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <returns>True for Latin language.</returns>
        public static bool IsLatinLanguage(Language language)
        {
            bool isLatinLanguage = true;

            if (language == Microsoft.Tts.Offline.Language.JaJP ||
                language == Microsoft.Tts.Offline.Language.KoKR ||
                language == Microsoft.Tts.Offline.Language.ZhCN ||
                language == Microsoft.Tts.Offline.Language.ZhTW)
            {
                isLatinLanguage = false;
            }

            return isLatinLanguage;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Get all token ID of the language.
        /// </summary>
        /// <param name="language">Language of the voice token to find.</param>
        /// <param name="token">Token registry.</param>
        /// <returns>Token registry path.</returns>
        private static string[] FindTts30Tokens(Language language, RegistryKey token)
        {
            List<string> tokens = new List<string>();
            foreach (string subName in token.GetSubKeyNames())
            {
                using (RegistryKey subToken = token.OpenSubKey(subName))
                {
                    string id = (string)subToken.GetValue("CLSID");
                    if (Tts30Clsid != id)
                    {
                        continue;
                    }

                    using (RegistryKey attri = subToken.OpenSubKey("Attributes"))
                    {
                        string version = (string)attri.GetValue("Version");
                        if (version != "3.0")
                        {
                            continue;
                        }

                        string vendor = (string)attri.GetValue("Vendor");
                        if (vendor != "Microsoft")
                        {
                            continue;
                        }

                        string lid = (string)attri.GetValue("Language");
                        Language lang = (Language)int.Parse(lid,
                            NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        if (lang == language)
                        {
                            tokens.Add(subName);
                        }
                    }
                }
            }

            return tokens.ToArray();
        }

        /// <summary>
        /// Find the voice token id for given language for TTS 3.0 at given token key.
        /// </summary>
        /// <param name="language">Language of the voice token to find.</param>
        /// <param name="voiceName">Voice name of the token to find.</param>
        /// <param name="token">Token registry.</param>
        /// <returns>Token registry path.</returns>
        private static string FindTts30TokenId(Language language, string voiceName, RegistryKey token)
        {
            string[] tokens = FindTts30Tokens(language, token);
            if (tokens.Length <= 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Can't find token for language[{0}]",
                    Localor.LanguageToString(language)));
            }

            string voiceToken = string.Empty;

            if (string.IsNullOrEmpty(voiceName))
            {
                voiceToken = tokens[0];
            }
            else
            {
                foreach (string tokenPath in tokens)
                {
                    using (RegistryKey subToken = token.OpenSubKey(tokenPath))
                    {
                        using (RegistryKey attri = subToken.OpenSubKey("Attributes"))
                        {
                            string name = (string)attri.GetValue("Name");
                            if (name.Equals(voiceName))
                            {
                                voiceToken = tokenPath;
                                break;
                            }
                        }
                    }
                }
            }

            return voiceToken;
        }

        #endregion
    }

    /// <summary>
    /// Exeption to identify that some COM not registered.
    /// </summary>
    [Serializable]
    public class ComNotFoundException : System.Exception
    {
        #region Fields

        private Guid _clsid;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="ComNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="clsid">Class id.</param>
        public ComNotFoundException(string message, Guid clsid)
            : base(message)
        {
            _clsid = clsid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner exception.</param>
        public ComNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        public ComNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComNotFoundException"/> class.
        /// </summary>
        public ComNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComNotFoundException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected ComNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Class id.
        /// </summary>
        public Guid Clsid
        {
            get { return _clsid; }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Get object data.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }
}