//----------------------------------------------------------------------------
// <copyright file="NusVoiceData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Defines object model for NUS voice data seriailzation.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Schema;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;
    using Microsoft.Tts.ServiceProvider.BaseUtils;
    using Microsoft.Tts.ServiceProvider.Extension;

    /// <summary>
    /// Base NUS voice section class.
    /// </summary>
    public abstract class NusVoiceSection
    {
        private byte[] _dataBytes = null;

        #region Constructor

        public NusVoiceSection()
        {
            TraceLevel = 0;
        }       

        #endregion

        public abstract uint SectionId { get; }

        public uint Offset { get; set; }

        public uint Size { get; set; }

        public ILogger Logger { get; set; }

        public abstract NonUniformUnit[] NusUnits { get; }

        /// <summary>
        /// Gets or sets trace level. 0: full trace information, others: simple trace information.
        /// </summary>
        public uint TraceLevel { get; set; }        
        
        public void Write(BinaryWriter writer)
        {
            Offset = (uint)writer.BaseStream.Position;

            if (_dataBytes != null)
            {
                writer.Write(_dataBytes);
            }
            else
            {
                WriteData(writer);
            }

            Size = (uint)writer.BaseStream.Position - Offset;
        }

        public void Load(BinaryReader reader, NusFontInfo info)
        {
            LoadData(reader, info);
        }

        public abstract void WriteData(BinaryWriter writer);

        public virtual void LoadData(BinaryReader reader, NusFontInfo info)
        {
            _dataBytes = reader.ReadBytes(checked((int)Size));
        }

        protected ItemRange[] WriteIndexedItems<T>(
            IList<ItemRange> indexes, IList<T> items,
            BinaryWriter writer, Func<T, byte[]> serializer)
        {
            Log("<WriteIndexedItems>");

            Helper.ThrowIfNull(items);
            Helper.ThrowIfNull(writer);

            if (indexes != null && indexes.Count != items.Count)
            {
                throw new ArgumentException("index and item length mismatch");
            }

            if (indexes == null)
            {
                indexes = items.Select(i => new ItemRange()).ToArray();
            }

            var sectionOffset = checked((int)writer.BaseStream.Position);

            writer.Write(indexes.Count);
            Log("<indexes.Count> {0}", indexes.Count);

            foreach (var indexEntry in indexes)
            {
                writer.Write(indexEntry.Start);

                if (TraceLevel == 0)
                {
                    Log("<indexEntry.Start> {0}", indexEntry.Start);
                }
                
                writer.Write(indexEntry.Length);
                
                if (TraceLevel == 0)
                {
                    Log("<indexEntry.Length> {0}", indexEntry.Length);
                }
            }

            for (int nuuIndex = 0; nuuIndex < items.Count; nuuIndex++)
            {
                var nuu = items[nuuIndex];
                var indexEntry = indexes[nuuIndex];

                indexEntry.Start = checked((int)writer.BaseStream.Position);

                if (TraceLevel == 0)
                {
                    Log("<serializer>");
                }
                
                writer.Write(serializer(nuu));

                indexEntry.Length = checked((int)writer.BaseStream.Position) - indexEntry.Start;
                indexEntry.Start -= sectionOffset;
            }

            return indexes.ToArray();
        }

        protected IList<T> ReadIndexedItems<T>(BinaryReader reader, NusFontInfo info, Func<byte[], NusFontInfo, T> deserializer)
        {
            IList<T> items = new List<T>();
            int itemCount = reader.ReadInt32();
            List<ItemRange> indexList = new List<ItemRange>();
            for (int i = 0; i < itemCount; i++)
            {
                ItemRange range = new ItemRange();
                range.Start = reader.ReadInt32();
                range.Length = reader.ReadInt32();
                indexList.Add(range);
            }

            for (int index = 0; index < indexList.Count; index++)
            {
                ItemRange range = indexList[index];
                byte[] selizedBytes = reader.ReadBytes(range.Length);
                T item = deserializer(selizedBytes, info);
                items.Add(item);
            }

            return items;
        }

        protected void Log(string format, params object[] args)
        {
            if (Logger != null)
            {
                Logger.LogLine(format, args);
            }
        }
    }

    /// <summary>
    /// NUS unit section.
    /// </summary>
    public class NusUnitSection : NusVoiceSection
    {
        private ItemRange[] _indexes = null;
        private NonUniformUnit[] _nusUnits = null;

        public NusUnitSection(NonUniformUnit[] nusUnits)
        {
            _nusUnits = nusUnits;
        }

        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_UNIT;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return _nusUnits; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public override void WriteData(BinaryWriter writer)
        {
            Log("<NusUnitSection>");
            Func<NonUniformUnit, byte[]> serializer = u =>
            {
                var stream = new MemoryStream();
                BinaryWriter memoryWriter = new BinaryWriter(stream);

                memoryWriter.Write(u.FeaturePhones.Length);
                if (TraceLevel == 0)
                {
                    Log("<FeaturePhones.Length> {0}", u.FeaturePhones.Length);
                }

                foreach (var featurePhone in u.FeaturePhones)
                {
                    memoryWriter.Write(featurePhone.PhoneId);
                    
                    if (TraceLevel == 0)
                    {
                        Log("<featurePhone.PhoneId> {0}", featurePhone.PhoneId);
                    }
                    
                    foreach (var featureValue in featurePhone.Features)
                    {
                        var stringType = Microsoft.Tts.ServiceProvider.FeatureValueType.FEATURE_VALUE_TYPE_STRING;
                        if (featureValue.ValueType == stringType)
                        {
                            throw new NotSupportedException("Feature value of string is not supported.");
                        }

                        memoryWriter.Write((int)featureValue.ValueType);

                        if (TraceLevel == 0)
                        {
                            Log("<featureValue.ValueType> {0}", featureValue.ValueType);
                        }
                        
                        memoryWriter.Write(featureValue.IntValue);

                        if (TraceLevel == 0)
                        {
                            Log("<featureValue.IntValue> {0}", featureValue.IntValue);
                        }
                    }
                }

                return stream.ToArray();
            };

            _indexes = WriteIndexedItems(_indexes, _nusUnits, writer, serializer);
        }

        public override void LoadData(BinaryReader reader, NusFontInfo info)
        {
            Func<byte[], NusFontInfo, NonUniformUnit> deserializer = (bytes, fontInfo) =>
            {
                NonUniformUnit unit = new NonUniformUnit();
                using (var stream = new MemoryStream(bytes))
                {
                    BinaryReader memoryReader = new BinaryReader(stream);
                    int phoneLength = memoryReader.ReadInt32();
                    unit.FeaturePhones = new FeaturePhone[phoneLength];
                    for (int i = 0; i < phoneLength; i++)
                    {
                        FeaturePhone phone = new FeaturePhone();
                        phone.PhoneId = memoryReader.ReadUInt16();
                        phone.Features = new FeatureVal[fontInfo.FeaturesLength];
                        for (int j = 0; j < fontInfo.FeaturesLength; j++)
                        {
                            FeatureVal val = new FeatureVal();
                            val.ValueType = (FeatureValueType)memoryReader.ReadInt32();
                            val.IntValue = memoryReader.ReadInt32();
                            phone.Features[j] = val;
                        }

                        unit.FeaturePhones[i] = phone;
                    }
                }

                return unit;
            };
            IList<NonUniformUnit> nuuItems = ReadIndexedItems(reader, info, deserializer);
            if (_nusUnits == null)
            {
                _nusUnits = new NonUniformUnit[nuuItems.Count];
                int idx = 0;
                nuuItems.ForEach(u => _nusUnits[idx++] = u);
            }
            else
            {
                if (_nusUnits.Count() != nuuItems.Count)
                {
                    throw new InvalidDataException("Loaded font is different from the give nus unit count.");
                }

                int idx = 0;
                nuuItems.ForEach(u => _nusUnits[idx++].FeaturePhones = u.FeaturePhones);
            }
        }
    }

    /// <summary>
    /// NUS target section.
    /// </summary>
    public class NusAcousticTargetSection : NusVoiceSection
    {
        private ItemRange[] _indexes = null;
        private NonUniformUnit[] _nusUnits = null;

        public NusAcousticTargetSection(NonUniformUnit[] nusUnits)
        {
            _nusUnits = nusUnits;
        }

        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_TARGET;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return _nusUnits; }
        }

        public override void WriteData(BinaryWriter writer)
        {
            Func<NonUniformUnit, byte[]> serializer = u =>
            {
                return u.PitchTarget.Serialize(sizeof(float));
            };

            _indexes = WriteIndexedItems(_indexes, _nusUnits, writer, serializer);
        }

        public override void LoadData(BinaryReader reader, NusFontInfo info)
        {
            Func<byte[], NusFontInfo, NonUniformUnit> deserializer = (bytes, fontInfo) =>
            {
                NonUniformUnit unit = new NonUniformUnit();
                int[] lengthAry = new int[1];
                int size = lengthAry.Length * sizeof(int);
                Buffer.BlockCopy(bytes, 0, lengthAry, 0, size);
                int length = lengthAry[0];
                float[] pitchTarget = new float[length];
                Buffer.BlockCopy(bytes, size, pitchTarget, 0, length * sizeof(float));
                unit.PitchTarget = pitchTarget;
                return unit;
            };
            IList<NonUniformUnit> nuuItems = ReadIndexedItems(reader, info, deserializer);
            if (_nusUnits == null || _nusUnits.Count() != nuuItems.Count)
            {
                throw new InvalidDataException("Loaded font is different from the give nus unit count.");
            }

            int idx = 0;
            nuuItems.ForEach(u => _nusUnits[idx++].PitchTarget = u.PitchTarget);
        }
    }

    /// <summary>
    /// NUS TRIE section.
    /// </summary>
    public class NusTrieSection : NusVoiceSection, IDisposable
    {
        private const int TrieAlign = 4;
        private uint _nuuGroupOffset;
        private uint _nuuGroupSize;
        private uint _trieOffset;
        private uint _trieSize;

        private ItemRange[] _indexes;

        private Dictionary<string, List<int>> _phonesToNuuIndexesMap;
        private int[][] _nuuGroupsOrderByTrieId;

        private Dictionary<string, List<int[]>> _phonesToTemplateIndexesMap;
        private Dictionary<string, List<int>> _phonesToTemplateIdsMap;
        private int[][][] _templateIndexsOrderByTrieId;

        private bool isTemplate = false;

        private TrieTree _trieTree;

        public NusTrieSection()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public NusTrieSection(NonUniformUnit[] nusUnits)
        {
            Helper.ThrowIfNull(nusUnits);

            isTemplate = nusUnits[0].IsTemplate;

            // Group NUS units by phone sequence
            _phonesToNuuIndexesMap = new Dictionary<string, List<int>>();
            _phonesToTemplateIndexesMap = new Dictionary<string, List<int[]>>();
            _phonesToTemplateIdsMap = new Dictionary<string, List<int>>();

            for (int i = 0; i < nusUnits.Length; i++)
            {
                var nusUnit = nusUnits[i];
                var phoneString = PhoneIdsToString(nusUnit.FeaturePhones.Select(f => f.PhoneId));

                if (string.IsNullOrEmpty(phoneString))
                {
                    throw new InvalidDataException("NUS unit ID " + nusUnit.Id.ToString() + " doesn't contain any phone!");
                }

                if (phoneString.Length > 128)
                {
                    throw new InvalidDataException("NUS unit ID " + nusUnit.Id.ToString() + " contains more phones than trietree supports!");
                }

                if (!isTemplate)
                {
                    if (!_phonesToNuuIndexesMap.ContainsKey(phoneString))
                    {
                        _phonesToNuuIndexesMap[phoneString] = new List<int>();
                    }

                    _phonesToNuuIndexesMap[phoneString].Add(i);
                }
                else
                {
                    if (!_phonesToTemplateIndexesMap.ContainsKey(phoneString))
                    {
                        _phonesToTemplateIndexesMap[phoneString] = new List<int[]>();
                    }

                    if (!_phonesToTemplateIdsMap.ContainsKey(phoneString))
                    {
                        _phonesToTemplateIdsMap[phoneString] = new List<int>();
                    }

                    _phonesToTemplateIndexesMap[phoneString].Add(nusUnit.Segment);

                    _phonesToTemplateIdsMap[phoneString].Add(nusUnit.Id);
                }
            }

            // Order unit groups by their TRIE id
            var keys = _phonesToNuuIndexesMap.Keys.ToList();

            if (isTemplate)
            {
                keys = _phonesToTemplateIndexesMap.Keys.ToList();
            }

            _trieTree = new TrieTree(keys);

            var keyIds = _trieTree.FindWords(keys);
            Debug.Assert(Enumerable.SequenceEqual(Enumerable.Range(0, keys.Count), keyIds.OrderBy(v => v)));

            if (isTemplate)
            {
                _templateIndexsOrderByTrieId = keys.Select((k, i) => new
                {
                    Key = k,
                    TrieId = keyIds[i],
                    NuGroup = _phonesToTemplateIndexesMap[k].ToArray()
                }).OrderBy(t => t.TrieId).Select(t => t.NuGroup).ToArray();
            }
            else
            {
                _nuuGroupsOrderByTrieId = keys.Select((k, i) => new
                {
                    Key = k,
                    TrieId = keyIds[i],
                    NuGroup = _phonesToNuuIndexesMap[k].ToArray()
                }).OrderBy(t => t.TrieId).Select(t => t.NuGroup).ToArray();
            }
        }

        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_TRIE;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return null; }
        }

        public int[][][] GetTemplateIndexs()
        {
            return _templateIndexsOrderByTrieId;
        }

        public override void WriteData(BinaryWriter writer)
        {
            if (isTemplate)
            {
                var keys = _phonesToTemplateIndexesMap.Keys.ToList();
                var keyIds = _trieTree.FindWords(keys);

                Log("trieId count {0}", keys.Count);

                for (int i = 0; i < keys.Count; i++)
                {
                    var trieId = keyIds[i];
                    var key = keys[i];
                    StringBuilder buffer = new StringBuilder();

                    foreach (int gid in _phonesToTemplateIdsMap[key])
                    {
                        buffer.Append(gid + " ");
                    }

                    Log("trieId {0} - groupID {1}", trieId, buffer.ToString());
                }
            }

            var sectionOffset = writer.BaseStream.Position;

            if (!isTemplate)
            {
                writer.Write(_nuuGroupOffset);
                writer.Write(_nuuGroupSize);
            }

            writer.Write(_trieOffset);
            writer.Write(_trieSize);

            Func<int[], byte[]> serializer = u =>
            {
                return u.Serialize(sizeof(uint));
            };

            if (!isTemplate)
            {
                _nuuGroupOffset = checked((uint)(writer.BaseStream.Position - sectionOffset));
                _indexes = WriteIndexedItems(_indexes, _nuuGroupsOrderByTrieId, writer, serializer);
                _nuuGroupSize = checked((uint)(writer.BaseStream.Position - sectionOffset - _nuuGroupOffset));
            }

            PadBytes(writer, TrieAlign);
            _trieOffset = checked((uint)(writer.BaseStream.Position - sectionOffset));
            writer.Write(_trieTree.GetTrieData());
            _trieSize = checked((uint)(writer.BaseStream.Position - sectionOffset - _trieOffset));
        }

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
                if (null != _trieTree)
                {
                    _trieTree.Dispose();
                }
            }
        }

        #endregion

        private string PhoneIdsToString(IEnumerable<ushort> phoneIds)
        {
            checked
            {
                return new string(phoneIds.Select(p => (char)p).ToArray());
            }
        }

        private int PadBytes(BinaryWriter writer, int alignment)
        {
            int padCount = 0;
            var position = writer.BaseStream.Position;

            if (position % alignment != 0)
            {
                padCount = (int)(alignment - (position % alignment));
                writer.Write(new byte[padCount]);
            }

            return padCount;
        }
    }

    /// <summary>
    /// NUS candidate section.
    /// </summary>
    public class NusCandidateSection : NusVoiceSection
    {
        private ItemRange[] _indexes = null;
        private NonUniformUnit[] _nusUnits = null;

        public NusCandidateSection(NonUniformUnit[] nusUnits)
        {
            _nusUnits = nusUnits;
        }

        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_CANDIDATE;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return _nusUnits; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public override void WriteData(BinaryWriter writer)
        {
            Func<NonUniformUnit, byte[]> serializer = u =>
            {
                uint candidateWidth = 0;
                int candidateLength = 0;
                var candidates = u.CandidateUntIndexes;
                if (candidates != null)
                {
                    candidateWidth = checked((uint)candidates.First().Length);
                    if (candidates.Any(c => c.Length != candidateWidth))
                    {
                        throw new InvalidDataException("Inconsistent NUS candidates width.");
                    }

                    candidateLength = candidates.Length;
                }

                var stream = new MemoryStream();
                BinaryWriter memoryWriter = new BinaryWriter(stream);

                memoryWriter.Write(candidateWidth);
                memoryWriter.Write(candidateLength);
                if (candidates != null)
                {
                    int[] indexes = candidates.SelectMany(c => c).ToArray();
                    byte[] indexBytes = new byte[indexes.Length * sizeof(uint)];
                    Buffer.BlockCopy(indexes, 0, indexBytes, 0, indexBytes.Length);
                    memoryWriter.Write(indexBytes);
                }

                return stream.ToArray();
            };

            _indexes = WriteIndexedItems(_indexes, _nusUnits, writer, serializer);
        }

        public override void LoadData(BinaryReader reader, NusFontInfo info)
        {
            Func<byte[], NusFontInfo, NonUniformUnit> deserializer = (bytes, fontInfo) =>
            {
                NonUniformUnit unit = new NonUniformUnit();
                using (var stream = new MemoryStream(bytes))
                {
                    BinaryReader memoryReader = new BinaryReader(stream);
                    uint candidateWidth = memoryReader.ReadUInt32();
                    int candidateLength = memoryReader.ReadInt32();
                    if (candidateLength > 0)
                    {
                        int allcandidateLength = checked((int)(candidateWidth * candidateLength * sizeof(int)));
                        byte[] indexBytes = memoryReader.ReadBytes(allcandidateLength);
                        int[][] candidateIndexs = new int[candidateLength][];
                        int size = checked((int)(sizeof(uint) * candidateWidth));
                        for (int i = 0; i < candidateLength; i++)
                        {
                            int offset = checked((int)(candidateWidth * i * sizeof(int)));
                            int[] unitindex = new int[candidateWidth];
                            Buffer.BlockCopy(indexBytes, offset, unitindex, 0, size);
                            candidateIndexs[i] = unitindex;
                        }

                        unit.CandidateUntIndexes = candidateIndexs;
                    }
                }

                return unit;
            };
            IList<NonUniformUnit> nuuItems = ReadIndexedItems(reader, info, deserializer);
            if (_nusUnits == null || _nusUnits.Count() != nuuItems.Count)
            {
                throw new InvalidDataException("Loaded font is different from the give nus unit count.");
            }

            int idx = 0;
            nuuItems.ForEach(u => _nusUnits[idx++].CandidateUntIndexes = u.CandidateUntIndexes);
        }
    }

    /// <summary>
    /// Long unit frame boundary section.
    /// </summary>
    public class NusEmotionSection : NusVoiceSection
    {
        private ItemRange[] _indexes = null;
        private NonUniformUnit[] _nusUnits = null;

        #region Constructor
        public NusEmotionSection(NonUniformUnit[] nusUnits)
        {
            _nusUnits = nusUnits;
        }
        #endregion

        /// <summary>
        /// Gets SectionId.
        /// </summary>
        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_EMOTION;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return _nusUnits; }
        }

        /// <summary>
        /// Section writer.
        /// </summary>
        /// <param name="writer">BinaryWriter.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public override void WriteData(BinaryWriter writer)
        {
            Func<NonUniformUnit, byte[]> serializer = u =>
            {
                var stream = new MemoryStream();
                return stream.ToArray();
            };

            writer.Write(_nusUnits.Length);
            uint[] emotionIndex = new uint[_nusUnits.Length];
            for (int i = 0; i < _nusUnits.Length; i++)
            {
                emotionIndex[i] = (uint)_nusUnits[i].Emotion;
            }

            byte[] indexBytes = new byte[emotionIndex.Length * sizeof(uint)];
            Buffer.BlockCopy(emotionIndex, 0, indexBytes, 0, indexBytes.Length);
            writer.Write(indexBytes);

            _indexes = WriteIndexedItems(_indexes, _nusUnits, writer, serializer);
        }

        public override void LoadData(BinaryReader reader, NusFontInfo info)
        {
            Func<byte[], NusFontInfo, NonUniformUnit> deserializer = (bytes, fontInfo) =>
            {
                NonUniformUnit unit = new NonUniformUnit();
                return unit;
            };

            int nuuItemCount = reader.ReadInt32();

            for (int i = 0; i < nuuItemCount; i++)
            {
                _nusUnits[i].Emotion = reader.ReadUInt32();
            }

            IList<NonUniformUnit> nuuItems = ReadIndexedItems(reader, info, deserializer);
        }
    }

    /// <summary>
    /// Nus unit prosody section.
    /// </summary>
    public class NusProsodySection : NusVoiceSection
    {
        private ItemRange[] _indexes = null;
        private NonUniformUnit[] _nusUnits = null;

        #region Constructor
        public NusProsodySection(NonUniformUnit[] nusUnits)
        {
            _nusUnits = nusUnits;
        }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether enable this section or not.
        /// </summary>
        public bool EnableSection { get; set; }

        /// <summary>
        /// Gets SectionId.
        /// </summary>
        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_PROSODY;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return _nusUnits; }
        }

        /// <summary>
        /// Section writer.
        /// </summary>
        /// <param name="writer">BinaryWriter.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public override void WriteData(BinaryWriter writer)
        {
            Func<NonUniformUnit, byte[]> serializer = u =>
            {
                var stream = new MemoryStream();
                return stream.ToArray();
            };

            if (EnableSection)
            {
                writer.Write((uint)_nusUnits.Length);

                var nusUnitsParallel = from u in _nusUnits.AsParallel() select u;
                nusUnitsParallel.ForAll((item) =>
                {
                    item.CalculateBestProsodyItem();
                });

                for (int i = 0; i < _nusUnits.Length; i++)
                {
                    writer.Write(_nusUnits[i].BestProsodyItem.HeadBr);
                    writer.Write(_nusUnits[i].BestProsodyItem.TailBr);
                    writer.Write(_nusUnits[i].BestProsodyItem.HeadTobi);
                    writer.Write(_nusUnits[i].BestProsodyItem.TailTobi);
                }
            }
            else
            {
                writer.Write((uint)0);
            }

            _indexes = WriteIndexedItems(_indexes, _nusUnits, writer, serializer);
        }

        public override void LoadData(BinaryReader reader, NusFontInfo info)
        {
            Func<byte[], NusFontInfo, NonUniformUnit> deserializer = (bytes, fontInfo) =>
            {
                NonUniformUnit unit = new NonUniformUnit();
                return unit;
            };

            int nuuItemCount = (int)reader.ReadUInt32();

            if (nuuItemCount == 0)
            {
                EnableSection = false;
            }
            else
            {
                EnableSection = true;

                for (int i = 0; i < nuuItemCount; i++)
                {
                    NUUProsodyItem prosodyItem = new NUUProsodyItem();
                    prosodyItem.HeadBr = reader.ReadByte();
                    prosodyItem.TailBr = reader.ReadByte();
                    prosodyItem.HeadTobi = reader.ReadByte();
                    prosodyItem.TailTobi = reader.ReadByte();

                    _nusUnits[i].CandidateProsodyItems = new NUUProsodyItem[] { prosodyItem };
                }
            }

            IList<NonUniformUnit> nuuItems = ReadIndexedItems(reader, info, deserializer);
        }
    }

    /// <summary>
    /// NUS template section.
    /// </summary>
    public class NusTemplateSection : NusVoiceSection
    {
        private int[][][] _templateIndexs;

        public NusTemplateSection()
        {
        }

        public NusTemplateSection(int[][][] indexs)
        {
            Helper.ThrowIfNull(indexs);

            _templateIndexs = indexs;
        }

        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_NUSTemplate;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return null; }
        }

        public override void WriteData(BinaryWriter writer)
        {
            var sectionOffset = writer.BaseStream.Position;

            writer.Write((uint)_templateIndexs.Length);

            foreach (int[][] templateIndex in _templateIndexs)
            {
                writer.Write((uint)templateIndex.Length);
                foreach (int[] index in templateIndex)
                {
                    int len = index.Length / 2;
                    writer.Write((uint)len);
                    foreach (int value in index)
                    {
                        writer.Write((uint)value);
                    }
                }
            }
        }

        public override void LoadData(BinaryReader reader, NusFontInfo info)
        {
            List<List<uint[]>> templateIndexs = new List<List<uint[]>>();

            int templateCount = reader.ReadInt32();

            for (uint i = 0; i < templateCount; i++)
            {
                uint instanceCount = reader.ReadUInt32();
                List<uint[]> instanceList = new List<uint[]>();
                templateIndexs.Add(instanceList);
                for (uint j = 0; j < instanceCount; j++)
                {
                    uint referenceCount = reader.ReadUInt32();
                    uint[] refrenceArray = new uint[referenceCount * 2];
                    instanceList.Add(refrenceArray);
                    for (int t = 0; t < referenceCount * 2; t++)
                    {
                        refrenceArray[t] = reader.ReadUInt32();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Overall NUS voice data serializer.
    /// </summary>
    public class NusVoiceData : IDisposable
    {
        private VoiceFontHeader _header = new VoiceFontHeader();
        private LinguisticFeatureInfo[] _linguisticFeatures;

        private NusUnitSection _nusUnitSection;
        private NusAcousticTargetSection _nusAcousticTargetSection;
        private NusCandidateSection _nusCandidateSection;
        private NusTrieSection _nusTrieSection;
        private NusEmotionSection _nusEmotionSection;
        private NusProsodySection _nusProsodySection;
        private StringPool _stringPool;
        private uint _stringPoolOffset;
        private uint _stringPoolSize;
        private NusVoiceSection[] sections;

        #region Constructor

        public NusVoiceData()
        {
        }

        public NusVoiceData(NonUniformUnit[] nusUnits, LinguisticFeatureInfo[] linguisticFeatures)
        {
            _header.FileTag = (uint)FontFileTag.FFT_NUS;
            _header.FormatTag = VoiceFontTag.FmtIdNusVoiceData;

            _linguisticFeatures = linguisticFeatures;

            _nusUnitSection = new NusUnitSection(nusUnits);
            _nusAcousticTargetSection = new NusAcousticTargetSection(nusUnits);
            _nusCandidateSection = new NusCandidateSection(nusUnits);
            _nusTrieSection = new NusTrieSection(nusUnits);
            _nusEmotionSection = new NusEmotionSection(nusUnits);
            _nusProsodySection = new NusProsodySection(nusUnits);

            sections = new NusVoiceSection[] 
            {
                _nusUnitSection,
                _nusAcousticTargetSection,
                _nusCandidateSection,
                _nusTrieSection,
                _nusEmotionSection,
                _nusProsodySection,
            };
        }

        #endregion

        public ILogger Logger { get; set; }

        public void Write(string path)
        {
            // first set the offset/file size fields
            FileStream file = new FileStream(path, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    Write(writer);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }

            // actual write
            file = new FileStream(path, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    Write(writer);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }
        }

        /// <summary>
        /// Load file to data.
        /// </summary>
        /// <param name="path">The font file path.</param>
        public void Load(string path)
        {
            FileStream file = new FileStream(path, FileMode.Open);
            try
            {
                using (BinaryReader reader = new BinaryReader(file))
                {
                    file = null;
                    Load(reader);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }
        }

        public NusVoiceSection GetNusVoiceSection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_UNIT
                       select entry;

            Debug.Assert(item.Count() == 1);
            return (NusVoiceSection)item.FirstOrDefault();
        }

        public NusCandidateSection GetNusCandidateSection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_CANDIDATE
                       select entry;

            Debug.Assert(item.Count() == 1);
            return (NusCandidateSection)item.FirstOrDefault();
        }

        public void EnableProsodySection(bool enableSection)
        {
            GetNusProsodySection().EnableSection = enableSection;
        }

        public NusProsodySection GetNusProsodySection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_PROSODY
                       select entry;

            Debug.Assert(item.Count() == 1);
            return (NusProsodySection)item.FirstOrDefault();
        }

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
        /// Disposes the resources used in this object.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _stringPool)
                {
                    _stringPool.Dispose();
                }

                if (null != _nusTrieSection)
                {
                    _nusTrieSection.Dispose();
                }
            }
        }

        #endregion

        private void Write(BinaryWriter writer)
        {
            // font header
            _header.Save(writer);

            var dataOffset = writer.BaseStream.Position;

            // nus header
            writer.Write(_linguisticFeatures.Length);
            writer.Write(sections.Length);
            writer.Write(_stringPoolOffset);
            writer.Write(_stringPoolSize);

            // build string pool
            _stringPool = new StringPool();

            foreach (var feature in _linguisticFeatures)
            {
                writer.Write((uint)_stringPool.Length);
                _stringPool.PutString(feature.Name);
            }

            foreach (var section in sections)
            {
                writer.Write(section.SectionId);
                writer.Write(section.Offset);
                writer.Write(section.Size);
                section.Logger = Logger;
            }

            // sections
            foreach (var section in sections)
            {
                section.Write(writer);
            }

            _stringPoolOffset = checked((uint)writer.BaseStream.Position);
            var stringBytes = _stringPool.ToArray();
            _stringPoolSize = checked((uint)stringBytes.Length);

            byte[] encryptedBytes = new byte[_stringPool.Length];
            HTSVoiceDataEncrypt.EncryptStringPool(_stringPool.ToArray(), encryptedBytes);

            writer.Write(encryptedBytes);

            // set font file header size
            _header.DataSize = checked((ulong)(writer.BaseStream.Position - dataOffset));
        }

        private void Load(BinaryReader reader)
        {
            _header.Load(reader);
            int linguisticFeaturesLength = reader.ReadInt32();
            int sectionLength = reader.ReadInt32();
            uint stringPoolOffSet = reader.ReadUInt32();
            uint stringPoolSize = reader.ReadUInt32();
            NusFontInfo nusInfo = new NusFontInfo();
            nusInfo.FeaturesLength = linguisticFeaturesLength;

            _linguisticFeatures = new LinguisticFeatureInfo[linguisticFeaturesLength];

            List<uint> ligusticFeatureNameOffset = new List<uint>();

            for (int i = 0; i < linguisticFeaturesLength; i++)
            {
                uint offset = reader.ReadUInt32();
                ligusticFeatureNameOffset.Add(offset);
            }

            NonUniformUnit[] nusUnits = null;
            List<SectionInfo> sectionInfo = new List<SectionInfo>();
            this.sections = new NusVoiceSection[sectionLength];
            for (int i = 0; i < sectionLength; i++)
            {
                SectionInfo info = new SectionInfo();
                info.SectionId = reader.ReadUInt32();
                info.Offset = reader.ReadUInt32();
                info.Size = reader.ReadUInt32();
                sectionInfo.Add(info);
            }

            int idx = 0;
            foreach (SectionInfo info in sectionInfo)
            {
                NusVoiceSection section = GetNusVoiceSection(info.SectionId, nusUnits);
                section.Offset = info.Offset;
                section.Size = info.Size;
                section.LoadData(reader, nusInfo);
                if (section.NusUnits != null)
                {
                    nusUnits = section.NusUnits;
                }

                if (reader.BaseStream.Position != info.Offset + info.Size)
                {
                    throw new InvalidDataException("The long unit file is not correct.");
                }

                this.sections[idx++] = section;
            }

            _stringPool = new StringPool();
            if (stringPoolSize > 0)
            {
                byte[] buffer = new byte[stringPoolSize];
                Microsoft.Tts.ServiceProvider.HTSVoiceDataEncrypt.DecryptStringPool(
                    reader.ReadBytes(checked((int)stringPoolSize)), buffer);
                _stringPool.PutBuffer(buffer);
            }

            for (int i = 0; i < linguisticFeaturesLength; i++)
            {
                LinguisticFeatureInfo linfo = new LinguisticFeatureInfo();
                _linguisticFeatures[i] = linfo;
                linfo.Name = _stringPool.Strings[i];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        private NusVoiceSection GetNusVoiceSection(uint sectionId, NonUniformUnit[] nusUnits)
        {
            NusVoiceSection section = null;
            switch (sectionId)
            {
                case (uint)NusSectionId.NSI_BOUNDARY:
                    if (nusUnits != null)
                    {
                        ItemRange[][] realFrames = new ItemRange[nusUnits.Length][];
                        section = new LongUnitBoundarySection(nusUnits, realFrames);
                    }
                    else
                    {
                        section = new LongUnitBoundarySection(null, null);
                    }

                    break;
                case (uint)NusSectionId.NSI_UNIT:
                    section = new NusUnitSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_CANDIDATE:
                    section = new NusCandidateSection(nusUnits);
                    break;
                case (uint)(uint)NusSectionId.NSI_TRIE:
                    section = new NusTrieSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_TARGET:
                    section = new NusAcousticTargetSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_EMOTION:
                    section = new NusEmotionSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_PROSODY:
                    section = new NusProsodySection(nusUnits);
                    break;
            }

            return section;
        }
    }

    /// <summary>
    /// Template NUS voice data serializer.
    /// </summary>
    public class TemplateNusVoiceData : IDisposable
    {
        private VoiceFontHeader _header = new VoiceFontHeader();
        private LinguisticFeatureInfo[] _linguisticFeatures;

        private NusUnitSection _nusUnitSection;
        private NusAcousticTargetSection _nusAcousticTargetSection;
        private NusCandidateSection _nusCandidateSection;
        private NusTrieSection _nusTrieSection;
        private NusEmotionSection _nusEmotionSection;
        private NusProsodySection _nusProsodySection;
        private NusTemplateSection _nusTemplateSection;
        private StringPool _stringPool;
        private uint _stringPoolOffset;
        private uint _stringPoolSize;
        private NusVoiceSection[] sections;

        #region Constructor

        public TemplateNusVoiceData()
        {
        }

        public TemplateNusVoiceData(NonUniformUnit[] nusUnits, LinguisticFeatureInfo[] linguisticFeatures)
        {
            _header.FileTag = (uint)FontFileTag.FFT_NUS;
            _header.FormatTag = VoiceFontTag.FmtIdTemplateNusVoiceData;

            _linguisticFeatures = linguisticFeatures;

            List<NonUniformUnit> templateUnitList = new List<NonUniformUnit>();
            List<NonUniformUnit> normalUnitList = new List<NonUniformUnit>();

            foreach (NonUniformUnit nusUnit in nusUnits)
            {
                if (nusUnit.IsTemplate)
                {
                    templateUnitList.Add(nusUnit);
                }
                else
                {
                    normalUnitList.Add(nusUnit);
                }
            }

            NonUniformUnit[] _normalNusUnits = normalUnitList.ToArray();
            NonUniformUnit[] _templateUnitList = templateUnitList.ToArray();

            _nusUnitSection = new NusUnitSection(_normalNusUnits);
            _nusAcousticTargetSection = new NusAcousticTargetSection(_normalNusUnits);
            _nusCandidateSection = new NusCandidateSection(_normalNusUnits);
            _nusTrieSection = new NusTrieSection(_templateUnitList);
            _nusTemplateSection = new NusTemplateSection(_nusTrieSection.GetTemplateIndexs());
            _nusEmotionSection = new NusEmotionSection(_normalNusUnits);
            _nusProsodySection = new NusProsodySection(_normalNusUnits);

            sections = new NusVoiceSection[] 
            {
                _nusUnitSection,
                _nusAcousticTargetSection,
                _nusCandidateSection,
                _nusTrieSection,
                _nusEmotionSection,
                _nusProsodySection,
                _nusTemplateSection,
            };
        }

        #endregion

        public ILogger Logger { get; set; }

        public void Write(string path)
        {
            // first set the offset/file size fields
            FileStream file = new FileStream(path, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    Write(writer);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }

            // actual write
            file = new FileStream(path, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    Write(writer);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }
        }

        /// <summary>
        /// Load file to data.
        /// </summary>
        /// <param name="path">The font file path.</param>
        public void Load(string path)
        {
            FileStream file = new FileStream(path, FileMode.Open);
            try
            {
                using (BinaryReader reader = new BinaryReader(file))
                {
                    file = null;
                    Load(reader);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }
        }

        public NusVoiceSection GetNusVoiceSection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_UNIT
                       select entry;

            Debug.Assert(item.Count() == 1);
            return (NusVoiceSection)item.FirstOrDefault();
        }

        public NusCandidateSection GetNusCandidateSection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_CANDIDATE
                       select entry;

            Debug.Assert(item.Count() == 1);
            return (NusCandidateSection)item.FirstOrDefault();
        }

        public void EnableProsodySection(bool enableSection)
        {
            GetNusProsodySection().EnableSection = enableSection;
        }

        public NusProsodySection GetNusProsodySection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_PROSODY
                       select entry;

            Debug.Assert(item.Count() == 1);
            return (NusProsodySection)item.FirstOrDefault();
        }

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
        /// Disposes the resources used in this object.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _stringPool)
                {
                    _stringPool.Dispose();
                }

                if (null != _nusTrieSection)
                {
                    _nusTrieSection.Dispose();
                }
            }
        }

        #endregion

        private void Write(BinaryWriter writer)
        {
            // font header
            _header.Save(writer);

            var dataOffset = writer.BaseStream.Position;

            // nus header
            writer.Write(_linguisticFeatures.Length);
            writer.Write(sections.Length);
            writer.Write(_stringPoolOffset);
            writer.Write(_stringPoolSize);

            // build string pool
            _stringPool = new StringPool();

            foreach (var feature in _linguisticFeatures)
            {
                writer.Write((uint)_stringPool.Length);
                _stringPool.PutString(feature.Name);
            }

            foreach (var section in sections)
            {
                writer.Write(section.SectionId);
                writer.Write(section.Offset);
                writer.Write(section.Size);
                section.Logger = Logger;
            }

            // sections
            foreach (var section in sections)
            {
                section.Write(writer);
            }

            _stringPoolOffset = checked((uint)writer.BaseStream.Position);
            var stringBytes = _stringPool.ToArray();
            _stringPoolSize = checked((uint)stringBytes.Length);

            byte[] encryptedBytes = new byte[_stringPool.Length];
            HTSVoiceDataEncrypt.EncryptStringPool(_stringPool.ToArray(), encryptedBytes);

            writer.Write(encryptedBytes);

            // set font file header size
            _header.DataSize = checked((ulong)(writer.BaseStream.Position - dataOffset));
        }

        private void Load(BinaryReader reader)
        {
            _header.Load(reader);
            int linguisticFeaturesLength = reader.ReadInt32();
            int sectionLength = reader.ReadInt32();
            uint stringPoolOffSet = reader.ReadUInt32();
            uint stringPoolSize = reader.ReadUInt32();
            NusFontInfo nusInfo = new NusFontInfo();
            nusInfo.FeaturesLength = linguisticFeaturesLength;

            _linguisticFeatures = new LinguisticFeatureInfo[linguisticFeaturesLength];

            List<uint> ligusticFeatureNameOffset = new List<uint>();

            for (int i = 0; i < linguisticFeaturesLength; i++)
            {
                uint offset = reader.ReadUInt32();
                ligusticFeatureNameOffset.Add(offset);
            }

            NonUniformUnit[] nusUnits = null;
            List<SectionInfo> sectionInfo = new List<SectionInfo>();
            this.sections = new NusVoiceSection[sectionLength];
            for (int i = 0; i < sectionLength; i++)
            {
                SectionInfo info = new SectionInfo();
                info.SectionId = reader.ReadUInt32();
                info.Offset = reader.ReadUInt32();
                info.Size = reader.ReadUInt32();
                sectionInfo.Add(info);
            }

            int idx = 0;
            foreach (SectionInfo info in sectionInfo)
            {
                NusVoiceSection section = GetNusVoiceSection(info.SectionId, nusUnits);
                section.Offset = info.Offset;
                section.Size = info.Size;
                section.LoadData(reader, nusInfo);
                if (section.NusUnits != null)
                {
                    nusUnits = section.NusUnits;
                }

                if (reader.BaseStream.Position != info.Offset + info.Size)
                {
                    throw new InvalidDataException("The long unit file is not correct.");
                }

                this.sections[idx++] = section;
            }

            _stringPool = new StringPool();
            if (stringPoolSize > 0)
            {
                byte[] buffer = new byte[stringPoolSize];
                Microsoft.Tts.ServiceProvider.HTSVoiceDataEncrypt.DecryptStringPool(
                    reader.ReadBytes(checked((int)stringPoolSize)), buffer);
                _stringPool.PutBuffer(buffer);
            }

            for (int i = 0; i < linguisticFeaturesLength; i++)
            {
                LinguisticFeatureInfo linfo = new LinguisticFeatureInfo();
                _linguisticFeatures[i] = linfo;
                linfo.Name = _stringPool.Strings[i];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        private NusVoiceSection GetNusVoiceSection(uint sectionId, NonUniformUnit[] nusUnits)
        {
            NusVoiceSection section = null;
            switch (sectionId)
            {
                case (uint)NusSectionId.NSI_BOUNDARY:
                    if (nusUnits != null)
                    {
                        ItemRange[][] realFrames = new ItemRange[nusUnits.Length][];
                        section = new LongUnitBoundarySection(nusUnits, realFrames);
                    }
                    else
                    {
                        section = new LongUnitBoundarySection(null, null);
                    }

                    break;
                case (uint)NusSectionId.NSI_UNIT:
                    section = new NusUnitSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_CANDIDATE:
                    section = new NusCandidateSection(nusUnits);
                    break;
                case (uint)(uint)NusSectionId.NSI_TRIE:
                    section = new NusTrieSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_TARGET:
                    section = new NusAcousticTargetSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_EMOTION:
                    section = new NusEmotionSection(nusUnits);
                    break;
            }

            return section;
        }
    }

    /// <summary>
    /// Prompt section.
    /// </summary>
    public class PromptSection : NusVoiceSection
    {
        #region Private fields
        private const int MAX_LOG_F0_DOWN_SCALE_FACTOR = 7; // e^7 = 1096.6Hz, to curve static/delta/acceleration value into 0~1
        private const int MEAN_UP_SCALE_AS_SHORT_FACTOR = 32768; // 2^15
        private const int GainMaximumMean = 10;  // e^MeanRange as maximum value of gain, data is in log

        private int _staticLsfMeanDownScaleTo256Factor = 4;
        private ItemRange[] _indexes = null;
        private NonUniformUnit[] _nusUnits = null;
        #endregion

        public PromptSection(NonUniformUnit[] nusUnits, uint inFeaturePreision, uint inLspOrder, uint inGainOrder, uint inF0Order)
        {
            _nusUnits = nusUnits;
            PromptFeaturePrecision = inFeaturePreision;
            LspOrder = inLspOrder;
            GainOrder = inGainOrder;
            F0Order = inF0Order;
        }

        // Prompt header information including feature precsion, the number of type of feature, LSP order, F0 order, gain order
        public uint PromptFeaturePrecision { get; set; }

        public uint LspOrder { get; set; }

        public uint F0Order { get; set; }

        public uint GainOrder { get; set; }

        // Candidate Index Table
        public uint[] NuSpsOffsetTable { get; set; }

        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_PROMPT;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return _nusUnits; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public override void WriteData(BinaryWriter writer)
        {
            Log("Prompt Section");
            Log("Promp Header Section");

            // Need to do change later, here is hard-coded
            writer.Write(PromptFeaturePrecision);
            if (PromptFeaturePrecision == 0)
            {
                Log("<Prompt Feature Precision> Float Point");
            }
            else if (PromptFeaturePrecision == 1)
            {
                Log("<Prompt Feature Precision> Fixed Point");
            }
            else
            {
                throw new NotSupportedException("Prompt feature only support float point or fixed point.");
            }

            writer.Write(LspOrder);
            Log("<Lsp Order> {0}", LspOrder);

            writer.Write(F0Order);
            Log("<F0 Order> {0}", F0Order);

            writer.Write(GainOrder);
            Log("<Gain Order> {0}", GainOrder);

            Log("<Prompt Trajectory Feature>");
            Func<NonUniformUnit, byte[]> serializer = u =>
            {
                uint LspFrameNumber = (uint)u.LspTrajectoryFeature.Count() / LspOrder;
                uint F0FrameNumber = (uint)u.F0TrajectoryFeature.Count() / F0Order;
                uint GainFrameOrder = (uint)u.GainTrajectoryFeature.Count() / GainOrder;
                if (CheckDataCompleteness(LspFrameNumber, F0FrameNumber, GainFrameOrder, u.DurationOfPhone))
                {
                    var stream = new MemoryStream();
                    BinaryWriter memoryWriter = new BinaryWriter(stream);

                    // Float point version
                    if (PromptFeaturePrecision == 0)
                    {
                        // Copy Lsp bytes
                        byte[] lspBytes = new byte[u.LspTrajectoryFeature.Length * sizeof(float)];
                        Buffer.BlockCopy(u.LspTrajectoryFeature, 0, lspBytes, 0, lspBytes.Length);
                        memoryWriter.Write(lspBytes);

                        // Copy f0 bytes
                        byte[] f0Bytes = new byte[u.F0TrajectoryFeature.Length * sizeof(float)];
                        Buffer.BlockCopy(u.F0TrajectoryFeature, 0, f0Bytes, 0, f0Bytes.Length);
                        memoryWriter.Write(f0Bytes);

                        // Copy gain bytes
                        byte[] gainBytes = new byte[u.GainTrajectoryFeature.Length * sizeof(float)];
                        Buffer.BlockCopy(u.GainTrajectoryFeature, 0, gainBytes, 0, gainBytes.Length);
                        memoryWriter.Write(gainBytes);

                        // Copy duration bytes
                        byte[] durationBytes = new byte[u.DurationOfPhone.Length * sizeof(uint)];
                        Buffer.BlockCopy(u.DurationOfPhone, 0, durationBytes, 0, durationBytes.Length);
                        memoryWriter.Write(durationBytes);
                    }
                    else // Fixed point version
                    {
                        byte[] QuantilizedLsp = new byte[u.LspTrajectoryFeature.Length];
                        short[] QuantilizedF0 = new short[u.F0TrajectoryFeature.Length];
                        short[] QuantilizedGain = new short[u.GainTrajectoryFeature.Length];
                        QuantilizeLsp(u.LspTrajectoryFeature, ref QuantilizedLsp, LspFrameNumber, LspOrder);
                        QuantilizeF0(u.F0TrajectoryFeature, ref QuantilizedF0);
                        QuantilizeGain(u.GainTrajectoryFeature, ref QuantilizedGain);

                        // Copy Lsp bytes
                        byte[] lspBytes = new byte[QuantilizedLsp.Length * sizeof(byte)];
                        Buffer.BlockCopy(QuantilizedLsp, 0, lspBytes, 0, lspBytes.Length);
                        memoryWriter.Write(lspBytes);

                        // Copy f0 bytes
                        byte[] f0Bytes = new byte[QuantilizedF0.Length * sizeof(short)];
                        Buffer.BlockCopy(QuantilizedF0, 0, f0Bytes, 0, f0Bytes.Length);
                        memoryWriter.Write(f0Bytes);

                        // Copy gain bytes
                        byte[] gainBytes = new byte[QuantilizedGain.Length * sizeof(short)];
                        Buffer.BlockCopy(QuantilizedGain, 0, gainBytes, 0, gainBytes.Length);
                        memoryWriter.Write(gainBytes);

                        // Copy duration bytes
                        byte[] durationBytes = new byte[u.DurationOfPhone.Length * sizeof(uint)];
                        Buffer.BlockCopy(u.DurationOfPhone, 0, durationBytes, 0, durationBytes.Length);
                        memoryWriter.Write(durationBytes);
                    }

                    return stream.ToArray();
                }
                else
                {
                    throw new InvalidDataException("The frame length is inconsistent");
                }
            };

            _indexes = WriteIndexedItems(_indexes, _nusUnits, writer, serializer);
        }

        private static double Clip(int min, double value, int max)
        {
            double result = Math.Min(value, max);
            result = Math.Max(result, min);
            return result;
        }

        private static double NormalizeToOne(double value, double max)
        {
            Debug.Assert(max != 0.0f, "Not to divide zero.");
            return (double)value / max;
        }

        private void QuantilizeF0(float[] f0Trajectory, ref short[] quantilizedF0)
        {
            for (int i = 0; i < f0Trajectory.Length; i++)
            {
                double mean = (double)f0Trajectory[i] / MAX_LOG_F0_DOWN_SCALE_FACTOR * MEAN_UP_SCALE_AS_SHORT_FACTOR;
                quantilizedF0[i] = (short)Clip(short.MinValue, Math.Round(mean), short.MaxValue);
            }
        }

        private void QuantilizeLsp(float[] lspTrajectory, ref byte[] quantilizedLsp, uint frameNumber, uint lspOrder)
        {
            if (lspOrder >= 40)
            {
                _staticLsfMeanDownScaleTo256Factor = 4;
            }
            else
            {
                _staticLsfMeanDownScaleTo256Factor = 8;
            }

            for (uint j = 0; j < frameNumber; j++)
            {
                short lastQuantizedMean = 0;
                for (int i = 0; i < lspOrder; i++)
                {
                    double quantizedMean = lspTrajectory[i + (j * lspOrder)] * short.MaxValue;
                    quantizedMean /= _staticLsfMeanDownScaleTo256Factor;
                    short deltaOfTwoLsf = (short)Clip((short)1, Math.Round(quantizedMean - lastQuantizedMean), (short)byte.MaxValue);
                    lastQuantizedMean = (short)(lastQuantizedMean + deltaOfTwoLsf);
                    quantilizedLsp[i + (j * lspOrder)] = (byte)deltaOfTwoLsf;
                }
            }
        }

        private void QuantilizeGain(float[] gainTrajectory, ref short[] quantilizedGain)
        {
            for (int i = 0; i < gainTrajectory.Length; i++)
            {
                double mean = NormalizeToOne(gainTrajectory[i], GainMaximumMean) * short.MaxValue;
                mean = Clip(short.MinValue, Math.Round(mean), short.MaxValue);
                quantilizedGain[i] = (short)mean;
            }
        }

        private bool CheckDataCompleteness(uint lspFrameNumber, uint f0FrameNumber, uint gainFrameNumber, uint[] phoneFrameNumber)
        {
            uint sum = 0;

            foreach (var number in phoneFrameNumber)
            {
                sum += number;
            }

            if (lspFrameNumber != sum || f0FrameNumber != sum || gainFrameNumber != sum)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Overall NuSPS voice data serializer.
    /// </summary>
    public class NuSpsVoiceData : IDisposable
    {
        private VoiceFontHeader _header = new VoiceFontHeader();
        private LinguisticFeatureInfo[] _linguisticFeatures;

        private NusUnitSection _nusUnitSection;
        private PromptSection _nusPromptSection;
        private NusTrieSection _nusTrieSection;
        private StringPool _stringPool;
        private uint _stringPoolOffset;
        private uint _stringPoolSize;

        private NusVoiceSection[] sections;

        public NuSpsVoiceData(NonUniformUnit[] nusUnits, LinguisticFeatureInfo[] linguisticFeatures, uint inFeaturePreision, uint inLspOrder, uint inGainOrder, uint inF0Order)
        {
            _header.FileTag = (uint)FontFileTag.FFT_NUS;
            _header.FormatTag = VoiceFontTag.FmtIdNusVoiceData;

            _linguisticFeatures = linguisticFeatures;

            _nusUnitSection = new NusUnitSection(nusUnits);
            _nusPromptSection = new PromptSection(nusUnits, inFeaturePreision, inLspOrder, inGainOrder, inF0Order);
            _nusTrieSection = new NusTrieSection(nusUnits);

            sections = new NusVoiceSection[] 
            {
                _nusUnitSection,
                _nusPromptSection,
                _nusTrieSection,
            };
        }

        public ILogger Logger { get; set; }

        public void Write(string path)
        {
            // first set the offset/file size fields
            FileStream file = new FileStream(path, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    Write(writer);
                }
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }

            // actual write
            file = new FileStream(path, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    Write(writer);
                }
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }

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
        /// Disposes the resources used in this object.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _stringPool)
                {
                    _stringPool.Dispose();
                }

                if (null != _nusTrieSection)
                {
                    _nusTrieSection.Dispose();
                }
            }
        }

        #endregion

        private void Write(BinaryWriter writer)
        {
            // font header
            _header.Save(writer);

            var dataOffset = writer.BaseStream.Position;

            // nus header
            writer.Write(_linguisticFeatures.Length);
            writer.Write(sections.Length);
            writer.Write(_stringPoolOffset);
            writer.Write(_stringPoolSize);

            // build string pool
            _stringPool = new StringPool();

            foreach (var feature in _linguisticFeatures)
            {
                writer.Write((uint)_stringPool.Length);
                _stringPool.PutString(feature.Name);
            }

            foreach (var section in sections)
            {
                writer.Write(section.SectionId);
                writer.Write(section.Offset);
                writer.Write(section.Size);
                section.Logger = Logger;
            }

            // sections
            foreach (var section in sections)
            {
                section.Write(writer);
            }

            _stringPoolOffset = checked((uint)writer.BaseStream.Position);
            var stringBytes = _stringPool.ToArray();
            _stringPoolSize = checked((uint)stringBytes.Length);

            byte[] encryptedBytes = new byte[_stringPool.Length];
            HTSVoiceDataEncrypt.EncryptStringPool(_stringPool.ToArray(), encryptedBytes);

            writer.Write(encryptedBytes);

            // set font file header size
            _header.DataSize = checked((ulong)(writer.BaseStream.Position - dataOffset));
        }
    }

    /// <summary>
    /// Long unit frame boundary section.
    /// </summary>
    public class LongUnitBoundarySection : NusVoiceSection
    {
        private ItemRange[] _indexes = null;
        private NonUniformUnit[] _nusUnits = null;
        private ItemRange[][] _realFrames = null;
        private Dictionary<NonUniformUnit, int> _unitIndex = null;

        #region Constructor
        public LongUnitBoundarySection(NonUniformUnit[] nusUnits, ItemRange[][] frames)
        {
            if (nusUnits.Length != frames.Length)
            {
                throw new Exception("It is not acceptable, the array nusUnits and frames lenth are not equal.");
            }

            _nusUnits = nusUnits;
            _realFrames = frames;

            // Creat the index map.
            _unitIndex = new Dictionary<NonUniformUnit, int>();
            foreach (var item in _nusUnits)
            {
                _unitIndex.Add(item, _unitIndex.Count);
            }
        }
        #endregion

        /// <summary>
        /// Gets SectionId.
        /// </summary>
        public override uint SectionId
        {
            get
            {
                return (uint)NusSectionId.NSI_BOUNDARY;
            }
        }

        public override NonUniformUnit[] NusUnits
        {
            get { return _nusUnits; }
        }

        public ItemRange[][] RealFrames
        {
            get { return _realFrames; }
        }

        /// <summary>
        /// Section writer.
        /// </summary>
        /// <param name="writer">BinaryWriter.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public override void WriteData(BinaryWriter writer)
        {
            Func<NonUniformUnit, byte[]> serializer = u =>
            {
                uint dataWidth = 2;
                int framesLength = 0;
                int[][] unitFrames = null;

                // Get specified LongUnit frames.
                int frameIndex = _unitIndex[u];
                var longUnitFrames = _realFrames[frameIndex];
                if (longUnitFrames != null)
                {
                    // Convert all ItemRange to int array.
                    unitFrames = longUnitFrames.Select(s => new int[] { s.Start, s.Length }).ToArray();

                    // The width should be equal 2.
                    dataWidth = checked((uint)unitFrames.First().Length);
                    framesLength = unitFrames.Length;
                }

                var stream = new MemoryStream();
                BinaryWriter memoryWriter = new BinaryWriter(stream);

                memoryWriter.Write(dataWidth);
                memoryWriter.Write(framesLength);
                if (unitFrames != null)
                {
                    int[] indexes = unitFrames.SelectMany(c => c).ToArray();
                    byte[] indexBytes = new byte[indexes.Length * sizeof(uint)];
                    Buffer.BlockCopy(indexes, 0, indexBytes, 0, indexBytes.Length);
                    memoryWriter.Write(indexBytes);
                }

                return stream.ToArray();
            };

            _indexes = WriteIndexedItems(_indexes, _nusUnits, writer, serializer);
        }

        public override void LoadData(BinaryReader reader, NusFontInfo info)
        {
            _realFrames = new ItemRange[_nusUnits.Count()][];
            int idx = 0;
            if (_unitIndex == null)
            {
                _unitIndex = new Dictionary<NonUniformUnit, int>();
            }

            Func<byte[], NusFontInfo, NonUniformUnit> deserializer = (bytes, fontInfo) =>
            {
                NonUniformUnit unit = new NonUniformUnit();
                using (var stream = new MemoryStream(bytes))
                {
                    BinaryReader memoryReader = new BinaryReader(stream);
                    uint dataWidth = memoryReader.ReadUInt32();
                    uint candidatesCount = memoryReader.ReadUInt32();
                    int indexBytesLength = checked((int)Size) - (sizeof(uint) * 2);
                    byte[] indexBytes = memoryReader.ReadBytes(indexBytesLength);
                    int size = checked((int)(dataWidth * sizeof(int)));
                    ItemRange[] itemsRanges = new ItemRange[candidatesCount];

                    for (int i = 0; i < candidatesCount; i++)
                    {
                        int offset = checked((int)(dataWidth * i * sizeof(int)));
                        int[] range = new int[dataWidth];
                        Buffer.BlockCopy(indexBytes, offset, range, 0, size);

                        ItemRange itemRange = new ItemRange();
                        itemRange.Start = range[0];
                        itemRange.Length = range[1];
                        itemsRanges[i] = itemRange;
                    }

                    _realFrames[idx++] = itemsRanges;
                }

                return unit;
            };
            IList<NonUniformUnit> nuuItems = ReadIndexedItems(reader, info, deserializer);
        }
    }

    /// <summary>
    /// Overall Long Unit voice data serializer.
    /// </summary>
    public class LongUnitVoiceData : IDisposable
    {
        private VoiceFontHeader _header = new VoiceFontHeader();
        private LinguisticFeatureInfo[] _linguisticFeatures;
        private NusUnitSection _nusUnitSection;
        private NusAcousticTargetSection _nusAcousticTargetSection;
        private NusCandidateSection _nusCandidateSection;
        private NusTrieSection _nusTrieSection;
        private LongUnitBoundarySection _longUnitBoundarySection;
        private StringPool _stringPool;
        private uint _stringPoolOffset;
        private uint _stringPoolSize;
        private NusVoiceSection[] sections;

        #region Constructor

        public LongUnitVoiceData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the LongUnitVoiceData class.
        /// </summary>
        /// <param name="nusUnits">Unit list.</param>
        /// <param name="linguisticFeatures">Linguistic features list.</param>
        /// <param name="realFrames">Long Unit segments frames list.</param>
        public LongUnitVoiceData(NonUniformUnit[] nusUnits, LinguisticFeatureInfo[] linguisticFeatures, ItemRange[][] realFrames)
        {
            _header.FileTag = (uint)FontFileTag.FFT_NUS;
            _header.FormatTag = VoiceFontTag.FmtIdNusVoiceData;

            _linguisticFeatures = linguisticFeatures;

            _nusUnitSection = new NusUnitSection(nusUnits);
            _nusAcousticTargetSection = new NusAcousticTargetSection(nusUnits);
            _nusCandidateSection = new NusCandidateSection(nusUnits);
            _nusTrieSection = new NusTrieSection(nusUnits);
            _longUnitBoundarySection = new LongUnitBoundarySection(nusUnits, realFrames);

            sections = new NusVoiceSection[] 
            {
                _nusUnitSection,
                _nusAcousticTargetSection,
                _nusCandidateSection,
                _nusTrieSection,
                _longUnitBoundarySection,
            };
        }
        #endregion

        /// <summary>
        /// Gets or sets Logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets trace level. 0: full trace information, others: simple trace information.
        /// </summary>
        public uint TraceLevel { get; set; }

        public LinguisticFeatureInfo[] LiguisticFeatures
        {
            get { return _linguisticFeatures; }
        }

        /// <summary>
        /// Save data to file.
        /// </summary>
        /// <param name="path">File full path.</param>
        public void Write(string path)
        {
            // first set the offset/file size fields
            FileStream file = new FileStream(path, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    Write(writer);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }

            // actual write
            file = new FileStream(path, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    file = null;
                    Write(writer);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }
        }

        /// <summary>
        /// Load file to data.
        /// </summary>
        /// <param name="path">The font file path.</param>
        public void Load(string path)
        {
            FileStream file = new FileStream(path, FileMode.Open);
            try
            {
                using (BinaryReader reader = new BinaryReader(file))
                {
                    file = null;
                    Load(reader);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }
        }

        public NusVoiceSection GetNusVoiceSection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_UNIT
                       select entry;
            return (NusVoiceSection)item.FirstOrDefault();
        }

        public NusCandidateSection GetNusCandidateSection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_CANDIDATE
                       select entry;
            return (NusCandidateSection)item.FirstOrDefault();
        }

        public LongUnitBoundarySection GetLongUnitBoundarySection()
        {
            var item = from entry in this.sections
                       where entry.SectionId == (uint)NusSectionId.NSI_BOUNDARY
                       select entry;
            return (LongUnitBoundarySection)item.FirstOrDefault();
        }

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
        /// Disposes the resources used in this object.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _stringPool)
                {
                    _stringPool.Dispose();
                }

                if (null != _nusTrieSection)
                {
                    _nusTrieSection.Dispose();
                }
            }
        }

        #endregion

        /// <summary>
        /// Write all data to binary writer.
        /// </summary>
        /// <param name="writer">BinaryWriter.</param>
        private void Write(BinaryWriter writer)
        {
            // font header
            _header.Save(writer);

            var dataOffset = writer.BaseStream.Position;

            // nus header
            writer.Write(_linguisticFeatures.Length);
            writer.Write(sections.Length);
            writer.Write(_stringPoolOffset);
            writer.Write(_stringPoolSize);

            // build string pool
            _stringPool = new StringPool();

            foreach (var feature in _linguisticFeatures)
            {
                writer.Write((uint)_stringPool.Length);
                _stringPool.PutString(feature.Name);
            }

            // Write sections key information.
            foreach (var section in sections)
            {
                writer.Write(section.SectionId);
                writer.Write(section.Offset);
                writer.Write(section.Size);
                section.Logger = Logger;
                section.TraceLevel = TraceLevel;
            }

            // Write sections data.
            foreach (var section in sections)
            {
                section.Write(writer);
            }

            _stringPoolOffset = checked((uint)writer.BaseStream.Position);
            var stringBytes = _stringPool.ToArray();
            _stringPoolSize = checked((uint)stringBytes.Length);

            byte[] encryptedBytes = new byte[_stringPool.Length];
            HTSVoiceDataEncrypt.EncryptStringPool(_stringPool.ToArray(), encryptedBytes);

            writer.Write(encryptedBytes);

            // set font file header size
            _header.DataSize = checked((ulong)(writer.BaseStream.Position - dataOffset));
        }

        private void Load(BinaryReader reader)
        {
            _header.Load(reader);
            int linguisticFeaturesLength = reader.ReadInt32();
            int sectionLength = reader.ReadInt32();
            uint stringPoolOffSet = reader.ReadUInt32();
            uint stringPoolSize = reader.ReadUInt32();
            NusFontInfo nusInfo = new NusFontInfo();
            nusInfo.FeaturesLength = linguisticFeaturesLength;

            _linguisticFeatures = new LinguisticFeatureInfo[linguisticFeaturesLength];

            List<uint> ligusticFeatureNameOffset = new List<uint>();

            for (int i = 0; i < linguisticFeaturesLength; i++)
            {
                uint offset = reader.ReadUInt32();
                ligusticFeatureNameOffset.Add(offset);
            }

            NonUniformUnit[] nusUnits = null;
            List<SectionInfo> sectionInfo = new List<SectionInfo>();
            this.sections = new NusVoiceSection[sectionLength];
            for (int i = 0; i < sectionLength; i++)
            {
                SectionInfo info = new SectionInfo();
                info.SectionId = reader.ReadUInt32();
                info.Offset = reader.ReadUInt32();
                info.Size = reader.ReadUInt32();
                sectionInfo.Add(info);
            }

            int idx = 0;
            foreach (SectionInfo info in sectionInfo)
            {
                NusVoiceSection section = GetNusVoiceSection(info.SectionId, nusUnits);
                section.Offset = info.Offset;
                section.Size = info.Size;
                section.LoadData(reader, nusInfo);
                if (section.NusUnits != null)
                {
                    nusUnits = section.NusUnits;
                }

                if (reader.BaseStream.Position != info.Offset + info.Size)
                {
                    throw new InvalidDataException("The long unit file is not correct.");
                }

                this.sections[idx++] = section;
            }

            _stringPool = new StringPool();
            if (stringPoolSize > 0)
            {
                byte[] buffer = new byte[stringPoolSize];
                Microsoft.Tts.ServiceProvider.HTSVoiceDataEncrypt.DecryptStringPool(
                    reader.ReadBytes(checked((int)stringPoolSize)), buffer);
                _stringPool.PutBuffer(buffer);
            }

            for (int i = 0; i < linguisticFeaturesLength; i++)
            {
                LinguisticFeatureInfo linfo = new LinguisticFeatureInfo();
                _linguisticFeatures[i] = linfo;
                linfo.Name = _stringPool.Strings[i];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        private NusVoiceSection GetNusVoiceSection(uint sectionId, NonUniformUnit[] nusUnits)
        {
            NusVoiceSection section = null;
            switch (sectionId)
            {
                case (uint)NusSectionId.NSI_BOUNDARY:
                    if (nusUnits != null)
                    {
                        ItemRange[][] realFrames = new ItemRange[nusUnits.Length][];
                        section = new LongUnitBoundarySection(nusUnits, realFrames);
                    }
                    else
                    {
                        section = new LongUnitBoundarySection(null, null);
                    }

                    break;
                case (uint)NusSectionId.NSI_UNIT:
                    section = new NusUnitSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_CANDIDATE:
                    section = new NusCandidateSection(nusUnits);
                    break;
                case (uint)(uint)NusSectionId.NSI_TRIE:
                    section = new NusTrieSection(nusUnits);
                    break;
                case (uint)NusSectionId.NSI_TARGET:
                    section = new NusAcousticTargetSection(nusUnits);
                    break;
            }

            return section;
        }
    }

    public class NusFontInfo
    {
        public int FeaturesLength { get; set; }
    }

    internal class SectionInfo
    {
        public uint SectionId { get; set; }

        public uint Offset { get; set; }

        public uint Size { get; set; }
    }
}