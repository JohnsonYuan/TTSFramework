//----------------------------------------------------------------------------
// <copyright file="ObjectSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class for serialize and deserialize the object
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.IO
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>  
    /// The class of object serialzier.
    /// </summary> 
    public class ObjectSerializer
    {
        /// <summary>  
        /// Temporary object folder.
        /// </summary> 
        public const string TempObjPath = "TempObjectSerializaitonFolder";

        /// <summary>
        /// Serialize the object to binary file.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="path">The path to save the object.</param>
        /// <param name="objectName">The name of the object.</param>
        /// <returns name ="string">The final path.</returns>  
        public static string Serialize(object obj, string path, string objectName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("The object should not be null");
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("The path should not be null");
            }
            
            if (string.IsNullOrEmpty(objectName))
            {
                throw new ArgumentNullException("The objectName should not be null");
            }

            Helper.EnsureFolderExistForFile(path);

            var tempDirInfo = Directory.CreateDirectory(Path.Combine(path, TempObjPath));
            var objPath = Path.Combine(tempDirInfo.FullName, objectName);
            using (FileStream fs = new FileStream(objPath, FileMode.Create))
            {
                BinaryFormatter bFormat = new BinaryFormatter();
                bFormat.Serialize(fs, obj);
            }

            return objPath;
        }

        /// <summary>  
        /// Deserialize the object from binary file.
        /// </summary>  
        /// <param name="path">The location of object.</param>  
        /// <returns name ="object">Object.</returns>  >
        public static object Deserialize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("The path should not be null");
            }

            Helper.CheckFileExists(path);

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BinaryFormatter bFormat = new BinaryFormatter();
                object obj = (object)bFormat.Deserialize(fs);
                return obj;
            }
        } 
    }
}