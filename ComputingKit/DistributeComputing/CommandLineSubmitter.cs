//----------------------------------------------------------------------------
// <copyright file="CommandLineSubmitter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements CommandLine job submitter
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Xml;

    /// <summary>
    /// Command line job submitter.
    /// </summary>
    public class CommandLineSubmitter : IDisposable
    {
        #region Fields

        private UdpClient _udpSender = new UdpClient();

        private string _serverIP;
        private int _serverPort;

        private object _syncOnXmlDataObject = new object();
        private string _receivedXmlData;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineSubmitter"/> class.
        /// </summary>
        /// <param name="serverIP">Server IP.</param>
        /// <param name="serverPort">Server Port.</param>
        public CommandLineSubmitter(string serverIP, int serverPort)
        {
            if (string.IsNullOrEmpty(serverIP))
            {
                throw new ArgumentNullException("serverIP");
            }

            ServerIP = serverIP;
            ServerPort = serverPort;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Server IP.
        /// </summary>
        public string ServerIP
        {
            get
            {
                return _serverIP;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _serverIP = value;
            }
        }

        /// <summary>
        /// Gets or sets Server port.
        /// </summary>
        public int ServerPort
        {
            get { return _serverPort; }
            set { _serverPort = value; }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Submit job.
        /// </summary>
        /// <param name="taskName">Task name.</param>
        /// <param name="jobName">Job name.</param>
        /// <param name="command">Command.</param>
        /// <param name="arguments">Arguments.</param>
        /// <param name="doneFile">File path to indicate this job is done.</param>
        public void Submit(string taskName, string jobName,
            string command, string arguments, string doneFile)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }

            if (string.IsNullOrEmpty(jobName))
            {
                throw new ArgumentNullException("jobName");
            }

            if (string.IsNullOrEmpty(arguments))
            {
                throw new ArgumentNullException("arguments");
            }

            Job job = new Job(CommandLineServerWedge.WedgeName, taskName);
            job.Name = jobName;
            job.Command = command;
            job.Arguments = arguments;

            if (!string.IsNullOrEmpty(doneFile))
            {
                job.DoneFile = doneFile;
                job.Name = Path.GetFileName(doneFile);
            }

            Submit(job);
        }

        /// <summary>
        /// Submit job.
        /// </summary>
        /// <param name="job">Job to submit.</param>
        public void Submit(Job job)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            XmlDocument dom = new XmlDocument();
            XmlElement ele = dom.CreateElement(MessageType.JobManage.ToString());
            ele.SetAttribute("command", CommandType.JobSubmit.ToString());

            string jobXml = job.ToXml();
            ele.InnerXml = jobXml;

            ProcessNode.SendMessage(_udpSender, ele.OuterXml, ServerIP, ServerPort);
        }

        /// <summary>
        /// Remain unfinished job count.
        /// </summary>
        /// <param name="taskName">Task name.</param>
        /// <returns>Unfinished job count.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Ignore.")]
        public int RemainJobCount(string taskName)
        {
            int listenPort = SocketHelper.FindSuitablePort(6600, 6699);
            if (listenPort == 0)
            {
                Console.WriteLine("Can not found suitable soket port from 6600 to 6699 to submit job");
                return -1;
            }

            try
            {
                using (UdpClient receiver = new UdpClient(listenPort))
                using (UdpClient sernder = new UdpClient())
                {
                    try
                    {
                        lock (_syncOnXmlDataObject)
                        {
                            _receivedXmlData = null;
                        }

                        receiver.BeginReceive(new AsyncCallback(AsyncReceivePackage), receiver);

                        XmlDocument dom = new XmlDocument();
                        XmlElement ele = dom.CreateElement(MessageType.JobManage.ToString());
                        ele.SetAttribute("command", CommandType.JobQuery.ToString());
                        if (!string.IsNullOrEmpty(taskName))
                        {
                            ele.SetAttribute("taskName", taskName);
                        }

                        NodeInfo ni = new NodeInfo();
                        ni.Name = System.Environment.MachineName;
                        ni.IP = SocketHelper.LocalIP;
                        ni.Port = listenPort;

                        XmlElement nodeEle = ni.ToXml(dom);
                        ele.AppendChild(nodeEle);

                        ProcessNode.SendMessage(sernder, ele.OuterXml, ServerIP, ServerPort);

                        TimeSpan duration = new TimeSpan(0, 0, 3);
                        DateTime startTime = DateTime.Now;
                        while (true)
                        {
                            Thread.Sleep(100);
                            lock (_syncOnXmlDataObject)
                            {
                                if (_receivedXmlData != null)
                                {
                                    break;
                                }
                            }

                            if (DateTime.Now.Ticks - startTime.Ticks > duration.Ticks)
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        ProcessNode.SendMessage(sernder,
                            BasicSignal.QuitUdpSocket.ToString(),
                            SocketHelper.LocalIP, listenPort);
                        Thread.Sleep(200);
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }

            if (_receivedXmlData != null &&
                _receivedXmlData.LastIndexOf(BasicSignal.QuitUdpSocket.ToString(), StringComparison.OrdinalIgnoreCase) < 0)
            {
                try
                {
                    XmlDocument dom = new XmlDocument();
                    dom.LoadXml(_receivedXmlData);

                    // query returned
                    int nonScheduled = int.Parse(dom.DocumentElement.GetAttribute("non-scheduled"),
                        CultureInfo.InvariantCulture);
                    int running = int.Parse(dom.DocumentElement.GetAttribute("running"),
                        CultureInfo.InvariantCulture);
                    int dispatched = int.Parse(dom.DocumentElement.GetAttribute("dispatched"),
                        CultureInfo.InvariantCulture);

                    return nonScheduled + running + dispatched;
                }
                catch (XmlException e)
                {
                    Console.WriteLine("RemainJobCount error on message {0}", _receivedXmlData);
                    Console.WriteLine(e.ToString());
                    return -1;
                }
            }

            return -1;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1063:ImplementIDisposableCorrectly", Justification = "Ignore.")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _udpSender.Close();
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Receiving UDP package procedure.
        /// </summary>
        /// <param name="ar">IAsyncResult.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore.")]
        protected void AsyncReceivePackage(IAsyncResult ar)
        {
            if (ar == null)
            {
                throw new ArgumentNullException("ar");
            }

            try
            {
                UdpClient receiver = (UdpClient)ar.AsyncState;
                IPEndPoint ep = new IPEndPoint(0, 0);
                byte[] data = receiver.EndReceive(ar, ref ep);
                lock (_syncOnXmlDataObject)
                {
                    _receivedXmlData = UnicodeEncoding.Unicode.GetString(data);
                }
            }
            catch (SocketException se)
            {
                Console.Error.WriteLine(se.ToString());
            }
        }

        #endregion
    }
}