//----------------------------------------------------------------------------
// <copyright file="ZipFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class for operating zip file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.IO
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Packaging;
    using System.Linq;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Zip file operation class.
    /// </summary>
    public class ZipFile : IDisposable
    {
        /// <summary>
        /// URI path delimiter.
        /// </summary>
        public const char UriPathDelimeter = '/';

        /// <summary>
        /// URI root.
        /// </summary>
        public const string UriRoot = "/";

        private const char LocalPathDelimeter = '\\';
        private Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipFile"/> class.
        /// </summary>
        protected ZipFile()
        {
        }

        /// <summary>
        /// Extract file progress delegate.
        /// </summary>
        /// <param name="relativeFilePath">Extract file path.</param>
        /// <param name="processedBytes">Processed bytes count.</param>
        public delegate void ExtractProgressDelegate(string relativeFilePath, long processedBytes);

        /// <summary>
        /// Gets or sets the zip package.
        /// </summary>
        public Package Package
        {
            get
            {
                return _package;
            }

            protected set
            {
                _package = value;
            }
        }

        /// <summary>
        /// Gets the count of the files in the ZIP file.
        /// </summary>
        public int FileCount
        {
            get
            {
                return Package.GetParts().Count(t => true);
            }
        }

        /// <summary>
        /// Gets the file total size in the ZIP file.
        /// </summary>
        public long ExtractedFileSize
        {
            get
            {
                long extractedFileSize = 0;
                foreach (PackagePart part in Package.GetParts())
                {
                    extractedFileSize += part.GetStream().Length;
                }

                return extractedFileSize;
            }
        }

        #region Public static methods

        /// <summary>
        /// Compare content of the two zip file.
        /// </summary>
        /// <param name="sourceZipFilePath">Source zip file path.</param>
        /// <param name="targetZipFilePath">Target zip file path.</param>
        /// <returns>Compare content of the two zip files.</returns>
        public static bool CompareContent(string sourceZipFilePath,
            string targetZipFilePath)
        {
            return CompareContent(sourceZipFilePath, targetZipFilePath, new Func<string, string, bool>((left, right) => { return true; }));
        }

        /// <summary>
        /// Compare content of the two zip file.
        /// </summary>
        /// <param name="sourceZipFilePath">Source zip file path.</param>
        /// <param name="targetZipFilePath">Target zip file path.</param>
        /// <param name="needCompareFile">Whether the file need compare.</param>
        /// <returns>Compare content of the two zip files.</returns>
        public static bool CompareContent(string sourceZipFilePath,
            string targetZipFilePath, Func<string, string, bool> needCompareFile)
        {
            Helper.ThrowIfFileNotExist(sourceZipFilePath);
            Helper.ThrowIfFileNotExist(targetZipFilePath);
            string sourceZipDir = Helper.GetTempFolderName();
            using (ZipFile zipFile = ZipFile.Open(sourceZipFilePath, FileAccess.Read))
            {
                zipFile.Extract(sourceZipDir);
            }

            string targetZipDir = Helper.GetTempFolderName();
            using (ZipFile zipFile = ZipFile.Open(targetZipFilePath, FileAccess.Read))
            {
                zipFile.Extract(targetZipDir);
            }

            bool isSame = Helper.CompareDirectory(sourceZipDir, targetZipDir, true, needCompareFile);
            Helper.SafeDelete(sourceZipDir);
            Helper.SafeDelete(targetZipDir);
            return isSame;
        }

        /// <summary>
        /// Create new zip file object.
        /// </summary>
        /// <param name="zipFilePath">Zip file path to be created to.</param>
        /// <returns>Created ZipFile object.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public static ZipFile Create(string zipFilePath)
        {
            ZipFile zipFile = new ZipFile();
            zipFile.Package = Package.Open(zipFilePath, FileMode.Create);
            return zipFile;
        }

        /// <summary>
        /// Create new zip file object.
        /// </summary>
        /// <param name="zipFilePath">Zip file path to be created to.</param>
        /// <param name="fileAccess">File access.</param>
        /// <returns>Created ZipFile object.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public static ZipFile Open(string zipFilePath, FileAccess fileAccess)
        {
            Helper.ThrowIfFileNotExist(zipFilePath);
            ZipFile zipFile = new ZipFile();
            zipFile.Package = Package.Open(zipFilePath,
                FileMode.Open, fileAccess);
            return zipFile;
        }

        /// <summary>
        /// Combine root URI with relative URI.
        /// </summary>
        /// <param name="rootUri">Root URI.</param>
        /// <param name="relativeUri">Relative URI.</param>
        /// <returns>Combined URI.</returns>
        public static string CombineUri(string rootUri, string relativeUri)
        {
            Helper.ThrowIfNull(rootUri);
            Helper.ThrowIfNull(relativeUri);
            string combinedPath = Path.Combine(ConvertUriToPath(rootUri),
                ConvertUriToPath(relativeUri));
            combinedPath = ConvertPathToUri(combinedPath);
            if (combinedPath[0] != UriPathDelimeter)
            {
                combinedPath = UriPathDelimeter + combinedPath;
            }

            return combinedPath;
        }

        /// <summary>
        /// Convert URI to path.
        /// </summary>
        /// <param name="uri">URI to be converted.</param>
        /// <returns>Converted path.</returns>
        public static string ConvertUriToPath(string uri)
        {
            Helper.ThrowIfNull(uri);

            // Remove the root delimiter
            if (uri[0] == UriPathDelimeter)
            {
                uri = uri.Substring(1);
            }

            return uri.Replace(UriPathDelimeter, LocalPathDelimeter);
        }

        /// <summary>
        /// Convert path to URI.
        /// </summary>
        /// <param name="path">Path to be converted.</param>
        /// <returns>Converted URI.</returns>
        public static string ConvertPathToUri(string path)
        {
            Helper.ThrowIfNull(path);
            return path.Replace(LocalPathDelimeter, UriPathDelimeter);
        }

        #endregion

        #region Public instance methods

        /// <summary>
        /// Pack all files in one directory to a ZIP file.
        /// </summary>
        /// <param name="packUriPath">Target ZIP file location.</param>
        /// <param name="packDir">Source file directory.</param>
        /// <param name="filesNameWithoutExtension">Files name without extension to be zipped.</param>
        public void AddDirPart(string packUriPath, string packDir,
            IEnumerable<string> filesNameWithoutExtension)
        {
            foreach (string relativeFileLocalPath in Helper.GetSubFilesRelativePath(packDir, "*"))
            {
                string id = Path.GetFileNameWithoutExtension(relativeFileLocalPath);
                if (filesNameWithoutExtension.Contains(id))
                {
                    string relativeFileUirPath = CombineUri(packUriPath, relativeFileLocalPath);
                    string localFilePath = Path.Combine(packDir, relativeFileLocalPath);
                    AddPart(relativeFileUirPath, localFilePath);
                }
            }
        }

        /// <summary>
        /// Pack all files in one directory to a ZIP file.
        /// </summary>
        /// <param name="packDir">Source file directory.</param>
        public void AddDirPart(string packDir)
        {
            AddDirPart(ZipFile.UriRoot, packDir);
        }

        /// <summary>
        /// Pack all files in one directory to a ZIP file.
        /// </summary>
        /// <param name="packUriPath">Target ZIP file location.</param>
        /// <param name="packDir">Source file directory.</param>
        public void AddDirPart(string packUriPath, string packDir)
        {
            foreach (string relativeFileLocalPath in Helper.GetSubFilesRelativePath(packDir, "*"))
            {
                string relativeFileUirPath = CombineUri(packUriPath, relativeFileLocalPath);
                string localFilePath = Path.Combine(packDir, relativeFileLocalPath);
                AddPart(relativeFileUirPath, localFilePath);
            }
        }

        /// <summary>
        /// Add one source file to the ZIP file.
        /// </summary>
        /// <param name="relativeUriPath">URI in the ZIP file.</param>
        /// <param name="filePath">Source file location to be zipped.</param>
        public void AddPart(string relativeUriPath, string filePath)
        {
            // This type is only used to generate "[Content_Types].xml" file,
            // This file is not used, but the Package API forced to generate this file.
            // so use the plain text as dummy type.
            // To make it consistent, we have text and binary files. current, only text file will be added
            AddPart(relativeUriPath, filePath,
                System.Net.Mime.MediaTypeNames.Text.Plain, false, false);
        }

        /// <summary>
        /// AddPart.
        /// </summary>
        /// <param name="relativeUriPath">Relative uri path.</param>
        /// <param name="filePath">File path.</param>
        /// <param name="needCompress">Need compress.</param>
        public void AddPart(string relativeUriPath, string filePath, bool needCompress)
        {
            // This type is only used to generate "[Content_Types].xml" file,
            // This file is not used, but the Package API forced to generate this file.
            // so use the plain text as dummy type.
            // To make it consistent, we have text and binary files. current, only text file will be added
            AddPart(relativeUriPath, filePath, System.Net.Mime.MediaTypeNames.Text.Plain, false, needCompress);
        }

        /// <summary>
        /// Add one source file to the ZIP file.
        /// </summary>
        /// <param name="relativeUriPath">URI in the ZIP file.</param>
        /// <param name="filePath">Source file location to be zipped.</param>
        /// <param name="contentType">File content type.</param>
        /// <param name="overwrite">Whether overwrite existing part.</param>
        /// <param name="needCompress">Compress ratio.</param>
        public void AddPart(string relativeUriPath, string filePath,
            string contentType, bool overwrite, bool needCompress)
        {
            Helper.ThrowIfNull(Package);
            Uri relativeUri = PackUriHelper.CreatePartUri(
                new Uri(relativeUriPath, UriKind.Relative));

            if (overwrite)
            {
                Package.DeletePart(relativeUri);
            }

            PackagePart packagePartDocument;

            if (needCompress)
            {
                packagePartDocument = Package.CreatePart(relativeUri, contentType, CompressionOption.Maximum);
            }
            else
            {
                packagePartDocument = Package.CreatePart(relativeUri, contentType);
            }

            using (FileStream fs = new FileStream(
                   filePath, FileMode.Open, FileAccess.Read))
            {
                CopyStream(packagePartDocument.GetStream(), fs);
            }
        }

        /// <summary>
        /// Extract all files in one ZIP file to target directory.
        /// </summary>
        /// <param name="targetDir">Target directory.</param>
        public void Extract(string targetDir)
        {
            Extract(targetDir, new ExtractProgressDelegate((relativeFilePath, processedBytes) => { }));
        }

        /// <summary>
        /// Extract all files in one ZIP file to target directory.
        /// </summary>
        /// <param name="targetDir">Target directory.</param>
        /// <param name="extractProgressDelegate">Extract progress delegate.</param>
        public void Extract(string targetDir, ExtractProgressDelegate extractProgressDelegate)
        {
            Helper.ThrowIfNull(targetDir);
            Helper.EnsureFolderExist(targetDir);
            Helper.ThrowIfNull(extractProgressDelegate);

            foreach (PackagePart part in Package.GetParts())
            {
                string relativeFilePath = ConvertUriToPath(part.Uri.OriginalString);
                string targetFile = Path.Combine(targetDir, relativeFilePath);
                Helper.EnsureFolderExistForFile(targetFile);
                using (FileStream fs = new FileStream(
                    targetFile, FileMode.Create, FileAccess.Write))
                {
                    CopyStream(fs, part.GetStream());
                    extractProgressDelegate(relativeFilePath, part.GetStream().Length);
                }
            }
        }

        /// <summary>
        /// Extract file in ZIP file to target location.
        /// </summary>
        /// <param name="relativeUriPath">URI of the file to be extracted.</param>
        /// <param name="targetFilePath">Target file location.</param>
        public void Extract(string relativeUriPath, string targetFilePath)
        {
            Helper.EnsureFolderExistForFile(targetFilePath);
            Uri relativeUri = new Uri(relativeUriPath, UriKind.Relative);
            PackagePart part = Package.GetPart(relativeUri);
            using (FileStream fs = new FileStream(
                targetFilePath, FileMode.Create, FileAccess.Write))
            {
                CopyStream(fs, part.GetStream());
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose the ZipFile.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing flag.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Package.Close();
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Copy bytes from source stream to target stream.
        /// </summary>
        /// <param name="target">Target stream to be copied to.</param>
        /// <param name="source">Source stream to be copied.</param>
        private static void CopyStream(Stream target, Stream source)
        {
            const int BufferSize = 0x1000;
            byte[] buf = new byte[BufferSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, BufferSize)) > 0)
            {
                target.Write(buf, 0, bytesRead);
            }
        }

        #endregion
    }
}