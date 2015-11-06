//----------------------------------------------------------------------------
// <copyright file="Job.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements job definition
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Job status.
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// Initial status of Job.
        /// </summary>
        Nonscheduled,

        /// <summary>
        /// Job is dispatched out.
        /// </summary>
        Dispatched,

        /// <summary>
        /// Job is running on remote machine.
        /// </summary>
        RemoteRunning,

        /// <summary>
        /// Job is running on local machine.
        /// </summary>
        LocalRunning,

        /// <summary>
        /// Job is finished.
        /// </summary>
        Done
    }

    /// <summary>
    /// Job definition.
    /// </summary>
    public class Job
    {
        #region Fields

        private string _guid = System.Guid.NewGuid().ToString();
        private JobStatus _status = JobStatus.Nonscheduled;
        private DateTime _updateTime;

        private NodeInfo _coordinator;
        private string _taskName = ProcessNode.DefaultTaskName;
        private string _wedgeName = CommandLineServerWedge.WedgeName;

        private string _name;

        private string _command;
        private string _arguments;

        private string _doneFile;

        private NodeInfo _runningNode;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class.
        /// </summary>
        /// <param name="wedgeName">Wedge name.</param>
        /// <param name="taskName">Task name.</param>
        public Job(string wedgeName, string taskName)
        {
            if (string.IsNullOrEmpty(wedgeName))
            {
                throw new ArgumentNullException("wedgeName");
            }

            if (string.IsNullOrEmpty(taskName))
            {
                throw new ArgumentNullException("taskName");
            }

            WedgeName = wedgeName;
            TaskName = taskName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Which node is this job working on.
        /// </summary>
        public NodeInfo RunningNode
        {
            get
            {
                return _runningNode;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _runningNode = value;
            }
        }

        /// <summary>
        /// Gets or sets Wedge this job used.
        /// </summary>
        public string WedgeName
        {
            get
            {
                return _wedgeName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _wedgeName = value;
            }
        }

        /// <summary>
        /// Gets or sets Task name.
        /// </summary>
        public string TaskName
        {
            get
            {
                return _taskName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _taskName = value;
            }
        }

        /// <summary>
        /// Gets or sets File path to test, where this job is done or not.
        /// </summary>
        public string DoneFile
        {
            get
            {
                return _doneFile;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _doneFile = value;
            }
        }

        /// <summary>
        /// Gets or sets Arguments string.
        /// </summary>
        public string Arguments
        {
            get
            {
                return _arguments;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _arguments = value;
            }
        }

        /// <summary>
        /// Gets or sets Command path.
        /// </summary>
        public string Command
        {
            get
            {
                return _command;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _command = value;
            }
        }

        /// <summary>
        /// Gets or sets Coodinator node that manages and dispatchs this job.
        /// </summary>
        public NodeInfo Coordinator
        {
            get
            {
                return _coordinator;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _coordinator = value;
            }
        }

        /// <summary>
        /// Gets GUID for this job, this should be unique 
        /// Across whole system to identify this job.
        /// </summary>
        public string Guid
        {
            get { return _guid; }
        }

        /// <summary>
        /// Gets or sets Name of this job.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return string.IsNullOrEmpty(_name) ? Guid : _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets Time eclipsed in milli-second
        /// Convert from Nanoseconds to Seconds. 
        /// 1 Second = 10(-9) Nanoseconds. 
        /// </summary>
        public long Ticks
        {
            get { return (DateTime.Now.Ticks - UpdateTime.Ticks) / 10000; }
        }

        /// <summary>
        /// Gets or sets Job status.
        /// </summary>
        public JobStatus Status
        {
            get
            {
                return _status;
            }

            set
            {
                _status = value;
                if (_status == JobStatus.Nonscheduled)
                {
                    _runningNode = null;
                }

                UpdateTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Gets or sets Job status updating time.
        /// </summary>
        public DateTime UpdateTime
        {
            get { return _updateTime; }
            set { _updateTime = value; }
        }

        #endregion

        #region Xml presentation process

        /// <summary>
        /// Parse Job from xml string.
        /// </summary>
        /// <param name="xml">XML message to parse.</param>
        public void ParseXml(string xml)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xml);

            ParseXml(dom.DocumentElement);
        }

        /// <summary>
        /// Parse Job from Xml Document object.
        /// </summary>
        /// <param name="element">XML element to parse.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        public virtual void ParseXml(XmlElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            _guid = element.GetAttribute("guid");
            _command = element.GetAttribute("command");
            _arguments = element.GetAttribute("arguments");
            _taskName = element.GetAttribute("taskName");

            if (element.HasAttribute("wedgeName")
                && !string.IsNullOrEmpty(element.GetAttribute("wedgeName")))
            {
                _wedgeName = element.GetAttribute("wedgeName");
            }
            else
            {
                _wedgeName = null;
            }

            if (element.HasAttribute("name")
                && !string.IsNullOrEmpty(element.GetAttribute("name")))
            {
                _name = element.GetAttribute("name");
            }
            else
            {
                _name = null;
            }

            if (element.HasAttribute("doneFile")
                && !string.IsNullOrEmpty(element.GetAttribute("doneFile")))
            {
                _doneFile = element.GetAttribute("doneFile");
            }
            else
            {
                _doneFile = null;
            }
        }

        /// <summary>
        /// Convert the object to XML string presentation.
        /// </summary>
        /// <returns>XML string.</returns>
        public virtual string ToXml()
        {
            XmlDocument dom = new XmlDocument();
            XmlElement ele = dom.CreateElement(MessageType.Job.ToString());
            ToXml(ele);

            if (Coordinator != null)
            {
                XmlElement coordinatorEle = Coordinator.ToXml(dom);
                ele.AppendChild(coordinatorEle);
            }

            dom.AppendChild(ele);
            return dom.OuterXml;
        }

        /// <summary>
        /// Convert the object to XML Document object presentation.
        /// </summary>
        /// <param name="element">XML element.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        protected void ToXml(XmlElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetAttribute("guid", Guid);
            element.SetAttribute("command", Command);
            element.SetAttribute("arguments", Arguments);
            element.SetAttribute("taskName", TaskName);
            if (!string.IsNullOrEmpty(_name))
            {
                element.SetAttribute("name", _name);
            }

            if (!string.IsNullOrEmpty(_doneFile))
            {
                element.SetAttribute("doneFile", _doneFile);
            }

            if (!string.IsNullOrEmpty(_wedgeName))
            {
                element.SetAttribute("wedgeName", _wedgeName);
            }
        }

        #endregion
    }
}