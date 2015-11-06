//----------------------------------------------------------------------------
// <copyright file="AggregatorServer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements aggregator server
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Aggregator server.
    /// </summary>
    public class AggregatorServer : ProcessNode
    {
        #region Fields

        private DateTime _lastResourceUpdated = DateTime.Now;
        private TimeSpan _minUpdateDuration = new TimeSpan(0, 0, 10);

        private int _localPort;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregatorServer"/> class.
        /// </summary>
        /// <param name="aggregatorIp">Aggregator IP address string.</param>
        /// <param name="aggregatorPort">Aggregator port.</param>
        public AggregatorServer(string aggregatorIp, int aggregatorPort)
            : base(aggregatorIp, aggregatorPort, NodeType.Aggregator)
        {
            LocalPort = aggregatorPort;

            HookEvents();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Local port.
        /// </summary>
        public int LocalPort
        {
            get { return _localPort; }
            set { _localPort = value; }
        }

        /// <summary>
        /// Gets Local listening port.
        /// </summary>
        public override int ListenPort
        {
            get { return LocalPort; }
        }

        /// <summary>
        /// Gets Ticks.
        /// </summary>
        private long UpdateEclipsedTicks
        {
            get { return DateTime.Now.Ticks - _lastResourceUpdated.Ticks; }
        } 

        #endregion

        #region Override methods

        /// <summary>
        /// Process after monitor tick.
        /// </summary>
        protected override void AfterMonitorTick()
        {
            if (UpdateEclipsedTicks > _minUpdateDuration.Ticks)
            {
                BroadcastResource();
            }
        }

        /// <summary>
        /// Dispose resource.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion

        #region events handlers

        /// <summary>
        /// Handle NodeUpdatedEvent event.
        /// </summary>
        /// <param name="sender">Event sendor.</param>
        /// <param name="e">Event argument.</param>
        private void AggregatorServer_NodeUpdatedEvent(object sender, NodeInfoEventArgs e)
        {
            if (UpdateEclipsedTicks > 1000 * 10000)
            {
                BroadcastResource();
            }
        }

        /// <summary>
        /// Handle NodeRemovedEvent event.
        /// </summary>
        /// <param name="sender">Event sendor.</param>
        /// <param name="e">Event argument.</param>
        private void AggregatorServer_NodeRemovedEvent(object sender, NodeInfoEventArgs e)
        {
            if (UpdateEclipsedTicks > 500 * 10000)
            {
                BroadcastResource();
            }
        }

        /// <summary>
        /// Handle NodeAddedEvent event.
        /// </summary>
        /// <param name="sender">Event sendor.</param>
        /// <param name="e">Event argument.</param>
        private void AggregatorServer_NodeAddedEvent(object sender, NodeInfoEventArgs e)
        {
            if (UpdateEclipsedTicks > 500 * 10000)
            {
                BroadcastResource();
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Hook events.
        /// </summary>
        private void HookEvents()
        {
            this.NodeAddedEvent += new EventHandler<NodeInfoEventArgs>(AggregatorServer_NodeAddedEvent);
            this.NodeRemovedEvent += new EventHandler<NodeInfoEventArgs>(AggregatorServer_NodeRemovedEvent);
            this.NodeUpdatedEvent += new EventHandler<NodeInfoEventArgs>(AggregatorServer_NodeUpdatedEvent);
        }

        /// <summary>
        /// Broadcast resource availabele to all nodes.
        /// </summary>
        private void BroadcastResource()
        {
            _lastResourceUpdated = DateTime.Now;

            int maxNodeNumberEachTime = 10;

            XmlDocument dom = new XmlDocument();
            XmlElement ele = dom.CreateElement(MessageType.Resource.ToString());

            ele.SetAttribute("type", NodeType.Execution.ToString());
            lock (((IProcessNode)this).Nodes)
            {
                int index = 0;
                foreach (NodeInfo info in ((IProcessNode)this).Nodes.Values)
                {
                    if (info.NodeType == NodeType.Execution
                        && !info.Busy)
                    {
                        XmlElement nodeELe = info.ToXml(dom);
                        ele.AppendChild(nodeELe);
                        index++;

                        if (index == maxNodeNumberEachTime)
                        {
                            SendToAll(ele.OuterXml, NodeType.Coordinator);
                            ele.RemoveAll();
                            ele.SetAttribute("type", NodeType.Execution.ToString());
                            index = 0;
                        }
                    }
                }

                if (index > 0)
                {
                    SendToAll(ele.OuterXml, NodeType.Coordinator);
                }
            }
        }

        #endregion
    }
}