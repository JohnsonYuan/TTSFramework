//----------------------------------------------------------------------------
// <copyright file="SocketHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements socket helper functions
// </summary>
//----------------------------------------------------------------------------

namespace DistributeComputing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Socket helper.
    /// </summary>
    public static class SocketHelper
    {
        #region Properties

        /// <summary>
        /// Gets Local IP address.
        /// </summary>
        public static string LocalIP
        {
            get
            {
                string ipv4 = null;

                IPHostEntry ip = Dns.GetHostEntry(System.Environment.MachineName);

                if (ip.AddressList != null)
                {
                    foreach (IPAddress addr in ip.AddressList)
                    {
                        if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ipv4 = addr.ToString();
                        }
                    }
                }

                return ipv4;
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Search un-used socket port.
        /// </summary>
        /// <param name="start">Search from.</param>
        /// <param name="end">Search end.</param>
        /// <returns>Un-used socket port.</returns>
        public static int FindSuitablePort(int start, int end)
        {
            int maxTryCount = end - start;
            int tryCount = 0;
            Random random = new Random();
            while (true)
            {
                tryCount++;
                int port = random.Next(maxTryCount) + start;
                if (!SocketHelper.IsPortInUsed(port))
                {
                    return port;
                }

                if (tryCount > maxTryCount)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Tell whether the socket port is in used.
        /// </summary>
        /// <param name="testPort">Port to test.</param>
        /// <returns>True if port is used, otherwise false.</returns>
        public static bool IsPortInUsed(int testPort)
        {
            bool result = false;
            string statFilePath = Helper.GetTempFileName();

            // dump netstat information into the file.
            CommandLine.RunCommandToFile("netstat", "-a -n", statFilePath, false,
                Environment.CurrentDirectory);

            using (StreamReader sr = new StreamReader(statFilePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] items = line.Split(new char[] { ' ', ':' },
                        StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length < 3)
                    {
                        continue;
                    }

                    int usedPort = 0;
                    if (!int.TryParse(items[2], System.Globalization.NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out usedPort))
                    {
                        continue;
                    }

                    if (usedPort == testPort)
                    {
                        result = true;
                        break;
                    }
                }
            }

            Helper.SafeDelete(statFilePath);

            return result;
        }

        #endregion
    }
}