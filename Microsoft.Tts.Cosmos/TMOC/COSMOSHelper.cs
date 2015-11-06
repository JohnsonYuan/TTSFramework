namespace Microsoft.Tts.Cosmos.TMOC
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security;
    using System.Text;
    using System.Threading;
    using Microsoft.Tts.Cosmos.Config;
    using Microsoft.Tts.Offline.Utility;
    using VcClient;

    /// <summary>
    /// The status of cosmos job.
    /// </summary>
    public struct CosmosJobStat
    {
        /// <summary>
        /// Wait time.
        /// </summary>
        public TimeSpan WaitTime;

        /// <summary>
        /// Run time.
        /// </summary>
        public TimeSpan RunTime;
    }

    /// <summary>
    /// Definition of all the hard coded variables.
    /// Note they are case-sensitive.
    /// </summary>
    public static class TmocConstants
    {
        /// <summary>
        /// Version.
        /// </summary>
        public const string Version = "1.0";

        #region Encode schema.

        /// <summary>
        /// UTF8.
        /// </summary>
        public const string Encode_UTF8 = @"utf-8";

        /// <summary>
        /// ASCII.
        /// </summary>
        public const string Encode_ASCII = @"Windows-1252";

        #endregion

        #region Scope toolkit.

        /// <summary>
        /// Scope tool.
        /// </summary>
        public const string Scope = @"Scope.exe";

        /// <summary>
        /// The config file of the scope.
        /// </summary>
        public const string ScopeConfig = @"scope.exe.config";

        /// <summary>
        /// The version of scope tool.
        /// </summary>
        public const string ScopeVersion = @"version_local.txt";

        #endregion

        /// <summary>
        /// Tsv format of training corpus.
        /// </summary>
        public const string TrainCorpusSetStream = @"trainset.tsv";

        /// <summary>
        /// The name of the strutured stream all the training set.
        /// </summary>
        public const string TrainCorpusSetSStream = @"trainset.ss";

        /// <summary>
        /// The name of the strutured stream of all the hypfiles uion together.
        /// </summary>
        public const string TrainHypFileSStream = @"trainhypfile.ss";

        /// <summary>
        /// Running log and summary files.
        /// </summary>
        public const string RunningLog = "TMOC.Running.log";

        /// <summary>
        /// The Tmoc libaray dll.
        /// </summary>
        public const string Tmoclibdll = @"Microsoft.TTS.Offline.dll";

        /// <summary>
        /// When training is done, done file is created.
        /// </summary>
        public const string DoneFileName = "done";

        /// <summary>
        /// The real time factor of alignment generation, in this context, lattice stream is available.
        /// </summary>
        public const double AlignmentRTF = 0.5;

        /// <summary>
        /// The real time factor of modeltrain.
        /// Actually, for fmpe/bmmi/mpe, it is about 0.15, for ml, it is about 0.08.
        /// </summary>
        public const double ModelTrainRTF = 0.1;

        /// <summary>
        /// Retry operation for vc operation.
        /// </summary>
        public const int VcOperationRetryTime = 10;

        /// <summary>
        /// Retry time for cosmos job failure due to cosmos system error.
        /// </summary>
        public const int VcJobRetryTime = 3;

        /// <summary>
        /// MaxUnavailability is the percentage of the job that can bypassed if unavailable without problem.
        /// The default for this parameter is 100, but we recommend specifying 0 unless you specifically want.
        /// The job to produce some results even if it could not access all inputs and you are prepared for.
        /// Potentially incomplete, biased and irreproducable outputs.
        /// </summary>
        public const int MaxUnavailability = 3;

        /// <summary>
        /// Delay between retries (millisecond).
        /// </summary>
        public const int VcOperationRetryDelay = 30000;

        /// <summary>
        /// Delay between submit and first JobInfoCheck (millisecond).
        /// </summary>
        public const int VcFirstJobInfoCheckDelayAfterSubmit = 30000;

        /// <summary>
        /// Expriation time for large files stored in cosmos.
        /// 90 days.
        /// </summary>
        public static readonly TimeSpan ExpriationTime = new TimeSpan(90, 0, 0, 0);

        /// <summary>
        /// Overall Job for information about vertices for the jobs.
        /// </summary>
        public const string OverAllJob = "Overall Job";
    }

    /// <summary>
    /// Retry class to invoke the job many times.
    /// </summary>
    public static class RetryClass
    {
        /// <summary>
        /// Retry method.
        /// </summary>
        /// <param name="function">Function.</param>
        /// <param name="inputs">Inputs.</param>
        /// <param name="maxNumTries">Max num tries.</param>
        /// <param name="sleepTime">Sleep time.</param>
        /// <param name="specialCases">Special cases.</param>
        /// <param name="specialCaseReturn">Special case return.</param>
        /// <returns>Object.</returns>
        public static object Retry(Delegate function, object[] inputs, int maxNumTries, int sleepTime,
            string[] specialCases = null, object specialCaseReturn = null)
        {
            StringBuilder execeptionLog = new StringBuilder();
            int numTries = 0;
            object ret = null;

            while (true)
            {
                try
                {
                    // Have to use dynamic invoke because of reflection from VCClient.
                    ret = function.DynamicInvoke(inputs);
                    break;
                }
                catch (Exception exception)
                {
                    // because of dynamic invoke, we look inside of exception.
                    exception = exception.InnerException;

                    if (inputs != null)
                    {
                        foreach (var input in inputs)
                        {
                            execeptionLog.Append(input);
                            execeptionLog.Append("\n");
                        }

                        execeptionLog.Append(")");
                    }

                    numTries++;

                    if (specialCases != null)
                    {
                        foreach (string specialCase in specialCases)
                        {
                            if (exception.GetType().Name.Contains(specialCase) ||
                                exception.ToString().Contains(specialCase) ||
                                exception.Message.Contains(specialCase))
                            {
                                return specialCaseReturn;
                            }
                        }
                    }

                    // If the number of retries is over the specific number.
                    if (numTries >= maxNumTries)
                    {
                        throw new ArgumentException("Cosmos Operation Retry failed", exception);
                    }
                }

                Thread.Sleep(sleepTime);
            }

            return ret;
        }
    }

    /// <summary>
    /// Exception class for cosmos errors.
    /// </summary>
    [Serializable]
    public class CosmosException : ApplicationException
    {
        /// <summary>
        ///  Initializes a new instance of the <see cref="CosmosException" /> class. Default constructor.
        /// </summary>
        public CosmosException()
            : base()
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="CosmosException" /> class. The constuctor.
        /// </summary>
        /// <param name="message">Mesage.</param>
        public CosmosException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="CosmosException" /> class. The constructor.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguements.</param>
        public CosmosException(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="CosmosException" /> class. The constructor.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner.</param>
        public CosmosException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="CosmosException" /> class. The constuctor.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="inner">Inner.</param>
        /// <param name="args">Arguements.</param>
        public CosmosException(string format, Exception inner, params object[] args)
            : base(string.Format(format, args), inner)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="CosmosException" /> class. The constructor.
        /// </summary>
        /// <param name="info">Information.</param>
        /// <param name="context">Context.</param>
        protected CosmosException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// COSMOS helper function which is to interact with COSMOS.
    /// </summary>   
    public class COSMOSHelper
    {
        /// <summary>
        /// Retry operation for vc operation.
        /// </summary>
        private const int OperationRetryTimeVC = 10;

        private TmocVcConfig configVC = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="COSMOSHelper" /> class. Default cosmos constructor.
        /// </summary>
        public COSMOSHelper()
        {
            configVC = new TmocVcConfig();
            if (configVC.VcProxy == VC.NoProxy)
            {
                VC.Setup(configVC.VcName, VC.NoProxy, null);
            }
            else
            {
                VC.Setup(configVC.VcName, configVC.VcProxy, null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="COSMOSHelper" /> class. COSMOS constructor which has input parameter.
        /// </summary>
        /// <param name="config">The COSMOS config file.</param>
        public COSMOSHelper(TmocVcConfig config)
            : base()
        {
            if (config == null)
            {
                throw new ArgumentNullException("The config of TMOC is null");
            }

            configVC = config;

            if (string.IsNullOrWhiteSpace(configVC.VcProxy))
            {
                VC.Setup(configVC.VcName, null, null);
            }
            else if (configVC.VcProxy.ToLower().Contains("no_vcProxy"))
            {
                VC.Setup(configVC.VcName, VC.NoProxy, null);
            }
            else
            {
                VC.Setup(configVC.VcName, configVC.VcProxy, null);
            }
        }

        /// <summary>
        /// Get basic job statistics specified by jobId.
        /// </summary>
        /// <param name="jobId">Job id.</param>
        /// <returns>Cosmos job status.</returns>
        public static CosmosJobStat GetJobStatistics(string jobId)
        {
            JobInfo jobinfo = GetJobInfo(Guid.Parse(jobId));

            DateTime submitTime = (DateTime)jobinfo.SubmitTime;
            DateTime startTime = (DateTime)jobinfo.StartTime;
            DateTime endTime = (DateTime)jobinfo.EndTime;

            TimeSpan waitTime = startTime - submitTime;
            TimeSpan runTime = endTime - startTime;

            CosmosJobStat jobStat;
            jobStat.RunTime = runTime;
            jobStat.WaitTime = waitTime;
            return jobStat;
        }

        /// <summary>
        /// Get job information.
        /// </summary>
        /// <param name="jobGuid">The jGuid.</param>
        /// <param name="delay">The delay.</param>
        /// <returns>Cosmos job information.</returns>
        [CLSCompliant(false)]
        public static JobInfo GetJobInfo(Guid jobGuid, int delay = TmocConstants.VcOperationRetryDelay)
        {
            return (JobInfo)RetryClass.Retry(new Func<Guid, bool, JobInfo>(VC.GetJobInfo),
                new object[] { jobGuid, true }, TmocConstants.VcOperationRetryTime, TmocConstants.VcOperationRetryDelay);
        }

        /// <summary>
        /// Get job statistics.
        /// </summary>
        /// <param name="jobGuid">The jGuid.</param>
        /// <param name="delay">The delay.</param>
        /// <returns>Cosmos job information.</returns>
        [CLSCompliant(false)]
        public static JobStatistics GetJobStats(string jobGuid, int delay = TmocConstants.VcOperationRetryDelay)
        {
            return (JobStatistics)RetryClass.Retry(new Func<string, JobStatistics>(VC.GetJobStatistics),
                new object[] { jobGuid }, TmocConstants.VcOperationRetryTime, TmocConstants.VcOperationRetryDelay);
        }

        /// <summary>
        /// Upload a file into cosmos, if the destinationStream exists, you should decide whether to overwrite.
        /// </summary>
        /// <param name="sourceFile">The path of local file.</param>
        /// <param name="destinationStream">Full VC path of cosmos stream.</param>
        /// <param name="proxyVC">The proxy of VC.</param>
        /// <param name="overwrite">Overwrite.</param>
        /// <param name="isBinary">Is binary.</param>
        /// <param name="maxRetry">The maximum retry time.</param>
        public static void UploadFile(string sourceFile, string destinationStream, string proxyVC = null, bool overwrite = true, bool isBinary = true, int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            if (overwrite)
            {
                DeleteStream(destinationStream);
            }

            // VcClient.VC.Upload(sourceFile, destinationStream, true).
            // cosmos SDK uploaded ss can not be recognized by cosmos scripts, scope.exe is the recommeneded way.
            string args = string.Format("copy {0} {1}", TmocPath.GetPathForCommandline(sourceFile), TmocPath.GetPathForCommandline(destinationStream));

            if (isBinary)
            {
                args += " -binary";
            }
            else
            {
                args += " -text";
            }

            if (!string.IsNullOrEmpty(proxyVC))
            {
                args += " -pxy " + proxyVC;
            }

            int numTries = 0;
            while (true)
            {
                if (numTries != 0 && overwrite)
                {
                    DeleteStream(destinationStream);
                }

                bool result = (bool)RetryClass.Retry(new Func<string, string, string, bool>(CommandLine.RunCommand),
                    new object[] { TmocConstants.Scope, args, string.Empty }, maxRetry, TmocConstants.VcOperationRetryDelay);
                if (result == false)
                {
                    numTries++;
                    if (numTries >= maxRetry)
                    {
                        throw new Exception("Upload file failed");
                    }

                    Thread.Sleep(TmocConstants.VcOperationRetryDelay);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Download a File from cosmos to local.
        /// </summary>
        /// <param name="sourceStream">Full VC path of cosmos stream.</param>
        /// <param name="destinationFile">The local file path.</param>
        /// <param name="proxyVC">The VC proxy.</param>
        /// <param name="overwrite">Whether should overwrite.</param>
        /// <param name="maxRetry">Max retry.</param>
        public static void DownloadFile(string sourceStream, string destinationFile, string proxyVC = null, bool overwrite = true,
                                        int maxRetry = TmocConstants.VcOperationRetryTime)
        {
            if (overwrite)
            {
                File.Delete(destinationFile);
            }

            string args = string.Format("copy {0} {1}{2}", TmocPath.GetPathForCommandline(sourceStream), TmocPath.GetPathForCommandline(destinationFile), overwrite ? " -overwrite" : string.Empty);
            if (!string.IsNullOrEmpty(proxyVC))
            {
                args += " -pxy " + proxyVC;
            }

            int numTries = 0;
            while (true)
            {
                bool result = (bool)RetryClass.Retry(new Func<string, string, string, bool>(CommandLine.RunCommand),
                    new object[] { TmocConstants.Scope, args, string.Empty }, maxRetry, TmocConstants.VcOperationRetryDelay);
                if (result == false)
                {
                    numTries++;
                    if (numTries >= maxRetry)
                    {
                        throw new Exception("Download file failed");
                    }

                    Thread.Sleep(TmocConstants.VcOperationRetryDelay);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Delete the stream on COSMOS.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public static void DeleteStream(string stream)
        {
            if (TmocFile.ExistsOnVC(stream))
            {
                TmocFile.DeleteOnVC(stream);
            }
        }

        /// <summary>
        /// Delete a live cosmos job.
        /// </summary>
        /// <param name="jobid">Job ID.</param>
        public static void DeleteJob(string jobid)
        {
            try
            {
                JobInfo info = GetJobInfo(Guid.Parse(jobid)); // is it necessary? can't VC.deleteJob handle it?
                JobInfo.JobState state = info.State;
                if ((state == JobInfo.JobState.Queued) || (state == JobInfo.JobState.Running))
                {
                    RetryClass.Retry(new Action<string>(VC.DeleteJob),
                        new object[] { jobid }, TmocConstants.VcOperationRetryTime, TmocConstants.VcOperationRetryDelay);
                }
            }
            catch
            {
                // not critical failure, do nothing
            }
        }

        /// <summary>
        /// Submit job.
        /// </summary>
        /// <param name="cosmosSDKDir">COSMOS SDK directory.</param>
        /// <param name="localworkdir">Local working directory.</param>
        /// <param name="friendlyName">The friendly name.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="scopeScriptFileName">Scope script filename.</param>
        /// <param name="nebulaArguments">The nebula arguments.</param>
        /// <param name="maxUnavailability">Max unavailability.</param>
        /// <returns>Guid id of the job.</returns>
        public Guid? Submit(string cosmosSDKDir, string localworkdir, string friendlyName, int tokens,
                            string scopeScriptFileName, int nebulaArguments, int maxUnavailability)
        {
            Guid? jobId = null;
            int retryNumber = 0;

            while (true)
            {
                jobId = SubmitNoRetry(cosmosSDKDir, localworkdir, friendlyName, tokens,
                                       scopeScriptFileName, nebulaArguments, maxUnavailability);
                if ((jobId != null) || (retryNumber++ == TmocConstants.VcOperationRetryTime))
                {
                    break;
                }

                Thread.Sleep(TmocConstants.VcOperationRetryDelay);
            }

            return jobId;
        }

        /// <summary>
        /// Wait until job is done.
        /// </summary>
        /// <param name="jobGuid">The jGuid.</param>
        /// <param name="sleepTimeSpan">Sleep time span.</param>
        /// <param name="jobinfo">Job information.</param>
        /// <returns>The flag to indicate if job is done.</returns>
        [CLSCompliant(false)]
        public bool WaitForJobCompletion(Guid jobGuid, TimeSpan sleepTimeSpan, out JobInfo jobinfo)
        {
            JobInfo.JobState state;
            while (true)
            {
                try
                {
                    // It can throw out exception
                    jobinfo = GetJobInfo(jobGuid);
                    state = jobinfo.State;

                    if (state == JobInfo.JobState.CompletedSuccess)
                    {
                        return true;
                    }

                    if ((state == JobInfo.JobState.None) || (state == JobInfo.JobState.Queued) || (state == JobInfo.JobState.Running))
                    {
                        Thread.Sleep(sleepTimeSpan);
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Submit the job without retry.
        /// </summary>
        /// <param name="cosmosSDKDir">COSMOS SDK directory.</param>
        /// <param name="localworkdir">Local working directory.</param>
        /// <param name="friendlyName">The friendly name.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="scopeScriptFileName">Scope script filename.</param>
        /// <param name="nebulaArguments">The nebula arguments.</param>
        /// <param name="maxUnavailability">Max unavailability.</param>
        /// <returns>Guid id of the job.</returns>
        private Guid? SubmitNoRetry(string cosmosSDKDir, string localworkdir, string friendlyName, int tokens, string scopeScriptFileName, int nebulaArguments = 0,
            int maxUnavailability = 0)
        {
            Guid? jobGuid = Guid.NewGuid();
            StringBuilder argument = new StringBuilder();
            argument.AppendFormat("submit -i {0} -vc {1}  -p {2} -f {3} -jobId {4} -workingRoot {5} -tempdir {5}",
                                            TmocPath.GetPathForCommandline(scopeScriptFileName), configVC.VcName, configVC.VcPriority, friendlyName,
                                            jobGuid.ToString(), TmocPath.GetPathForCommandline(localworkdir));
            if (maxUnavailability != 0)
            {
                argument.AppendFormat(" -maxUnavailability {0}", maxUnavailability);
            }

            if (!(nebulaArguments == 0 || nebulaArguments < 0))
            {
                var str = string.Format(@"-ExtractGroupDefaultDataSize {0}", nebulaArguments);
                argument.AppendFormat(@" -n ""{0}""", str);
            }

            if (tokens > 0)
            {
                argument.AppendFormat(@" -tokens ""{0}""", tokens);
            }

            if (!string.IsNullOrEmpty(configVC.VcProxy))
            {
                argument.AppendFormat(" -pxy {0}", string.Format("\"{0}\"", configVC.VcProxy));
            }

            // Because Scope.exe will put a NebulaCommandLine.txt in the current directory, which will make conflicts.
            // So we change the current directory firstly and then change back after it is done.
            string curDir = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(localworkdir);
            Directory.SetCurrentDirectory(localworkdir);

            string workingDir = Path.Combine(cosmosSDKDir, "ScopeSDK");
            string toolName = Path.Combine(workingDir, TmocConstants.Scope);
            bool result = CommandLine.RunCommand(toolName, argument.ToString(), workingDir);

            if (!result)
            {
                jobGuid = null;
            }

            Directory.SetCurrentDirectory(curDir);

            return jobGuid;
        }

        private void LogPerformanceStats(Guid guid)
        {
            try
            {
                JobInfo info = GetJobInfo(guid);
                JobStatistics stats = GetJobStats(guid.ToString());
                VertexExecutionStats overalljob =
                    stats.VertexStats.Find(s => s.VertexClassName.CompareTo(TmocConstants.OverAllJob) == 0);

                if (overalljob != null)
                {
                    Console.WriteLine("JobID: {0} InforName: {1}", guid.ToString(), info.Name);
                }
            }
            catch (Exception ex)
            {
                // not critical issue, do not interrupt the training flow. just warning.
                Console.WriteLine(
                    string.Format("Get Job Statistic failed for job {0}_Jobs/{1}\nThe exception message is {2}", configVC.VcName, guid.ToString(), ex.Message));
            }
        }
    }
}
