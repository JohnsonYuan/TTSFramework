//----------------------------------------------------------------------------
// <copyright file="HPCParallelComputation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is an parallel computation calls which is based on HPC class. It helps to create
//     submit jobs.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using Microsoft.Hpc.Scheduler;
    using Microsoft.Hpc.Scheduler.Properties;

    // surpress the Obsolete warning of ISchedulerTask.IsParametric
    #pragma warning disable 618

    /// <summary>
    /// The enumeration of HPC job state.
    /// </summary>
    public enum HpcJobState
    {
        /// <summary>
        /// Create status.
        /// </summary>
        Create,

        /// <summary>
        /// Wait status.
        /// </summary>
        Wait,

        /// <summary>
        /// Pend status.
        /// </summary>
        Pend,

        /// <summary>
        /// Runing status.
        /// </summary>
        Running,

        /// <summary>
        /// Finished status.
        /// </summary>
        Finished,

        /// <summary>
        /// Succeed status.
        /// </summary>
        Succeed,

        /// <summary>
        /// Failed status.
        /// </summary>
        Failed,

        /// <summary>
        /// Canceled status.
        /// </summary>
        Canceled
    }

    /// <summary>
    /// The enumeration of HPC job priority.
    /// </summary>
    public enum HpcJobPriority
    {
        /// <summary>
        /// Hpc Job's priority lowest.
        /// </summary>
        Lowest = 0,

        /// <summary>
        /// Hpc Job's priority below normal.
        /// </summary>
        BelowNormal = 1,

        /// <summary>
        /// Hpc Job's priority normal.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Hpc Job's priority above normal.
        /// </summary>
        AboveNormal = 3,

        /// <summary>
        /// Hpc Job's priority highest.
        /// </summary>
        Highest = 4
    }

    /// <summary>
    /// The enumeration of HPC unit type.
    /// </summary>
    public enum TtsHpcFarmUnitType
    {
        /// <summary>
        /// Hpc Job's unit type, base unit is core.
        /// </summary>
        Core = 0,

        /// <summary>
        /// Hpc Job's unit type, base unit is node.
        /// </summary>
        Node = 1,

        /// <summary>
        /// Hpc Job's unit type, base unit is socket.
        /// </summary>
        Socket = 2
    }

    /// <summary>
    /// The definition of HPC exception.
    /// </summary>
    [Serializable]
    public class HpcException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HpcException"/> class.
        /// The constructor of HPC exception with message.
        /// </summary>
        /// <param name="message">The message.</param>
        public HpcException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HpcException"/> class.
        /// The constructor of HPC exception with message and inner exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The innerException.</param>
        public HpcException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// The base class of TTS HPC farm.
    /// </summary>
    public abstract class TtsHpcFarm : IDisposable
    {
        // private members
        private bool disposed = false;

        /// <summary>
        /// The delegate routine of error event handler.
        /// </summary>
        /// <param name="message">The error message.</param>
        public delegate void OnErrorEventHandler(string message);

        /// <summary>
        /// The envent of error.
        /// </summary>
        public event OnErrorEventHandler OnError;

        /// <summary>
        /// The dispose method.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The abstract method of create job.
        /// </summary>
        /// <param name="command">The command of the job.</param>
        /// <param name="name">The name of the job.</param>
        /// <param name="logFile">The log file of the job.</param>
        /// <param name="isExclusive">Indicates whether the job is exclusive.</param>
        /// <param name="startValue">Sweep job's start value.</param>
        /// <param name="endValue">Sweep job's end value.</param>
        /// <param name="incrementalValue">Sweep job's incremental value.</param>
        /// <param name="priority">The priority of the job.</param>
        /// <returns>The job object.</returns>
        public abstract TtsHpcJob CreateJob(string command, string name, string logFile, bool isExclusive, int startValue, int endValue, int incrementalValue, HpcJobPriority priority);

        /// <summary>
        /// The abstract method of submit job.
        /// </summary>
        /// <param name="job">The job to submit.</param>
        /// <returns>Bool.</returns>
        public abstract bool Submit(TtsHpcJob job);

        /// <summary>
        /// The abstract method of kill job.
        /// </summary>
        /// <param name="job">The job to kill.</param>
        /// <returns>Bool.</returns>
        public abstract bool Kill(TtsHpcJob job);

        /// <summary>
        /// The abstract method of query job by owner.
        /// </summary>
        /// <param name="owner">The name of owner.</param>
        /// <returns>List.</returns>
        public abstract List<TtsHpcJob> QueryJobByOwner(string owner);

        /// <summary>
        /// The abstract method to get hpc farm information.
        /// </summary>
        /// <returns>String.</returns>
        public abstract string GetHpcFarmInfo();

        /// <summary>
        /// The abstract method to get hpc farm node list.
        /// </summary>
        /// <returns>List.</returns>
        public abstract List<string> GetNodeList();

        /// <summary>
        /// The Dispose methodlogy.
        /// </summary>
        /// <param name="disposing">The indicator of disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free managed resource. Not implemented here
            }

            // Free unmanaged resource.Not implmented here
            this.disposed = true;
        }

        /// <summary>
        /// Raise error event.
        /// </summary>
        /// <param name="result">The result.</param>
        protected virtual void RaiseErrorEvent(string result)
        {
            if (OnError != null)
            {
                OnError(result);
            }
        }
    }

    /// <summary>
    /// The base class of Tts HPC job.
    /// </summary>
    public abstract class TtsHpcJob
    {
        #region Properties : Before submitted - User creation
        /// <summary>
        /// Gets or sets The name of the job. Could be String.Empty.
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Gets or sets The command line of the job.
        /// </summary>
        public virtual string Command { get; protected set; }

        /// <summary>
        /// Gets or sets The log file of the job. The standard ouput and error will be redirected to this file.
        /// </summary>
        public virtual string LogFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the job is exclusive.
        /// </summary>
        public virtual bool UseWholeMachine { get; protected set; }

        /// <summary>
        /// Gets or sets The start value.
        /// </summary>
        public virtual int StartValue { get; protected set; }

        /// <summary>
        /// Gets or sets The end value.
        /// </summary>
        public virtual int EndValue { get; protected set; }

        /// <summary>
        /// Gets or sets The incremental value.
        /// </summary>
        public virtual int IncrementalValue { get; protected set; }

        /// <summary>
        /// Gets or sets The owner of the job.
        /// </summary>
        public virtual string Owner { get; set; }

        /// <summary>
        /// Gets or sets The nodes on which the job will be started. If user use this property in multi-thread context, the HpcFarm.Submit method should be mutexed.
        /// </summary>
        public virtual List<string> RequestedNodes { get; set; }

        /// <summary>
        /// Gets or sets The job priority of the job. This property may be modified while the job is Running.
        /// </summary>
        public virtual HpcJobPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets The id of the job. If the job is not submitted, the id is -1.
        /// </summary>
        public virtual int Id { get; set; }

        /// <summary>
        /// Gets or sets The state of the job.
        /// </summary>
        public virtual HpcJobState State { get; set; }

        /// <summary>
        /// Gets or sets the machine on which the job is running.
        /// </summary>
        public virtual string Machine { get; protected set; }

        /// <summary>
        /// Gets or sets the cpu time spent on the job.
        /// </summary>
        public virtual float CpuTime { get; protected set; }

        /// <summary>
        /// Gets or sets the job submit time.
        /// </summary>
        public virtual DateTime SubmitTime { get; set; }

        /// <summary>
        /// Gets or sets The custom information.
        /// </summary>
        public virtual Dictionary<string, object> CustomInformation { get; set; }

        #endregion

        #region Methods
        /// <summary>
        /// Sync the job with a IHpcJob object.
        /// </summary>
        /// <param name="job">The job object.</param>
        public virtual void SyncFrom(TtsHpcJob job)
        {
            this.Id = job.Id;
            this.State = job.State;
            this.Priority = job.Priority;
            this.Machine = job.Machine;
            this.CpuTime = job.CpuTime;
        }
        #endregion
    }

    /// <summary>
    /// An implementation of TTsHpcSweepTaskFarm that use the parametric sweep tasks.
    /// </summary>
    [CLSCompliant(false)]
    public class TtsHpcSweepTaskFarm : TtsHpcFarm
    {
        #region Fields
        /// <summary>
        /// Indicate the number of HPC jobs.
        /// </summary>
        private const int MaxNumOfHpcJobs = 100;

        /// <summary>
        /// Indicate whether the object has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// The scheduler used to communicate with HPC server.
        /// </summary>
        private IScheduler scheduler = null;

        /// <summary>
        /// The job that contains all tasks.
        /// </summary>
        private ISchedulerJob containerJob = null;

        /// <summary>
        /// The job's status.
        /// </summary>
        private HpcJobState statusOfJob = HpcJobState.Create;

        private ManualResetEvent manualEvent = new ManualResetEvent(false);

        private bool jobCreating = false;

        private List<string> doneFileList = null;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="TtsHpcSweepTaskFarm"/> class.
        /// </summary>
        /// <param name="clusterName">The name of the cluster.</param>
        /// <param name="userName">The name of the user used to connect to the cluster.</param>
        /// <param name="maxCores">The max cores job run on.</param>
        /// <param name="maxNodes">The max nodes job run on.</param>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="isExclusive">Is exlusive on node or core.</param>
        public TtsHpcSweepTaskFarm(string clusterName, string userName,
            int maxCores, int maxNodes, string jobName, bool isExclusive)
        {
            this.ClusterName = clusterName;
            this.UserName = userName;
            this.MaxCores = maxCores;
            this.MaxNodes = maxNodes;
            this.JobName = jobName;
            this.Priority = HpcJobPriority.Normal;
            this.IsExclusive = false;
            this.scheduler = new Scheduler();
            this.scheduler.Connect(clusterName);
            CheckJob();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the name of the cluster.
        /// </summary>
        public string ClusterName { get; set; }

        /// <summary>
        /// Gets or sets the name of the user used to connect to the cluster.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the max job running at the same time.
        /// </summary>
        public int MaxCores { get; set; }

        /// <summary>
        /// Gets or sets The maximum number of nodes.
        /// </summary>
        public int MaxNodes { get; set; }

        /// <summary>
        /// Gets or sets The name of job.
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// Gets or sets The job's priority.
        /// </summary>
        public HpcJobPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Is the job exlusive on machine.
        /// </summary>
        public bool IsExclusive { get; set; }

        /// <summary>
        /// Gets or sets done file list.
        /// </summary>
        public List<string> DoneFileList
        {
            get
            { 
                return doneFileList; 
            }

            set 
            {
                if (value != null)
                {
                    doneFileList = value;
                }
            }
        }

        #endregion

        /// <summary>
        /// Wait for job unitl it's done.
        /// </summary>
        /// <returns>The state of job.</returns>
        public HpcJobState WaitForJobDone()
        {
            // This is another scheme to detect if it's done.
            if (CheckJobDone())
            {
                manualEvent.Set();
                statusOfJob = HpcJobState.Finished;
            }

            manualEvent.WaitOne();
            return statusOfJob;
        }

        #region Overrides
        /// <summary>
        /// Create a job that can be dealt with this Farm.
        /// </summary>
        /// <param name="command">The command of the job.</param>
        /// <param name="name">The name of the job.</param>
        /// <param name="logFile">The log file of the job.</param>
        /// <param name="isExclusive">Indicates whether the job is exclusive.</param>
        /// <param name="startValue">Sweep job's start value.</param>
        /// <param name="endValue">Sweep job's end value.</param>
        /// <param name="incrementalValue">Sweep job's incremental value.</param>
        /// <param name="priority">The priority of the job.</param>
        /// <returns>The job object.</returns>
        public override TtsHpcJob CreateJob(string command, string name, string logFile, bool isExclusive, int startValue, int endValue, int incrementalValue, HpcJobPriority priority)
        {
            return new TtsHpcTaskJob(command, name, logFile, isExclusive, startValue, endValue, incrementalValue, priority);
        }

        /// <summary>
        /// Submit a job to the farm.
        /// </summary>
        /// <param name="job">The job object.</param>
        /// <returns>Indicates whether the job is submitted successfully.</returns>
        public override bool Submit(TtsHpcJob job)
        {
            if (!(job is TtsHpcTaskJob))
            {
                throw new HpcException("The internal job type is not compatible with the farm object.");
            }

            try
            {
                if (this.containerJob == null)
                {
                    this.IsExclusive = job.UseWholeMachine;
                    this.CreateJob();
                }
                else
                {
                    var statesFinish = JobState.Finishing |
                        JobState.Finished |
                        JobState.Failed;
                    this.containerJob.Refresh();
                    if (statesFinish.HasFlag(this.containerJob.State))
                    {
                        this.IsExclusive = job.UseWholeMachine;
                        this.CreateJob();
                    }
                }

                var hpcJob = job as TtsHpcTaskJob;
                this.containerJob.Priority = (JobPriority)hpcJob.Priority;
                var task = this.containerJob.CreateTask();
                task.IsParametric = true;
                task.Name = hpcJob.Name;
                task.IsRerunnable = true;
                task.StartValue = hpcJob.StartValue;
                task.EndValue = hpcJob.EndValue;
                task.IncrementValue = hpcJob.IncrementalValue;
                task.CommandLine = hpcJob.Command;
                task.IsExclusive = hpcJob.UseWholeMachine;
                if (hpcJob.LogFile != null)
                {
                    task.StdOutFilePath = hpcJob.LogFile;
                    task.StdErrFilePath = hpcJob.LogFile;
                }

                if (this.jobCreating)
                {
                    this.containerJob.AddTask(task);
                    this.scheduler.SubmitJob(this.containerJob, this.UserName, null);
                    task.Refresh();
                    this.jobCreating = false;
                }
                else
                {
                    this.containerJob.SubmitTask(task);
                }

                hpcJob.InitializeFrom(task);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return false;
            }
        }

        /// <summary>
        /// Kill a job.
        /// </summary>
        /// <param name="job">The job object.</param>
        /// <returns>Indicates whether the job is killed successfully.</returns>
        public override bool Kill(TtsHpcJob job)
        {
            if (!(job is TtsHpcTaskJob))
            {
                throw new HpcException("The internal job type is not compatitable with the farm object.");
            }

            try
            {
                if (null == this.containerJob)
                {
                    return true;
                }
                else
                {
                    var statesFinish = JobState.Finishing |
                        JobState.Finished |
                        JobState.Failed;
                    this.containerJob.Refresh();
                    if (statesFinish.HasFlag(this.containerJob.State))
                    {
                        return true;
                    }
                }

                var hpcJob = job as TtsHpcTaskJob;
                this.containerJob.CancelTask(hpcJob.TaskId);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return false;
            }
        }

        /// <summary>
        /// Get a job list of the specified user.
        /// </summary>
        /// <param name="owner">The owner of the user. Useless for this implementation.</param>
        /// <returns>The list of jobs.</returns>
        public override List<TtsHpcJob> QueryJobByOwner(string owner)
        {
            try
            {
                if (null == this.containerJob)
                {
                    return new List<TtsHpcJob>();
                }
                else
                {
                    var statesFinish = JobState.Finishing |
                        JobState.Finished |
                        JobState.Failed;
                    this.containerJob.Refresh();
                    if (statesFinish.HasFlag(this.containerJob.State))
                    {
                        return new List<TtsHpcJob>();
                    }
                }

                var states = TaskState.Running |
                    TaskState.Queued |
                    TaskState.Dispatching |
                    TaskState.Submitted |
                    TaskState.Validating;
                var filters = this.scheduler.CreateFilterCollection();
                filters.Add(
                    FilterOperator.HasBitSet,
                    TaskPropertyIds.State,
                    states);
                var tasks = this.containerJob.GetTaskList(filters, null, false);
                List<TtsHpcJob> jobList = new List<TtsHpcJob>();
                foreach (ISchedulerTask task in tasks)
                {
                    TtsHpcJob job = TtsHpcTaskJob.CreateFromInnerJob(task, this.containerJob);
                    jobList.Add(job);
                }

                return jobList;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return null;
            }
        }

        /// <summary>
        /// Get the information of the farm.
        /// </summary>
        /// <returns>The information of the farm.</returns>
        public override string GetHpcFarmInfo()
        {
            return string.Format("Not Implemented");
        }

        /// <summary>
        /// Get the node list of the farm.
        /// </summary>
        /// <returns>The node list.</returns>
        public override List<string> GetNodeList()
        {
            List<string> ret = new List<string>();
            foreach (ISchedulerNode node in this.scheduler.GetNodeList(null, null))
            {
                ret.Add(node.Name);
            }

            return ret;
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        /// <param name="disposing">Indicating whether to release the managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
            }

            this.scheduler.Dispose();
            base.Dispose(disposing);
            this.disposed = true;

            if (null != manualEvent)
            {
                manualEvent.Dispose();
            }
        }
        #endregion

        private bool CheckJobDone()
        {
            bool jobDone = false;
            DateTime startTime = DateTime.Now;

            while (!jobDone)
            {
                foreach (var file in doneFileList)
                {
                    if (File.Exists(file))
                    {
                        jobDone = true;
                    }
                    else
                    {
                        jobDone = false;
                        break;
                    }
                }

                Thread.Sleep(5000);
                DateTime endTime = DateTime.Now;
                if ((endTime - startTime).Days > 2)
                {
                    throw new TimeoutException("Time out of HPC job");
                }
            }

            // clean up the job
            foreach (var file in doneFileList)
            {
                File.Delete(file);
            }

            return jobDone;
        }

        private void CheckJob()
        {
            var states = JobState.Configuring |
                JobState.Submitted |
                JobState.Queued |
                JobState.Running |
                JobState.Validating |
                JobState.ExternalValidation;

            List<TtsHpcJob> listJobs = new List<TtsHpcJob>(MaxNumOfHpcJobs);
            IFilterCollection filters = this.scheduler.CreateFilterCollection();
            filters.Add(FilterOperator.Equal, JobPropertyIds.Name, this.JobName);
            filters.Add(FilterOperator.HasBitSet, JobPropertyIds.State, states);
            filters.Add(FilterOperator.Equal, JobPropertyIds.UserName, this.UserName);
            filters.Add(FilterOperator.Equal, JobPropertyIds.Owner, Environment.UserDomainName + "\\" + Environment.UserName);
            ISchedulerCollection jobs = scheduler.GetJobList(filters, null);
            if (jobs.Count > 0)
            {
                this.containerJob = jobs[0] as ISchedulerJob;
            }
        }

        private void CreateJob()
        {
            this.containerJob = this.scheduler.CreateJob();
            this.containerJob.Name = this.JobName;
            this.containerJob.Priority = (JobPriority)this.Priority;
            this.containerJob.IsExclusive = this.IsExclusive;

            if (this.IsExclusive)
            {
                this.containerJob.MinimumNumberOfNodes = 1;
                if (this.MaxNodes > 0)
                {
                    this.containerJob.MaximumNumberOfNodes = this.MaxNodes;
                }
                else
                {
                    this.containerJob.MaximumNumberOfNodes = this.MaxCores;
                }
            }
            else
            {
                this.containerJob.MinimumNumberOfCores = 1;
                if (this.MaxCores > 0)
                {
                    this.containerJob.MaximumNumberOfCores = this.MaxCores;
                }
                else
                {
                    this.containerJob.MaximumNumberOfCores = this.MaxNodes;
                }
            }

            this.jobCreating = true;
        }
    }

    /// <summary>
    /// The class of TtsHpcTaskJob inherited from TtsHpcJob.
    /// </summary>
    [CLSCompliant(false)]
    public class TtsHpcTaskJob : TtsHpcJob
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the TtsHpcTaskJob class.
        /// </summary>
        /// <param name="command">The command of the job.</param>
        /// <param name="name">The name of the job.</param>
        /// <param name="logFile">The log file of the job.</param>
        /// <param name="isExclusive">Indicates whether the job is exclusive.</param>
        /// <param name="startValue">Sweep job's start value.</param>
        /// <param name="endValue">Sweep job's end value.</param>
        /// <param name="incrementValue">Sweep job's incremental value.</param>
        /// <param name="priority">The priority of the job.</param>
        public TtsHpcTaskJob(string command, string name, string logFile, bool isExclusive, int startValue, int endValue, int incrementValue, HpcJobPriority priority)
        {
            // Initialize value
            this.Name = name;
            this.Command = command;
            this.LogFile = logFile;
            this.UseWholeMachine = isExclusive;
            this.StartValue = startValue;
            this.EndValue = endValue;
            this.IncrementalValue = incrementValue;

            // Default value
            this.Owner = null;
            this.Priority = priority;
            this.State = HpcJobState.Create;
            this.Id = -1;
            this.Machine = null;
            this.CpuTime = 0;
        }

        /// <summary>
        /// Initializes a new instance of the TtsHpcTaskJob class.
        /// </summary>
        /// <param name="command">The command of the job.</param>
        /// <param name="owner">The owner of the job.</param>
        /// <param name="name">The name of the job.</param>
        /// <param name="logFile">The log file of the job.</param>
        /// <param name="priority">The priority of the job.</param>
        /// <param name="isExclusive">Indicates whether the job is exclusive.</param>
        public TtsHpcTaskJob(string command, string owner, string name, string logFile, HpcJobPriority priority, bool isExclusive)
        {
            // Initialize value
            this.Name = name;
            this.Owner = owner;
            this.Command = command;
            this.LogFile = logFile;
            this.Priority = priority;
            this.UseWholeMachine = isExclusive;

            // Default value
            this.State = HpcJobState.Create;
            this.Id = -1;
            this.Machine = null;
            this.CpuTime = 0;
        }

        /// <summary>
        /// Prevents a default instance of the TtsHpcTaskJob class from being created.
        /// </summary>
        private TtsHpcTaskJob()
        {
            // Default value
            this.Name = null;
            this.Command = null;
            this.LogFile = null;
            this.UseWholeMachine = false;
            this.Owner = null;
            this.Priority = HpcJobPriority.Normal;
            this.State = HpcJobState.Create;
            this.Id = -1;
            this.Machine = null;
            this.CpuTime = 0;
        }
        #endregion

        /// <summary>
        /// Gets or sets The id of the internal task.
        /// </summary>
        public ITaskId TaskId { get; set; }

        /// <summary>
        /// Create a HpcJob according to the inner task and job.
        /// </summary>
        /// <param name="taskIn">The inner task.</param>
        /// <param name="jobIn">The inner job.</param>
        /// <returns>The HpcJob.</returns>
        public static TtsHpcTaskJob CreateFromInnerJob(ISchedulerTask taskIn, ISchedulerJob jobIn)
        {
            TtsHpcTaskJob job = new TtsHpcTaskJob();
            job.Id = taskIn.TaskId.JobTaskId;
            job.Name = taskIn.Name;
            job.State = TtsHpcTaskJob.ConvertState(taskIn.State);
            if (taskIn.AllocatedNodes != null && taskIn.AllocatedNodes.Count > 0)
            {
                job.Machine = taskIn.AllocatedNodes[0];
            }

            job.Command = taskIn.CommandLine;
            job.LogFile = taskIn.StdOutFilePath;
            job.CpuTime = job.CpuTime;
            job.Priority = (HpcJobPriority)jobIn.Priority;
            return job;
        }

        /// <summary>
        /// Convert the TaskState to HpcJobState.
        /// </summary>
        /// <param name="state">The TaskState.</param>
        /// <returns>The HpcJobState.</returns>
        public static HpcJobState ConvertState(TaskState state)
        {
            switch (state)
            {
                case TaskState.Running:
                    return HpcJobState.Running;
                case TaskState.Queued:
                    return HpcJobState.Pend;
                case TaskState.Dispatching:
                    return HpcJobState.Pend;
                case TaskState.Submitted:
                    return HpcJobState.Pend;
                case TaskState.Finishing:
                    return HpcJobState.Finished;
                case TaskState.Finished:
                    return HpcJobState.Finished;
                case TaskState.Canceled:
                    return HpcJobState.Finished;
                case TaskState.Failed:
                    return HpcJobState.Finished;
                case TaskState.Canceling:
                    return HpcJobState.Finished;
                default:
                    return HpcJobState.Wait;
            }
        }

        /// <summary>
        /// Initialize a job after it is submited.
        /// </summary>
        /// <param name="task">The submited task.</param>
        internal void InitializeFrom(ISchedulerTask task)
        {
            this.Id = task.TaskId.JobTaskId;
            this.State = ConvertState(task.State);
        }
    }

    /// <summary>
    /// HPC ParallelComputation class.
    /// </summary>
    public class HPCParallelComputaion : ParallelComputation, IDisposable
    {
        #region Fields

        /// <summary>
        /// Specify the name of head node.
        /// </summary>
        public string HPCHeadNode = DefaultHPCHeadNode;

        /// <summary>
        /// Specify the number of to node to calculate.
        /// </summary>
        public int NodeNumber = DefaultNodeNumber;

        /// <summary>
        /// Specify the name of job.
        /// </summary>
        public string JobName = string.Empty;

        /// <summary>
        /// Specify the input of BroadCast Command.
        /// </summary>
        public string BroadCastCommand = string.Empty;

        /// <summary>
        /// Specify the input of Reduce Command.
        /// </summary>
        public string ReduceCommand = string.Empty;

        /// <summary>
        /// Specify the sweep start value.
        /// </summary>
        public int SweepStart = 0;

        /// <summary>
        /// Specify the sweep end value.
        /// </summary>
        public int SweepEnd = 0;

        /// <summary>
        /// Specify the sweep increment value.
        /// </summary>
        public int SweepIncrement = 1;

        /// <summary>
        /// Specify the Task name;.
        /// </summary>
        public string TaskName = string.Empty;

        /// <summary>
        /// Job Done File List.
        /// </summary>
        public List<string> JobDoneList = null;

        private const string DefaultHPCHeadNode = "TTS-R710-11";

        /// <summary>
        /// Specify the node number.
        /// </summary>
        private const int DefaultNodeNumber = 4;

        /// <summary>
        /// Specify the core number.
        /// </summary>
        private const int CoreNumer = 72;

        private ParallelComputation _singleMachine = null;
        private TtsHpcSweepTaskFarm _ttsHpcFarm = null;
        private HpcJobPriority _jobPriority = HpcJobPriority.Normal;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="HPCParallelComputaion"/> class.
        /// </summary>
        public HPCParallelComputaion()
        {
            ParallelComputation.CreateComputationPlatform(ComputationPlatform.SingleMachinePlatform, out _singleMachine);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets The Single machine computation platform to execute the task.
        /// </summary>
        public SingleMachineComputation SingleMachineComputation
        {
            get
            {
                return (SingleMachineComputation)_singleMachine;
            }
        }

        /// <summary>
        /// Gets or sets the HPC job priority.
        /// </summary>
        public HpcJobPriority HpcJobPriority
        {
            get
            {
                return _jobPriority;
            }

            set
            {
                _jobPriority = value;
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the resources used in this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the RewindableTextReader.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _ttsHpcFarm)
                {
                    _ttsHpcFarm.Dispose();
                }
            }
        }

        #endregion

        /// <summary>
        /// The Initialize() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool Initialize()
        {
            try
            {
                _ttsHpcFarm = new TtsHpcSweepTaskFarm(HPCHeadNode, Environment.UserName,
                            CoreNumer, NodeNumber, JobName, true);

                if (SweepStart > SweepEnd)
                {
                    throw new HpcException("The sweep start value should be less than or equal to sweep end value");
                }
                else
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return false;
            }
        }

        /// <summary>
        /// The BroadCast() methods to broadcast all task.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool BroadCast()
        {
            try
            {
                if (BroadCastCommand == string.Empty)
                {
                    throw new HpcException("The broadcast command should not be null");
                }

                if (JobDoneList != null)
                {
                    // Clean up done file firstly
                    foreach (var doneFile in JobDoneList)
                    {
                        File.Delete(doneFile);
                    }

                    _ttsHpcFarm.DoneFileList = JobDoneList;
                }

                TtsHpcJob job = _ttsHpcFarm.CreateJob(BroadCastCommand, TaskName, string.Empty, true, SweepStart, SweepEnd, SweepIncrement, HpcJobPriority);

                return _ttsHpcFarm.Submit(job);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return false;
            }
        }

        /// <summary>
        /// The Reduce() methods to reduce all task dispatched.
        /// </summary>
        /// <returns>Bool.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        protected override bool Reduce()
        {
            try
            {
                HpcJobState jobState = _ttsHpcFarm.WaitForJobDone();
                if (ReduceCommand == string.Empty)
                {
                    Console.WriteLine("The Reduce Command is null");
                }
                else if (jobState != HpcJobState.Finished)
                {
                    throw new HpcException("The broadcasted job failed, please check");
                }
                else
                {
                    string command = string.Empty;
                    string arguments = string.Empty;
                    if (ReduceCommand.Contains(' '))
                    {
                        command = ReduceCommand.Substring(0, ReduceCommand.IndexOf(' ', 0));
                        arguments = ReduceCommand.Substring(ReduceCommand.IndexOf(' ', 0));
                    }
                    else
                    {
                        command = ReduceCommand;
                    }

                    CommandLine.RunCommand(
                            command, arguments, Directory.GetCurrentDirectory());
                }

                _singleMachine.Execute();
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return false;
            }
        }

        /// <summary>
        /// The ValidateResult() methods to broadcast all task.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool ValidateResult()
        {
            return true;
        }
    }
}