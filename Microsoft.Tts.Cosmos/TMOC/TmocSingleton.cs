namespace Microsoft.Tts.Cosmos.TMOC
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Xml.Linq;
    using Microsoft.Tts.Cosmos.Config;
    using Microsoft.Tts.Offline.Utility;
    using ScopeClient;
    using VcClient;

    /// <summary>
    /// A list of vc.
    /// </summary>
    public class VCList
    {
        private const string VclistName = "tmoc.vclist.xml";
        private const string VclistPath = ".\\Config\\vclist.xml";

        /// <summary>
        /// Get the token number of the given VC.
        /// </summary>
        /// <param name="nameVC">The VC name.</param>
        /// <returns>Token number.</returns>
        public int GetVcTokeNum(string nameVC)
        {
            Helper.EnsureFolderExistForFile(VclistPath);
            if (!File.Exists(VclistPath))
            {
                Stream stream = null;
                try
                {
                    stream = System.Reflection.Assembly.GetCallingAssembly().GetManifestResourceStream(VclistName);
                    using (Stream vclistSr = File.Create(VclistPath))
                    {
                        stream.CopyTo(vclistSr);
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }

            Helper.ThrowIfFileNotExist(VclistPath);
            var doc = XDocument.Load(VclistPath);
            Dictionary<string, string> map = doc.Root.Elements("VC").ToDictionary(
            x => x.Attribute("name").Value.ToLower(),
            x => x.Attribute("tokennum").Value);

            string tokenNum;
            if (!map.TryGetValue(nameVC.ToLower(), out tokenNum))
            {
                throw new KeyNotFoundException(string.Format(
                    "Can't find information for VC {0}, you should first add information in {1}",
                    nameVC, Path.GetFullPath(VclistPath)));
            }
            else
            {
                return Convert.ToInt32(tokenNum);
            }
        }
    }

    /// <summary>
    /// The class of job submitter.
    /// </summary>
    public class JobSubmitter
    {
        /// <summary>
        /// The possible cosmos system errors .
        /// Will add more if we encounter more.
        /// </summary>
        private static string[] cosmosUserError = new string[] 
        {
            "Vertex user code error"
        };

        private TmocVcConfig configVC;

        /// <summary>
        /// The directory to store generated job.
        /// </summary>
        private string tmpLocalDir;

        private COSMOSHelper cosmosObj;
        private LocalrunENV localrunenv;

        /// <summary>
        ///  Initializes a new instance of the <see cref="JobSubmitter" /> class. Initialize the job summit for local run mode.
        /// </summary>
        /// <param name="tmpLocalDir">The local directory for saving the scope scrips temporarily.</param>
        public JobSubmitter(string tmpLocalDir = @".\")
        {
            this.tmpLocalDir = tmpLocalDir;

            var env = ScopeEnvironment.Instance;
            localrunenv.WorkingRoot = tmpLocalDir;
            localrunenv.ScopePath = env.ScopePath;
            localrunenv.TmpDirectory = tmpLocalDir;
            localrunenv.InputStreamPath = env.InputStreamPath;
            localrunenv.OutputStreamPath = env.OutputStreamPath;
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="JobSubmitter" /> class. Intialize the job submmit for runing in cosmos.
        /// </summary>
        /// <param name="configVC">The vc to use.</param>
        /// <param name="tmpLocalDir">The tmp dir to store the tmp generated scripts.</param>
        public JobSubmitter(TmocVcConfig configVC, string tmpLocalDir = @".\")
        {
            this.tmpLocalDir = tmpLocalDir;
            this.configVC = configVC;
            cosmosObj = new COSMOSHelper(configVC);
        }

        /// <summary>
        /// Whether the error msg is a COSMOS system error or not.
        /// </summary>
        /// <param name="errorMsg">Error message.</param>
        /// <returns>If the errorMsg contains cosmosUserError.</returns>
        public static bool IsUserError(string errorMsg)
        {
            foreach (var msg in cosmosUserError)
            {
                if (errorMsg.Contains(msg))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether the job failure is caused by user input error exception.
        /// </summary>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>True.</returns>
        public static bool IsUserInputErrorException(string errorMsg)
        {
            return true;
        }

        /// <summary>
        /// Settings that control scope environment for script compilation and execution.
        /// </summary>
        /// <param name="workingRoot">Root directory for script cache folders and temporary folders for script execution. </param>
        /// <param name="scopePath">This directory is used as a base directory for relative resouce and reference files (local execution only). </param>
        /// <param name="tmpDirectory">Directory for storing temporary files created by Scope editor or API methods. </param>
        /// <param name="inputStreamPath">This directory is used as a base directory for relative streams in EXTRACT command (local execution only). </param>
        /// <param name="outputStreamPath">This directory is used as a base directory for relative streams in OUTPUT command (local execution only). </param>
        public void SetLocalRunEnv(string workingRoot,
            string scopePath = null,
            string tmpDirectory = null,
            string inputStreamPath = null,
            string outputStreamPath = null)
        {
            if (!string.IsNullOrEmpty(workingRoot))
            {
                localrunenv.WorkingRoot = workingRoot;
            }

            if (!string.IsNullOrEmpty(scopePath))
            {
                localrunenv.ScopePath = scopePath;
            }

            if (!string.IsNullOrEmpty(tmpDirectory))
            {
                localrunenv.TmpDirectory = tmpDirectory;
            }

            if (!string.IsNullOrEmpty(inputStreamPath))
            {
                localrunenv.InputStreamPath = inputStreamPath;
            }

            if (!string.IsNullOrEmpty(outputStreamPath))
            {
                localrunenv.OutputStreamPath = outputStreamPath;
            }
        }

        /// <summary>
        /// Run the job and block until finish.
        /// </summary>
        /// <param name="jobscripts">The local path of the scritps.</param>
        /// <param name="jobName">The name for saving scripts file and loging.</param>
        /// <param name="jobDisplayName">The friendly display name on Cosmos.</param>
        /// <param name="scriptBackupDir">Script backup dir.</param>
        /// <param name="nebularArguments">The nebularArguments such as "-ExtractGroupDefaultDataSize 150000".</param>
        /// <param name="isLocalRun">Is local run.</param>
        /// <param name="maxUnavailability">The max unavailability.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="wait">Wait.</param>
        /// <returns>If it is finish.</returns>
        public string SubmitAndWaitJob(string jobscripts, string jobName, string jobDisplayName,
                                       string scriptBackupDir, int nebularArguments, bool? isLocalRun = null,
                                        int maxUnavailability = 0, int tokens = 0, bool wait = true)
        {
            // build scripts file
            string scriptsfn = BuildJobScripts(jobscripts, jobName, scriptBackupDir);
            bool isLocalRunFlag = isLocalRun.HasValue ? isLocalRun.Value : configVC.LocalRun;

            // run job on cosmos or local
            if (isLocalRunFlag)
            {
                return RunJobLocal(scriptsfn, jobName);
            }
            else
            {
                return RunJobOnVC(scriptsfn, jobDisplayName, nebularArguments, maxUnavailability, tokens, wait);
            }
        }

        private string MakeArgsFromScopeEnv()
        {
            StringBuilder args = new StringBuilder();
            if (!string.IsNullOrEmpty(localrunenv.WorkingRoot))
            {
                args.AppendFormat(" -workingRoot {0}", TmocPath.GetPathForCommandline(localrunenv.WorkingRoot));
            }

            if (!string.IsNullOrEmpty(localrunenv.ScopePath))
            {
                args.AppendFormat(" -SCOPE_PATH {0}", TmocPath.GetPathForCommandline(localrunenv.ScopePath));
            }

            if (!string.IsNullOrEmpty(localrunenv.TmpDirectory))
            {
                args.AppendFormat(" -tempdir {0}", TmocPath.GetPathForCommandline(localrunenv.TmpDirectory));
            }

            if (!string.IsNullOrEmpty(localrunenv.InputStreamPath))
            {
                args.AppendFormat(" -INPUT_PATH {0}", TmocPath.GetPathForCommandline(localrunenv.InputStreamPath));
            }

            if (!string.IsNullOrEmpty(localrunenv.OutputStreamPath))
            {
                args.AppendFormat(" -OUTPUT_PATH {0}", TmocPath.GetPathForCommandline(localrunenv.OutputStreamPath));
            }

            return args.ToString();
        }

        /// <summary>
        /// Save the scripts to a file for job submitting. and upload this file to workfolder for backup.
        /// </summary>
        /// <param name="jobscriptsStr">Job scripts string.</param>
        /// <param name="jobName">Job name.</param>
        /// <param name="scriptBackupDir">The dir to store the scripts as a backup.</param>
        /// <returns>Script file.</returns>
        private string BuildJobScripts(string jobscriptsStr, string jobName, string scriptBackupDir)
        {
            string scriptsBuf = jobscriptsStr;

            // Write the script as a temp file
            string scriptsFile = Path.Combine(tmpLocalDir, jobName + ".script");   // simply write to the current dir, should be a temp dir

            TmocFile.Delete(scriptsFile); // clear first 

            TmocDirectory.CreateForFile(scriptsFile);
            File.WriteAllText(scriptsFile, scriptsBuf);

            BackupJobScriptFile(scriptsFile, jobName, scriptBackupDir);

            return scriptsFile;
        }

        private void BackupJobScriptFile(string scriptsFile, string jobName, string scriptBackupDir)
        {
            try
            {
                if (!string.IsNullOrEmpty(scriptBackupDir))
                {
                    if (configVC.LocalRun)
                    {
                        TmocDirectory.CreateForFile(scriptBackupDir);
                        string dst = scriptBackupDir + "\\" + jobName + ".script";
                        TmocFile.CopyOnLocal(scriptsFile, dst, overwrite: true);
                    }
                    else
                    {
                        string dstPathName = TmocPath.RelativeToFullVCPath(TmocPath.Combine(scriptBackupDir, jobName + ".script"));
                        TmocFile.Copy(scriptsFile, dstPathName, true);
                    }
                }
            }
            catch
            {
                // not critical issue, do not interrupt the training flow. just warning
                throw;
            }
        }

        /// <summary>
        /// Run the job locally.
        /// </summary>
        /// <param name="scriptsfn">The script fn.</param>
        /// <param name="jobName">The job name.</param>
        /// <returns>Success or failed.</returns>
        private string RunJobLocal(string scriptsfn, string jobName)
        {
            // Complile the scope script firstly.
            string args = " compile -i " + TmocPath.GetPathForCommandline(scriptsfn);
            args += MakeArgsFromScopeEnv();

            string workingDir = Path.Combine(TmocGlobal.Instance.TmocWorkRoot, "ScopeSDK");
            string toolName = Path.Combine(workingDir, TmocConstants.Scope);
            var isSuccess = CommandLine.RunCommand(toolName, args, workingDir);

            // Run the scope script then.
            if (isSuccess)
            {
                args = "run -i " + TmocPath.GetPathForCommandline(scriptsfn);
                args += MakeArgsFromScopeEnv();
                isSuccess = CommandLine.RunCommand(toolName, args, TmocGlobal.Instance.TmocWorkRoot);
            }

            return isSuccess ? "Success" : "Fail";
        }

        /// <summary>
        /// Run Job on VC cluster.
        /// </summary>
        /// <param name="scriptsfn">Scope scripts file.</param>
        /// <param name="jobname">The job name.</param>
        /// <param name="nebulaArgs">The nebula args.</param>
        /// <param name="maxUnavailability">The max unavailability.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="wait">If wait.</param>
        /// <returns>Job guide.</returns>
        private string RunJobOnVC(string scriptsfn, string jobname, int nebulaArgs = 0, int maxUnavailability = 0, int tokens = 0, bool wait = true)
        {
            Guid? jobGuid = null;
            TimeSpan waitDelay = new TimeSpan(0, 0, 10);        // 30 seconds.
            bool jobResult = false;
            VcClient.JobInfo jobInfo = null;
            int nTry = TmocConstants.VcJobRetryTime;
            int tryCnt = 0;
            while (tryCnt < nTry - 1)
            {
                string logmsg = string.Empty;
                if (tryCnt == 0)
                {
                    logmsg = string.Format("Submit job {0} to VC {1}, the jobname is {2}", scriptsfn, configVC.VcName, jobname);
                }
                else
                {
                    logmsg = string.Format("Retry {0}th times to Submit job {1} to VC {2}, the jobname is {3}", tryCnt, scriptsfn, configVC.VcName, jobname);
                }

                jobGuid = cosmosObj.Submit(TmocGlobal.Instance.TmocWorkRoot, tmpLocalDir, jobname, tokens, scriptsfn, nebulaArgs, maxUnavailability);

                if (jobGuid != null)
                {
                    // Record Cosmos running job.
                    TmocGlobal.Instance.RunningJobsSet.Add(jobGuid.ToString());

                    if (!wait)
                    {
                        break;
                    }

                    Thread.Sleep(TmocConstants.VcFirstJobInfoCheckDelayAfterSubmit);

                    jobResult = cosmosObj.WaitForJobCompletion(jobGuid.Value, waitDelay, out jobInfo);

                    // Success.
                    if (jobResult)
                    {
                        // Remove Cosmos job.
                        TmocGlobal.Instance.RunningJobsSet.Remove(jobGuid.ToString());
                        break;
                    }
                    else if (!jobResult && jobInfo.State == JobInfo.JobState.CompletedFailure && !IsUserError(jobInfo.Error))
                    {
                        // if job is failed not due to user error, wait for 1 minute and retry.
                        // Wait for 1 minute.
                        System.Threading.Thread.Sleep(60000);
                        tryCnt++;
                        continue;
                    }
                    else
                    {
                        // Job was cancelled, don't continue.
                        // Remove Cosmos job.
                        TmocGlobal.Instance.RunningJobsSet.Remove(jobGuid.ToString());
                        break;
                    }
                }
            }

            if (jobGuid == null)
            {
                throw new VcClientExceptions.JobSubmissionException(string.Format("Unable to submit the job to {0}. This could be because of the network issues between your machine and the Cosmos.", configVC.VcName));
            }
            else if (!jobResult && wait)
            {
                string mesg = string.Format("job {0} failed, the error is \n{1}", jobname, jobInfo.Error);

                if (IsUserInputErrorException(jobInfo.Error))
                {
                    throw new Exception(mesg);
                }
                else if (!IsUserError(jobInfo.Error))
                {
                    throw new CosmosException(mesg);
                }
                else
                {
                    throw new ApplicationException(mesg);
                }
            }

            return jobGuid.ToString();
        }

        private struct LocalrunENV
        {
            /// <summary>
            /// Working root.
            /// </summary>
            public string WorkingRoot;

            /// <summary>
            /// The path of scope.
            /// </summary>
            public string ScopePath;

            /// <summary>
            /// Temporary directory.
            /// </summary>
            public string TmpDirectory;

            /// <summary>
            /// The path of input stream.
            /// </summary>
            public string InputStreamPath;

            /// <summary>
            /// The path of output stream.
            /// </summary>
            public string OutputStreamPath;
        }
    }

    /// <summary>
    /// Tmoc global instance.
    /// </summary>
    internal class TmocGlobal
    {
        private static TmocGlobal _instance = null;
        private static bool _isInitalized = false;

        #region private field

        private TmocVcConfig configVC = null;
        private JobSubmitter _jobSubmitter = null;
        private string _tmocPackageRoot = string.Empty;
        private string _tmocBinaryRoot = string.Empty;
        private string _localTempWorkingDir = string.Empty;
        private int percentAllocationVC = 0;
        private bool _isAutoSetTokenNum = false;
        private string _tmocWorkRoot = string.Empty;
        private HashSet<string> _runningJobsSet;

        #endregion

        private TmocGlobal()
        {
            if (string.IsNullOrEmpty(_localTempWorkingDir))
            {
                _localTempWorkingDir = MakeLocalTempWorkDirectory();
            }

            _runningJobsSet = new HashSet<string>();
        }

        /// <summary>
        /// Gets the instance of TMOC.
        /// </summary>
        public static TmocGlobal Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TmocGlobal();
                }

                return _instance;
            }
        }

        #region property field
        /// <summary>
        /// Gets the property of vc config.
        /// </summary>
        public TmocVcConfig VcConfig
        {
            get
            {
                return configVC;
            }
        }

        /// <summary>
        /// Gets the working Directory.
        /// </summary>
        public string TmocWorkRoot
        {
            get
            {
                return _tmocWorkRoot;
            }
        }

        /// <summary>
        /// Gets the job submitter engine.
        /// </summary>
        public JobSubmitter JobEngine
        {
            get
            {
                return _jobSubmitter;
            }
        }

        /// <summary>
        /// Gets the Tmoc package root.
        /// </summary>
        public string TmocPackageRoot
        {
            get
            {
                return _tmocPackageRoot;
            }
        }

        /// <summary>
        /// Gets the Tmoc binary root.
        /// </summary>
        public string TmocBinaryRoot
        {
            get
            {
                return _tmocBinaryRoot;
            }
        }

        /// <summary>
        /// Gets the local temp directory.
        /// </summary>
        public string LocalTempWorkingDir
        {
            get
            {
                return _localTempWorkingDir;
            }
        }

        /// <summary>
        /// Gets VC percent allocation. An integer number between 1 and 100.
        /// Will be calculated from the maxTokenNum. 
        /// VCPercentAllocation = maxTokenNum/totalToken * 100%.
        /// </summary>
        public int VcPercentAllocation
        {
            get
            {
                return percentAllocationVC;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the core number will determined automatically. If true, maxTokenNum will the VCConfig.DefaultMaxToken first.Then reset to a value automatically caculated from the duration of the speech corpus.
        /// </summary>
        public bool IsAutoSetTokenNum
        {
            get
            {
                return _isAutoSetTokenNum;
            }
        }

        /// <summary>
        /// Gets record running/queuing jobs in Cosmos.
        /// </summary>
        public HashSet<string> RunningJobsSet
        {
            get
            {
                return _runningJobsSet;
            }
        }

        #endregion

        /// <summary>
        /// Decide Vc percentage.
        /// </summary>
        /// <param name="totalSpeechDuration">Total speech duration.</param>
        public void DecideVcPercetage(double totalSpeechDuration)
        {
            if (!_isAutoSetTokenNum)
            {
                var totalToken = GetTokenNum();

                // Deal with auto-set token number and reset the parameters.
                var maxTokenNum = TmocVcConfig.GetRecommendedTokenNum(configVC.VcName, totalSpeechDuration);

                // Calculate the vc percentage from the machine number.
                percentAllocationVC = Math.Max(1, Math.Min(100, Convert.ToInt32(100 * (Convert.ToDouble(maxTokenNum) / Convert.ToDouble(totalToken)))));
            }
        }

        /// <summary>
        /// Load the setting from the config.xml.
        /// </summary>
        /// <param name="inputXml">The input xml.</param>
        /// <param name="workingDir">The working directory.</param>
        public void Initalize(string inputXml, string workingDir)
        {
            if (_isInitalized)
            {
                return;
            }

            _isInitalized = true;
            _tmocWorkRoot = workingDir;
            string packageRoot = string.Empty;

            // Inital TMOC config file.
            configVC = new TmocVcConfig(inputXml);

            // Make temporaray folder.
            if (string.IsNullOrEmpty(_localTempWorkingDir))
            {
                _localTempWorkingDir = MakeLocalTempWorkDirectory();
            }

            string runningLog = TmocPath.Combine(_localTempWorkingDir, TmocConstants.RunningLog);

            if (TmocFile.Exists(runningLog))
            {
                TmocFile.Delete(runningLog);
            }

            // Load VC token number to calculate the VC percentage.
            var totalToken = GetTokenNum();

            // Calculate the vc percentage from the machine number.
            percentAllocationVC = Math.Max(1, Math.Min(100, Convert.ToInt32(100 * (Convert.ToDouble(configVC.MaxTokenNum) / Convert.ToDouble(totalToken)))));
            _tmocPackageRoot = configVC.TmocDataPath;
            if (string.IsNullOrEmpty(_localTempWorkingDir))
            {
                _localTempWorkingDir = MakeLocalTempWorkDirectory();
            }

            // Initialize the local run or cosmos run.
            if (string.IsNullOrEmpty(configVC.VcName))
            {
                _tmocBinaryRoot = Path.Combine(packageRoot, "bin");
                _jobSubmitter = new JobSubmitter(_localTempWorkingDir);
            }
            else
            {
                if (TmocPath.IsCosmosPath(configVC.VcName))
                {
                    _tmocBinaryRoot = TmocPath.Combine(packageRoot, "bin");
                    _jobSubmitter = new JobSubmitter(configVC, _localTempWorkingDir);
                }
                else
                {
                    throw new ArgumentException(string.Format("vcname must start with http://, but it is {0}", configVC.VcName));
                }
            }
        }

        /// <summary>
        /// Clean up running job.
        /// </summary>
        public void CleanupRunningJob()
        {
            foreach (string jobid in _runningJobsSet)
            {
                COSMOSHelper.DeleteJob(jobid);
            }
        }

        private int GetTokenNum()
        {
            // A random number.
            int totalToken = 10;

            if (!string.IsNullOrEmpty(configVC.VcName))
            {
                VCList vclist = new VCList();
                totalToken = vclist.GetVcTokeNum(configVC.VcName);
            }

            return totalToken;
        }

        /// <summary>
        /// Make the local temporary directory for this training. It makes a runxxx in the current direcoty and make sure it is not used before.
        /// </summary>
        /// <returns>The full path of tmp work directory.</returns>
        private string MakeLocalTempWorkDirectory()
        {
            string tmpworkdir;
            for (int i = 0;; i++)
            {
                tmpworkdir = string.Format("run{0:D3}", i);
                if (!Directory.Exists(tmpworkdir))
                {
                    break;
                }
            }

            Directory.CreateDirectory(tmpworkdir);

            return Path.GetFullPath(tmpworkdir);
        }
    }
}
