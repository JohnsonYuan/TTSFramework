namespace Microsoft.Tts.Cosmos.TMOC
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.Tts.Offline.IO;
    using Microsoft.Tts.Offline.Utility;
    using VcClient;

    /// <summary>
    /// The class of TMOC path.
    /// </summary>
    public static class TmocPath
    {
        /// <summary>
        /// The enumeration of file format type.
        /// </summary>
        public enum FileFormatType
        {
            /// <summary>
            /// ASCII.
            /// </summary>
            ASCII,

            /// <summary>
            /// UTF8.
            /// </summary>
            UTF8,

            /// <summary>
            /// Unknown.
            /// </summary>
            UNKOWN
        }

        /// <summary>
        /// Get path for commandline.
        /// </summary>
        /// <param name="filename">File name.</param>
        /// <returns>Sub file name.</returns>
        public static string GetPathForCommandline(string filename)
        {
            return filename.IndexOf(" ") == -1 ? filename : string.Format("\"{0}\"", filename);
        }

        /// <summary>
        /// Get none empty directory name.
        /// </summary>
        /// <param name="filename">File name.</param>
        /// <returns>Directory name.</returns>
        public static string GetNonemptyDirectoryName(string filename)
        {
            return Path.GetDirectoryName(filename) == string.Empty ? "." : Path.GetDirectoryName(filename);
        }

        /// <summary>
        /// Get path name with new extension.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        /// <param name="newExtension">New extension.</param>
        /// <returns>Path.</returns>
        public static string GetPathnameWithNewExtension(string pathname, string newExtension)
        {
            return Path.Combine(Path.GetDirectoryName(pathname), Path.GetFileNameWithoutExtension(pathname) + "." + newExtension);
        }

        /// <summary>
        /// Return the full path of the input local path.
        /// If not local path, just return.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        /// <returns>Full path.</returns>
        public static string GetLocalFullPath(string pathname)
        {
            if (IsCosmosPath(pathname))
            {
                return pathname;
            }
            else
            {
                string fullpathname = Path.GetFullPath(pathname);
                return fullpathname.Replace("\\", "/");
            }
        }

        /// <summary>
        /// Returns if the filepath is .
        /// 1. full cosmos path (http(s):// ).
        /// 2. relative cosmos path (/).
        /// </summary>
        /// <param name="fname">The filepath.</param>
        /// <returns>Boolean evaluation.</returns>
        public static bool IsCosmosPath(string fname)
        {
            var ci = new System.Globalization.CultureInfo("en-US");
            return fname.StartsWith("http://", true, ci) || fname.StartsWith("/", true, ci) || fname.StartsWith("https://", true, ci);
        }

        /// <summary>
        /// Determine whether it is a cosmos relative path starting with "/".
        /// </summary>
        /// <param name="fname">Fname.</param>
        /// <returns>New fname.</returns>
        public static bool IsCosmosRelativePath(string fname)
        {
            var ci = new System.Globalization.CultureInfo("en-US");
            return fname.StartsWith("/", true, ci);
        }

        /// <summary>
        /// Whether is a absolute path local or http.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>Is or not an absolute uri.</returns>
        public static bool IsFullPath(string path)
        {
            return new Uri(path, UriKind.RelativeOrAbsolute).IsAbsoluteUri;
        }

        /// <summary>
        /// Whether is a absolute local path.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>Is or not an absolute uri.</returns>
        public static bool IsLocalFullPath(string path)
        {
            Uri uri = new Uri(path, UriKind.RelativeOrAbsolute);
            if (IsFullPath(path))
            {
                return uri.IsFile;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns if the filepath is .
        /// 1. full cosmos path start with http(s):// .
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <returns>Is or not a path.</returns>
        public static bool IsCosmosFullPath(string fname)
        {
            var ci = new System.Globalization.CultureInfo("en-US");
            return fname.StartsWith("http://", true, ci) || fname.StartsWith("https://", true, ci);
        }

        /// <summary>
        /// Combine the path.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>The full path.</returns>
        public static string Combine(params string[] args)
        {
            string fullpath = string.Empty;
            int cnt = 0;
            foreach (var a in args)
            {
                ++cnt;
                string ac;
                ac = a.Replace("\\", "/");
                if (string.IsNullOrEmpty(a))
                {
                    continue;
                }

                // we will not strip the final /.
                if (cnt != args.Length)
                {
                    if (ac.Substring(ac.Length - 1) == "/")
                    {
                        ac = ac.Substring(0, ac.Length - 1);
                    }
                }

                if (fullpath == string.Empty)
                {
                    fullpath = ac;
                }
                else
                {
                    if (ac.Substring(0, 1) == "/")
                    {
                        ac = ac.Substring(1);
                    }

                    fullpath = fullpath + "/" + ac;
                }
            }

            return fullpath;
        }

        /// <summary>
        /// Get the relative path.
        /// </summary>
        /// <param name="fullpath">The full path.</param>
        /// <param name="rootPath">The root path.</param>
        /// <param name="slash">Which slash you prefer ? '\\' '/' or do not change, default not change.</param>
        /// <returns>The relative path.</returns>
        public static string MakeRelativePath(string fullpath, string rootPath, char slash = ' ')
        {
            string relativRath = string.Empty;
            if (rootPath == string.Empty)
            {
                relativRath = fullpath;
            }
            else
            {
                // default using /?
                Uri uriPath = new Uri(fullpath);

                Uri uriRootPath = new Uri(rootPath);
                relativRath = uriRootPath.MakeRelativeUri(uriPath).ToString();

                if (!relativRath.StartsWith("/") && IsCosmosPath(fullpath))
                {
                    relativRath = "/" + relativRath;
                }
            }

            if (slash == '\\')
            {
                return relativRath.Replace('/', '\\');
            }
            else if (slash == '/')
            {
                return relativRath.Replace('\\', '/');
            }
            else
            {
                return relativRath;
            }
        }

        /// <summary>
        /// Converta relative path to full vc path, will concatenate the VCpath.
        /// The slash will be automatically normalize to '\\' for local path and.
        /// '/' for http VC path.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <param name="nameVC">VC name.</param>
        /// <returns>The full path.</returns>
        public static string RelativeToFullVCPath(string relativePath, string nameVC)
        {
            if (null == relativePath)
            {
                return null;
            }

            if (relativePath.StartsWith(".."))
            {
                throw new Exception("Can not handle relative path start with ..");
            }
            else if (relativePath.StartsWith(@".\") || relativePath.StartsWith(@"./"))
            {
                relativePath = relativePath.Remove(0, 2);
            }
            else if (relativePath.StartsWith(@"\") || relativePath.StartsWith(@"/"))
            {
                relativePath = relativePath.Remove(0, 1);
            }

            string fullPath = Path.Combine(nameVC, relativePath);
            if (string.IsNullOrEmpty(nameVC))
            {
                fullPath = fullPath.Replace('/', '\\');
            }
            else
            {
                // For VC path, http.
                fullPath = fullPath.Replace('\\', '/');
            }

            return fullPath;
        }

        /// <summary>
        /// Converta relative path to full vc path, will concatenate the VCpath.
        /// The slash will be automatically normalize to '\\' for local path and.
        /// '/' for http VC path.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>Full path.</returns>
        public static string RelativeToFullVCPath(string relativePath)
        {
            return RelativeToFullVCPath(relativePath, TmocGlobal.Instance.VcConfig.VcName);
        }

        /// <summary>
        /// Convert the full vc path into relative path within VC.
        /// </summary>
        /// <param name="fullVCPath">The full VC path.</param>
        /// <param name="slash">Which slash you prefer ? "\" "/" or do not change, default not change.</param>
        /// <returns>Relative path.</returns>
        public static string FullVCToRelativePath(string fullVCPath, char slash = ' ')
        {
            return MakeRelativePath(fullVCPath, TmocGlobal.Instance.VcConfig.VcName, slash);
        }
    }

    /// <summary>
    /// The class of TMOC directory.
    /// </summary>
    public static class TmocDirectory
    {
        /// <summary>
        /// Check the diretory exists or not, deal with both local or vc path.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="maxRetry">The max retry.</param>
        /// <returns>Is existed.</returns>
        public static bool Exists(string path, int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            bool isExisted = false;
            if (TmocPath.IsCosmosPath(path))
            {
                if (!TmocPath.IsCosmosFullPath(path))
                {
                    path = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, path);
                }

                var info = RetryClass.Retry(
                    new Func<string, bool, List<StreamInfo>>(VC.GetDirectoryInfo),
                    new object[] { path, true },
                    maxRetry, TmocConstants.VcOperationRetryDelay,
                    new string[] { "Could not get directory info for", "StreamSetNotFoundException" }, null);

                if (info == null)
                {
                    isExisted = false;
                }
                else
                {
                    isExisted = true;
                }
            }
            else
            {
                isExisted = Directory.Exists(path);
            }

            return isExisted;
        }

        /// <summary>
        /// Delete a directory, either local or VC directory.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        public static void Delete(string pathname)
        {
            if (TmocPath.IsCosmosPath(pathname))
            {
                DeleteOnVC(pathname);
            }
            else
            {
                DeleteOnLocal(pathname);
            }
        }

        /// <summary>
        /// Return all the files under the directory, not recursive .
        /// </summary>
        /// <param name="pathname">Path name.</param>
        /// <returns>Return the list of files in full vc path. </returns>
        public static string[] GetFilesOnVC(string pathname)
        {
            if (!TmocPath.IsCosmosPath(pathname))
            {
                throw new Exception(string.Format("{0} is not a valid VC path", pathname));
            }

            var flist = new List<string>();
            if (Exists(pathname))
            {
                if (!TmocPath.IsCosmosFullPath(pathname))
                {
                    pathname = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, pathname);
                }

                List<StreamInfo> infoList = GetDirInfoWithRetry(pathname);
                foreach (var sinfo in infoList)
                {
                    if (!sinfo.IsDirectory)
                    {
                        flist.Add(sinfo.StreamName);
                    }
                }
            }

            return flist.ToArray();
        }

        /// <summary>
        /// Get directory information with retry.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        /// <param name="maxRetry">The max retry.</param>
        /// <returns>Directory information.</returns>
        [CLSCompliant(false)]
        public static List<StreamInfo> GetDirInfoWithRetry(string pathname, int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            return (List<StreamInfo>)RetryClass.Retry(new Func<string, bool, List<StreamInfo>>(VC.GetDirectoryInfo),
                new object[] { pathname, true }, maxRetry, TmocConstants.VcOperationRetryDelay);
        }

        /// <summary>
        /// Get files.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        /// <param name="option">The option.</param>
        /// <returns>The files path.</returns>
        public static string[] GetFiles(string pathname, SearchOption option = SearchOption.AllDirectories)
        {
            if (TmocPath.IsCosmosPath(pathname))
            {
                return GetFilesOnVC(pathname, option);
            }
            else
            {
                return Directory.GetFiles(pathname, "*", option);
            }
        }

        /// <summary>
        /// Return all the files under the directory, either recursive or non-resursive.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        /// <param name="searchOption">The search option.</param>
        /// <returns>The full vc path of all the files.</returns>
        public static string[] GetFilesOnVC(string pathname, SearchOption searchOption)
        {
            if (SearchOption.TopDirectoryOnly == searchOption)
            {
                return GetFilesOnVC(pathname);
            }
            else
            {
                var flist = new List<string>();
                GetFilesOnVCResursive(pathname, flist);
                flist.Sort();
                return flist.ToArray();
            }
        }

        /// <summary>
        /// Copy a local directory into cosmos.
        /// </summary>
        /// <param name="srcDir">The source dir.</param>
        /// <param name="vcname">The vc name.</param>
        /// <param name="tgtDir">The target dir.</param>
        /// <param name="overwrite">Overwrite.</param>
        public static void CopyLocalToCosmos(string srcDir, string vcname, string tgtDir, bool overwrite)
        {
            string[] filelist = Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories);
            foreach (string file in filelist)
            {
                string des = TmocPath.Combine(vcname, tgtDir, file.Substring(srcDir.Length));
                TmocFile.CopyLocalToCosmos(file, des, overwrite);
            }
        }

        /// <summary>
        /// Copy cosmos file to local.
        /// </summary>
        /// <param name="vcname">The vc name.</param>
        /// <param name="srcDir">The source dir.</param>
        /// <param name="tgtDir">The target dir.</param>
        /// <param name="overwrite">Overwrite.</param>
        public static void CopyCosmosToLocal(string vcname, string srcDir, string tgtDir, bool overwrite)
        {
            var fList = GetFilesOnVC(srcDir, SearchOption.AllDirectories);
            foreach (string file in fList)
            {
                string des = TmocPath.Combine(tgtDir, TmocPath.FullVCToRelativePath(file, '\\'));
                TmocFile.CopyCosmosToLocal(file, des, overwrite);
            }
        }

        /// <summary>
        /// Copy directory on local.
        /// </summary>
        /// <param name="sourceDirName">The source dir name.</param>
        /// <param name="destDirName">The destination.</param>
        /// <param name="overwrite">Overwrite.</param>
        public static void CopyOnLocal(string sourceDirName, string destDirName, bool overwrite)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, overwrite);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                CopyOnLocal(subdir.FullName, temppath, overwrite);
            }
        }

        /// <summary>
        /// Create the directory for a file.
        /// </summary>
        /// <param name="fname">The fname.</param>
        public static void CreateForFile(string fname)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fname)))
            {
                if (Path.GetDirectoryName(fname) != string.Empty)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fname));
                }
            }
        }

        /// <summary>
        /// Create the directory.
        /// </summary>
        /// <param name="pathname">The fname.</param>
        public static void CreateDirectory(string pathname)
        {
            if (TmocPath.IsCosmosPath(pathname))
            {
                // on cosmos, you don't need to create a directory explicitly
                return;
            }

            if (!Directory.Exists(pathname))
            {
                Directory.CreateDirectory(pathname);
            }
        }

        /// <summary>
        /// Create dir for a list of files .
        /// </summary>
        /// <param name="fnameList">The fname list.</param>
        public static void CreateForFiles(IEnumerable<string> fnameList)
        {
            HashSet<string> dirList = new HashSet<string>();
            foreach (var f in fnameList)
            {
                string dir = Path.GetDirectoryName(f);
                dirList.Add(dir);
            }

            foreach (var dir in dirList)
            {
                if (!Directory.Exists(dir))
                {
                    if (dir != string.Empty)
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
            }
        }

        /// <summary>
        /// Return true if a share is a network share.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>If a share is a network share.</returns>
        public static bool IsNetworkPath(string path)
        {
            if (!path.StartsWith(@"/") && !path.StartsWith(@"\"))
            {
                // check if the drive is a mapped drive
                string rootPath = System.IO.Path.GetPathRoot(path);
                System.IO.DriveInfo driveInfo = new System.IO.DriveInfo(rootPath);
                return driveInfo.DriveType == DriveType.Network;
            }

            return true;
        }

        /// <summary>
        /// Resursive search of all files.
        /// </summary>
        /// <param name="pathname">The path name.</param>
        /// <param name="flist">The file list to fill.</param>
        private static void GetFilesOnVCResursive(string pathname, List<string> flist)
        {
            if (!TmocPath.IsCosmosPath(pathname))
            {
                throw new Exception(string.Format("{0} is not a valid VC path", pathname));
            }

            if (Exists(pathname))
            {
                if (!TmocPath.IsCosmosFullPath(pathname))
                {
                    pathname = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, pathname);
                }

                List<StreamInfo> infoList = GetDirInfoWithRetry(pathname);
                foreach (var sinfo in infoList)
                {
                    if (!sinfo.IsDirectory)
                    {
                        flist.Add(sinfo.StreamName);
                    }
                    else
                    {
                        // Do the DFS search to fill the list.
                        GetFilesOnVCResursive(sinfo.StreamName, flist);
                    }
                }
            }
        }

        /// <summary>
        /// Delete of a local directory, enhancement of Directory.Delete.
        /// Resursively.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        private static void DeleteOnLocal(string pathname)
        {
            if (Directory.Exists(pathname))
            {
                string[] fileArr = Directory.GetFiles(pathname, "*", SearchOption.AllDirectories);
                foreach (var fname in fileArr)
                {
                    TmocFile.RemoveReadOnlyAttr(fname);
                }

                Directory.Delete(pathname, true);
            }
        }

        /// <summary>
        /// Delete of a directory on VC, resursively.
        /// </summary>
        /// <param name="pathname">Path name.</param>
        private static void DeleteOnVC(string pathname)
        {
            if (Exists(pathname))
            {
                var fList = GetFilesOnVC(pathname, SearchOption.AllDirectories);
                foreach (var fname in fList)
                {
                    TmocFile.Delete(fname);
                }
            }
        }
    }

    /// <summary>
    /// The class of TMOC file.
    /// </summary>
    public static class TmocFile
    {
        /// <summary>
        /// Calculate the value for job's NebularArgument parameter.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <param name="tokenNumber">Default value is 13.</param>
        /// <returns>The number of nubular argument.</returns>
        public static int CalculateNebularArgument(string file, int tokenNumber = 13)
        {
            long fileSize = FileSize(file);
            long nubularArgument = fileSize / tokenNumber;
            return (int)nubularArgument;
        }

        /// <summary>
        /// Zip file and split file.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <param name="count">The count.</param>
        /// <returns>The file list.</returns>
        public static List<string> ZipAndSplit(string fname, int count)
        {
            List<string> fileList = new List<string>();
            if (!File.Exists(fname))
            {
                throw new ArgumentException(string.Format("{0} doesn't exist", fname));
            }

            using (ZipFile zipFile = ZipFile.Create(fname + ".zip"))
            {
                zipFile.AddPart(Path.GetFileName(fname), fname, true);
            }

            var bytesArray = File.ReadAllBytes(fname + ".zip");
            int sizePerChunk = (int)Math.Ceiling(bytesArray.Count() / ((double)count));
            var splitArray = EnumerableExtension.Split(bytesArray, sizePerChunk);

            int i = 0;
            foreach (var splitPart in splitArray)
            {
                string fileName = fname + ".zip" + i.ToString();
                fileList.Add(fileName);
                File.WriteAllBytes(fileName, splitPart);
                i++;
            }

            return fileList;
        }

        /// <summary>
        /// Zip files into zip file.
        /// </summary>
        /// <param name="fList">The f list.</param>
        /// <param name="zipFileName">The zip file name.</param>
        public static void ZipFiles(List<string> fList, string zipFileName)
        {
            using (ZipFile zipFile = ZipFile.Create(zipFileName + ".zip"))
            {
                foreach (var file in fList)
                {
                    zipFile.AddPart(Path.GetFileName(file), file, true);
                }
            }
        }

        /// <summary>
        /// Compress file.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <returns>Zip.</returns>
        public static string CompressFile(string fname)
        {
            using (ZipFile zipFile = ZipFile.Create(fname + ".zip"))
            {
                zipFile.AddPart(Path.GetFileName(fname), fname, true);
            }

            return fname + ".zip";
        }

        /// <summary>
        /// Decompress file.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <param name="useDefaultFolder">The default folder.</param>
        /// <returns>The fname without extension.</returns>
        public static string DecompressFile(string fname, bool useDefaultFolder = true)
        {
            using (ZipFile zipFile = ZipFile.Open(fname, FileAccess.Read))
            {
                if (useDefaultFolder)
                {
                    zipFile.Extract("./");
                }
                else
                {
                    zipFile.Extract(Path.GetDirectoryName(fname));
                }
            }

            return Path.GetFileNameWithoutExtension(fname);
        }

        /// <summary>
        /// Split file.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="chunksize">The chunk size.</param>
        /// <returns>The file list.</returns>
        public static List<string> SplitFile(string inputFile, int chunksize)
        {
            const int BUFFER_SIZE = 100 * 1024 * 1024;
            byte[] buffer = new byte[BUFFER_SIZE];
            List<string> fileList = new List<string>();

            using (Stream input = File.OpenRead(inputFile))
            {
                int index = 0;
                while (input.Position < input.Length)
                {
                    using (Stream output = File.Create(inputFile + index))
                    {
                        int remaining = chunksize, bytesRead;
                        while (remaining > 0 && (bytesRead = input.Read(buffer, 0,
                                Math.Min(remaining, BUFFER_SIZE))) > 0)
                        {
                            output.Write(buffer, 0, bytesRead);
                            remaining -= bytesRead;
                        }
                    }

                    fileList.Add(inputFile + index);
                    index++;
                }
            }

            return fileList;
        }

        /// <summary>
        /// Combine file list and decompress files.
        /// </summary>
        /// <param name="fileList">The file list.</param>
        /// <param name="outFileName">The output file name.</param>
        public static void CombineAndDecompress(List<string> fileList, string outFileName)
        {
            IEnumerable<byte> zipArray = Enumerable.Empty<byte>();
            foreach (var bytesChunk in fileList.Select(x => File.ReadAllBytes(x)))
            {
                zipArray = zipArray.Concat(bytesChunk);
            }

            File.WriteAllBytes("./temp.zip", zipArray.ToArray());

            string decompressFolder = "./zip";
            Helper.SafeDelete(decompressFolder);

            using (ZipFile zipFile = ZipFile.Open("./temp.zip", FileAccess.Read))
            {
                Directory.CreateDirectory(decompressFolder);
                zipFile.Extract(decompressFolder);
            }

            File.WriteAllBytes(outFileName, File.ReadAllBytes(Directory.GetFiles(decompressFolder).First()));
        }

        /// <summary>
        /// Make a judgement of if file exists, both support local path and full VC path with http:.
        /// </summary>
        /// <param name="fname">File name, can be Local path or vc path. if vc path, must be the full path.</param>
        /// <returns>If the file exists.</returns>
        public static bool Exists(string fname)
        {
            if (TmocPath.IsCosmosPath(fname))
            {
                return ExistsOnVC(fname);
            }
            else
            {
                return ExistsOnLocal(fname);
            }
        }

        /// <summary>
        /// Normalize the text.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <param name="isFile">If it is file.</param>
        /// <returns>Content.</returns>
        public static string NormalizeOutput(string fname, bool isFile = true)
        {
            string content = string.Empty;

            if (isFile)
            {
                content = File.ReadAllText(fname);
            }
            else
            {
                content = fname;
            }

            return content.Replace("#N#", "\n").Replace("#R#", "\r");
        }

        /// <summary>
        /// Encode the text uploaded to COSMOS.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <returns>The all text of fname.</returns>
        public static string EncodeOutput(string fname)
        {
            return File.ReadAllText(fname).Replace("\n", "#N#").Replace("\r", "#R#");
        }

        /// <summary>
        /// Whether a file exist on VC.
        /// </summary>
        /// <param name="fname">
        /// 1. full vc path with http:.
        /// 2. relative path with /.</param>
        /// <returns>If it is exists on VC with retry.</returns>
        public static bool ExistsOnVC(string fname)
        {
            if (!TmocPath.IsCosmosPath(fname))
            {
                throw new ArgumentException(string.Format("{0} is not a valid VC path", fname));
            }

            if (!TmocPath.IsCosmosFullPath(fname))
            {
                fname = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, fname);
            }

            return ExistsOnVCWithRetry(fname);
        }

        /// <summary>
        /// Check if a array of files exist.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <returns>Exist or not exist.</returns>
        public static bool Exists(params string[] files)
        {
            foreach (var f in files)
            {
                if (!Exists(f))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if file is existed.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <returns>Exist or not exist.</returns>
        public static bool Exists(IEnumerable<string> files)
        {
            foreach (var f in files)
            {
                if (!Exists(f))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get the file size in bytes.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The size of file in bytes.</returns>
        public static long FileSize(string fileName)
        {
            if (!TmocPath.IsCosmosPath(fileName))
            {
                return new FileInfo(fileName).Length;
            }
            else
            {
                return FileSizeWithRetry(fileName);
            }
        }

        /// <summary>
        /// Set expiration date.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="expireIn">The expire in.</param>
        public static void ExpireIn(string fileName, TimeSpan expireIn)
        {
            // if it is local do not set expiration date
            if (!TmocPath.IsCosmosPath(fileName))
            {
                return;
            }

            ExpireInWithRetry(fileName, expireIn);
        }

        /// <summary>
        /// Check file size with retry.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="maxRetry">The number of max retry.</param>
        /// <returns>The length of file.</returns>
        public static long FileSizeWithRetry(string fileName, int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            if (TmocPath.IsCosmosPath(fileName))
            {
                StreamInfo si = (StreamInfo)RetryClass.Retry(new Func<string, bool, bool, StreamInfo>(VC.GetStreamInfo),
                    new object[] { fileName, false, false }, maxRetry, TmocConstants.VcOperationRetryDelay);
                return si.Length;
            }
            else
            {
                return (new FileInfo(fileName)).Length;
            }
        }

        /// <summary>
        /// Throw exception if stream is not there or incomplete.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <param name="compression">The compression.</param>
        /// <param name="maxRetry">The number of max retry.</param>
        /// <returns>If it is complete.</returns>
        public static bool IsExistedOrIncompleteVCStream(string fname, bool compression = true, int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            StreamInfo si = (StreamInfo)RetryClass.Retry(new Func<string, bool, bool, StreamInfo>(VC.GetStreamInfo), new object[] { fname, compression, true },
            maxRetry, TmocConstants.VcOperationRetryDelay);
            return si.IsComplete;
        }

        /// <summary>
        /// General delete, will detect whether on local or on VC.
        /// If path start with http:// or / it will be a VC path.
        /// Otherwise a local path.
        /// </summary>
        /// <param name="fname">File name to be deleted.</param>
        public static void Delete(string fname)
        {
            if (TmocPath.IsCosmosPath(fname))
            {
                DeleteOnVC(fname);
            }
            else
            {
                DeleteOnLocal(fname);
            }
        }

        /// <summary>
        /// Delete a file on VC.
        /// </summary>
        /// <param name="fname">The fname.</param>
        public static void DeleteOnVC(string fname)
        {
            if (!TmocPath.IsCosmosFullPath(fname))
            {
                fname = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, fname);
            }

            if (TmocFile.Exists(fname))
            {
                DeleteOnVCWithRetry(fname);
            }
        }

        /// <summary>
        /// General copy, will detect whether on local or on VC.
        /// If path start with http:// or / it will be a VC path.
        /// Otherwise a local path.
        /// </summary>
        /// <param name="files">The files.</param>
        public static void Delete(IEnumerable<string> files)
        {
            foreach (var f in files)
            {
                Delete(f);
            }
        }

        /// <summary>
        /// Copies an existing stream on VC to a new file. Overwriting a file of the same name is allowed.
        /// Be careful since it might be very slow, e.g., ten minutes or more.
        /// </summary>
        /// <param name="sourceFileName">The file to copy.</param>
        /// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
        /// <param name="overwrite">True if the destination file can be overwritten; otherwise, false.</param>
        public static void CopyOnVC(string sourceFileName, string destFileName, bool overwrite = true)
        {
            if (!TmocPath.IsCosmosFullPath(destFileName))
            {
                destFileName = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, destFileName);
            }

            if (!TmocPath.IsCosmosFullPath(sourceFileName))
            {
                sourceFileName = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, sourceFileName);
            }

            if (overwrite)
            {
                if (ExistsOnVCWithRetry(destFileName))
                {
                    DeleteOnVCWithRetry(destFileName);
                }

                ConcatenateWithRetry(sourceFileName, destFileName);
            }
            else
            {
                if (ExistsOnVCWithRetry(destFileName))
                {
                    throw new FieldAccessException(string.Format(@"File already  exists, you set not to overwrite it. {0}", destFileName));
                }
                else
                {
                    ConcatenateWithRetry(sourceFileName, destFileName);
                }
            }
        }

        /// <summary>
        /// Copy small binary file on VC.
        /// </summary>
        /// <param name="copyList">The copy list.</param>
        /// <param name="overwrite">If overwrite.</param>
        public static void CopySmallBinaryFileOnVC(Dictionary<string, string> copyList, bool overwrite = true)
        {
            if (overwrite)
            {
                foreach (var e in copyList)
                {
                    string destFileName = e.Value;
                    if (!TmocPath.IsCosmosFullPath(destFileName))
                    {
                        destFileName = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, destFileName);
                    }

                    if (ExistsOnVCWithRetry(destFileName))
                    {
                        DeleteOnVCWithRetry(destFileName);
                    }
                }
            }
            else
            {
                Dictionary<string, string> newCopyList = new Dictionary<string, string>();
                foreach (var e in copyList)
                {
                    string destFileName = e.Value;
                    if (!TmocPath.IsCosmosFullPath(destFileName))
                    {
                        destFileName = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, destFileName);
                    }

                    if (!ExistsOnVCWithRetry(destFileName))
                    {
                        newCopyList.Add(e.Key, e.Value);
                    }
                }

                copyList = newCopyList;
            }

            if (copyList.Count != 0)
            {
                string header = string.Format(
    @"REFERENCE @""{0}/Microsoft.TTS.Offline.dll"";
RESOURCE  @""{1}/Microsoft.TTS.Offline.pdb"";
#DECLARE FakeDataPath string = @""{2}/fakedata.ss"";
FakeData = SSTREAM @FakeDataPath;",
                TmocGlobal.Instance.TmocBinaryRoot,
                TmocGlobal.Instance.TmocBinaryRoot,
                TmocGlobal.Instance.TmocBinaryRoot);
                StringBuilder script = new StringBuilder();
                script.AppendLine(header);
                script.AppendLine(string.Empty);
                script.AppendLine(string.Empty);

                /*
                RESOURCE @"/local/SpeechAM/devtest/file1";
                RESOURCE @"/local/SpeechAM/devtest/file2";
                flist = @" "file1", "file2" ";
                */

                StringBuilder args = new StringBuilder();
                var flist = new List<string>();
                int i = 0;
                foreach (var e in copyList)
                {
                    flist.Add(Path.GetFileName(e.Key));
                    args.AppendFormat(@"""{0}""", Path.GetFileName(e.Key));
                    if (++i != copyList.Count)
                    {
                        args.Append(",");
                    }

                    string srcFile = e.Key;
                    string resourceCode = string.Format(@"RESOURCE @""{0}"";", srcFile);
                    script.AppendLine(resourceCode);
                }

                script.AppendLine(string.Empty);
                script.AppendLine(string.Empty);

                // Do the copy 
                string copyCode = string.Format(
    @"DATA=REDUCE FakeData 
ON FakeKey 
USING AMOC.Scope.FileCopyReducer({0});", args.ToString());
                script.AppendLine(copyCode);
                script.AppendLine(string.Empty);
                script.AppendLine(string.Empty);

                // Do the output.
                /*SELECT * 
                FROM DATA 
                WHERE FileName == "file1"
                ORDER BY PartId;
                OUTPUT  TO @"/my/file1" USING FilePartOutputter;*/

                i = 0;
                foreach (var e in copyList)
                {
                    string dstFile = e.Value;
                    string outputCode = string.Format(
       @"SELECT * 
FROM DATA 
WHERE FileName == ""{0}""
ORDER BY PartId;
OUTPUT  TO @""{1}"" USING FilePartOutputter;", flist[i++], dstFile);
                    script.AppendLine(outputCode);
                    script.AppendLine(string.Empty);
                }

                // Do the output.
                string displayjobname = "CopyFilesOnVC";

                // In cosmos run, concatenate workroot with jobname to be a more meanful job name.
                if (!string.IsNullOrWhiteSpace(TmocGlobal.Instance.TmocWorkRoot) && TmocPath.IsCosmosPath(TmocGlobal.Instance.TmocWorkRoot))
                {
                    string taskName = TmocGlobal.Instance.TmocWorkRoot.Replace('/', '#').Trim('#');
                    displayjobname = taskName + "---" + displayjobname;
                }

                JobSubmitter jobrun = TmocGlobal.Instance.JobEngine;
                jobrun.SubmitAndWaitJob(script.ToString(), @"CopySmallBinaryFileOnVC", displayjobname, null, 0);
                foreach (var e in copyList)
                {
                    if (!TmocFile.ExistsOnVC(e.Value))
                    {
                        throw new Exception(string.Format("copy {0}failed", e.Key));
                    }
                }
            }
        }

        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is allowed.
        /// Extension of TmocFile.Copy, force to override readonly .
        /// </summary>
        /// <param name="sourceFileName">The file to copy.</param>
        /// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
        /// <param name="overwrite">True if the destination file can be overwritten; otherwise, false.</param>
        public static void CopyOnLocal(string sourceFileName, string destFileName, bool overwrite = true)
        {
            // avoid copy between same address
            if (Path.GetFullPath(sourceFileName) == Path.GetFullPath(destFileName))
            {
                return;
            }

            if (!overwrite)
            {
                if (!File.Exists(destFileName))
                {
                    TmocDirectory.CreateForFile(destFileName);
                    File.Copy(sourceFileName, destFileName, false);
                }
            }
            else
            {
                if (File.Exists(destFileName))
                {
                    RemoveReadOnlyAttr(destFileName);
                }

                TmocDirectory.CreateForFile(destFileName);
                File.Copy(sourceFileName, destFileName, true);
            }
        }

        /// <summary>
        /// Copy small binary file on local.
        /// </summary>
        /// <param name="copyList">The copy list.</param>
        /// <param name="overwrite">Overwrite.</param>
        public static void CopySmallBinaryFileOnLocal(
        Dictionary<string, string> copyList,
        bool overwrite = true)
        {
            foreach (var e in copyList)
            {
                CopyOnLocal(e.Key, e.Value, overwrite);
            }
        }

        /// <summary>
        /// CAUSITION** to use this.
        /// Copy with this on VC will be very slow, *10 min* for each file irrespective of the file size.
        /// General copy, will detect whether on local or on VC.
        /// If path start with http:// or / it will be a VC path.
        /// Otherwise a local path.
        /// </summary>
        /// <param name="sourceFileName">The file to copy.</param>
        /// <param name="destFileName">The name of the destination file. This cannot be a directory.</param>
        /// <param name="overwrite">True if the destination file can be overwritten; Otherwise, false.</param>
        /// <param name="isBinary">True if the sourceFileName is structured stream; Otherwise, false.</param>
        public static void Copy(string sourceFileName, string destFileName, bool overwrite = true, bool isBinary = true)
        {
            if (TmocPath.IsCosmosPath(sourceFileName) && TmocPath.IsCosmosPath(destFileName))
            {
                CopyOnVC(sourceFileName, destFileName, overwrite);
            }
            else if (TmocPath.IsCosmosPath(sourceFileName) && (!TmocPath.IsCosmosPath(destFileName)))
            {
                CopyCosmosToLocal(sourceFileName, destFileName, overwrite);
            }
            else if ((!TmocPath.IsCosmosPath(sourceFileName)) && TmocPath.IsCosmosPath(destFileName))
            {
                CopyLocalToCosmos(sourceFileName, destFileName, overwrite, isBinary);
            }
            else
            {
                CopyOnLocal(sourceFileName, destFileName, overwrite);
            }
        }

        /// <summary>
        /// Copy small binary files.
        /// **CAUSION TO USE CORRECTLY**
        /// It is faster to copy small files ~100M on VC.
        /// But only for the files used as RESOURCE files later.
        /// Do not use it to copy strutures stream or files to be used for extractors.
        /// Use it for model files and lexicon copy only.
        /// </summary>
        /// <param name="copyList">The copy list.</param>
        /// <param name="overwrite">Overwrite.</param>
        public static void CopySmallBinaryFile(Dictionary<string, string> copyList, bool overwrite = true)
        {
            bool bVC = false;
            foreach (var e in copyList)
            {
                if (TmocPath.IsCosmosPath(e.Key))
                {
                    bVC = true;
                    break;
                }
            }

            foreach (var e in copyList)
            {
                if (bVC != TmocPath.IsCosmosPath(e.Key))
                {
                    throw new Exception("All the files must be all local/vc path");
                }
            }

            if (bVC)
            {
                CopySmallBinaryFileOnVC(copyList, overwrite);
            }
            else
            {
                CopySmallBinaryFileOnLocal(copyList, overwrite);
            }
        }

        /// <summary>
        /// Copy local file into cosmos.
        /// </summary>
        /// <param name="sourceFile">Source files name.</param>
        /// <param name="destinationStream">Relative or full vc path of destination.</param>
        /// <param name="overwrite">Overwrite.</param>
        /// <param name="isBinary">Is binary.</param>
        public static void CopyLocalToCosmos(string sourceFile, string destinationStream, bool overwrite = true, bool isBinary = true)
        {
            if (!TmocPath.IsCosmosPath(destinationStream))
            {
                throw new Exception(string.Format("Destination stream {0} is either a relative or full vc path", destinationStream));
            }

            if (!TmocPath.IsCosmosFullPath(destinationStream))
            {
                destinationStream = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, destinationStream);
            }

            COSMOSHelper.UploadFile(sourceFile, destinationStream, TmocGlobal.Instance.VcConfig.VcProxy, overwrite, isBinary);
        }

        /// <summary>
        /// Copy from local file into cosmos.
        /// </summary>
        /// <param name="sourceStream">Relative or full vc path of source stream on cosmos.</param>
        /// <param name="destinationFile">The destination file.</param>
        /// <param name="overwrite">Overwrite.</param>
        public static void CopyCosmosToLocal(string sourceStream, string destinationFile, bool overwrite = true)
        {
            if (!TmocPath.IsCosmosPath(sourceStream))
            {
                throw new Exception(string.Format("Src stream {0} is either a relative or full vc path", sourceStream));
            }

            if (!TmocPath.IsCosmosFullPath(sourceStream))
            {
                sourceStream = TmocPath.Combine(TmocGlobal.Instance.VcConfig.VcName, sourceStream);
            }

            TmocDirectory.CreateForFile(destinationFile);
            COSMOSHelper.DownloadFile(sourceStream, destinationFile, TmocGlobal.Instance.VcConfig.VcProxy, overwrite);
        }

        /// <summary>
        /// Remove the read only attribute of the files.
        /// </summary>
        /// <param name="fname">The fname.</param>
        public static void RemoveReadOnlyAttr(string fname)
        {
            FileAttributes attributes = File.GetAttributes(fname);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                // make the file writable
                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                File.SetAttributes(fname, attributes);
            }
        }

        /// <summary>
        /// Determine whether input file is Ascii-format.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <returns>If the type is Microsoft.Tts.Offline.TMOC.TmocPath.FileFormatType.ASCII.</returns>
        public static bool IsAsciiFile(string fname)
        {
            Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType type = CheckAsciiUTF8File(fname);

            return type == Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.ASCII;
        }

        /// <summary>
        /// Determine whether input file is UTF8.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <returns>If the type is Microsoft.Tts.Offline.TMOC.TmocPath.FileFormatType.UTF8.</returns>
        public static bool IsUTF8File(string fname)
        {
            Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType type = CheckAsciiUTF8File(fname);

            return type == Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.UTF8;
        }

        /// <summary>
        /// Determine whether input file is ASCII and UTF8.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <returns>If the type is Microsoft.Tts.Offline.TMOC.TmocPath.FileFormatType.ASCII or UTF8.</returns>
        public static bool IsAsciiUTF8File(string fname)
        {
            Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType type = CheckAsciiUTF8File(fname);

            return type == Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.UTF8 || type == Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.ASCII;
        }

        /// <summary>
        /// Convert UTF8 to ASCII string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The string of text.</returns>
        public static string CvtUTF8toASCIIStr(string text)
        {
            Encoding utf8 = Encoding.UTF8;
            byte[] encodedBytes = utf8.GetBytes(text);
            byte[] convertedBytes = Encoding.Convert(Encoding.UTF8, Encoding.ASCII, encodedBytes);
            Encoding ascii = Encoding.ASCII;

            return ascii.GetString(convertedBytes);
        }

        /// <summary>
        /// Convert UTF8 to ASCII file.
        /// </summary>
        /// <param name="infname">The in fname.</param>
        /// <param name="outfname">The out fname.</param>
        /// <param name="outputUnixFormat">The output unix format.</param>
        public static void CvtUTF8toASCIIFile(string infname, string outfname, bool outputUnixFormat = false)
        {
            List<string> outStr = new List<string>();
            using (StreamReader sr = new StreamReader(infname, Encoding.GetEncoding(TmocConstants.Encode_UTF8)))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    outStr.Add(CvtUTF8toASCIIStr(line));
                }
            }

            TmocDirectory.CreateForFile(outfname);
            if (TmocFile.Exists(outfname))
            {
                TmocFile.RemoveReadOnlyAttr(outfname);
            }

            using (StreamWriter sw = new StreamWriter(outfname, false, Encoding.GetEncoding(TmocConstants.Encode_ASCII)))
            {
                if (outputUnixFormat)
                {
                    sw.NewLine = "\n";
                }

                foreach (string item in outStr)
                {
                    sw.WriteLine(item);
                }
            }
        }

        private static void ConcatenateWithRetry(string sourceFileName, string destFileName, int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            RetryClass.Retry(new Action<string, string>(VC.Concatenate), new object[] { sourceFileName, destFileName },
                maxRetry, TmocConstants.VcOperationRetryDelay);

            // The Concatenate can be very slow.
            int sleepMilliSecondSleepOut = 1000;
            while (IsExistedOrIncompleteVCStream(destFileName))
            {
                Thread.Sleep(sleepMilliSecondSleepOut);
            }
        }

        private static void DeleteOnVCWithRetry(string fname, int maxRetries = TmocConstants.VcOperationRetryTime)
        {
            // We had while here - which could be reason why the software was stuck... removing this while.
            RetryClass.Retry(new Action<string>(VC.Delete), new object[] { fname }, maxRetries, TmocConstants.VcOperationRetryDelay);
        }

        /// <summary>
        /// Delete a file on local drive.
        /// </summary>
        /// <param name="fname">The fname.</param>
        private static void DeleteOnLocal(string fname)
        {
            if (File.Exists(fname))
            {
                RemoveReadOnlyAttr(fname);
                File.Delete(fname);
            }
        }

        /// <summary>
        /// Exists with retries.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <param name="maxRetries">The max retries.</param>
        /// <returns>If the file is Exists.</returns>
        private static bool ExistsOnVCWithRetry(string fname, int maxRetries = TmocConstants.VcOperationRetryTime)
        {
            return (bool)RetryClass.Retry(new Func<string, bool, bool>(VC.StreamExists), new object[] { fname, false },
                maxRetries, TmocConstants.VcOperationRetryDelay);
        }

        private static bool ExistsOnLocal(string fname)
        {
            return File.Exists(fname);
        }

        /// <summary>
        /// Determine whether input stream is ASCII or UTF8-format.
        /// only content instead of file format matters.
        /// UTF8/ASCII/UNKONWN.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <returns>File format type.</returns>
        private static Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType CheckAsciiUTF8File(string fname)
        {
            Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType type = Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.UNKOWN;

            string content = File.ReadAllText(fname, Encoding.GetEncoding(TmocConstants.Encode_UTF8));
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            type = IsUTF8String(buffer);

            return type;
        }

        /// <summary>
        /// Determine whether input stream is ASCII or UTF8-format.
        /// UTF8/ASCII/UNKONWN.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>File format type.</returns>
        private static Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType IsUTF8String(byte[] str)
        {
            int i = 0;
            int nBytes = 0; // possibly, uft8 spans 1~6 bytes. 
            byte chr = 0;
            bool bAllAscii = true;

            while (i < str.Length)
            {
                chr = str[i];
                if ((chr & 0x80) != 0)
                {
                    bAllAscii = false;
                }

                if (nBytes == 0)
                {
                    if ((chr & 0x80) != 0)
                    {
                        while ((chr & 0x80) != 0)
                        {
                            chr <<= 1;
                            nBytes++;
                        }

                        if (nBytes < 2 || nBytes > 6)
                        {
                            return Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.UNKOWN;
                        }

                        // first byte should be equal as/greater than 110x xxxx .
                        nBytes--; // exclusive of current byte in question .
                    }
                }
                else
                {
                    // multipe-byte words exclusive of previous ones
                    if ((chr & 0xc0) != 0x80)
                    {
                        return Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.UNKOWN;   // reminder should be in 10xx xxxx format.
                    }

                    nBytes--;
                }

                ++i;
            }

            if (bAllAscii)
            {
                return Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.ASCII;
            }

            return (nBytes == 0) ? Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.UTF8 : Microsoft.Tts.Cosmos.TMOC.TmocPath.FileFormatType.UNKOWN;
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        /// <summary>
        /// To see if the stream is complete or not.
        /// </summary>
        /// <param name="fname">The fname.</param>
        /// <param name="maxRetry">The max retyies.</param>
        /// <returns>Is it in complete VC stream.</returns>
        private static bool IsInCompleteVCStream(string fname, int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            object exists = RetryClass.Retry(new Func<string, bool, bool>(VC.StreamExists), new object[] { fname, false },
                maxRetry, TmocConstants.VcOperationRetryDelay);

            if (exists is int && (int)exists == -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Set expriation date.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <param name="expireIn">The expire in.</param>
        /// <param name="maxRetry">The max retyies.</param>
        private static void ExpireInWithRetry(string fileName, TimeSpan expireIn,
                                              int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            RetryClass.Retry(new Action<string, TimeSpan>(VC.SetStreamExpirationTime),
                new object[] { fileName, expireIn }, maxRetry, TmocConstants.VcOperationRetryDelay);
        }
    }
}
