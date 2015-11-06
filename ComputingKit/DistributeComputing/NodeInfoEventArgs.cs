//----------------------------------------------------------------------------
// <copyright file="NodeInfoEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements NodeInfo event argments
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// NodeInfo event argments.
    /// </summary>
    public class NodeInfoEventArgs : EventArgs
    {
        #region Fields

        private NodeInfo _nodeInfo;
        private XmlDocument _message;
        private string _remark;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeInfoEventArgs"/> class.
        /// </summary>
        /// <param name="nodeInfo">Node info.</param>
        public NodeInfoEventArgs(NodeInfo nodeInfo)
        {
            if (nodeInfo == null)
            {
                throw new ArgumentNullException("nodeInfo");
            }

            NodeInfo = nodeInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeInfoEventArgs"/> class.
        /// </summary>
        /// <param name="nodeInfo">Node info.</param>
        /// <param name="remark">Remark string.</param>
        public NodeInfoEventArgs(NodeInfo nodeInfo, string remark)
        {
            if (nodeInfo == null)
            {
                throw new ArgumentNullException("nodeInfo");
            }

            if (string.IsNullOrEmpty(remark))
            {
                throw new ArgumentNullException("remark");
            }

            NodeInfo = nodeInfo;
            Remark = remark;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeInfoEventArgs"/> class.
        /// </summary>
        /// <param name="nodeInfo">Node info.</param>
        /// <param name="message">XML message.</param>
        public NodeInfoEventArgs(NodeInfo nodeInfo, XmlDocument message)
        {
            if (nodeInfo == null)
            {
                throw new ArgumentNullException("nodeInfo");
            }

            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            NodeInfo = nodeInfo;
            Message = message;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Remark.
        /// </summary>
        public string Remark
        {
            get
            {
                return _remark;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _remark = value;
            }
        }

        /// <summary>
        /// Gets or sets Message of this node.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "Ignore.")]
        public XmlDocument Message
        {
            get
            {
                return _message;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _message = value;
            }
        }

        /// <summary>
        /// Gets or sets Node information.
        /// </summary>
        public NodeInfo NodeInfo
        {
            get
            {
                return _nodeInfo;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _nodeInfo = value;
            }
        }

        #endregion
    }
}