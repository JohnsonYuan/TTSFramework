//----------------------------------------------------------------------------
// <copyright file="StringPool.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements string Pool
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;

    /// <summary>
    /// A simple string pool class, multiple instances of same string will share storage.
    /// </summary>
    public class StringPool : IDisposable
    {
        #region Fields

        private static readonly byte[] StringEnd = new byte[] { 0, 0 };

        private MemoryStream _memoryStream = new MemoryStream();

        private Dictionary<string, int> _offsetHash = new Dictionary<string, int>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the length of the memory stream.
        /// </summary>
        /// <returns>Length of the memory stream.</returns>
        public int Length
        {
            get { return (int)_memoryStream.Length; }
        }

        /// <summary>
        /// Gets the current position within the string pool.
        /// </summary>
        /// <returns>Position where new string will be put.</returns>
        public int Position
        {
            get { return (int)_memoryStream.Position; }
        }

        /// <summary>
        /// Gets all strings in this string pool.
        /// </summary>
        public string[] Strings
        {
            get
            {
                List<string> items = new List<string>();
                using (new PositionRecover(_memoryStream, 0, SeekOrigin.Begin))
                {
                    while (_memoryStream.Position < Length)
                    {
                        items.Add(ReadString());
                    }
                }

                return items.ToArray();
            }
        }

        #endregion

        #region Static opearation

        /// <summary>
        /// Put words into string pool.
        /// </summary>
        /// <param name="words">Words.</param>
        /// <param name="stringPool">String pool.</param>
        /// <param name="offsets">Offset list.</param>
        public static void WordsToStringPool(List<string> words, StringPool stringPool,
            ICollection<int> offsets)
        {
            Helper.ThrowIfNull(words);
            Helper.ThrowIfNull(stringPool);
            Helper.ThrowIfNull(offsets);

            foreach (string word in words)
            {
                offsets.Add(stringPool.PutString(word));
            }
        }

        #endregion

        #region Public operation

        /// <summary>
        /// Put string into string pool.
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>Offset in the memory stream of written string.</returns>
        public int PutString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (_offsetHash.ContainsKey(str))
            {
                return _offsetHash[str];
            }

            int offset = Position;
            byte[] buffer = Encoding.Unicode.GetBytes(str);
            _memoryStream.Write(buffer, 0, buffer.Length);
            _memoryStream.Write(StringEnd, 0, StringEnd.Length);
            _offsetHash.Add(str, offset);
            return offset;
        }

        /// <summary>
        /// Get one string from the given position of data in the string Pool.
        /// </summary>
        /// <param name="position">Position of the string in the string Pool.</param>
        /// <returns>String retrieved from string Pool.</returns>
        public string GetString(int position)
        {
            using (new PositionRecover(_memoryStream, position, SeekOrigin.Begin))
            {
                return ReadString();
            }
        }

        /// <summary>
        /// Write a block of bytes to the memory stream using data reading from buffer.
        /// </summary>
        /// <param name="buffer">Buffer to write.</param>
        /// <returns>Offset in the memory stream of written buffer.</returns>
        public int PutBuffer(byte[] buffer)
        {
            Helper.ThrowIfNull(buffer);
            int offset = Position;
            Debug.Assert(Equals(StringEnd, buffer.Skip(buffer.Length - 2).ToArray()),
                "Buffer should be a valid string.");
            _memoryStream.Write(buffer, 0, buffer.Length);

            return offset;
        }

        /// <summary>
        /// Get the buffer of the memory stream.
        /// </summary>
        /// <returns>Buffer in bytes.</returns>
        public byte[] ToArray()
        {
            return _memoryStream.ToArray();
        }

        /// <summary>
        /// Resets the lengths of current string pool to zero.
        /// </summary>
        public void Reset()
        {
            _memoryStream.SetLength(0);
            _offsetHash.Clear();
        }

        /// <summary>
        /// Saves the current contents to a binary writer.
        /// </summary>
        /// <param name="writer">The given binary writer.</param>
        public void Save(BinaryWriter writer)
        {
            writer.Write(ToArray(), 0, Length);
        }

        /// <summary>
        /// Discards the current contents and loads from a binary reader.
        /// </summary>
        /// <param name="reader">The given binary reader.</param>
        /// <param name="size">The size to read.</param>
        public void Load(BinaryReader reader, int size)
        {
            _offsetHash = new Dictionary<string, int>();
            _memoryStream = new MemoryStream();

            byte[] bytes = reader.ReadBytes(size);

            int start = 0;
            while (bytes.Length > start)
            {
                // Gets chars from bytes.
                char[] chars = Encoding.Unicode.GetChars(bytes, start, bytes.Length - 2 - start);
                _offsetHash.Add(new string(chars), start);

                // Gets the length of this chars.
                int length = Encoding.Unicode.GetByteCount(chars);

                // Then, moves the start index.
                start += length + 2; // Since two zero are written in the ending of single string.
            }

            if (start != bytes.Length)
            {
                throw new InvalidDataException("Not a valid string pool");
            }

            _memoryStream.Write(bytes, 0, bytes.Length);
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the resources used in this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the RewindableTextReader.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _memoryStream)
                {
                    _memoryStream.Dispose();
                }
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Check whether two lists are equal with each other.
        /// </summary>
        /// <typeparam name="T">Type of the element in list.</typeparam>
        /// <param name="left">Left list.</param>
        /// <param name="right">Right list.</param>
        /// <returns>True if equal, otherwise false.</returns>
        private static bool Equals<T>(IList<T> left, IList<T> right)
        {
            Helper.ThrowIfNull(left);
            Helper.ThrowIfNull(right);

            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Read one string of text from current position of the string pool.
        /// </summary>
        /// <returns>String read, null if reaching the end of stream.</returns>
        private string ReadString()
        {
            if (_memoryStream.Position == Length)
            {
                return null;
            }

            List<byte> bytes = new List<byte>();
            byte[] one = new byte[sizeof(char)];
            while (_memoryStream.Read(one, 0, one.Length) == one.Length)
            {
                if (Equals(one, StringEnd))
                {
                    Debug.Assert(bytes.Count > 0);
                    break;
                }

                bytes.AddRange(one);
            }

            return Encoding.Unicode.GetString(bytes.ToArray());
        }

        #endregion
    }
}