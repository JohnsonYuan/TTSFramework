//----------------------------------------------------------------------------
// <copyright file="XmlConfigureBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Implement a common base class for xml configure file loading.
//
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml.Serialization;

    /// <summary>
    /// Common base class for xml configure file loader.
    /// </summary>
    public class XmlConfigureBase
    {
        #region Operations
        /// <summary>
        /// Using XmlSerializer, Save all public properties and fields to XML file.
        /// </summary>
        /// <param name="xmlFilePath">XML file to save.</param>
        public void Save(string xmlFilePath)
        {
            Helper.EnsureFolderExistForFile(xmlFilePath);
            XmlSerializer xs = new XmlSerializer(this.GetType());
            using (TextWriter tw = new StreamWriter(xmlFilePath))
            {
                xs.Serialize(tw, this);
            }
        }

        /// <summary>
        /// Using XmlSerializer, Load all public properties and fields from XML file.
        /// </summary>
        /// <param name="xmlFilePath">XML file to load.</param>
        public void Load(string xmlFilePath)
        {
            const BindingFlags AllFieldBindingFlags = BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.Public | BindingFlags.NonPublic;

            XmlSerializer xs = new XmlSerializer(this.GetType());
            using (TextReader tr = new StreamReader(xmlFilePath))
            {
                object o = xs.Deserialize(tr);
                foreach (FieldInfo fieldInfo in this.GetType().GetFields(AllFieldBindingFlags))
                {
                    fieldInfo.SetValue(this, fieldInfo.GetValue(o));
                }
            }
        }

        #endregion
    }
}