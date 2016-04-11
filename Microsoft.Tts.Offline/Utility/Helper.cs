//----------------------------------------------------------------------------
// <copyright file="Helper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements help functions
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Waveform;
    using Microsoft.Win32;

    /// <summary>
    /// The voice fonts.
    /// </summary>
    public enum VoiceFontNames
    {
        /// <summary>
        /// Zh-CN Lily.
        /// </summary>
        ZhCN,

        /// <summary>
        /// Zh-CN Yaoyao APM.
        /// </summary>
        ZhCNYaoyao,

        /// <summary>
        /// En-US Helen 8K HTS.
        /// </summary>
        EnUS,

        /// <summary>
        /// En-US Rus.
        /// </summary>
        EnUSRus,

        /// <summary>
        /// En-US fixed point.
        /// </summary>
        EnUSFixedPoint,
    }

    /// <summary>
    /// Waveform utility.
    /// </summary>
    public static class WaveUtil
    {
        /// <summary>
        /// Gets the supported list of samples per second.
        /// </summary>
        public static IList<int> SupportedSamplesPerSecond
        {
            get
            {
                return Enum.GetValues(typeof(WaveSamplesPerSecond))
                    .Cast<WaveSamplesPerSecond>()
                    .Where(s => s != WaveSamplesPerSecond.Undefined)
                    .Select(s => (int)s).ToArray();
            }
        }
    }

    /// <summary>
    /// Static helper, including various help functions.
    /// </summary>
    public static class Helper
    {
        #region Public const fields

        /// <summary>
        /// Max length of short Windows file or directory path.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "short", Justification = "Ignore.")]
        public const int MaxShortPath = 260;

        /// <summary>
        /// Define a default Threshold value for comparing double number.
        /// </summary>
        public const double ThresholdConst = 0.000001;

        /// <summary>
        /// Plus Double zero.
        /// </summary>
        public const double PlusDoubleZero = 1e-15;

        /// <summary>
        /// Minus Double zero.
        /// </summary>
        public const double MinusDoubleZero = -1e-15; 

        #endregion

        #region Play Sound

        /// <summary>
        /// SND_SYNC .
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member", Justification = "Ignore.")]
        public const int SND_SYNC = 0x0;

        /// <summary>
        /// SND_ASYNC .
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member", Justification = "Ignore.")]
        public const int SND_ASYNC = 0x1;

        /// <summary>
        /// SND_MEMORY .
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member", Justification = "Ignore.")]
        public const int SND_MEMORY = 0x4;

        /// <summary>
        /// SND_FILENAME .
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "FILENAME", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member", Justification = "Ignore.")]
        public const int SND_FILENAME = 0x20000;

        #endregion

        private const string ExplorerExe = "explorer";

        #region Public delegates

        /// <summary>
        /// Delegate for string line handling, such as for iterating text file.
        /// </summary>
        /// <param name="line">Line string.</param>
        /// <param name="stop">Indicate whether or not to stop iterating.</param>
        public delegate void LineHandler(string line, ref bool stop);

        /// <summary>
        /// Indicating whether ignore the line when compare the text file.
        /// </summary>
        /// <param name="line">Line to compare.</param>
        /// <returns>Whether ignore the line when compare the text file.</returns>
        public delegate bool IgnoreLineWhenCompare(string line);

        #endregion

        /// <summary>
        /// Gets Temp file/folder full path.
        /// </summary>
        public static string TempFullPath
        {
            get
            {
                // Because windows will create a empty file after call "GetTempFileName",
                // add a suffix "offline" to get a file path that has not been created.
                return Path.Combine(Path.GetTempPath(), GetTempFileName() + "Offline");
            }
        }

        #region Console operations

        /// <summary>
        /// Print message into output or error with different color according to severity.
        /// </summary>
        /// <param name="severity">Severity.</param>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments to format.</param>
        public static void PrintColorMessage(ErrorSeverity severity, string format, params object[] args)
        {
            PrintColorMessage(severity, Helper.NeutralFormat(format, args));
        }

        /// <summary>
        /// Print message into output or error with different color according to severity.
        /// </summary>
        /// <param name="severity">Severity.</param>
        /// <param name="message">Message.</param>
        public static void PrintColorMessage(ErrorSeverity severity, string message)
        {
            ConsoleColor _oldColor = Console.ForegroundColor;
            switch (severity)
            {
                case ErrorSeverity.MustFix:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case ErrorSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    break;
            }

            switch (severity)
            {
                case ErrorSeverity.MustFix:
                case ErrorSeverity.Warning:
                    Console.Error.WriteLine(message);
                    break;
                default:
                    Console.WriteLine(message);
                    break;
            }

            Console.ForegroundColor = _oldColor;
        }

        /// <summary>
        /// Print success message into output.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments to format.</param>
        public static void PrintSuccessMessage(string format, params object[] args)
        {
            PrintColorMessageToOutput(ConsoleColor.Green, format, args);
        }

        /// <summary>
        /// Print message into output.
        /// </summary>
        /// <param name="color">Color.</param>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments to format.</param>
        public static void PrintColorMessageToOutput(ConsoleColor color, string format, params object[] args)
        {
            PrintColorMessageToOutput(color, Helper.NeutralFormat(format, args));
        }

        /// <summary>
        /// Print message into output.
        /// </summary>
        /// <param name="color">Color.</param>
        /// <param name="message">Message.</param>
        public static void PrintColorMessageToOutput(ConsoleColor color, string message)
        {
            ConsoleColor _oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = _oldColor;
        }

        /// <summary>
        /// Print message into error.
        /// </summary>
        /// <param name="color">Color.</param>
        /// <param name="format">Format string.</param>
        /// <param name="args">Arguments to format.</param>
        public static void PrintColorMessageToError(ConsoleColor color, string format, params object[] args)
        {
            PrintColorMessageToError(color, Helper.NeutralFormat(format, args));
        }

        /// <summary>
        /// Print message into error.
        /// </summary>
        /// <param name="color">Color.</param>
        /// <param name="message">Message.</param>
        public static void PrintColorMessageToError(ConsoleColor color, string message)
        {
            ConsoleColor _oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = _oldColor;
        }
        #endregion

        #region Windows message

        /// <summary>
        /// Send message.
        /// </summary>
        /// <param name="hWnd">Window handle to post message to.</param>
        /// <param name="message">Message.</param>
        /// <param name="wParam">WParam.</param>
        /// <param name="lParam">LParam.</param>
        /// <returns>Return code.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "w", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "l", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "h", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wnd", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param", Justification = "Ignore."), 
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Ignore."), 
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability",
            "CA1401:PInvokesShouldNotBeVisible", Justification = "Ignore."),
        DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWnd,
            [MarshalAs(UnmanagedType.U4)] int message, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Post message.
        /// </summary>
        /// <param name="hWnd">Window handle to post message to.</param>
        /// <param name="message">Message.</param>
        /// <param name="wParam">WParam.</param>
        /// <param name="lParam">LParam.</param>
        /// <returns>True if succeeded, otherwise flase.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "w", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "l", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "h", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Wnd", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param", Justification = "Ignore."), 
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability",
            "CA1401:PInvokesShouldNotBeVisible", Justification = "Ignore."),
        DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd,
            [MarshalAs(UnmanagedType.U4)] int message, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Play Sound

        /// <summary>
        /// Windows API play sound.
        /// </summary>
        /// <param name="soundFile">Sound file to play.</param>
        /// <param name="hmod">Play mode.</param>
        /// <param name="falgs">Flags.</param>
        /// <returns>Return code.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "hmod", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "falgs", Justification = "Ignore."), 
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability",
            "CA1901:PInvokeDeclarationsShouldBePortable", MessageId = "1", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability",
            "CA1401:PInvokesShouldNotBeVisible", Justification = "Ignore."),
        DllImport("winmm.dll", EntryPoint = "PlaySound", CharSet = CharSet.Unicode)]
        public static extern int PlaySound(string soundFile, int hmod, int falgs);

        #endregion

        #region File system

        /// <summary>
        /// Tests whether a directory exists and is empty.
        /// </summary>
        /// <param name="path">The given directory to test.</param>
        /// <returns>True for the given directory is an empty one, otherwise false.</returns>
        public static bool IsEmptyDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(Helper.NeutralFormat("Directory \"{0}\" does not exist", path));
            }

            return Directory.GetFileSystemEntries(path).Length == 0;
        }

        /// <summary>
        /// Get absolute path.
        /// </summary>
        /// <param name="path">Relative path.</param>
        /// <returns>String.</returns>
        public static string GetAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            return GetFullPath(Environment.CurrentDirectory, path);
        }

        /// <summary>
        /// Get full path to the file.
        /// </summary>
        /// <param name="dataDir">Data directory.</param>
        /// <param name="path">File path.</param>
        /// <returns>Full path.</returns>
        public static string GetFullPath(string dataDir, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("path is null");
            }

            string rootedPath = Path.IsPathRooted(path) ? path : 
                string.IsNullOrEmpty(dataDir) ? Path.GetFullPath(path) : Path.Combine(dataDir, path);
            if (!Path.IsPathRooted(rootedPath))
            {
                string message = Helper.NeutralFormat("Cannot make a full path from path [{0}] and [{1}]", dataDir, path);
                throw new ArgumentException(message);
            }

            return rootedPath;
        }

        /// <summary>
        /// Get sub files relative path.
        /// </summary>
        /// <param name="dir">Search root dir.</param>
        /// <param name="searchPattern">Search pattern.</param>
        /// <returns>All sub files relative path.</returns>
        public static Collection<string> GetSubFilesRelativePath(string dir, string searchPattern)
        {
            return GetSubFilesRelativePath(dir, searchPattern, true);
        }

        /// <summary>
        /// Get sub files relative path.
        /// </summary>
        /// <param name="dir">Search root dir.</param>
        /// <param name="searchPattern">Search pattern.</param>
        /// <param name="recursive">Search recursivly.</param>
        /// <returns>All sub files relative path.</returns>
        public static Collection<string> GetSubFilesRelativePath(string dir, string searchPattern,
            bool recursive)
        {
            Collection<string> result = new Collection<string>();
            if (!Path.IsPathRooted(dir))
            {
                dir = Path.Combine(Environment.CurrentDirectory, dir);
            }

            string rootDir = dir.Trim();
            if (rootDir[rootDir.Length - 1] != '\\')
            {
                rootDir = rootDir + @"\";
            }

            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (string filePath in Directory.GetFiles(rootDir, searchPattern, searchOption))
            {
                int index = filePath.IndexOf(rootDir, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Can't find root [{0}] dir in sub dir [{1}]",
                        dir, filePath));
                }

                result.Add(filePath.Substring(index + rootDir.Length));
            }

            return result;
        }

        /// <summary>
        /// Search the exactly location for given file with given file name.
        /// </summary>
        /// <param name="fileName">File name of the file to search for.</param>
        /// <returns>The location of the file, null if not found.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static string SearchFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            string[] searchDirs = FindSearchDirs();

            string foundFilePath = null;
            foreach (string dir in searchDirs)
            {
                string testFilePath = Path.Combine(dir, fileName);
                if (Helper.IsValidPath(testFilePath) && File.Exists(testFilePath))
                {
                    foundFilePath = testFilePath;
                    break;
                }
            }

            return foundFilePath;
        }

        /// <summary>
        /// Ensure the file exist and convert the long path to short path.
        /// </summary>
        /// <param name="longPath">Long file path.</param>
        /// <returns>Short path.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static string FileToShortPath(string longPath)
        {
            if (!File.Exists(longPath))
            {
                // make a empty file, so that we can call to GetShortPathName
                Helper.EnsureFolderExistForFile(longPath);
                using (File.Create(longPath))
                {
                }
            }

            return ToShortPath(longPath);
        }

        /// <summary>
        /// Ensure the directory exist, if no, create it and convert the path to short path.
        /// </summary>
        /// <param name="longPath">Long directory path.</param>
        /// <returns>Short path.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static string DirToShortPath(string longPath)
        {
            if (!Directory.Exists(longPath))
            {
                // make a directory, so that we can call to GetShortPathName
                Helper.EnsureFolderExist(longPath);
            }

            return ToShortPath(longPath);
        }

        /// <summary>
        /// Convert the path to short path, if the file/directory does not exist,
        /// Throw an InvalidDataException exception.
        /// </summary>
        /// <param name="longPath">Long path.</param>
        /// <returns>Short path.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static string ToShortPath(string longPath)
        {
            if (string.IsNullOrEmpty(longPath))
            {
                throw new ArgumentNullException("longPath");
            }

            StringBuilder sb = new StringBuilder(MaxShortPath);

            if (Helper.GetShortPathName(longPath, sb, sb.Capacity) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (sb.Length == 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Zero length of the result of conversion to short path.");
                throw new InvalidDataException(message);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Call to Path.GetTempFileName() with exception handling
        /// <param />
        /// Suppress this warning to make this API the same as Path.GetTempFileName().
        /// </summary>
        /// <returns>Temporary file path.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate", Justification = "Ignore.")]
        public static string GetTempFileName()
        {
            try
            {
                return Path.GetTempFileName();
            }
            catch (ArgumentException ae)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Failed to get the path of the current system's temporary folder.");
                throw new IOException(message, ae);
            }
            catch (IOException ioe)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Failed to create new temporary file from the current system's temporary folder [{0}].",
                    Path.GetTempPath());
                throw new IOException(message, ioe);
            }
            catch (NotSupportedException nse)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Failed to create new temporary file from the current system's temporary folder [{0}].",
                    Path.GetTempPath());
                throw new IOException(message, nse);
            }
            catch (SecurityException se)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Failed to create new temporary file from the current system's temporary folder.");
                throw new IOException(message, se);
            }
        }

        /// <summary>
        /// Get temporary folder name
        /// Add a suffix "_offline" to clarify its an offline data folder.
        /// </summary>
        /// <returns>Temporary folder name.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate", Justification = "Ignore.")]
        public static string GetTempFolderName()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                string tempFolder;
                do
                {
                    tempFolder = Path.Combine(tempPath, Path.GetRandomFileName());
                    tempFolder += "_offline";
                }
                while (Directory.Exists(tempFolder));

                Directory.CreateDirectory(tempFolder);
                return tempFolder;
            }
            catch (SecurityException se)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Failed to create new temporary folder from the current system's temporary folder.");
                throw new IOException(message, se);
            }
        }

        /// <summary>
        /// Append text file to existing file.
        /// </summary>
        /// <param name="targetFile">Target file to be appended to.</param>
        /// <param name="encoding">File encoding both for input file an output file.</param>
        /// <param name="files">Locations of source text files to read from for appending.</param>
        public static void AppendTextFiles(string targetFile, Encoding encoding, params string[] files)
        {
            if (string.IsNullOrEmpty(targetFile))
            {
                throw new ArgumentNullException("targetFile");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            Helper.TestWritable(targetFile);

            for (int i = 0; i < files.Length; i++)
            {
                if (files[i] == null)
                {
                    throw new ArgumentNullException(string.Format(CultureInfo.InstalledUICulture,
                        "files[{0}]", i));
                }

                if (!File.Exists(files[i]))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException), files[i]);
                }
            }
            
            using (StreamWriter sw = new StreamWriter(targetFile, true, encoding))
            {
                foreach (string file in files)
                {
                    using (StreamReader sr = new StreamReader(file, encoding))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Whether the file is readonly.
        /// </summary>
        /// <param name="filePath">File path to be checked.</param>
        /// <returns>Whehter the file is readonly.</returns>
        public static bool IsReadonlyFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("filePath");
            }

            bool isReadonly = false;

            FileInfo fileInfo = new FileInfo(filePath);
            if ((fileInfo.Attributes & FileAttributes.ReadOnly) > 0)
            {
                isReadonly = true;
            }

            return isReadonly;
        }

        /// <summary>
        /// Create INI file and return path without file extension.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>String.</returns>
        public static string GetVoicePathByCreatingIniFile(string content)
        {
            string iniFileNameWithoutExtension = GetTempFileName();
            using (StreamWriter sw = new StreamWriter(iniFileNameWithoutExtension + ".ini"))
            {
                sw.Write(content);
            }

            return iniFileNameWithoutExtension;
        }

        /// <summary>
        /// Get local share file full path, if it's not local share file, return input file.
        /// Input: \\localhost\ccs_work\v-hub\Test.txt. 
        /// Output: D:\ccs_work\v-hub\Test.txt.
        /// </summary>
        /// <param name="shareFile">Local share file path.</param>
        /// <returns>Full path.</returns>
        public static string GetLocalShareFileFullPath(string shareFile)
        {
            // 1.Is share File?
            const string UNCPathHeader = @"\\";
            if (File.Exists(shareFile) && shareFile.StartsWith(UNCPathHeader))
            {
                string sharePath = shareFile.Replace(UNCPathHeader, string.Empty);
                string[] sharePathItems = sharePath.Split(Path.DirectorySeparatorChar);
                string computer = sharePathItems[0];
                string shareName = sharePathItems[1];

                try
                {
                    System.Net.IPHostEntry shareHost = System.Net.Dns.GetHostEntry(computer);
                    System.Net.IPHostEntry localHost = System.Net.Dns.GetHostEntry(Environment.MachineName);

                    // 2.Is local share file?
                    if (shareHost.HostName.Equals(localHost.HostName))
                    {
                        // 3.Query local path from share names.
                        string query = string.Format("select  *  from  win32_share where Name =\"{0}\"", shareName);
                        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                        {
                            ManagementObjectCollection results = searcher.Get();
                            if (results != null && results.Count == 1)
                            {
                                ManagementBaseObject result = results.Cast<ManagementBaseObject>().First();
                                sharePathItems[0] = string.Empty;
                                sharePathItems[1] = result["Path"].ToString();
                                string localFullPath = Path.Combine(sharePathItems);
                                if (File.Exists(localFullPath))
                                {
                                    shareFile = localFullPath;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string message = Helper.NeutralFormat("The server[{0}] could not be found!", computer);
                    throw new InvalidDataException(message, ex);
                }
            }

            return shareFile;
        }

        /// <summary>
        /// Check whether the input path is a local path.
        /// </summary>
        /// <param name="path">Input path.</param>
        /// <returns>Ture if the input path is a local path.</returns>
        public static bool IsLocalPath(string path)
        {
            bool isLocalPath = false;

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            string driveName = Path.GetPathRoot(path);
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo d in drives)
            {
                if (d.Name == driveName)
                {
                    isLocalPath = true;
                }
            }

            return isLocalPath;
        }

        #endregion

        #region Test relative

        /// <summary>
        /// Build command argument string.
        /// </summary>
        /// <param name="args">Command line argument list.</param>
        /// <returns>Command line argument string.</returns>
        public static string BuildArgument(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string arg in args)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                // Should append @"\" if the last character of "arg" is @"\"
                // Because (\") will be transformed to (") by the Command-line Building rule
                string newArg = arg;
                char[] argChars = arg.ToCharArray();
                if (argChars[argChars.Length - 1].Equals('\\'))
                {
                    newArg += @"\";
                }

                // use current culture, as it mainly handles file names.
                sb.Append(Helper.NeutralFormat(@"""{0}""", newArg));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Build command argument string.
        /// </summary>
        /// <param name="args">Command line argument list.</param>
        /// <returns>Command line argument string.</returns>
        public static string BuildArgument(ICollection<string> args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string arg in args)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                // Should append @"\" if the last character of "arg" is @"\"
                // Because (\") will be transformed to (") by the Command-line Building rule
                string newArg = arg;
                char[] argChars = arg.ToCharArray();
                if (argChars[argChars.Length - 1].Equals('\\'))
                {
                    newArg += @"\";
                }

                // use current culture, as it mainly handles file names.
                sb.Append(Helper.NeutralFormat(@"""{0}""", newArg));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Append argument list to original argument.
        /// </summary>
        /// <param name="originalArgument">Original argument to be appended.</param>
        /// <param name="args">Argument list to append.</param>
        /// <returns>Full argument string.</returns>
        public static string AppendArgument(string originalArgument, string[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(originalArgument);
            if (args.Length > 0)
            {
                sb.Append(" ");
                sb.Append(BuildArgument(args));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Triggle debug break.
        /// </summary>
        /// <param name="yesNo">Break or not.</param>
        /// <returns>True.</returns>
        public static bool DebuggerBreak(bool yesNo)
        {
            if (yesNo)
            {
                System.Diagnostics.Debugger.Break();
            }

            return true;
        }

        /// <summary>
        /// Finds the testing root path.
        /// <example>~\target\offline\objd\i386.</example>
        /// </summary>
        /// <returns>Test root directory path.</returns>
        public static string FindTestRootPath()
        {
            RegistryKey regkey = Registry.CurrentUser.OpenSubKey(@"Tts\Offline\", false);
            if (regkey == null)
            {
                throw new InvalidDataException(@"Registry key not found at HKEY_CURRENT_USER\Tts\Offline");
            }

            string testRoot = (string)regkey.GetValue("TestRoot");
            if (string.IsNullOrEmpty(testRoot))
            {
                throw new InvalidDataException(@"HKEY_CURRENT_USER\Tts\Server\Offline\@TestRoot not exist");
            }

            if (Directory.Exists(testRoot))
            {
                return testRoot;
            }
            else
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    testRoot);
            }
        }

        /// <summary>
        /// Finds the testing data path.
        /// <example>~\tts\tools\Offline\testdata.</example>
        /// </summary>
        /// <returns>Test data directory path.</returns>
        public static string FindTestDataPath()
        {
            RegistryKey regkey = Registry.CurrentUser.OpenSubKey(@"Tts\Offline\", false);
            if (regkey == null)
            {
                throw new InvalidDataException(@"Registry key not found at HKEY_CURRENT_USER\Tts\Offline");
            }

            string testDataPath = (string)regkey.GetValue("TestData");
            if (string.IsNullOrEmpty(testDataPath))
            {
                throw new InvalidDataException(@"HKEY_CURRENT_USER\Tts\Server\Offline\@TestData not exist");
            }

            if (Directory.Exists(testDataPath))
            {
                return testDataPath;
            }
            else
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    testDataPath);
            }
        }

        /// <summary>
        /// Finds the TTS target path in build environment by its relative location to offline test root path.
        /// </summary>
        /// <returns>TTS target path in build environment.</returns>
        public static string FindTtsTargetPath()
        {
            DirectoryInfo ttsOfflineTestDirectory = new DirectoryInfo(FindTestRootPath());

            return ttsOfflineTestDirectory.Parent.FullName;
        }

        /// <summary>
        /// Finds the locale handler path.
        /// </summary>
        /// <returns>Location of locale handler folder.</returns>
        public static string FindLocaleHandlerPath()
        {
            string localePath = Path.Combine(FindTestRootPath(), "LocaleHandler");

            if (string.IsNullOrEmpty(localePath))
            {
                // Tell whether there is localehandler directory under current assembly folder
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().FullName);
                if (Directory.Exists(Path.Combine(assemblyDir, "LocaleHandler")))
                {
                    localePath = Path.Combine(assemblyDir, "LocaleHandler");
                }
            }

            Debug.Assert(!string.IsNullOrEmpty(localePath) && Directory.Exists(localePath),
                "Locale handler path not be found.");

            return localePath;
        }

        /// <summary>
        /// Finds the voice font path.
        /// </summary>
        /// <param name="fontName">The name of the voice font to find.</param>
        /// <returns>Location of the locale handler folder.</returns>
        public static string FindVoiceFontPath(VoiceFontNames fontName)
        {
            switch (fontName)
            {
                case VoiceFontNames.ZhCN:
                    return Path.Combine(Helper.FindTtsTargetPath(), @"testdata\zh-CN\LilyT");
                case VoiceFontNames.ZhCNYaoyao:
                    return Path.Combine(Helper.FindTestDataPath(), @"zh-CN\Yaoyao\M2052SVR");
                case VoiceFontNames.EnUS:
                    return Path.Combine(Helper.FindTestDataPath(), @"en-US\helen8k\M1033SVR");
                case VoiceFontNames.EnUSRus:
                    return Path.Combine(Helper.FindTtsTargetPath(), @"testdata\RUSVoiceFont\1033");
                case VoiceFontNames.EnUSFixedPoint:
                    return Path.Combine(Helper.FindTestDataPath(), @"en-US\FixedpointEnUs\1033");
            }

            throw new InvalidDataException(NeutralFormat("Unknown font name - \"{0}\"", fontName));
        }

        #endregion

        #region File operations

        /// <summary>
        /// Check if it's valid UNC path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>Bool.</returns>
        public static bool IsUncDrive(string path)
        {
            if (path.StartsWith(@"\\"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Open directory and select file.
        /// </summary>
        /// <param name="filePath">File to be selected.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static void OpenDirAndSelectFile(string filePath)
        {
            Helper.ThrowIfFileNotExist(filePath);
            string dir = Path.GetDirectoryName(filePath);
            CommandLine.RunCommand(ExplorerExe,
                Helper.NeutralFormat(@"/select,""{0}""", filePath),
                dir);
        }

        /// <summary>
        /// Open file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static void OpenFile(string filePath)
        {
            Helper.ThrowIfFileNotExist(filePath);
            CommandLine.RunCommand(ExplorerExe, filePath, Environment.CurrentDirectory);
        }

        /// <summary>
        /// GetItemValueFromIniFile.
        /// </summary>
        /// <param name="iniFile">IniFile.</param>
        /// <param name="itemName">ItemName.</param>
        /// <param name="encoding">Encoding.</param>
        /// <returns>Item value from test case log.</returns>
        public static string GetItemValueFromIniFile(string iniFile, string itemName, Encoding encoding)
        {
            string itemValue = string.Empty;
            foreach (string line in Helper.FileLines(iniFile, encoding, true))
            {
                Match m = Regex.Match(line, itemName + "[ \t]*=[ \t]*(?<value>.+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    itemValue = m.Groups["value"].Value.Trim();
                }
            }

            return itemValue;
        }

        /// <summary>
        /// GetItemValueFromIniFile.
        /// </summary>
        /// <param name="iniFile">IniFile.</param>
        /// <param name="itemName">ItemName.</param>
        /// <param name="sectionName">SectionName.</param>
        /// <param name="encoding">Encoding.</param>
        /// <returns>Item value from test case log.</returns>
        public static string GetItemValueFromIniFile(string iniFile, string itemName, string sectionName, Encoding encoding)
        {
            string itemValue = string.Empty;
            bool section = false;

            foreach (string line in Helper.FileLines(iniFile, encoding, true))
            {
                if (!section)
                {
                    // Section such as [en-US Helen] start
                    if (string.Equals(line.Trim(), "[" + sectionName + "]", StringComparison.InvariantCultureIgnoreCase))
                    {
                        section = true;
                        continue;
                    }
                }

                if (section)
                {
                    Match m = Regex.Match(line, itemName + "[ \t]*=[ \t]*(?<value>.+)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        itemValue = m.Groups["value"].Value.Trim();
                        break;
                    }
                }

                // Next section start, end loop
                if (section && Regex.Match(line, Regex.Escape("^[")).Success)
                {
                    break;
                }
            }

            return itemValue;
        }

        /// <summary>
        /// Append new line text.
        /// </summary>
        /// <param name="filePath">File path to be appended.</param>
        /// <param name="format">Text format.</param>
        /// <param name="args">Text arguments.</param>
        public static void AppendTextNewLine(string filePath, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException("format");
            }

            AppendText(filePath, format, args);
            File.AppendAllText(filePath, Environment.NewLine, Encoding.Unicode);
        }

        /// <summary>
        /// Append line text.
        /// </summary>
        /// <param name="filePath">File path to be appended.</param>
        /// <param name="format">Text format.</param>
        /// <param name="args">Text arguments.</param>
        public static void AppendText(string filePath, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException("format");
            }

            File.AppendAllText(filePath, Helper.NeutralFormat(format, args), Encoding.Unicode);
        }

        /// <summary>
        /// Iterate a text file.
        /// </summary>
        /// <param name="handler">Line string handler.</param>
        /// <param name="filePath">Text file path.</param>
        public static void IterateTextFile(LineHandler handler, string filePath)
        {
            if (handler == null)
            {
                return;
            }

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                bool needBreak = false;
                while ((line = sr.ReadLine()) != null)
                {
                    handler(line, ref needBreak);
                    if (needBreak)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Loads all text sessions/blocks into lists, which is separated by given line.
        /// </summary>
        /// <param name="filePath">Location of text file to read lines.</param>
        /// <param name="separatorLine">The separator line to separate sessions.</param>
        /// <returns>Text sessions.</returns>
        public static List<List<string>> LoadTextSessions(string filePath, string separatorLine)
        {
            List<List<string>> sessions = new List<List<string>>();
            List<string> session = new List<string>();

            foreach (var line in AllFileLines(filePath))
            {
                if (string.CompareOrdinal(line.Trim(), separatorLine) == 0)
                {
                    if (!string.IsNullOrEmpty(separatorLine))
                    {
                        session.Add(line);
                    }

                    if (session.Count != 0)
                    {
                        sessions.Add(session);
                        session = new List<string>();
                    }
                }
                else
                {
                    session.Add(line);
                }
            }

            if (session.Count != 0)
            {
                sessions.Add(session);
            }

            return sessions;
        }

        /// <summary>
        /// Reads all text lines from given file.
        /// </summary>
        /// <param name="filePath">Location of text file to read lines.</param>
        /// <returns>Enumerator of the lines in the given file.</returns>
        public static IEnumerable<string> AllFileLines(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        /// <summary>
        /// Reads text lines from given file, while dropping empty lines in the text file.
        /// </summary>
        /// <param name="filePath">Location of text file to read lines.</param>
        /// <returns>Enumerator of the lines in the given file.</returns>
        public static IEnumerable<string> FileLines(string filePath)
        {
            return FileLines(filePath, true);
        }

        /// <summary>
        /// Read text lines from given file.
        /// </summary>
        /// <param name="filePath">Location of text file to read lines.</param>
        /// <param name="ignoreBlankLine">IgnoreBlankLine.</param>
        /// <returns>Enumerator of the lines in the given file.</returns>
        public static IEnumerable<string> FileLines(string filePath, bool ignoreBlankLine)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (ignoreBlankLine && string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    yield return line;
                }
            }
        }

        /// <summary>
        /// Read text lines from given file.
        /// </summary>
        /// <param name="filePath">Location of text file to read lines.</param>
        /// <param name="encoding">The encoding of the file.</param>
        /// <returns>Enumerator of the lines in the file.</returns>
        public static IEnumerable<string> FileLines(string filePath, Encoding encoding)
        {
            return FileLines(filePath, encoding, true);
        }

        /// <summary>
        /// Read text lines from given file.
        /// </summary>
        /// <param name="filePath">Location of text file to read lines.</param>
        /// <param name="encoding">The encoding of the file.</param>
        /// <param name="ignoreBlankLine">Whether ignore blank line.</param>
        /// <returns>Enumerator of the lines in the file.</returns>
        public static IEnumerable<string> FileLines(string filePath, Encoding encoding, bool ignoreBlankLine)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            using (StreamReader sr = new StreamReader(filePath, encoding))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (ignoreBlankLine && string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    yield return line;
                }
            }
        }

        /// <summary>
        /// Write text lines to given file.
        /// </summary>
        /// <param name="lines">Text lines.</param>
        /// <param name="filePath">Location of text file to write lines.</param>
        /// <param name="append">Append or not.</param>
        /// <param name="encoding">The encoding of the file.</param>
        public static void WriteLines(IEnumerable<string> lines, string filePath, bool append, Encoding encoding)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            EnsureFolderExistForFile(filePath);

            using (StreamWriter sw = new StreamWriter(filePath, append, encoding))
            {
                foreach (string line in lines)
                {
                    sw.WriteLine(line);
                }
            }
        }
        
        /// <summary>
        /// Get file lines from file.
        /// </summary>
        /// <param name="reader">The reader of text stream to load.</param>
        /// <returns>Enumerator of the lines in file.</returns>
        public static IEnumerable<string> Streamlines(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line.Trim()))
                {
                    continue;
                }

                yield return line;
            }
        }

        /// <summary>
        /// Ensure directory exist for certain file path, this is,
        /// If the directory does not exist, create it.
        /// </summary>
        /// <param name="filePath">File path to process.</param>
        public static void EnsureFolderExistForFile(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            EnsureFolderExist(dir);
        }

        /// <summary>
        /// Ensure directory exist for certain file path, this is,
        /// If the directory does not exist, create it.
        /// </summary>
        /// <param name="dirPath">Directory path to process.</param>
        public static void EnsureFolderExist(string dirPath)
        {
            if (!string.IsNullOrEmpty(dirPath) &&
                !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        /// <summary>
        /// Checks file exists.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        public static void ThrowIfFileNotExist(string filePath)
        {
            ThrowIfNull(filePath);
            if (!File.Exists(filePath))
            {
                throw CreateException(typeof(FileNotFoundException), filePath);
            }
        }

        /// <summary>
        /// Checks dir exists.
        /// </summary>
        /// <param name="dirPath">DirPath.</param>
        public static void ThrowIfDirectoryNotExist(string dirPath)
        {
            ThrowIfNull(dirPath);
            if (!Directory.Exists(dirPath))
            {
                throw CreateException(typeof(DirectoryNotFoundException), dirPath);
            }
        }

        /// <summary>
        /// Checks object argument not as null.
        /// </summary>
        /// <param name="instance">Object instance to check.</param>
        public static void ThrowIfNull(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
        }

        /// <summary>
        /// Checks two lists are not null and equal length.
        /// </summary>
        /// <typeparam name="T">List type.</typeparam>
        /// <param name="list1">List 1.</param>
        /// <param name="list2">List 2.</param>
        public static void ThrowIfNullOrUnequalLength<T>(IList<T> list1, IList<T> list2)
        {
            ThrowIfNull(list1);
            ThrowIfNull(list2);

            if (list1.Count != list2.Count)
            {
                throw new ArgumentException("Input lists must be equal length");
            }
        }

        /// <summary>
        /// Checks object argument not as null.
        /// </summary>
        /// <param name="str">String instance to check.</param>
        public static void ThrowIfNull(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException("str");
            }
        }

        /// <summary>
        /// Checks file path with extension.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="fileExtension">File extension.</param>
        public static void ThrowIfNotWithFileExtension(string filePath, string fileExtension)
        {
            Helper.ThrowIfNull(filePath);
            Helper.ThrowIfNull(fileExtension);
            if (!filePath.IsWithFileExtension(fileExtension))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "File [{0}] should with extension [{1}] but actual with [{2}]",
                    filePath, fileExtension, Path.GetExtension(filePath)));
            }
        }

        /// <summary>
        /// Check array object not null or empty.
        /// </summary>
        /// <param name="array">Array instance to check.</param>
        public static void ThrowIfArrayNullOrEmpty(Array array)
        {
            if (array == null || array.Length == 0)
            {
                throw new ArgumentNullException("The array should not be null or empty.");
            }
        }

        /// <summary>
        /// Verify whether a text exist and not with zero-length size.
        /// </summary>
        /// <param name="filePath">File to test.</param>
        /// <returns>Boolean, indicating OK or not.</returns>
        public static bool FileValidExists(string filePath)
        {
            bool result = false;
            if (File.Exists(filePath))
            {
                // It's safe in multi-thread environment to get FileInfo  
                FileInfo info = new FileInfo(filePath);
                if (info.Length > 0)
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Verify whether a file exists and contains valid texts.
        /// </summary>
        /// <param name="filePath">File to test.</param>
        /// <param name="acceptWhiteSpace">Whether accept file with only white space as valid.</param>
        /// <returns>Boolean, indicating OK or not.</returns>
        public static bool FileValidExists(string filePath, bool acceptWhiteSpace)
        {
            bool result = false;
            if (acceptWhiteSpace)
            {
                result = FileValidExists(filePath);
            }
            else
            {
                if (File.Exists(filePath))
                {
                    using (StreamReader sr = new StreamReader(filePath, true))
                    {
                        if (!string.IsNullOrWhiteSpace(sr.ReadToEnd()))
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check file exists in a folder.
        /// </summary>
        /// <param name="dir">Directory.</param>
        /// <param name="fileName">File name.</param>
        public static void CheckFileExistsInFolder(string dir, string fileName)
        {
            if (!IsValidPath(dir))
            {
                string message = Helper.NeutralFormat("the path [{0}] is invalid path", dir);
                throw new FileNotFoundException(message);
            }

            if (!Helper.FileValidExists(Path.Combine(dir, fileName)))
            {
                string message = Helper.NeutralFormat(" The file [{0}] could not be found in folder [{1}]", fileName, dir);
                throw new FileNotFoundException(message);
            }
        }

        /// <summary>
        /// Check file exists.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public static void CheckFileExists(string filePath)
        {
            CheckFileExistsInFolder(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
        }

        /// <summary>
        /// Check whether directory exists and has file in it.
        /// </summary>
        /// <param name="dirPath">Directory path to process.</param>
        public static void CheckFolderNotEmpty(string dirPath)
        {
            if (!string.IsNullOrEmpty(dirPath) &&
                !Directory.Exists(dirPath))
            {
                string message = Helper.NeutralFormat("Folder [{0}] does not exist.", dirPath);
                throw new FileNotFoundException(message);
            }
            else if (Directory.GetFiles(dirPath).Length == 0)
            {
                string message = Helper.NeutralFormat("Folder [{0}] does not contain any file.", dirPath);
                throw new FileNotFoundException(message);
            }
        }

        /// <summary>
        /// SetFileReadOnly.
        /// </summary>
        /// <param name="filePath">File to set.</param>
        /// <param name="readOnly">Readonly flag to set.</param>
        /// <returns>Whether succeed.</returns>
        public static bool SetFileReadOnly(string filePath, bool readOnly)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            bool succeeded = true;
            if (File.Exists(filePath))
            {
                try
                {
                    FileInfo fi = new FileInfo(filePath);
                    fi.IsReadOnly = readOnly;
                }
                catch (ArgumentException)
                {
                    succeeded = false;
                }
            }

            return succeeded;
        }

        /// <summary>
        /// Tell whether a file is in unicode.
        /// </summary>
        /// <param name="filePath">File to test.</param>
        /// <returns>True if Unicode file, otherwise false.</returns>
        public static bool IsUnicodeFile(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            try
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs = null;
                    if (br.BaseStream.Length < sizeof(ushort))
                    {
                        return false;
                    }

                    ushort mob = br.ReadUInt16();
                    if (mob == 0xFEFF)
                    {
                        return true;
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

            return false;
        }

        /// <summary>
        /// Tell whether a path string is valid.
        /// </summary>
        /// <param name="path">Path to test.</param>
        /// <returns>True if valid, otherwise false.</returns>
        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return false;
            }

            string fileName = Path.GetFileName(path);
            while (!string.IsNullOrEmpty(fileName))
            {
                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return false;
                }

                path = Path.GetDirectoryName(path);
                fileName = Path.GetFileName(path);
            }

            return true;
        }

        /// <summary>
        /// Format the string in language independent way.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="arg">Arguments to format.</param>
        /// <returns>Formated string.</returns>
        public static string NeutralFormat(string format, params object[] arg)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException("format");
            }

            return string.Format(CultureInfo.InvariantCulture, format, arg);
        }

        /// <summary>
        /// TestDirWritable.
        /// </summary>
        /// <param name="dirPath">DirPath.</param>
        public static void TestDirWritable(string dirPath)
        {
            Helper.TestWritable(Path.Combine(dirPath, Guid.NewGuid().ToString()));
        }

        /// <summary>
        /// Test whether the file can be written. if this file is not writable,
        /// Exception (of FileStream) will be thrown out.
        /// </summary>
        /// <param name="filePath">The file path to be checked.</param>
        public static void TestWritable(string filePath)
        {
            Helper.EnsureFolderExistForFile(filePath);
            bool exist = File.Exists(filePath);

            // create file to test write access permission
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.Write))
            {
            }

            // if file does not exist before testing, remove the temporary created file
            if (!exist)
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Test file writtable.
        /// </summary>
        /// <param name="filePath">File path to be tested.</param>
        /// <returns>Error message.</returns>
        public static string TestWritableWithoutException(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            const string TraceFormat = "[User message] : {0}\r\n[Stack trace] : \r\n{1}";

            string errorMessage = string.Empty;
            string stackTrace = string.Empty;
            try
            {
                Helper.TestWritable(filePath);
            }
            catch (ArgumentException e)
            {
                errorMessage = Helper.NeutralFormat("The encoding is not supported; " +
                    "the filename is empty, contains only white space, or contains " +
                    "one or more invalid characters for file path [{0}]", filePath);
                stackTrace = e.StackTrace;
            }
            catch (UnauthorizedAccessException e)
            {
                errorMessage = Helper.NeutralFormat("Access is denied for file path [{0}]", filePath);
                stackTrace = e.StackTrace;
            }
            catch (DirectoryNotFoundException e)
            {
                errorMessage = Helper.NeutralFormat("The directory to write to is not found for file [{0}]", filePath);
                stackTrace = e.StackTrace;
            }
            catch (IOException e)
            {
                errorMessage = Helper.NeutralFormat("The filename includes an incorrect or invalid syntax for " +
                    "file name, directory name, or volume label syntax for file [{0}]", filePath);
                stackTrace = e.StackTrace;
            }
            catch (SecurityException e)
            {
                errorMessage = Helper.NeutralFormat("The caller does not have the required permission for file [{0}]",
                    filePath);
                stackTrace = e.StackTrace;
            }

            Trace.WriteLine(Helper.NeutralFormat(TraceFormat, errorMessage, stackTrace));
            return errorMessage;
        }

        /// <summary>
        /// Used handle all exception in the main function.
        /// </summary>
        /// <param name="exception">Exception to be checked.</param>
        /// <returns>Whether the exception should be filtered.</returns>
        public static bool HandleAllException(Exception exception)
        {
            Helper.PrintColorMessage(ErrorSeverity.MustFix, BuildExceptionMessage(exception));
            Trace.WriteLine(exception.StackTrace);
            return true;
        }

        /// <summary>
        /// If the exception is in the filter exception list only print the
        /// Exception to the console and return true, else return false.
        /// In most case if return true, caller will return error code, else
        /// Throw the exception.
        /// </summary>
        /// <param name="exception">Exception object to be filtered.</param>
        /// <returns>Return true if the exception in the filterExceptions,
        /// else return false.</returns>
        public static bool FilterException(Exception exception)
        {
            // some exception such ArgumentNullException should not included
            // here ,because they should be ensured in code level.
            Type[] filterExceptions = new Type[]
            {
                typeof(GeneralException),
                typeof(ApplicationException),
                typeof(NotSupportedException),
                typeof(ArgumentException),
                typeof(FileNotFoundException),
                typeof(DirectoryNotFoundException),
                typeof(OverflowException),
                typeof(InvalidDataException),
                typeof(SecurityException),
                typeof(UnauthorizedAccessException),
                typeof(PathTooLongException),

                // IOException must put tail, because it is parent of
                // FileNotFoundException and DirectoryNotFoundException
                typeof(IOException)
            };

            return FilterException(exception, filterExceptions);
        }

        /// <summary>
        /// Gets the exit code of the exception.
        /// </summary>
        /// <param name="exception">Exception object to be filtered.</param>
        /// <returns>Exit code of the exception.</returns>
        public static int GetExceptionErrorCode(Exception exception)
        {
            int errorCode = ExitCode.GenericError;

            if (exception is ArgumentException)
            {
                errorCode = ExitCode.InvalidArgument;
            }

            return errorCode;
        }

        /// <summary>
        /// If the exception is in the filter exception list only print the
        /// Exception to the console and return true, else return false.
        /// In most case if return true, caller will return error code, else
        /// Throw the exception.
        /// </summary>
        /// <param name="exception">Exception object to be filtered.</param>
        /// <param name="filterExceptions">Exception types to be filetered.</param>
        /// <returns>Return true if the exception in the filterExceptions,
        /// else return false.</returns>
        public static bool FilterException(Exception exception, Type[] filterExceptions)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            Debug.WriteLine(exception);

            Type currType = exception.GetType();
            foreach (Type type in filterExceptions)
            {
                if (type.Equals(currType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Create new exception instance with given exception type and parameter.
        /// </summary>
        /// <param name="type">Exception type.</param>
        /// <param name="parameter">
        /// 1. if type is FileNotFoundException/DirectoryNotFoundException,
        /// Parameter should be file/directory name.
        /// </param>
        /// <returns>Exception created.</returns>
        public static Exception CreateException(Type type, string parameter)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            // check public parameter
            if (string.IsNullOrEmpty(parameter))
            {
                parameter = string.Empty;
            }

            if (type.Equals(typeof(FileNotFoundException)))
            {
                string message = Helper.NeutralFormat("Could not find file [{0}].", parameter);
                return new FileNotFoundException(message, parameter);
            }
            else if (type.Equals(typeof(DirectoryNotFoundException)))
            {
                string message = Helper.NeutralFormat("Could not find a part of the path [{0}].", parameter);
                return new DirectoryNotFoundException(message);
            }
            else
            {
                string message = Helper.NeutralFormat("Unsupported exception message with parameter [{0}]",
                    parameter);
                Debug.Assert(true, message);
                return new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Get all inner exceptions message string.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>Exception message.</returns>
        public static string BuildExceptionMessage(Exception exception)
        {
            StringBuilder sb = new StringBuilder();
            for (Exception current = exception; current != null;
                current = current.InnerException)
            {
                if (current.InnerException != null)
                {
                    sb.AppendLine(current.Message);
                }
                else
                {
                    sb.Append(current.Message);
                }

                ExceptionCollection coll = current as ExceptionCollection;
                if (coll != null && coll.Exceptions.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Exception collection:");
                    coll.Exceptions.ForEach(e => sb.AppendLine(BuildExceptionMessage(e)));
                    sb.AppendLine("End of exception collection:");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Compares two ICollection.
        /// </summary>
        /// <typeparam name="T">The template of ICollection.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="ignoreOrder">True to indicate the order in collection should be ignored.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool Compare<T>(IEnumerable<T> expected, IEnumerable<T> actual, bool ignoreOrder)
        {
            int count = expected.Count();
            if (count != actual.Count())
            {
                return false;
            }

            if (ignoreOrder)
            {
                return expected.Intersect(actual).Count() == count;
            }
            else
            {
                IEnumerator<T> expectedEnumerator = expected.GetEnumerator();
                IEnumerator<T> actualEnumerator = actual.GetEnumerator();
                while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
                {
                    if (!expectedEnumerator.Current.Equals(actualEnumerator.Current))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Compare two text files.
        /// </summary>
        /// <param name="leftFile">Left file.</param>
        /// <param name="rightFile">Right file.</param>
        /// <param name="ignoreBlank">Ignore blank comparing or not.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareTextFile(string leftFile, 
            string rightFile,
            bool ignoreBlank,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            IgnoreLineWhenCompare ignoreLineWhenCompare =
                delegate
                {
                    return false;
                };

            return CompareTextFile(leftFile, rightFile, ignoreBlank, ignoreLineWhenCompare, compareFloatWithImprecise, threshold);
        }

        /// <summary>
        /// Compare two text files.
        /// </summary>
        /// <param name="leftFile">Left file.</param>
        /// <param name="rightFile">Right file.</param>
        /// <param name="ignoreBlank">Ignore blank comparing or not.</param>
        /// <param name="ignoreLineWhenCompare">IgnoreLineWhenCompare.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareTextFile(string leftFile, 
            string rightFile,
            bool ignoreBlank, 
            IgnoreLineWhenCompare ignoreLineWhenCompare,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            return CompareTextFile(leftFile, rightFile, ignoreBlank, ignoreLineWhenCompare, 0, compareFloatWithImprecise, threshold);
        }

        /// <summary>
        /// Compare two text files.
        /// </summary>
        /// <param name="leftFile">Left file.</param>
        /// <param name="rightFile">Right file.</param>
        /// <param name="ignoreBlank">Ignore blank comparing or not.</param>
        /// <param name="columnIndex">Compare per columns less than the index.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareTextFile(string leftFile, 
            string rightFile,
            bool ignoreBlank, 
            int columnIndex,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            IgnoreLineWhenCompare ignoreLineWhenCompare =
                delegate
                {
                    return false;
                };

            return CompareTextFile(leftFile, rightFile, ignoreBlank, ignoreLineWhenCompare, columnIndex, compareFloatWithImprecise, threshold);
        }

        /// <summary>
        /// Compare two text files.
        /// </summary>
        /// <param name="leftFile">Left file.</param>
        /// <param name="rightFile">Right file.</param>
        /// <param name="ignoreBlank">Ignore blank comparing or not.</param>
        /// <param name="ignoreLineWhenCompare">IgnoreLineWhenCompare.</param>
        /// <param name="columnIndex">Compare per columns less than the index.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareTextFile(string leftFile, 
            string rightFile,
            bool ignoreBlank,
            IgnoreLineWhenCompare ignoreLineWhenCompare, 
            int columnIndex,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            bool bSame = true;
            using (StreamReader srLeft = new StreamReader(leftFile))
            using (StreamReader srRight = new StreamReader(rightFile))
            {
                string left = srLeft.ReadLine();
                string right = srRight.ReadLine();

                while (left != null && right != null)
                {
                    // If user supplied ignore line delegate, then check whether left or right line
                    // match the delegate, if one of the two matches ignore delegate, then skip
                    // comparing the line.
                    if (ignoreLineWhenCompare != null &&
                        (ignoreLineWhenCompare(left) && ignoreLineWhenCompare(right)))
                    {
                        // Ignore comparing the line
                        // Use this empty statement to make the logical more easily to understand.
                    }
                    else
                    {
                        if (columnIndex == 0 || columnIndex > left.Length || columnIndex > right.Length)
                        {
                            bSame = CompareString(left, right, ignoreBlank, compareFloatWithImprecise, threshold);
                        }
                        else
                        {
                            // Cut before given column index then compare.
                            bSame = CompareString(left.Substring(0, columnIndex - 1), right.Substring(0, columnIndex - 1), ignoreBlank, compareFloatWithImprecise, threshold);
                        }

                        if (!bSame)
                        {
                            break;
                        }
                    }

                    left = srLeft.ReadLine();
                    right = srRight.ReadLine();
                }

                if (bSame &&
                    (left != null || right != null))
                {
                    bSame = false;
                }
            }

            return bSame;
        }

        /// <summary>
        /// Compare text files in both directories.
        /// </summary>
        /// <param name="leftDir">Left direcroty.</param>
        /// <param name="rightDir">Right directory.</param>
        /// <param name="ignoreBlank">Ignore blank comparing or not.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareTextDir(string leftDir, 
            string rightDir,
            bool ignoreBlank,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            return CompareTextDir(leftDir, rightDir, ignoreBlank, null, compareFloatWithImprecise, threshold);
        }
        
        /// <summary>
        /// Compare text files in both directories.
        /// </summary>
        /// <param name="leftDir">Left direcroty.</param>
        /// <param name="rightDir">Right directory.</param>
        /// <param name="ignoreBlank">Ignore blank comparing or not.</param>
        /// <param name="ignoreLineWhenCompare">IgnoreLineWhenCompare.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareTextDir(string leftDir, 
            string rightDir,
            bool ignoreBlank, 
            IgnoreLineWhenCompare ignoreLineWhenCompare,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            return CompareTextDir(leftDir, 
                rightDir, 
                ignoreBlank, 
                ignoreLineWhenCompare,
                FileExtensions.Text.CreateSearchPatternWithFileExtension(),
                compareFloatWithImprecise,
                threshold);
        }

        /// <summary>
        /// Compare text files in both directories.
        /// </summary>
        /// <param name="leftDir">Left direcroty.</param>
        /// <param name="rightDir">Right directory.</param>
        /// <param name="ignoreBlank">Ignore blank comparing or not.</param>
        /// <param name="ignoreLineWhenCompare">IgnoreLineWhenCompare.</param>
        /// <param name="searchPattern">Text search pattern.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareTextDir(string leftDir, 
            string rightDir,
            bool ignoreBlank, 
            IgnoreLineWhenCompare ignoreLineWhenCompare, 
            string searchPattern,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            string[] leftFiles = Directory.GetFiles(leftDir, searchPattern);
            string[] rightFiles = Directory.GetFiles(rightDir, searchPattern);

            if (leftFiles.Length != rightFiles.Length)
            {
                return false;
            }

            for (int i = 0; i < leftFiles.Length; i++)
            {
                string leftFilePath = leftFiles[i];
                string rightFilePath = rightFiles[i];

                if (string.Compare(Path.GetFileName(leftFilePath), Path.GetFileName(rightFilePath), 
                    StringComparison.OrdinalIgnoreCase) != 0)
                {
                    return false;
                }

                if (!CompareTextFile(leftFilePath, rightFilePath, ignoreBlank, ignoreLineWhenCompare, compareFloatWithImprecise, threshold))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compare two excel files.
        /// </summary>
        /// <param name="leftFile">Left file.</param>
        /// <param name="rightFile">Right file.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if file contents are equal, otherwise false.</returns>
        public static bool CompareExcelFile(string leftFile, 
            string rightFile,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            Microsoft.Office.Interop.Excel.Application excel = null;
            Microsoft.Office.Interop.Excel.Workbook left = null;
            Microsoft.Office.Interop.Excel.Workbook right = null;
            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();
                left = excel.Workbooks.Open(leftFile);
                right = excel.Workbooks.Open(rightFile);

                if (left.Worksheets.Count != right.Worksheets.Count)
                {
                    return false;
                }

                for (int i = 1; i <= left.Worksheets.Count; i++)
                {
                    dynamic leftSheet = left.Worksheets[i];
                    dynamic rightSheet = right.Worksheets[i];

                    object[,] leftValues = leftSheet.UsedRange.Value2;
                    object[,] rightValues = rightSheet.UsedRange.Value2;

                    if (leftValues.Length != rightValues.Length)
                    {
                        return false;
                    }

                    if (leftValues.GetLength(0) != rightValues.GetLength(0)
                        || leftValues.GetLength(1) != rightValues.GetLength(1))
                    {
                        return false;
                    }

                    for (int x = 1; x <= leftValues.GetLength(0); x++)
                    {
                        for (int y = 1; y <= leftValues.GetLength(1); y++)
                        {
                            if (compareFloatWithImprecise) 
                            {
                                return CompareFloatWithImpreciseBetweenTwoString(
                                    ((Microsoft.Office.Interop.Excel.Range)((Microsoft.Office.Interop.Excel.Worksheet)left.Worksheets[i]).Cells[x, y]).Value.ToString().Trim(), 
                                    ((Microsoft.Office.Interop.Excel.Range)((Microsoft.Office.Interop.Excel.Worksheet)right.Worksheets[i]).Cells[x, y]).Value.ToString().Trim(), 
                                    threshold);
                            }
                            else
                            {
                                if (!object.Equals(leftValues[x, y], rightValues[x, y]))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }
            finally
            {
                if (left != null)
                {
                    left.Close();
                }

                if (right != null)
                {
                    right.Close();
                }

                if (excel != null)
                {
                    excel.Quit();
                }
            }
        }

        /// <summary>
        /// Compare two strings.
        /// </summary>
        /// <param name="left">Left string.</param>
        /// <param name="right">Right string.</param>
        /// <param name="ignoreBlank">Ignore blank comparing or not.</param>
        /// <param name="compareFloatWithImprecise">
        /// If compare float number with precision comparison, When string is including some float number.
        /// </param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareString(string left,
            string right,
            bool ignoreBlank,
            bool compareFloatWithImprecise = false,
            double threshold = ThresholdConst)
        {
            if (ignoreBlank)
            {
                left = Regex.Replace(left, @"\s+", string.Empty);
                right = Regex.Replace(right, @"\s+", string.Empty);
            }

            if (!compareFloatWithImprecise)
            {
                return string.Compare(left, right, System.StringComparison.Ordinal) == 0;
            }
            else
            {
                return CompareFloatWithImpreciseBetweenTwoString(left, right, threshold);
            }
        }

        /// <summary>
        /// Compare two strings, 
        /// meantime need compare float number with not precise comparison when it includes float number string.
        /// </summary>
        /// <param name="left">Left string.</param>
        /// <param name="right">Right string.</param>
        /// <param name="threshold">A threshold to do a pass rule.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareFloatWithImpreciseBetweenTwoString(string left, string right, double threshold = ThresholdConst)
        {
            bool result = true;

            // Initialization
            if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
            {
                return true;
            }
            else if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return false;
            }

            // Define a float number template
            Regex r = new Regex(@"-?\d+\.\d+");

            MatchCollection leftFloatCollection = r.Matches(left);
            MatchCollection rightFloatCollection = r.Matches(right);

            // If the line string doesn't includes float number, we only need to do string comparison
            if ((leftFloatCollection.Count == rightFloatCollection.Count) && leftFloatCollection.Count == 0)
            {
                return string.Compare(left, right, System.StringComparison.Ordinal) == 0;
            }
            else if (leftFloatCollection.Count != rightFloatCollection.Count)
            {
                // Certainly return false if their float number cout is different
                return false;
            }
            else
            {
                // Replace all double number, because we need compare all of characters, but not only double number
                string tempLeft = Regex.Replace(left, @"-?\d+\.\d+", string.Empty, RegexOptions.IgnoreCase);
                string tempRight = Regex.Replace(right, @"-?\d+\.\d+", string.Empty, RegexOptions.IgnoreCase);

                if (string.Compare(tempLeft, tempRight, System.StringComparison.Ordinal) != 0)
                {
                    // Certainly return false if non float string are different
                    return false;
                }

                // Begin to compare float string with no precision
                for (int i = 0; i < leftFloatCollection.Count; i++)
                {
                    if (string.Compare(leftFloatCollection[i].ToString(),
                        rightFloatCollection[i].ToString(),
                        System.StringComparison.Ordinal) == 0)
                    {
                        continue;
                    }
                    else
                    {
                        // Convert string into float
                        double leftTemp = 1e-16;
                        double rightTemp = 1e-16;

                        try
                        {
                            // Parse string type threshold into float type
                            if (!double.TryParse(leftFloatCollection[i].ToString(), out leftTemp))
                            {
                                throw new Exception("Left string is " + leftFloatCollection[i].ToString());
                            }

                            if (!double.TryParse(rightFloatCollection[i].ToString(), out rightTemp))
                            {
                                throw new Exception("Right string is " + rightFloatCollection[i].ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            result = false;
                            throw new Exception("Convert left and right string into double failure ..." + ex.Message);
                        }

                        // Need to verify plus and minus double number
                        if (Math.Abs(leftTemp) <= PlusDoubleZero && Math.Abs(rightTemp) <= PlusDoubleZero)
                        {
                            continue;
                        }
                        else if (Math.Abs(leftTemp) <= PlusDoubleZero || Math.Abs(rightTemp) <= PlusDoubleZero)
                        {
                            return false;
                        }
                        else
                        {
                            // Same Positive Number and Negative Number
                            if (((leftTemp > PlusDoubleZero) && (rightTemp > PlusDoubleZero)) || ((leftTemp < MinusDoubleZero) && (rightTemp < MinusDoubleZero)))
                            {
                                double delta = Math.Abs(leftTemp - rightTemp) / Math.Abs(leftTemp);
                                if (delta < threshold)
                                {
                                    continue;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                // Of course, this is fail if they are different Positive Number and Negative Number
                                return false;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Append binaries files into a target file.
        /// </summary>
        /// <param name="targetFile">Terget file to append to.</param>
        /// <param name="files">File path list to append.</param>
        public static void AppendBinary(string targetFile, params string[] files)
        {
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            FileStream fs = new FileStream(targetFile, FileMode.Append);
            try
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    foreach (string file in files)
                    {
                        byte[] data = File.ReadAllBytes(file);
                        bw.Write(data);
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

        /// <summary>
        /// Copy directory.
        /// </summary>
        /// <param name="source">Source directory to copy from.</param>
        /// <param name="target">Target directory to copy to.</param>
        /// <param name="overwrite">Falg to overwrite existint target file or not.</param>
        public static void CopyDirectory(string source, string target,
            bool overwrite)
        {
            Helper.EnsureFolderExist(target);
            foreach (string file in Directory.GetFiles(source))
            {
                string targetFile = Path.Combine(target, Path.GetFileName(file));
                if (overwrite && File.Exists(targetFile))
                {
                    SetFileReadOnly(targetFile, false);
                }

                File.Copy(file, targetFile, overwrite);
            }

            foreach (string subDir in Directory.GetDirectories(source))
            {
                string targetSubDir = Path.Combine(target, Path.GetFileName(subDir));
                CopyDirectory(subDir, targetSubDir, overwrite);
            }
        }

        /// <summary>
        /// Delete dir even if the files in the dir has readonly attribute.
        /// </summary>
        /// <param name="dir">Directory to be deleted.</param>
        public static void ForcedDeleteDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                foreach (string filePath in Directory.GetFiles(dir))
                {
                    SetFileReadOnly(filePath, false);
                    File.Delete(filePath);
                }

                foreach (string subDir in Directory.GetDirectories(dir))
                {
                    ForcedDeleteDir(subDir);
                }

                Directory.Delete(dir, true);
            }
        }

        /// <summary>
        /// Delete one file.
        /// </summary>
        /// <param name="filePath">File need to be deleted.</param>
        public static void ForcedDeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                SetFileReadOnly(filePath, false);
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Force copy file.
        /// </summary>
        /// <param name="sourceFilePath">Source file path.</param>
        /// <param name="targetFilePath">Target file path.</param>
        public static void ForceCopyFile(string sourceFilePath, string targetFilePath)
        {
            Helper.ThrowIfFileNotExist(sourceFilePath);
            if (File.Exists(targetFilePath))
            {
                Helper.SetFileReadOnly(targetFilePath, false);
                File.Delete(targetFilePath);
            }

            Helper.EnsureFolderExistForFile(targetFilePath);
            File.Copy(sourceFilePath, targetFilePath);
        }

        /// <summary>
        /// Copy file if not exist.
        /// </summary>
        /// <param name="sourceFilePath">Source file path.</param>
        /// <param name="targetFilePath">Target file path.</param>
        public static void CopyFileIfTargetNotExist(string sourceFilePath, string targetFilePath)
        {
            Helper.ThrowIfNull(sourceFilePath);
            Helper.ThrowIfNull(targetFilePath);

            if (!File.Exists(targetFilePath))
            {
                Helper.ThrowIfFileNotExist(sourceFilePath);
                Helper.EnsureFolderExistForFile(targetFilePath);
                File.Copy(sourceFilePath, targetFilePath);
            }
        }

        /// <summary>
        /// Copy file if not exist.
        /// </summary>
        /// <param name="sourceFilePath">Source file path.</param>
        /// <param name="targetFilePath">Target file path.</param>
        /// <param name="overwrite">Whether it is overwrite.</param>
        /// <returns>Whether the file is copied.</returns>
        public static bool CopyFileIfSourceExist(string sourceFilePath, string targetFilePath, bool overwrite)
        {
            Helper.ThrowIfNull(sourceFilePath);
            Helper.ThrowIfNull(targetFilePath);

            bool copied = false;
            if (File.Exists(sourceFilePath))
            {
                Helper.EnsureFolderExistForFile(targetFilePath);
                if (overwrite)
                {
                    Helper.ForceCopyFile(sourceFilePath, targetFilePath);
                    copied = true;
                }
                else
                {
                    if (!File.Exists(targetFilePath))
                    {
                        File.Copy(sourceFilePath, targetFilePath);
                        copied = true;
                    }
                }
            }

            return copied;
        }

        /// <summary>
        /// Safe delete a file or directory.
        /// </summary>
        /// <param name="path">Path of file or directory.</param>
        /// <returns>Error string if exception found.</returns>
        public static string SafeDelete(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (IOException ioe)
            {
                string message = Helper.NeutralFormat("Encounter IO exception while deleting [{0}], for {1} {2} {3}",
                    path, ioe.Message, Environment.NewLine, ioe.StackTrace);
                Trace.WriteLine(message);
                return message;
            }
            catch (UnauthorizedAccessException uae)
            {
                string message = Helper.NeutralFormat("Encounter unauthorized access exception while deleting [{0}], for {1} {2} {3}",
                    path, uae.Message, Environment.NewLine, uae.StackTrace);
                Trace.WriteLine(message);
                return message;
            }

            return null;
        }

        /// <summary>
        /// Check file alignment for each sentence id.
        /// </summary>
        /// <param name="leftDir">Left file directory.</param>
        /// <param name="leftExtension">Left file extension.</param>
        /// <param name="rightDir">Right file directory.</param>
        /// <param name="rightExtension">Right file extension.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet CheckFilesAlign(string leftDir, string leftExtension,
            string rightDir, string rightExtension)
        {
            // Validation
            if (string.IsNullOrEmpty(leftDir))
            {
                throw new ArgumentNullException("leftDir");
            }

            if (string.IsNullOrEmpty(leftExtension))
            {
                throw new ArgumentNullException("leftExtension");
            }

            if (string.IsNullOrEmpty(rightDir))
            {
                throw new ArgumentNullException("rightDir");
            }

            if (string.IsNullOrEmpty(rightExtension))
            {
                throw new ArgumentNullException("rightExtension");
            }

            DataErrorSet errorSet = new DataErrorSet();
            Dictionary<string, string> leftFiles = FileListMap.Build(leftDir, leftExtension);
            Dictionary<string, string> rightFiles = FileListMap.Build(rightDir, rightExtension);

            foreach (string sid in leftFiles.Keys)
            {
                if (!rightFiles.ContainsKey(sid))
                {
                    string leftFile = Path.Combine(leftDir, leftFiles[sid] + leftExtension);
                    string rightFile = Path.Combine(rightDir, leftFiles[sid] + rightExtension);

                    errorSet.Errors.Add(new DataError(rightFile,
                        " With corresponding file " + leftFile + " exists, but file " + rightFile + " does not exist", sid));
                }
            }

            foreach (string sid in rightFiles.Keys)
            {
                if (!leftFiles.ContainsKey(sid))
                {
                    string leftFile = Path.Combine(leftDir, rightFiles[sid] + leftExtension);
                    string rightFile = Path.Combine(rightDir, rightFiles[sid] + rightExtension);

                    errorSet.Errors.Add(new DataError(leftFile,
                        " With corresponding file " + rightFile + " exists, but file " + leftFile + " does not exist", sid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Find the regedit.exe tool path.
        /// </summary>
        /// <returns>Regedit.exe file path.</returns>
        public static string FindRegEditTool()
        {
            return FindSystemTool("regedit.exe");
        }

        /// <summary>
        /// Find the regsvr32.exe tool path.
        /// </summary>
        /// <returns>RegSvr32.exe file path.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Regsvr", 
            Justification = "regsvr32 is a tool")]
        public static string FindRegsvr32Tool()
        {
            return FindSystemTool("regsvr32.exe");
        }

        /// <summary>
        /// Tell the processor architecture of current machine through 
        /// Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").
        /// </summary>
        /// <returns>True for 64bit machine, else false for 32bit machine.</returns>
        public static bool Is64BitMachine()
        {
            string procArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            if (string.IsNullOrEmpty(procArch))
            {
                string message = Helper.NeutralFormat("Invalid environment without PROCESSOR_ARCHITECTURE parameter");
                throw new ArgumentException(message);
            }

            if (procArch.IndexOf("64", StringComparison.OrdinalIgnoreCase) > 0)
            {
                // 64bit machine
                return true;
            }
            else
            {
                Debug.Assert(string.CompareOrdinal(procArch, "x86") == 0);
                return false;
            }
        }

        #endregion

        #region General operations

        /// <summary>
        /// Is valid enum value.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="enumValue">Enum value.</param>
        /// <returns>Whether the value is a valid enum value.</returns>
        public static bool IsValidateEnum<T>(T enumValue)
        {
            if (!typeof(T).BaseType.Equals(typeof(Enum)))
            {
                throw new InvalidDataException("Please make sure set T as enum!");
            }

            bool isValid = false;
            foreach (var item in Enum.GetValues(typeof(T)))
            {
                if (enumValue.ToString().Equals(item.ToString()))
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Find index of item in item collection.
        /// </summary>
        /// <param name="item">Item to find.</param>
        /// <param name="items">Item collection.</param>
        /// <returns>Index.</returns>
        public static int IndexOf(string item, string[] items)
        {
            if (string.IsNullOrEmpty(item))
            {
                throw new ArgumentNullException("item");
            }

            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == item)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// RandomizeCollectionElements.
        /// </summary>
        /// <param name="random">Random.</param>
        /// <param name="data">The data to shuffle.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static void RandomizeCollectionElements<T>(Random random, Collection<T> data)
        {
            Debug.Assert(data != null);

            for (int i = 0; i < data.Count; ++i)
            {
                int randomPosition = random.Next(0, data.Count - i);
                data.Add(data[randomPosition]);
                data.RemoveAt(randomPosition);
            }
        }

        /// <summary>
        /// Append from source collection to target one.
        /// </summary>
        /// <param name="targetCollection">Target collection.</param>
        /// <param name="sourceCollection">Source sollection.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static void AppendCollection<T>(Collection<T> targetCollection, IEnumerable<T> sourceCollection)
        {
            Debug.Assert(targetCollection != null);
            Debug.Assert(sourceCollection != null);
            foreach (T item in sourceCollection)
            {
                targetCollection.Add(item);
            }
        }

        /// <summary>
        /// Convert a char array to char collection.
        /// </summary>
        /// <param name="set">Char array.</param>
        /// <returns>Char collection.</returns>
        public static Collection<char> Convert2Collection(char[] set)
        {
            Collection<char> coll = new Collection<char>();
            if (set == null || set.Length == 0)
            {
                return coll;
            }

            foreach (char c in set)
            {
                coll.Add(c);
            }

            return coll;
        }

        /// <summary>
        /// Convert a string array to collection.
        /// </summary>
        /// <param name="set">String array.</param>
        /// <returns>String collection.</returns>
        public static Collection<string> Convert2Collection(string[] set)
        {
            Collection<string> coll = new Collection<string>();
            if (set == null || set.Length == 0)
            {
                return coll;
            }

            foreach (string s in set)
            {
                coll.Add(s);
            }

            return coll;
        }

        /// <summary>
        /// Split Collection.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="sourceCollection">SourceCollection.</param>
        /// <param name="splitSize">SplitSize.</param>
        /// <param name="splittedCollections">SplittedCollections.</param>
        public static void SplitCollection<T>(Collection<T> sourceCollection,
            int splitSize, Collection<Collection<T>> splittedCollections)
        {
            Collection<T> currGroup = new Collection<T>();
            foreach (T t in sourceCollection)
            {
                if (splittedCollections.Count == 0 ||
                    splittedCollections[splittedCollections.Count - 1].Count == splitSize)
                {
                    splittedCollections.Add(new Collection<T>());
                }

                splittedCollections[splittedCollections.Count - 1].Add(t);
            }
        }

        /// <summary>
        /// Fill all strings in data into collection object.
        /// </summary>
        /// <param name="data">Data string array.</param>
        /// <param name="container">Mapping dictionary.</param>
        public static void FillData(string[] data, Collection<string> container)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            container.Clear();
            foreach (string item in data)
            {
                container.Add(item);
            }
        }

        /// <summary>
        /// File entry pair in data array into map, one is id,
        /// And the other is integer value.
        /// <example>
        ///     "a", "1",
        ///     "b", "2",.
        /// </example>
        /// </summary>
        /// <param name="data">Data string array.</param>
        /// <param name="container">Mapping dictionary.</param>
        public static void FillData(string[] data, Dictionary<string, int> container)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            Debug.Assert(data.Length % 2 == 0);
            container.Clear();
            for (int i = 0; i < data.Length / 2; i++)
            {
                container.Add(data[i * 2], int.Parse(data[(i * 2) + 1], CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// File entry pair in data array into map, one is id,
        /// And the other is string value.
        /// <example>
        ///     "a", "aa",
        ///     "b", "bb",.
        /// </example>
        /// </summary>
        /// <param name="data">Data string array.</param>
        /// <param name="container">Mapping dictionary.</param>
        public static void FillData(string[] data, Dictionary<string, string> container)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            Debug.Assert(data.Length % 2 == 0);
            container.Clear();
            for (int i = 0; i < data.Length / 2; i++)
            {
                container.Add(data[i * 2], data[(i * 2) + 1]);
            }
        }

        /// <summary>
        /// File map left to right into the map object
        /// Like one-by-one mapping for tts phone set and sr phone set.
        /// </summary>
        /// <param name="left">Left string array.</param>
        /// <param name="right">Right string array.</param>
        /// <param name="container">Mapping result collection.</param>
        public static void FillMap(string[] left, string[] right,
            Collection<string> container)
        {
            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (right == null)
            {
                throw new ArgumentNullException("right");
            }

            Debug.Assert(left.Length == right.Length);
            if (left.Length != right.Length)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Both lengths of string array should be the same.");
                throw new ArgumentException(message);
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            container.Clear();
            for (int i = 0; i < left.Length; i++)
            {
                container.Add(left[i]);
                container.Add(right[i]);
            }
        }

        /// <summary>
        /// Make safe regular express pattern.
        /// </summary>
        /// <param name="pattern">Raw pattern.</param>
        /// <returns>Safe pattern.</returns>
        public static string MakePattern(string pattern)
        {
            return Regex.Replace(pattern, @"\?", @"\?");
        }

        /// <summary>
        /// Remove empty/only whitespace string items
        /// From the source string array.
        /// </summary>
        /// <param name="items">String array.</param>
        /// <returns>String array with empty item removed.</returns>
        public static string[] RemoveEmptyItems(string[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            List<string> retItems = new List<string>();
            foreach (string item in items)
            {
                if (string.IsNullOrEmpty(item)
                    || string.IsNullOrEmpty(item.Trim()))
                {
                    continue;
                }

                retItems.Add(item);
            }

            return retItems.ToArray();
        }

        /// <summary>
        /// Validate whether given value is defined or not, if not defined,
        /// An ArgumentException will be thrown.
        /// </summary>
        /// <param name="type">Given enumeration type.</param>
        /// <param name="value">Given enumeration value.</param>
        public static void ValidateEnumValue(Type type, int value)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (!Enum.IsDefined(type, value))
            {
                string message = Helper.NeutralFormat(
                    "[{0}] is invalid value in enum type [{1}]",
                    value, type.ToString());
                throw new ArgumentException(message);
            }
        }

        /// <summary>
        /// Convert the byte arrayy into object.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="rawValue">RawValue.</param>
        /// <returns>T.</returns>
        public static T FromBytes<T>(byte[] rawValue)
        {
            GCHandle handle = GCHandle.Alloc(rawValue, GCHandleType.Pinned);
            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return structure;
        }

        /// <summary>
        /// Convert the object into byte array.
        /// </summary>
        /// <param name="source">Object.</param>
        /// <returns>Byte array.</returns>
        public static byte[] ToBytes(object source)
        {
            byte[] buff = new byte[Marshal.SizeOf(source)];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(buff.Length);
                Marshal.StructureToPtr(source, ptr, false);
                Marshal.Copy(ptr, buff, 0, buff.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return buff;
        }

        #endregion

        #region string operations

        /// <summary>
        /// Check if the line is comment.
        /// </summary>
        /// <param name="line">Line to be checked.</param>
        /// <returns>Whether the line is comment.</returns>
        public static bool IsCommentLine(this string line)
        {
            bool isComment = false;
            if (!string.IsNullOrEmpty(line) &&
                line.Trim().StartsWith(Delimitor.CommentChar.ToString(),
                StringComparison.Ordinal))
            {
                isComment = true;
            }

            return isComment;
        }

        /// <summary>
        /// Remove duplicate blank space.
        /// </summary>
        /// <param name="line">Line to be processed.</param>
        /// <returns>Processed line.</returns>
        public static string RemoveDuplicateBlank(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                line = Regex.Replace(line, " [ ]+", " ");
            }

            return line;
        }

        /// <summary>
        /// Check the word whether is in Pascal format, which means first letter is in upper case,
        /// And the others are in lower case.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>Ture is the word is in Pascal format.</returns>
        public static bool IsPascal(string word)
        {
            bool isPascal = char.IsUpper(word[0]);
            if (isPascal && word.Length > 1)
            {
                for (int i = 1; i < word.Length; i++)
                {
                    isPascal = !char.IsUpper(word[i]);
                    if (!isPascal)
                    {
                        break;
                    }
                }
            }

            return isPascal;
        }

        /// <summary>
        /// Convert the word into Pascal format, which means first letter is in upper case,
        /// And the others are in lower case.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>Word in Pascal format.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public static string ToPascal(string word)
        {
            string pascalWord = word.ToLower(CultureInfo.InvariantCulture);
            char firstCharInUpper = char.ToUpper(pascalWord[0], CultureInfo.InvariantCulture);
            pascalWord = pascalWord.Remove(0, 1);
            pascalWord = pascalWord.Insert(0, firstCharInUpper.ToString());
            return pascalWord;
        }

        /// <summary>
        /// Check whether all the letters of the word are in upper.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>True for all letters are in upper.</returns>
        public static bool IsUpper(string word)
        {
            bool isUpper = true;
            if (string.IsNullOrEmpty(word))
            {
                isUpper = false;
            }
            else
            {
                foreach (char letter in word)
                {
                    if (char.IsLower(letter))
                    {
                        isUpper = false;
                        break;
                    }
                }
            }

            return isUpper;
        }

        /// <summary>
        /// Check whether all the letters of the word are in lower.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>True for all letters are in lower.</returns>
        public static bool IsLower(string word)
        {
            bool isLower = true;
            if (string.IsNullOrEmpty(word))
            {
                isLower = false;
            }
            else
            {
                foreach (char letter in word)
                {
                    if (char.IsUpper(letter))
                    {
                        isLower = false;
                        break;
                    }
                }
            }

            return isLower;
        }

        /// <summary>
        /// Check whether all the letters of the word are letters.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>True for all letters are letters.</returns>
        public static bool IsLetter(string word)
        {
            bool isLetter = true;
            if (string.IsNullOrEmpty(word))
            {
                isLetter = false;
            }
            else
            {
                foreach (char letter in word)
                {
                    if (!char.IsLetter(letter))
                    {
                        isLetter = false;
                        break;
                    }
                }
            }

            return isLetter;
        }

        /// <summary>
        /// Check whether all the letters of the word are english letters.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>True for all letters are english letters.</returns>
        public static bool IsEnglishWord(string word)
        {
            foreach (char c in word)
            {
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check whether all the letters of the word are numbers.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>True for all letters are numbers.</returns>
        public static bool IsNumber(string word)
        {
            bool isNumber = true;
            if (string.IsNullOrEmpty(word))
            {
                isNumber = false;
            }
            else
            {
                foreach (char letter in word)
                {
                    if (!char.IsDigit(letter))
                    {
                        isNumber = false;
                        break;
                    }
                }
            }

            return isNumber;
        }

        /// <summary>
        /// Check whether all the letters contains at least one letter.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>True all the letters contains at least one letter.</returns>
        public static bool ContainsLetter(string word)
        {
            bool containsLetter = false;
            foreach (char letter in word)
            {
                if (!char.IsControl(letter) &&
                    !char.IsNumber(letter) &&
                    !char.IsPunctuation(letter) &&
                    !char.IsSeparator(letter) &&
                    !char.IsSymbol(letter) &&
                    !char.IsWhiteSpace(letter))
                {
                    containsLetter = true;
                    break;
                }
            }
            
            return containsLetter;
        }

        /// <summary>
        /// Check whether all the letters contains at least one number.
        /// </summary>
        /// <param name="word">Word.</param>
        /// <returns>True all the letters contains at least one number.</returns>
        public static bool ContainsNumber(string word)
        {
            bool containsNumber = false;
            foreach (char letter in word)
            {
                if (char.IsDigit(letter))
                {
                    containsNumber = true;
                    break;
                }
            }

            return containsNumber;
        }

        /// <summary>
        /// Split string with given splitters.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="splitters">Splitters.</param>
        /// <returns>Splitted strings.</returns>
        public static string[] Split(string text, char[] splitters)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            return text.Split(splitters, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Split string with white space.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <returns>Splitted strings.</returns>
        public static string[] Split(string text)
        {
            return Split(text, new char[] { ' ' });
        }

        /// <summary>
        /// This method used to avoid exception when parsing invalid string.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="t">IgnoreCase.</param>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>Error.</returns>
        public static Error TryParse<T>(string value, ref T t)
        {
            bool succeeded = false;
            foreach (object obj in Enum.GetValues(typeof(T)))
            {
                if (string.Compare(obj.ToString(), value, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    t = (T)Enum.Parse(typeof(T), value, true);
                    succeeded = true;
                }
            }

            return succeeded ? null : new Error(CommonError.FailedParseEnum,
                typeof(T).FullName, value);
        }

        /// <summary>
        /// Coverts unit file tag to string value.
        /// </summary>
        /// <param name="tag">The file tag of unit format.</param>
        /// <returns>The string value of file tag.</returns>
        [CLSCompliantAttribute(false)]
        public static string UintToString(uint tag)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte slice in BitConverter.GetBytes(tag))
            {
                sb.Append((char)slice);
            }

            return sb.ToString();
        }

        #endregion

        #region Public static comparing operations

        /// <summary>
        /// Compare two dictionaries by key/value.
        /// </summary>
        /// <typeparam name="TKey">Key.</typeparam>
        /// <typeparam name="TValue">TValue.</typeparam>
        /// <param name="left">Left key/value pair.</param>
        /// <param name="right">Right key/value pair.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareDictionary<TKey, TValue>(IDictionary<TKey, TValue> left, IDictionary<TKey, TValue> right)
        {
            return left.CompareDictionary(right, null);
        }

        /// <summary>
        /// Compare two dictionaries by key/value and value comparer.
        /// </summary>
        /// <typeparam name="TKey">Key.</typeparam>
        /// <typeparam name="TValue">TValue.</typeparam>
        /// <param name="left">Left key/value pair.</param>
        /// <param name="right">Right key/value pair.</param>
        /// <param name="valueComparer">Value comparer.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareDictionary<TKey, TValue>(
            this IDictionary<TKey, TValue> left, IDictionary<TKey, TValue> right,
            IEqualityComparer<TValue> valueComparer)
        {
            if (left == right)
            {
                return true;
            }

            if ((left == null) || (right == null))
            {
                return false;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            if (valueComparer == null)
            {
                valueComparer = EqualityComparer<TValue>.Default;
            }

            foreach (var keyValuePair in left)
            {
                TValue rightValue;
                if (!right.TryGetValue(keyValuePair.Key, out rightValue))
                {
                    return false;
                }

                if (!valueComparer.Equals(keyValuePair.Value, rightValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compare two file as binary mode.
        /// </summary>
        /// <param name="leftFile">Path of left file to compare.</param>
        /// <param name="rightFile">Path of right file to compare.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareBinary(string leftFile, string rightFile)
        {
            using (FileStream leftStream =
                new FileStream(leftFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream rightStream =
                new FileStream(rightFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (leftStream.Length != rightStream.Length)
                {
                    return false;
                }

                int bufferLen = 4 * 1024; // 4k
                byte[] bufLeft = new byte[bufferLen];
                byte[] bufRight = new byte[bufferLen];

                int lenLeft, lenRight;
                for (int offset = 0; offset < leftStream.Length; offset += lenLeft)
                {
                    lenLeft = leftStream.Read(bufLeft, 0, bufferLen);
                    lenRight = rightStream.Read(bufRight, 0, bufferLen);

                    if (lenLeft != lenRight)
                    {
                        return false;
                    }

                    for (int i = 0; i < lenLeft; ++i)
                    {
                        if (bufLeft[i] != bufRight[i])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Delete all empty folders in the directory.
        /// </summary>
        /// <param name="dir">Directory to be operated.</param>
        public static void DeleteEmptyFolders(string dir)
        {
            foreach (string subDir in Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly))
            {
                if (Directory.GetFiles(subDir, "*", SearchOption.AllDirectories).Length == 0)
                {
                    Directory.Delete(subDir, true);
                }
                else
                {
                    DeleteEmptyFolders(subDir);
                }
            }
        }

        /// <summary>
        /// Compare two directory.
        /// </summary>
        /// <param name="leftDir">Left directory.</param>
        /// <param name="rightDir">Right directory.</param>
        /// <param name="recursive">True: compare as recursive mode; false: don't compare sub-folders.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareDirectory(string leftDir, string rightDir, bool recursive)
        {
            return CompareDirectory(leftDir, rightDir, recursive, new Func<string, string, bool>((left, right) => { return true; }));
        }

        /// <summary>
        /// Compare two directory.
        /// </summary>
        /// <param name="leftDir">Left directory.</param>
        /// <param name="rightDir">Right directory.</param>
        /// <param name="recursive">True: compare as recursive mode; false: don't compare sub-folders.</param>
        /// <param name="needCompareFile">Whether need compare the file.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool CompareDirectory(string leftDir, string rightDir,
            bool recursive, Func<string, string, bool> needCompareFile)
        {
            Helper.ThrowIfDirectoryNotExist(leftDir);
            Helper.ThrowIfDirectoryNotExist(rightDir);
            Helper.ThrowIfNull(needCompareFile);

            // compare files in current directory.
            string[] leftFiles = Directory.GetFiles(leftDir);
            
            if (leftFiles.Length != Directory.GetFiles(rightDir).Length)
            {
                return false;
            }

            foreach (string leftFilePath in leftFiles)
            {
                string leftFileName = Path.GetFileName(leftFilePath);
                string[] rightFiles = Directory.GetFiles(rightDir, leftFileName);
                if (rightFiles.Length != 1)
                {
                    return false;
                }

                if (needCompareFile(leftFilePath, rightFiles[0]) &&
                    !CompareBinary(leftFilePath, rightFiles[0]))
                {
                    return false;
                }
            }

            if (recursive)
            {
                // use recursive method to compare sub-folders.
                string[] leftSubDirs = Directory.GetDirectories(leftDir);
                if (leftSubDirs.Length != Directory.GetDirectories(rightDir).Length)
                {
                    return false;
                }

                foreach (string leftSubDirPath in leftSubDirs)
                {
                    string leftSubDirName = Path.GetFileName(leftSubDirPath);
                    string[] rightSubDirs = Directory.GetDirectories(rightDir, leftSubDirName);
                    if (rightSubDirs.Length != 1)
                    {
                        return false;
                    }

                    if (!CompareDirectory(leftSubDirPath, rightSubDirs[0], recursive, needCompareFile))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get build architecture.
        /// </summary>
        /// <returns>The architecture of current assembly.</returns>
        public static ProcessorArchitecture GetBuildArchitecture()
        {
            ProcessorArchitecture architecture = System.Reflection.Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
            return architecture;
        }

        /// <summary>
        /// Judge if build type is debug.
        /// </summary>
        /// <returns>Ture if debug build, or release.</returns>
        public static bool IsBuildDebug()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var attributes = assembly.GetCustomAttributes(typeof(System.Diagnostics.DebuggableAttribute), false);
            if (attributes.Length > 0)
            {
                var debuggable = attributes[0] as System.Diagnostics.DebuggableAttribute;
                if (debuggable != null)
                {
                    return (debuggable.DebuggingFlags & System.Diagnostics.DebuggableAttribute.DebuggingModes.Default) == System.Diagnostics.DebuggableAttribute.DebuggingModes.Default;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Public configuration operation

        /// <summary>
        /// Get the value from the configuration file for the given value name.
        /// <param />
        /// With name and value pairs, the configuration file example could be:
        /// <example>
        ///     TARGETKIND = FESTREAM
        ///     TARGETGUID = 16kHzDynamicStreamYV2M2.
        /// </example>
        /// </summary>
        /// <param name="configFile">The location of configuration file.</param>
        /// <param name="valueName">The case-sentitive name of the value to get from.
        /// The value name will be used in the regular expression directly.</param>
        /// <returns>Value string, null if not found.</returns>
        public static string ReadValue(string configFile, string valueName)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                throw new ArgumentNullException("configFile");
            }

            if (string.IsNullOrEmpty(valueName))
            {
                throw new ArgumentNullException("valueName");
            }

            string value = null;

            using (StreamReader sr = new StreamReader(configFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match m = Regex.Match(line, @"^\s*" + valueName + @"\s*=\s*(.*)$");
                    if (m.Success)
                    {
                        value = m.Groups[1].Value;
                        break;
                    }
                }
            }

            return value;
        }

        #endregion

        #region Public static Id operations

        /// <summary>
        /// Load sentence ids from file.
        /// </summary>
        /// <param name="filePath">Sentence id list file.</param>
        /// <returns>Id dictionary.</returns>
        public static Dictionary<string, string> ReadAllIds(string filePath)
        {
            Dictionary<string, string> ids = new Dictionary<string, string>();

            if (!File.Exists(filePath))
            {
                return ids;
            }

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    ids.Add(line.Trim(), null);
                }
            }

            return ids;
        }

        /// <summary>
        /// Save sentence ids to file.
        /// </summary>
        /// <param name="ids">Sentence ids to save.</param>
        /// <param name="filePath">Target file path.</param>
        public static void SaveIds(Dictionary<string, string> ids,
            string filePath)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("ids");
            }

            if (ids.Keys == null)
            {
                throw new ArgumentException("ids.Keys is null");
            }

            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                foreach (string sid in ids.Keys)
                {
                    sw.WriteLine(sid);
                }
            }
        }

        /// <summary>
        /// Save sentence ids to file.
        /// </summary>
        /// <param name="ids">Sentence ids to save.</param>
        /// <param name="filePath">Target file path.</param>
        public static void SaveIds(IEnumerable<string> ids,
            string filePath)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("ids");
            }

            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                foreach (string sid in ids)
                {
                    sw.WriteLine(sid);
                }
            }
        }

        /// <summary>
        /// Check regex expression.
        /// </summary>
        /// <param name="expression">Regex expression.</param>
        /// <returns>Whether the expression is valid.</returns>
        public static bool CheckRegex(string expression)
        {
            bool validExpression = true;
            try
            {
                Regex.Match(string.Empty, expression);
            }
            catch (ArgumentException)
            {
                validExpression = false;
            }

            return validExpression;
        }

        #endregion

        #region SSML helper operations

        /// <summary>
        /// Build say as SSML element.
        /// </summary>
        /// <param name="content">Content to speak.</param>
        /// <param name="sayAs">Say as value.</param>
        /// <returns>Ssml say as element.</returns>
        public static string BuildSayAsSsmlElement(string content, string sayAs)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException("content");
            }

            if (string.IsNullOrEmpty(sayAs))
            {
                throw new ArgumentNullException("sayAs");
            }

            string sayAsSsmlElementPattern = @"<say-as interpret-as='{0}'>{1}</say-as>";
            return Helper.NeutralFormat(sayAsSsmlElementPattern,
                HttpUtility.HtmlEncode(sayAs),
                HttpUtility.HtmlEncode(content));
        }

        /// <summary>
        /// Build SSML content.
        /// </summary>
        /// <param name="language">Language to be speak.</param>
        /// <param name="ssmlElement">Ssml element.</param>
        /// <returns>SSML content to be spokens.</returns>
        public static string BuildSsmlContent(Language language, string ssmlElement)
        {
            string ssmlContentPattern = @"<?xml version=""1.0""?>" + Environment.NewLine +
                @"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" " + Environment.NewLine +
                @"xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""" + Environment.NewLine +
                @"xsi:schemaLocation=""http://www.w3.org/2001/10/synthesis" + Environment.NewLine +
                @"http://www.w3.org/TR/speech-synthesis/synthesis.xsd""" + Environment.NewLine +
                @"xml:lang=""{0}"">" + Environment.NewLine +
                @"{1}" + Environment.NewLine +
                @"</speak>";

            if (string.IsNullOrEmpty(ssmlElement))
            {
                throw new ArgumentNullException("ssmlElement");
            }

            return Helper.NeutralFormat(ssmlContentPattern,
                Localor.LanguageToString(language), ssmlElement);
        }

        #endregion

        #region Public static List/Collection operations

        /// <summary>
        /// Shuffle a List(T) instance.
        /// </summary>
        /// <typeparam name="T">Type of each element.</typeparam>
        /// <param name="data">The list to be shuffled.</param>
        /// <param name="count">Number of (heading) elements to be shuffled.</param>
        /// <param name="autoRand">The random generator.</param>
        public static void ShuffleList<T>(List<T> data, int count, Random autoRand)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (count <= 0 || count > data.Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            for (int i = 0; i < count; i++)
            {
                int randomPosition = autoRand.Next(i, data.Count);
                T temp = data[randomPosition];
                data[randomPosition] = data[i];
                data[i] = temp;
            }
        }

        /// <summary>
        /// Find the index of name in the name array.
        /// </summary>
        /// <param name="names">Name array.</param>
        /// <param name="queryName">Query name.</param>
        /// <returns>Index, -1 for not found.</returns>
        public static int FindIndex(ICollection<string> names, string queryName)
        {
            int index = 0;
            if (names == null)
            {
                throw new ArgumentNullException("names");
            }

            foreach (string name in names)
            {
                if (queryName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                index++;
            }

            if (index >= names.Count)
            {
                index = -1;
            }

            return index;
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Find the locations of directories to search for the file.
        /// Currently it includes:
        /// 1. The directory location of currently executing assembly
        /// 2. Current working directory
        /// 3. Path in the system path.
        /// </summary>
        /// <returns>Directories which are to search in.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private static string[] FindSearchDirs()
        {
            List<string> dirs = new List<string>();

            dirs.Add(Environment.CurrentDirectory);

            string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            dirs.Add(Path.GetDirectoryName(appPath));

            string pathValue = Environment.GetEnvironmentVariable("PATH");
            string[] paths = pathValue.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string path in paths)
            {
                string cleanPath = path.Trim(new char[] { ' ', '"' });
                if (Path.IsPathRooted(cleanPath))
                {
                    dirs.Add(cleanPath);
                }
            }

            return dirs.ToArray();
        }

        /// <summary>
        /// Pre-allocate StringBuilder instance to hold result.
        /// </summary>
        /// <param name="longPath">Long path.</param>
        /// <param name="shortPath">Short path.</param>
        /// <param name="bufferSize">Beffer size of short path.</param>
        /// <returns>Return code.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Ignore."),
        DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int GetShortPathName([MarshalAs(UnmanagedType.LPWStr)] string longPath,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder shortPath,
            [MarshalAs(UnmanagedType.U4)] int bufferSize);

        /// <summary>
        /// Find tool under the windows system path.
        /// </summary>
        /// <param name="toolName">Tool name to find.</param>
        /// <returns>Tool location path.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization",
            "CA1302:DoNotHardcodeLocaleSpecificStrings", MessageId = "SysWOW64", Justification = "Ignore.")]
        private static string FindSystemTool(string toolName)
        {
            string systemRoot = System.Environment.GetEnvironmentVariable("SystemRoot");
            if (string.IsNullOrEmpty(systemRoot))
            {
                return null;
            }

            string toolPath = string.Empty;

            if (Is64BitMachine())
            {
                toolPath = Path.Combine(systemRoot, "SysWOW64");
                toolPath = Path.Combine(toolPath, toolName);
            }
            else
            {
                toolPath = Path.Combine(systemRoot, toolName);
                if (!File.Exists(toolPath))
                {
                    toolPath = Path.Combine(Environment.SystemDirectory, toolName);
                }
            }

            if (!File.Exists(toolPath))
            {
                return null;
            }

            return toolPath;
        }

        #endregion
    }

    /// <summary>
    /// Describe variable length items.
    /// </summary>
    public class ItemRange
    {
        /// <summary>
        /// Gets or sets the range start.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the range length.
        /// </summary>
        public int Length { get; set; }
    }
}