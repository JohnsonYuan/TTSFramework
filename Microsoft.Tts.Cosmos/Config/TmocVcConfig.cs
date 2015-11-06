// ----------------------------------------------------------------------------
// <copyright file="TmocVcConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// 
// <summary>
//     This module implements TmocVcConfig
// </summary>
// ----------------------------------------------------------------------------

namespace Microsoft.Tts.Cosmos.Config
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Cosmos.TMOC;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Load Configures form xml.
    /// </summary>
    public class TmocVcConfig
    {
        #region Constant Value

        /// <summary>
        /// The default vc proxy is noproxy.
        /// </summary>
        public const string DefaulVCProxy = VcClient.VC.NoProxy;

        /// <summary>
        /// The default number of machines dedicated to this job.
        /// now only used in the first phrases auto-set mode.
        /// </summary>
        public const int DefaulMaxTokenNum = 30;

        /// <summary>
        /// The min token number for a job.
        /// </summary>
        public const int DefaulMinTokenNum = 30;

        /// <summary>
        /// The default value of work priority.
        /// </summary>
        public const int DefaultPriority = 1000;

        /// <summary>
        /// The default value of maximum vertex computing time for each vertex.
        /// 3 hours default, unit is seconds.
        /// will influence the data partition for cosmos run.
        /// </summary>
        public const int DefaultMaxVertexTime = 20 * 60;  // 20 minutes.

        /// <summary>
        /// The min number of GROUP INTO when generating sstream.
        /// </summary>
        public const int MinStreamPartitioNum = 2;

        /// <summary>
        /// The max number of GROUP INTO when generating sstream.
        /// </summary>
        public const int MaxStreamPartitioNum = 2499;

        /// <summary>
        /// The longest job time allowed on VC in seconds.
        /// Althrough the setting is 7 days in VC.
        /// We would like a shorter time, say, 3 days.
        /// </summary>
        public static int JobTimeoutVC = 3600 * 15;

        #endregion

        #region  Private Fields

        private static XmlSchema _schema;
        private string nameVC = "http://cosmos05.osdinfra.net:88/cosmos/ipe.adhoc/";
        private int priorityVC = DefaultPriority;
        private string proxyVC = DefaulVCProxy;
        private string tmocDataPath = string.Empty;
        private int jobTimeoutInHoursVC;
        private int _maxTokenNumber = DefaulMaxTokenNum;
        private int _maxVertexTime = DefaultMaxVertexTime;
        private bool _localRun = true;

        #endregion Private Fields

        #region Method

        /// <summary>
        /// Initializes a new instance of the <see cref="TmocVcConfig" /> class.
        /// </summary>
        /// <param name="filePath">The file path of the config file.</param>
        public TmocVcConfig(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                Load(filePath);

                // Check();
            }
            else
            {
                throw new ArgumentException(string.Format("Make sure {0} is existed", filePath));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TmocVcConfig" /> class.
        /// </summary>
        public TmocVcConfig()
        {
        }

        #region Properties

        /// <summary>
        /// Gets configuration schema.
        /// </summary>
        public static XmlSchema ConfigSchema
        {
            get
            {
                if (_schema == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _schema = XmlHelper.LoadSchemaFromResource(assembly, "Microsoft.Tts.Cosmos.Config.TmocVcConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets the name of VC.
        /// </summary>
        public string VcName
        {
            get
            {
                return nameVC;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                nameVC = value;
            }
        }

        /// <summary>
        /// Gets or sets the priority of VC.
        /// </summary>
        public int VcPriority
        {
            get
            {
                return priorityVC;
            }

            set
            {
                priorityVC = value;
            }
        }

        /// <summary>
        /// Gets or sets the proxy name of VC.
        /// </summary>
        public string VcProxy
        {
            get
            {
                return proxyVC;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    proxyVC = string.Empty;
                }
                else
                {
                    proxyVC = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the path of TMOC.
        /// </summary>
        public string TmocDataPath
        {
            get
            {
                return tmocDataPath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                tmocDataPath = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the indicator if it's local run.
        /// </summary>
        public bool LocalRun
        {
            get
            {
                return _localRun;
            }

            set
            {
                _localRun = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum token number the job can get.
        /// </summary>
        public int MaxTokenNum
        {
            get
            {
                return _maxTokenNumber;
            }

            set
            {
                _maxTokenNumber = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum vertex time the job can occupy.
        /// </summary>
        public int MaxVertexTime
        {
            get
            {
                return _maxVertexTime;
            }

            set
            {
                _maxVertexTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout in hours the job submit to VC.
        /// </summary>
        public int VcJobTimeoutInHours
        {
            get
            {
                return jobTimeoutInHoursVC;
            }

            set
            {
                jobTimeoutInHoursVC = value;
            }
        }

        #endregion Public Properties

        /// <summary>
        /// Get the recommended core num for this processing, just a empirical value.
        /// </summary>
        /// <param name="nameVC">The name of VC.</param>
        /// <param name="speechDuration">Speech duration in seconds.</param>
        /// <returns>RecToken.</returns>
        public static int GetRecommendedTokenNum(string nameVC, double speechDuration)
        {
            // a random number.
            int totalToken = 10;
            if (!string.IsNullOrEmpty(nameVC))
            {
                VCList vclist = new VCList();
                totalToken = vclist.GetVcTokeNum(nameVC);
            }

            // Min(Max(30,recommend core for the data size), 80%*total cores).
            // Linear curve.
            // [0,0]->[10000,1000]->[x,1000].
            // 10000hours allocated with 1000 cores.
            int recTokenNum = Convert.ToInt32(speechDuration * 1000.0 / (10000.0 * 3600.0));
            int maxTokenNum = Convert.ToInt32(totalToken * 80.0 / 100.0);
            int minTokenNum = DefaulMinTokenNum;
            recTokenNum = Math.Min(Math.Max(minTokenNum, recTokenNum), maxTokenNum);
            return recTokenNum;
        }

        /// <summary>
        /// Calculate the expected vertex number to process this corpus according to their duration and make sure the vertex will not timeout the
        /// expectedVertexProcessTime.
        /// </summary>
        /// <param name="corpusarr">The array of corpus.</param>
        /// <param name="processRealTimeFactor">The real time factor of this processing.</param>
        /// <param name="expectedVertexProcessTime">The expected time of each vertex run.</param>
        /// <returns>The expected vertex number.</returns>
        public int CalExpectedVertexNum(TmocCorpus[] corpusarr, double processRealTimeFactor, int expectedVertexProcessTime)
        {
            return CalExpectedVertexNum(TmocCorpus.CalSpeechDuration(corpusarr), processRealTimeFactor, expectedVertexProcessTime);
        }

        /// <summary>
        /// Calculate the expected vertex number to process this corpus according to their duration and make sure the vertex will not timeout the
        /// expectedVertexProcessTime.
        /// </summary>
        /// <param name="speechDuration">The duration of speech corpus.</param>
        /// <param name="processRealTimeFactor">The real time factor of this processing.</param>
        /// <param name="expectedVertexProcessTime">The expected time of each vertex run.</param>
        /// <returns>The expected vertex number.</returns>
        public int CalExpectedVertexNum(double speechDuration, double processRealTimeFactor, int expectedVertexProcessTime)
        {
            int vertexNum = Convert.ToInt32(processRealTimeFactor * speechDuration / expectedVertexProcessTime);

            // We would like to use all the machines allocated to this job.
            vertexNum = Math.Max(_maxTokenNumber, vertexNum);

            // The minimum by cosmo is 2.
            vertexNum = Math.Max(MinStreamPartitioNum, vertexNum);

            // The max limit by cosmos is 2500.
            vertexNum = Math.Min(MaxStreamPartitioNum, vertexNum);

            return vertexNum;
        }

        /// <summary>
        /// Get the nebular parameters for model training purpose.
        /// </summary>
        /// <param name="streamName">Stream name.</param>
        /// <param name="speechDuration">Speech duration.</param>
        /// <returns>Exract group size.</returns>
        public string GetExractGroupSizeModelTraining(string streamName, double speechDuration)
        {
            return GetExractGroupSize(streamName, speechDuration, TmocConstants.ModelTrainRTF);
        }

        /// <summary>
        /// Get the nebular parameters according to the speech duration and speechProcessRTF.
        /// </summary>
        /// <param name="streamName">Stream name.</param>
        /// <param name="speechDuration">Speech duration.</param>
        /// <param name="speechProcessingRTF">Speech processing RTF.</param>
        /// <returns>Nebular arguments.</returns>
        public string GetExractGroupSize(string streamName, double speechDuration, double speechProcessingRTF)
        {
            int vertexNum = CalExpectedVertexNum(speechDuration, speechProcessingRTF, TmocGlobal.Instance.VcConfig.MaxVertexTime);
            long extractGroupDefaultDataSize = TmocFile.FileSize(streamName) / Convert.ToInt64(vertexNum);
            string nebularArguments = null;
            if (!_localRun)
            {
                nebularArguments = string.Format(@"-ExtractGroupDefaultDataSize {0}", extractGroupDefaultDataSize);
            }

            return nebularArguments;
        }

        /// <summary>
        /// Load tmoc vc config from XML config file.
        /// </summary>
        /// <param name="filePath">Config filepath.</param>
        private void Load(string filePath)
        {
            // check the configuration file first.
            try
            {
                XmlHelper.Validate(filePath, ConfigSchema);
            }
            catch (InvalidDataException ide)
            {
                string message = Helper.NeutralFormat("The configuration file [{0}] error is found. {1} {2}",
                    filePath, System.Environment.NewLine, ide.Message);
                throw new InvalidDataException(message, ide);
            }

            // Load configuration.
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", ConfigSchema.TargetNamespace);
            dom.Load(filePath);

            // Test whether the namespace of the configuration file is designed.
            if (string.Compare(dom.DocumentElement.NamespaceURI,
                ConfigSchema.TargetNamespace, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string message = Helper.NeutralFormat(
                    "The configuration xml file [{0}] must use the schema namespace [{1}]. " +
                    "Currently the config file uses namespace [{2}]",
                    filePath, ConfigSchema.TargetNamespace, dom.DocumentElement.NamespaceURI);
                throw new InvalidDataException(message);
            }

            XmlNode node;
            node = dom.DocumentElement.SelectSingleNode(@"tts:VC/tts:VcName", nsmgr);
            VcName = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:VC/tts:VcProxy", nsmgr);
            VcProxy = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:VC/tts:Priority", nsmgr);
            VcPriority = int.Parse(node.InnerText);

            node = dom.DocumentElement.SelectSingleNode(@"tts:VC/tts:LocalRun", nsmgr);
            LocalRun = bool.Parse(node.InnerText);

            node = dom.DocumentElement.SelectSingleNode(@"tts:TmocPath", nsmgr);
            TmocDataPath = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:VC/tts:Parameter", nsmgr);

            XmlElement parameter = node as XmlElement;

            VcJobTimeoutInHours = int.Parse(parameter.GetAttribute("VcJobTimeoutInHours"));
            MaxTokenNum = int.Parse(parameter.GetAttribute("MaxTokenNum"));
            MaxVertexTime = int.Parse(parameter.GetAttribute("MaxVertexTime"));
        }

        /// <summary>
        /// Do the check, if not valid, then throw exception.
        /// </summary>
        private void Check()
        {
            if (VcName != string.Empty &&
                !TmocDirectory.Exists(TmocPath.Combine(VcName, @"my/")))
            {
                throw new Exception(string.Format("the vc {0} does not exist", VcName));
            }
        }

        #endregion
    }
}
