//----------------------------------------------------------------------------
// <copyright file="ControlMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements control message
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// ControlMessage.
    /// </summary>
    public class ControlMessage
    {
        #region Fields

        private string _command;
        private string _guid;
        private string _result; 

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlMessage"/> class.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <param name="guid">Guid.</param>
        /// <param name="result">Result.</param>
        public ControlMessage(string command, string guid, string result)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }

            if (string.IsNullOrEmpty(guid))
            {
                throw new ArgumentNullException("guid");
            }

            if (string.IsNullOrEmpty(result))
            {
                Result = null;
            }
            else
            {
                Result = result;
            }

            Command = command;
            Guid = guid;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Result.
        /// </summary>
        public string Result
        {
            get
            {
                return _result;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _result = null;
                }
                else
                {
                    _result = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets Guid.
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
        /// Gets or sets Command.
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

        #endregion

        #region Xml presentation

        /// <summary>
        /// Convert this object into XML presentation.
        /// </summary>
        /// <param name="nodeInfo">Node info.</param>
        /// <returns>XML string.</returns>
        public string ToXml(NodeInfo nodeInfo)
        {
            if (nodeInfo == null)
            {
                throw new ArgumentNullException("nodeInfo");
            }

            XmlDocument dom = new XmlDocument();
            XmlElement ele = dom.CreateElement(MessageType.Control.ToString());

            ele.SetAttribute("command", Command);
            ele.SetAttribute("guid", Guid);

            if (!string.IsNullOrEmpty(Result))
            {
                ele.SetAttribute("result", Result);
            }

            if (nodeInfo != null)
            {
                XmlElement subNodeInfo = nodeInfo.ToXml(dom);
                ele.AppendChild(subNodeInfo);
            }

            return ele.OuterXml;
        } 

        #endregion
    }
}