//----------------------------------------------------------------------------
// <copyright file="NodeInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements definition of nod information
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Node type.
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// To execute job.
        /// </summary>
        Execution,

        /// <summary>
        /// Aggregate resource information.
        /// </summary>
        Aggregator,

        /// <summary>
        /// Coodination node.
        /// </summary>
        Coordinator
    }

    /// <summary>
    /// Node information.
    /// </summary>
    public class NodeInfo
    {
        #region Fields

        private string _guid = System.Guid.NewGuid().ToString();

        private NodeType _nodeType = NodeType.Execution;
        private string _name;

        private string _ip;
        private int _port;

        /// <summary>
        /// Capability.
        /// </summary>
        private int _processorCount;

        private bool _busy;
        private DateTime _activeTime;
        private DateTime _freeStartTime = DateTime.Now;
        private bool _fixed;
        private string _lastDoneTaskName;

        private int _doneCount;
        private bool _jobWorking;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeInfo"/> class.
        /// </summary>
        public NodeInfo()
        {
            StatusUpdatedEvent = delegate
            {
            };
        }

        #endregion

        #region Events

        /// <summary>
        /// Status updated event.
        /// </summary>
        public event EventHandler<NodeInfoEventArgs> StatusUpdatedEvent;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Free time duration.
        /// </summary>
        public TimeSpan FreeTime
        {
            get { return DateTime.Now - _freeStartTime; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Working status.
        /// </summary>
        public bool JobWorking
        {
            get { return _jobWorking; }
            set { _jobWorking = value; }
        }

        /// <summary>
        /// Gets or sets The number of jobs finished.
        /// </summary>
        public int DoneCount
        {
            get { return _doneCount; }
            set { _doneCount = value; }
        }

        /// <summary>
        /// Gets or sets Processor count of the machine.
        /// </summary>
        public int ProcessorCount
        {
            get { return _processorCount; }
            set { _processorCount = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Fixed node.
        /// </summary>
        public bool Fixed
        {
            get { return _fixed; }
            set { _fixed = value; }
        }

        /// <summary>
        /// Gets or sets Node type.
        /// </summary>
        public NodeType NodeType
        {
            get { return _nodeType; }
            set { _nodeType = value; }
        }

        /// <summary>
        /// Gets or sets Guid of this node.
        /// </summary>
        public string Guid
        {
            get
            {
                return _guid;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _guid = value;
            }
        }

        /// <summary>
        /// Gets or sets Last done task name.
        /// </summary>
        public string LastDoneTaskName
        {
            get
            {
                return _lastDoneTaskName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _lastDoneTaskName = value;
            }
        }

        /// <summary>
        /// Gets Id of this node.
        /// </summary>
        public string Id
        {
            get { return IP + ":" + Port.ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Gets or sets Name of this node, can be machine name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                string orginal = _name;
                _name = value;
                if (orginal != _name)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        DistributeComputing.Properties.Resources.NodeNameChangeMessage,
                        orginal, _name);
                    StatusUpdatedEvent(this, new NodeInfoEventArgs(this, message));
                }
            }
        }

        /// <summary>
        /// Gets or sets Ip address of this node.
        /// </summary>
        public string IP
        {
            get
            {
                return _ip;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _ip = value;
            }
        }

        /// <summary>
        /// Gets or sets Udp port which this node listens on.
        /// </summary>
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        } 

        /// <summary>
        /// Gets or sets Latest active time of this node.
        /// </summary>
        public DateTime ActiveTime
        {
            get { return _activeTime; }
            set { _activeTime = value; }
        } 

        /// <summary>
        /// Gets or sets a value indicating whether this node on busy.
        /// </summary>
        public bool Busy
        {
            get
            {
                return _busy;
            }

            set
            {
                bool orginal = _busy;
                _busy = value;
                if (orginal != _busy)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "busy status is set from {0} to {1}", orginal, _busy);
                    StatusUpdatedEvent(this, new NodeInfoEventArgs(this, message));

                    if (!_busy)
                    {
                        _freeStartTime = DateTime.Now;
                    }
                }
            }
        } 

        #endregion

        #region Public operations

        /// <summary>
        /// Parse XML element to get node info instance.
        /// </summary>
        /// <param name="element">XML element.</param>
        /// <returns>Node info instance.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        public static NodeInfo Parse(XmlElement element)
        {
            NodeInfo ni = new NodeInfo();
            ni.ParseXml(element);

            return ni;
        } 

        /// <summary>
        /// Copy node info from other instance to this instance.
        /// </summary>
        /// <param name="info">Node info to copy from.</param>
        public void Copy(NodeInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (info.Guid == null)
            {
                throw new ArgumentNullException("info");
            }

            if (info.IP == null)
            {
                throw new ArgumentNullException("info");
            }

            if (info.Name == null)
            {
                throw new ArgumentNullException("info");
            }

            Guid = info.Guid;
            IP = info.IP;
            Name = info.Name;
            NodeType = info.NodeType;
            Port = info.Port;
            ProcessorCount = info.ProcessorCount;
            Busy = info.Busy;
        }

        /// <summary>
        /// Convert this node into into XML element.
        /// </summary>
        /// <param name="dom">Parent XML document.</param>
        /// <returns>Xml element.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization",
            "CA1305:SpecifyIFormatProvider", MessageId = "System.int.ToString", Justification = "Ignore."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        public XmlElement ToXml(XmlDocument dom)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            XmlElement ele = dom.CreateElement("Node");

            ele.SetAttribute("type", NodeType.ToString());
            ele.SetAttribute("name", Name);

            ele.SetAttribute("ip", IP);
            ele.SetAttribute("port", Port.ToString(CultureInfo.InvariantCulture));
            ele.SetAttribute("guid", Guid);

            ele.SetAttribute("busy", Busy.ToString());

            ele.SetAttribute("processorCount", ProcessorCount.ToString(CultureInfo.InvariantCulture));
            ele.SetAttribute("jobWorking", JobWorking.ToString());

            return ele;
        }

        /// <summary>
        /// Parse XML element to get node info data.
        /// </summary>
        /// <param name="element">Xml element.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        public void ParseXml(XmlElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            this.NodeType = (NodeType)Enum.Parse(typeof(NodeType), element.GetAttribute("type"));
            this.Name = element.GetAttribute("name");
            this.IP = element.GetAttribute("ip");

            // handle IPv6: 
            Match m = Regex.Match(this.IP, @":(\d+\.\d+\.\d+\.\d+)%");
            if (m.Success)
            {
                this.IP = m.Groups[1].Value;
            }

            this.Port = int.Parse(element.GetAttribute("port"), CultureInfo.InvariantCulture);
            this.Guid = element.GetAttribute("guid");
            this.Busy = bool.Parse(element.GetAttribute("busy"));

            if (element.HasAttribute("processorCount"))
            {
                this.ProcessorCount = int.Parse(element.GetAttribute("processorCount"),
                    CultureInfo.InvariantCulture);
            }

            if (element.HasAttribute("jobWorking"))
            {
                this.JobWorking = bool.Parse(element.GetAttribute("jobWorking"));
            }
        }

        #endregion

        #region Override Object methods

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            return Name + "@" + Id;
        }

        #endregion
    }
}