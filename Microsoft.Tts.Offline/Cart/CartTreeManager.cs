//----------------------------------------------------------------------------
// <copyright file="CartTreeManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements CartTreeManager
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Cart
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Tts.Offline.Cart;
    using Microsoft.Tts.Offline.Interop;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// CartTree Manager.
    /// </summary>
    public class CartTreeManager
    {
        #region Fields

        private Dictionary<int, CartTree> _cartTrees = new Dictionary<int, CartTree>();

        private MetaCart _metaCart;

        private string _unitDescriptFile;
        private string _cartTreeDir;
        private string _cartQuestionFile;

        #endregion

        #region Properties

        /// <summary>
        /// Gets CART tree dictionary, indexed by unit id.
        /// </summary>
        public Dictionary<int, CartTree> CartTrees
        {
            get { return _cartTrees; }
        }

        /// <summary>
        /// Gets or sets Unit description file path.
        /// </summary>
        public string UnitDescriptFile
        {
            get
            {
                return _unitDescriptFile;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _unitDescriptFile = value;
            }
        }

        /// <summary>
        /// Gets or sets Meta CART information.
        /// </summary>
        public MetaCart MetaCart
        {
            get
            {
                return _metaCart;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _metaCart = value;
            }
        }

        /// <summary>
        /// Gets or sets CART tree directory.
        /// </summary>
        public string CartTreeDir
        {
            get
            {
                return _cartTreeDir;
            }

            set 
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _cartTreeDir = value;
            }
        }

        /// <summary>
        /// Gets or sets CART question file path.
        /// </summary>
        public string CartQuestionFile
        {
            get
            {
                return _cartQuestionFile;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _cartQuestionFile = value;
            }
        }
        #endregion

        #region Operations

        /// <summary>
        /// Compose CRT file.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="engine">Engine type.</param>
        /// <param name="unitListFile">Unit list file path.</param>
        /// <param name="cartQuestionFile">CART question file path.</param>
        /// <param name="binTreeDir">Binary CART tree directory.</param>
        /// <param name="crtFile">File list path.</param>
        public static void ComposeCrtFile(Language language, EngineType engine,
            string unitListFile, string cartQuestionFile, string binTreeDir,
            string crtFile)
        {
            // parse CART question data file
            MetaCart meta = new MetaCart(language, engine);
            meta.Initialize(cartQuestionFile);
            byte[] metaData = meta.ToBytes();

            // Handle unit list file
            short unitIndex = 1;
            int cartIndexingOffset = 0;
            Collection<CartIndexingSerial> cartIndexingItems = new Collection<CartIndexingSerial>();
            Collection<string> cartFiles = new Collection<string>();
            using (StreamReader sr = new StreamReader(unitListFile))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    // each unit name
                    string unitName = line.Trim();
                    if (string.Compare(unitName, "_sil_", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (unitIndex == 1)
                        {
                            // ignore the first silence unit in the list,
                            // and all un-silence unit will start indexing from 1
                            continue;
                        }
                        else
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "silence unit _sil_ found in the middle of the unit list file [{0}].",
                                unitListFile);
                            throw new InvalidDataException(message);
                        }
                    }

                    string binCartFile = Path.Combine(binTreeDir, unitName + ".tree");
                    if (!File.Exists(binCartFile))
                    {
                        throw Helper.CreateException(typeof(FileNotFoundException),
                            binCartFile);
                    }

                    cartFiles.Add(binCartFile);
                    FileInfo fi = new FileInfo(binCartFile);
                    CartIndexingSerial cartIndexing = new CartIndexingSerial();
                    cartIndexing.UnitTypeId = unitIndex;
                    cartIndexing.StartOffset = cartIndexingOffset;
                    cartIndexing.CartFileSize = (int)fi.Length;
                    cartIndexingOffset += cartIndexing.CartFileSize;

                    cartIndexingItems.Add(cartIndexing);
                    unitIndex++;
                }
            }

            CartHeaderSerial header = new CartHeaderSerial();
            header.FeatureOffset = (uint)Marshal.SizeOf(typeof(CartHeaderSerial));
            header.FeatureSize = (uint)metaData.Length;

            header.CartIdxOffset = header.FeatureOffset + header.FeatureSize;
            header.CartIdxNum = (uint)cartIndexingItems.Count;
            header.CartDataOffset = header.CartIdxOffset
                + (header.CartIdxNum * (uint)Marshal.SizeOf(typeof(CartIndexingSerial)));

            using (FileStream fs = new FileStream(crtFile, FileMode.Create, FileAccess.Write))
            {
                // write header information of CRT file
                byte[] data = header.ToBytes();
                fs.Write(data, 0, data.Length);

                // write CART question
                fs.Write(metaData, 0, metaData.Length);

                // write CART tree indexing items for each unit CART tree
                foreach (CartIndexingSerial cartIndexingItem in cartIndexingItems)
                {
                    data = cartIndexingItem.ToBytes();
                    fs.Write(data, 0, data.Length);
                }

                // write CART data files
                foreach (string binTreeFile in cartFiles)
                {
                    data = File.ReadAllBytes(binTreeFile);
                    fs.Write(data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Initialize the instance of CART tree manager
        /// 1) load and parse the CART question file
        /// 2) initialize unit description table data from the phoneme of the 
        ///    specified language and engine type.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="engine">Engine type.</param>
        /// <param name="cartQuestionFilePath">CART question file path.</param>
        public void Initialize(Language language, EngineType engine, 
            string cartQuestionFilePath)
        {
            if (string.IsNullOrEmpty(cartQuestionFilePath))
            {
                throw new ArgumentNullException("cartQuestionFilePath");
            }

            _cartQuestionFile = cartQuestionFilePath;
            _metaCart = new MetaCart(language, engine);
            _metaCart.Initialize(_cartQuestionFile);
        }

        #endregion

        #region Structs

        /// <summary>
        /// CART header.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct CartHeaderSerial
        {
            internal uint Version;
            internal uint FeatureOffset;
            internal uint FeatureSize;
            internal uint CartIdxOffset;
            internal uint CartIdxNum;
            internal uint Reserverd1;
            internal uint CartDataOffset;
            internal uint Reserverd2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 224)]
            internal byte[] Peddings;

            /// <summary>
            /// Read a CartHeaderSerial block from binary stream.
            /// </summary>
            /// <param name="br">Binary stream to read.</param>
            /// <returns>CartHeaderSerial.</returns>
            public static CartHeaderSerial Read(BinaryReader br)
            {
                int size = Marshal.SizeOf(typeof(CartHeaderSerial));
                byte[] buff = br.ReadBytes(size);

                if (buff.Length != size)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found, for there is no enough data for CART tree header.");
                    throw new InvalidDataException(message);
                }

                IntPtr ptr = IntPtr.Zero;
                try
                {
                    ptr = Marshal.AllocHGlobal(size);
                    Marshal.Copy(buff, 0, ptr, size);
                    return (CartHeaderSerial)Marshal.PtrToStructure(ptr, typeof(CartHeaderSerial));
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            /// <summary>
            /// Convert to byte array.
            /// </summary>
            /// <returns>Byte array.</returns>
            public byte[] ToBytes()
            {
                byte[] buff = new byte[Marshal.SizeOf(typeof(CartHeaderSerial))];

                IntPtr ptr = IntPtr.Zero;
                try
                {
                    ptr = Marshal.AllocHGlobal(buff.Length);
                    Marshal.StructureToPtr(this, ptr, false);
                    Marshal.Copy(ptr, buff, 0, buff.Length);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                return buff;
            }
        }

        /// <summary>
        /// CART indexing.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct CartIndexingSerial
        {
            internal short UnitTypeId;
            internal short Padding;
            internal int StartOffset;
            internal int CartFileSize;

            /// <summary>
            /// Read a CartIndexingSerial block from binary stream.
            /// </summary>
            /// <param name="br">Binary stream to read CART indexing.</param>
            /// <returns>CartIndexingSerial.</returns>
            public static CartIndexingSerial Read(BinaryReader br)
            {
                int size = Marshal.SizeOf(typeof(CartIndexingSerial));
                byte[] buff = br.ReadBytes(size);

                if (buff.Length != size)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found, for there is no enough data for CART indexing.");
                    throw new InvalidDataException(message);
                }

                IntPtr ptr = IntPtr.Zero;
                try
                {
                    ptr = Marshal.AllocHGlobal(size);
                    Marshal.Copy(buff, 0, ptr, size);
                    return (CartIndexingSerial)Marshal.PtrToStructure(ptr, typeof(CartIndexingSerial));
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            /// <summary>
            /// Convert to byte array.
            /// </summary>
            /// <returns>Byte array.</returns>
            public byte[] ToBytes()
            {
                byte[] buff = new byte[Marshal.SizeOf(typeof(CartIndexingSerial))];

                IntPtr ptr = IntPtr.Zero;
                try
                {
                    ptr = Marshal.AllocHGlobal(buff.Length);
                    Marshal.StructureToPtr(this, ptr, false);
                    Marshal.Copy(ptr, buff, 0, buff.Length);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                return buff;
            }
        }

        #endregion
    }
}