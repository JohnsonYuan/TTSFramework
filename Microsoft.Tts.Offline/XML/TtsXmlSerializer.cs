//----------------------------------------------------------------------------
// <copyright file="TtsXmlSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements XML serialization
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.XML
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// TtsXmlSerializer.
    /// </summary>
    public class TtsXmlSerializer
    {
        /// <summary>
        /// LoadConfig.
        /// </summary>
        /// <typeparam name="T">Serialze type.</typeparam>
        /// <param name="configFile">ConfigFile.</param>
        /// <param name="schema">Schema.</param>
        /// <returns>Serialzed object.</returns>
        public static T LoadConfig<T>(string configFile, XmlSchema schema)
        {
            Helper.ThrowIfNull(schema);
            if (string.IsNullOrEmpty(configFile))
            {
                throw new ArgumentNullException("configFile");
            }

            if (!File.Exists(configFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), configFile);
            }

            XmlHelper.Validate(configFile, schema);

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            T config = default(T);

            try
            {
                using (FileStream file = new FileStream(configFile, FileMode.Open, FileAccess.Read))
                {
                    config = (T)serializer.Deserialize(file);
                }
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "\"{0}\" format error.{1}Details: {2}",
                    configFile, Environment.NewLine, e.Message), e);
            }

            return config;
        }

        /// <summary>
        /// SaveConfig.
        /// </summary>
        /// <typeparam name="T">T.</typeparam>
        /// <param name="t">T t.</param>
        /// <param name="destFile">DestFile.</param>
        public static void SaveConfig<T>(T t, string destFile)
        {
            if (t == null)
            {
                throw new ArgumentNullException("T");
            }

            if (string.IsNullOrEmpty(destFile))
            {
                throw new ArgumentNullException("destFile");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StreamWriter sw = new StreamWriter(destFile, false, Encoding.UTF8))
            {
                XmlTextWriter writer = new XmlTextWriter(sw);
                writer.Formatting = Formatting.Indented;
                serializer.Serialize(writer, t);
            }
        }
    }
}