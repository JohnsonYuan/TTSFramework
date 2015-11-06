//----------------------------------------------------------------------------
// <copyright file="WaveInventoryPostProcess.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the split and compress WVE file.
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Font
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Interop;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// The spliter class.
    /// </summary>
    public class WaveInventoryPostProcess
    {
        // Log writer, if you want save some print infromation into file, you need set it.
        private static StreamWriter logWriter = null;

        /// <summary>
        /// Start the pruning.
        /// </summary>
        /// <param name="fontPath">Voice font path, like .\1033 .</param>
        /// <param name="configFile">Config file which will tell how many sentence will be include in each sub wve.</param>
        /// <param name="bFillData">Whether fill data if the sub WVE can't be exact division.</param>
        /// <param name="outputFolder">Target directory.</param>
        /// <returns>New WVE File path List.</returns>
        public static List<string> InventorySplit(string fontPath, string configFile, bool bFillData, string outputFolder)
        {
            string unitfile = fontPath + ".unt";
            string wihFile = fontPath + ".wih";
            string wveFile = fontPath + ".wve";
            string outputUnit = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(unitfile) + ".Splited.unt");

            PrintLog("Start to update split the WVE file...", true);

            // spliet the file.
            List<string> newWVEFileList = new List<string>();

            // Load the WIH file.
            WaveInfoHeader header = new WaveInfoHeader();
            header.Load(wihFile);
            const uint CompressFrameSize = 640;

            Dictionary<string, uint> untFrameUpdateList = new Dictionary<string, uint>();
            Dictionary<string, uint> acdFrameUpdateList = new Dictionary<string, uint>();
            uint curFillFrameCount = 0;
            Dictionary<string, int> namedUnitTypeId = new Dictionary<string, int>();
            Dictionary<int, WaveCandidateInfo> updatedCandidates = new Dictionary<int, WaveCandidateInfo>();
            using (UnitIndexingFile indexFile = new UnitIndexingFile(namedUnitTypeId))
            {
                indexFile.Load(unitfile);

                uint bytesPerFrame = indexFile.SamplePerFrame * header.BytesPerSample;
                uint compressFrameCount = CompressFrameSize / bytesPerFrame;

                // Get all sentence id information from the index file canidate, and save them in a sorted dictionary.
                // Because the sentece is sorted by a sorted dictionary in Font traing process.
                List<string> sentencIDs = indexFile.WaveCandidates.SelectMany(c => c.Value.Select(k => k.Value.SentenceId).Distinct()).Distinct().ToList();
                Dictionary<string, int> dic = sentencIDs.Select((v, i) => new { Value = v, Index = i }).ToDictionary(p => p.Value, p => p.Index);
                SortedDictionary<string, int> sortedSentenceDic = new SortedDictionary<string, int>(dic);
                List<string> orderedSentenceIDs = sortedSentenceDic.Select(e => e.Key).ToList();

                // Get the confing data.
                List<uint> sentencCountList = ReadConfigFile(configFile, (uint)orderedSentenceIDs.Count);

                using (FileStream readerStream = File.Open(wveFile, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(readerStream);

                    // load header
                    VoiceFontHeader fontHeader = new VoiceFontHeader();
                    fontHeader.Load(reader);

                    PrintLog("[Totoal Frame in each new VWF file.]", true);

                    // the data offset from the header
                    long dataOffSet = reader.BaseStream.Position;
                    uint outputFileSuffix = 0;
                    uint readFrameNumber = 0;
                    int curSplitSegmentIndex = 0;
                    for (int index = 0; index <= orderedSentenceIDs.Count; index++)
                    {
                        if (index < sentencCountList[curSplitSegmentIndex])
                        {
                            untFrameUpdateList.Add(orderedSentenceIDs[index], curFillFrameCount);
                            continue;
                        }

                        curSplitSegmentIndex++;
                        string newWVEFileName = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(wveFile) + "_" + outputFileSuffix.ToString() + ".wve");
                        PrintLog(string.Format("{0} Frame Update Count: {1}", Path.GetFileName(newWVEFileName), curFillFrameCount), true);

                        using (FileStream wveFileStream = File.Open(newWVEFileName, FileMode.Create))
                        {
                            BinaryWriter wveWriter = new BinaryWriter(wveFileStream);
                            fontHeader.Save(wveWriter);
                            long writerOffSet = wveWriter.BaseStream.Position;

                            // calculate lenth
                            long length = 0;
                            uint frameCount = 0;
                            if (index == orderedSentenceIDs.Count)
                            {
                                length = reader.BaseStream.Length - reader.BaseStream.Position;
                                frameCount = (uint)(length / bytesPerFrame);
                            }
                            else
                            {
                                // Get all candidate which are got from the specfied sentenc and sorted by frame index.
                                string endSentenceID = orderedSentenceIDs[index];
                                List<WaveCandidateInfo> endSenteceWaveInfo = indexFile.WaveCandidates.SelectMany(c => c.Value.Where(p => p.Value.SentenceId == endSentenceID))
                                                                                                             .Select(i => i.Value).SortBy(k => k.FrameIndexInSentence).ToList();

                                // Get the end candiate in the specifid sentence.
                                WaveCandidateInfo firstWaveCandidate = endSenteceWaveInfo[0];

                                frameCount = firstWaveCandidate.FrameIndex - firstWaveCandidate.FrameIndexInSentence - readFrameNumber;
                                length = bytesPerFrame * frameCount;
                                readFrameNumber = firstWaveCandidate.FrameIndex - firstWaveCandidate.FrameIndexInSentence;
                            }

                            // we need calculate how many bit will be filled into the WVE when it will be commpressed.
                            // 640 bit will be used in the compress tool, so we will reference this.
                            uint byteGap = (uint)((CompressFrameSize - (length % CompressFrameSize)) % CompressFrameSize);
                            uint updateFrameCount = (byteGap / bytesPerFrame) % compressFrameCount;
                            curFillFrameCount += updateFrameCount;

                            reader.BaseStream.Seek(dataOffSet, SeekOrigin.Begin);
                            byte[] data = reader.ReadBytes((int)length);
                            wveWriter.Write(data);

                            // fill data if the length can't be exact division.
                            if (bFillData)
                            {
                                FillSilenceData(wveWriter, byteGap);
                            }

                            // save the header file
                            fontHeader.DataSize = (ulong)(wveWriter.BaseStream.Position - writerOffSet);
                            SaveFontHeader(fontHeader, wveWriter);

                            dataOffSet = reader.BaseStream.Position;
                            PrintLog(string.Format("{0} Total Frame Count: {1} + {2}", Path.GetFileName(newWVEFileName), frameCount, updateFrameCount.ToString()), true);
                            PrintLog(string.Empty, true);

                            // recode the new file path.
                            newWVEFileList.Add(newWVEFileName);

                            if (index != orderedSentenceIDs.Count)
                            {
                                acdFrameUpdateList.Add(orderedSentenceIDs[index], curFillFrameCount);
                            }
                        }

                        header.Save(Path.ChangeExtension(newWVEFileName, "wih"));
                        outputFileSuffix++;

                        if (index != orderedSentenceIDs.Count)
                        {
                            index--;
                        }
                    }
                }

                // update ACD file.
                PrintLog("Update ACD file!", true);
                UpdateACDFile(indexFile, acdFrameUpdateList, fontPath + ".acd", Path.ChangeExtension(outputUnit, "acd"));

                // update index file
                PrintLog("Update UNT file!", true);
                UpdateIndexingFile(indexFile, untFrameUpdateList);
                indexFile.Save(outputUnit);
            }

            PrintLog("Split successfully!", true);

            return newWVEFileList;
        }

        /// <summary>
        /// Compress spicifed wve file.
        /// </summary>
        /// <param name="wveFilePath">WVE file path.</param>
        /// <param name="wihFilePath">WIH file paht.</param>
        /// <param name="codec">Compress type.</param>
        /// <param name="outputFolder">New font path.</param>
        /// <param name="enableAnalysis">Enable analysis or not.</param>
        /// <returns>Encode block count.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "False alarm!")]
        public static int CompressWVE(string wveFilePath, string wihFilePath, SpeechCodecEnc.Codec codec, string outputFolder, bool enableAnalysis)
        {
            WaveInfoHeader wih = new WaveInfoHeader();
            wih.Load(wihFilePath);
            if (wih.Compression != Microsoft.Tts.Offline.Config.WaveCompressCatalog.Unc)
            {
                throw new NotSupportedException("Current font has already compress !");
            }

            if (wih.SamplesPerSecond != 16000 || wih.BytesPerSample != 2)
            {
                return -1;
            }

            string strWVECompressedFilePath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(wveFilePath) + "." + codec.ToString() + @".WVE");
            string strWIHCompressedFilePath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(wveFilePath) + "." + codec.ToString() + @".WIH");
            string strRADCompressedFilePath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(wveFilePath) + "." + codec.ToString() + @".RAD");

            SpeechCodecDec.Codec decCodec = SpeechCodecDec.Codec.SILK;

            PrintLog("Start Compress font.", true);
            switch (codec)
            {
                // case SpeechCodecEnc.Codec.AMRWB:
                // wih.Compression = Microsoft.Tts.Offline.Config.WaveCompressCatalog.AMRWB;
                // break;
                case SpeechCodecEnc.Codec.MSRTA:
                    {
                        wih.Compression = Microsoft.Tts.Offline.Config.WaveCompressCatalog.MSRTA;
                        break;
                    }

                case SpeechCodecEnc.Codec.SILK:
                    {
                        wih.Compression = Microsoft.Tts.Offline.Config.WaveCompressCatalog.SILK;
                        decCodec = SpeechCodecDec.Codec.SILK;
                        break;
                    }

                case SpeechCodecEnc.Codec.OpusSILK:
                    {
                        wih.Compression = Microsoft.Tts.Offline.Config.WaveCompressCatalog.OpusSILK;
                        decCodec = SpeechCodecDec.Codec.OpusSILK;
                        break;
                    }

                default:
                    {
                        throw new Exception("Not supported compression!");
                    }
            }

            wih.Save(strWIHCompressedFilePath);

            string wveSamplePath = strWVECompressedFilePath + @".SAMPLE.PCM";
            string orgPcmPath = wveSamplePath + ".org.pcm";
            string encodedPath = wveSamplePath + ".opus";
            string lspTextPath = wveSamplePath + ".lsp.txt";

            List<byte> blockOffsets = new List<byte>();
            int nTotalEncoded = 0;
            const uint RAD_TAG = 0x4441522E;   // .RAD

            SpeechCodecDec decoder = new SpeechCodecDec();
            decoder.Open(decCodec);
            bool supoortRAD = decoder.SupportRAD;
            SpeechCodecEnc encoder = new SpeechCodecEnc();
            encoder.Open(codec);

            using (Obfuscation obf = new Obfuscation(ObfuscationDefaultPassword.ODP_INVENTORYBUILDING))
            using (FileStream wveStream = new FileStream(wveFilePath, FileMode.Open))
            using (FileStream compressedWVEStream = new FileStream(strWVECompressedFilePath, FileMode.Create))
            using (BinaryWriter binwRADStream = new BinaryWriter(new FileStream(strRADCompressedFilePath, FileMode.Create)))
            using (FileStream wveSampleStream = new FileStream(wveSamplePath, FileMode.Create))
            using (FileStream orgPcmStream = new FileStream(orgPcmPath, FileMode.Create))
            using (FileStream encodedStream = new FileStream(encodedPath, FileMode.Create))
            using (StreamWriter lspWriter = new StreamWriter(lspTextPath))
            {
                BinaryReader br = new BinaryReader(wveStream);
                BinaryWriter bw = new BinaryWriter(compressedWVEStream);

                VoiceFontHeader vfh = new VoiceFontHeader();
                vfh.Load(br);
                vfh.Save(bw);
                long dataOffset = bw.BaseStream.Position;
                if (codec == SpeechCodecEnc.Codec.SILK || codec == SpeechCodecEnc.Codec.OpusSILK)
                {
                    // save offset table's offset
                    bw.Write((int)0);
                }

                // create a decoder for RAD stat calculating
                SpeechCodecDec decoderRAD = new SpeechCodecDec();
                decoderRAD.Open(decCodec);
                OpusSILKRADStatSerializer radSerializer = new OpusSILKRADStatSerializer();
                VoiceFontHeader voiceFontRADHeader = new VoiceFontHeader
                {
                    FileTag = RAD_TAG,
                    FormatTag = VoiceFontTag.FmtIdRandomAccessDecodingData,
                    DataSize = 0,
                    Version = 0,
                    Build = 0
                };

                long dataBegPos = 0;
                if (supoortRAD)
                {
                    voiceFontRADHeader.Save(binwRADStream);
                    dataBegPos = binwRADStream.BaseStream.Position;
                    binwRADStream.Write((uint)0); // stat num
                    binwRADStream.Write((uint)0); // stat bytes num
                }

                List<int> listOffsets = new List<int>();
                int nTotalRADBytes = 0;

                BinaryWriter bw2 = null;
                BinaryWriter bw3 = null;
                BinaryWriter bw4 = null;
                if (enableAnalysis)
                {
                    bw2 = new BinaryWriter(wveSampleStream);
                    bw3 = new BinaryWriter(orgPcmStream);
                    bw4 = new BinaryWriter(encodedStream);
                }

                short[] sampleBuf = new short[320];
                byte[] readBytes = new byte[640];
                int nReadBytes = 0;
                int nLoops = 0;

                ulong nosieCountDecodedSample = 0;
                while ((nReadBytes = br.Read(readBytes, 0, (int)(sampleBuf.Length * 2))) > 0)
                {
                    obf.DeObfuscate(readBytes, (int)(br.BaseStream.Position - nReadBytes - dataOffset));
                    Buffer.BlockCopy(readBytes, 0, sampleBuf, 0, nReadBytes);

                    byte[] encodedBits = encoder.EncodeSamples(sampleBuf);

                    if (supoortRAD)
                    {
                        byte[] radBytes = decoderRAD.CalculateRADStat(encodedBits);
                        byte[] serializedRADBytes = radBytes;
                        if (codec == SpeechCodecEnc.Codec.OpusSILK)
                        {
                            serializedRADBytes = radSerializer.Serialize(radBytes);
                        }
                        
                        binwRADStream.Write(serializedRADBytes);
                        listOffsets.Add(nTotalRADBytes);
                        nTotalRADBytes += serializedRADBytes.Length;
                        decoder.SetRADStat(radBytes);
                    }

                    short[] decodedSamples = decoder.DecodeSamples(encodedBits);
                    ulong noiseCount = Microsoft.Tts.ServiceProvider.WaveGenerator.DetectNoise16k16bMono(decodedSamples);
                    if (noiseCount > 0)
                    {
                        nosieCountDecodedSample += noiseCount;
                    }

                    if (enableAnalysis)
                    {
                        if (bw3.BaseStream.Position <= 64000)
                        {
                            foreach (var sample in sampleBuf)
                            {
                                bw3.Write(sample);
                            }

                            bw3.Flush();
                        }

                        if (bw2.BaseStream.Position <= 64000)
                        {
                            bw4.Write(encodedBits);
                            bw4.Flush();

                            foreach (var sample in decodedSamples)
                            {
                                bw2.Write(sample);
                            }

                            bw2.Flush();

                            float[] lspValues = decoder.DecodeLsp(encodedBits);
                            foreach (var lsp in lspValues)
                            {
                                lspWriter.Write("{0:N6}\t", lsp);
                            }

                            lspWriter.WriteLine();
                            lspWriter.Flush();
                        }
                    }

                    bw.Write(encodedBits, 0, encodedBits.Length);
                    if (encodedBits.Length > byte.MaxValue)
                    {
                        throw new Exception("Unexpcted encoding length!");
                    }

                    blockOffsets.Add((byte)encodedBits.Length);
                    nTotalEncoded += encodedBits.Length;

                    nLoops++;
                    if (nLoops == 500)
                    {
                        Console.Write("Encoded: {0}/{1}/{2}%         \r", br.BaseStream.Position - dataOffset, vfh.DataSize,
                            (float)(br.BaseStream.Position - dataOffset) / vfh.DataSize * 100);
                        nLoops = 0;
                    }
                }

                decoderRAD.Close();
                PrintLog(string.Format("Encoded: {0}/{1}/{2}%         \r", br.BaseStream.Position - dataOffset, vfh.DataSize,
                    (float)(br.BaseStream.Position - dataOffset) / vfh.DataSize * 100), true);
                PrintLog(string.Empty, true);

                if (codec == SpeechCodecEnc.Codec.SILK || codec == SpeechCodecEnc.Codec.OpusSILK)
                {
                    // save offset table
                    foreach (var offset in blockOffsets)
                    {
                        bw.Write(offset);
                    }

                    PrintLog(string.Format("Total blocks: {0}", blockOffsets.Count), true);
                }

                if (supoortRAD)
                {
                    foreach (var offset in listOffsets)
                    {
                        binwRADStream.Write(offset);
                    }
                }

                PrintLog(string.Empty, true);
                PrintLog(string.Format("Detect {0} noise in decoded samples.", nosieCountDecodedSample), true);

                vfh.DataSize = (ulong)(bw.BaseStream.Position - dataOffset);

                bw.Seek(0, SeekOrigin.Begin);
                vfh.Save(bw);

                if (supoortRAD)
                {
                    voiceFontRADHeader.DataSize = (ulong)(binwRADStream.BaseStream.Position - dataBegPos);
                    binwRADStream.Seek(0, SeekOrigin.Begin);
                    voiceFontRADHeader.Save(binwRADStream);
                    binwRADStream.Write(blockOffsets.Count); // stat num
                    binwRADStream.Write(nTotalRADBytes); // stat bytes num
                }

                if (codec == SpeechCodecEnc.Codec.SILK || codec == SpeechCodecEnc.Codec.OpusSILK)
                {
                    // update offset table's offset
                    bw.Write((int)nTotalEncoded);
                }
            }

            encoder.Close();
            decoder.Close();

            if (!enableAnalysis)
            {
                Helper.SafeDelete(wveSamplePath);
                Helper.SafeDelete(orgPcmPath);
                Helper.SafeDelete(encodedPath);
                Helper.SafeDelete(lspTextPath);
            }

            if (!supoortRAD)
            {
                Helper.SafeDelete(strRADCompressedFilePath);
            }

            return blockOffsets.Count;
        }

        public static void SetLogWiter(StreamWriter loger)
        {
            if (loger != null)
            {
                logWriter = loger;
            }
        }

        /// <summary>
        /// Read the split config file.
        /// </summary>
        /// <param name="filepath">Config file path.</param>
        /// <param name="maxSentenceCount">Sentence count in UNT file.</param>
        /// <returns>The max sentence index in each splited WVE file.</returns>
        private static List<uint> ReadConfigFile(string filepath, uint maxSentenceCount)
        {
            List<uint> retValues = new List<uint>();
            using (StreamReader sr = new StreamReader(filepath))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        uint count = uint.Parse(line);
                        uint preValue = 0;
                        if (retValues.Count != 0)
                        {
                            preValue = retValues[retValues.Count - 1];
                        }

                        if (count <= maxSentenceCount)
                        {
                            retValues.Add(count + preValue);
                        }
                        else
                        {
                            if ((retValues.Count == 0) || (retValues[retValues.Count - 1] != maxSentenceCount))
                            {
                                retValues.Add(maxSentenceCount);
                            }

                            // recoder the real sentence segment.
                            PrintLog("Config file need update. please check the log file to get the real value", true);
                            PrintLog("[Real Sentece Count List]", false);
                            foreach (uint segementSenteceCount in retValues)
                            {
                                PrintLog(segementSenteceCount.ToString(), false);
                            }

                            break;
                        }
                    }
                }

                if (retValues.Count == 0 || (retValues[retValues.Count - 1] != maxSentenceCount))
                {
                    retValues.Add(maxSentenceCount);
                }
            }

            return retValues;
        }

        /// <summary>
        /// Fill silence data to the stream.
        /// </summary>
        /// <param name="writer">File writer.</param>
        /// <param name="length">Data length.</param>
        private static void FillSilenceData(BinaryWriter writer, uint length)
        {
            for (int i = 0; i < length; i++)
            {
                writer.Write((byte)0);
            }
        }

        /// <summary>
        /// Save the voice font header.
        /// </summary>
        /// <param name="fontHeader"> Font header.</param>
        /// <param name="writer"> Writer.</param>
        private static void SaveFontHeader(VoiceFontHeader fontHeader, BinaryWriter writer)
        {
            using (PositionRecover recover = new PositionRecover(writer, 0))
            {
                fontHeader.Save(writer);
            }
        }

        /// <summary>
        /// Update the indexing file based pm tje candiate info.
        /// </summary>
        /// <param name="indexFile">The index file.</param>
        /// <param name="sentenceFrameUpdateList">SentenceOrder list.</param>
        private static void UpdateIndexingFile(UnitIndexingFile indexFile, Dictionary<string, uint> sentenceFrameUpdateList)
        {
            // update all global index.
            indexFile.WaveCandidates.Values.ForEach(p => p.ForEach(w =>
            {
                WaveCandidateInfo wunt = w.Value;
                if (sentenceFrameUpdateList.ContainsKey(wunt.SentenceId))
                {
                    wunt.FrameIndex += sentenceFrameUpdateList[wunt.SentenceId];
                }
                else
                {
                    throw new Exception(string.Format("Please check the code, some sentence missed. senteceID {0}", wunt.SentenceId));
                }
            }));
        }

        /// <summary>
        /// Update the ACD file.
        /// When we split the WVE, if the sub WVE data can't be divided exactly using a frame data size. 
        /// To keep consistent with compressed font, we need add some data into the sub WVE.
        /// After this operation, we need update the frame index in UNT, all freame index behind of the added frame will be increase some frame offset.
        /// However, when we get the frame information in ACD, we need using the frame index in unt file. 
        /// So to keep consistent, we need insert some frame in ACD file the same time.
        /// </summary>
        /// <param name="indexFile">Unit index data.</param>
        /// <param name="sentencFrameUpdateList">Frame update list.</param>
        /// <param name="acdFilePath">Old acd file path.</param>
        /// <param name="newAcdFilePath">New acd file path.</param>
        private static void UpdateACDFile(UnitIndexingFile indexFile, Dictionary<string, uint> sentencFrameUpdateList, string acdFilePath, string newAcdFilePath)
        {
            if (sentencFrameUpdateList.Where(k => k.Value != 0).ToList().Count != 0 && !File.Exists(acdFilePath))
            {
                PrintLog(string.Format("Please check your font, the ACD file need to be update!"), true);
            }

            if (File.Exists(acdFilePath))
            {
                ACDData acdData = null;
                using (FileStream acdFile = File.Open(acdFilePath, FileMode.Open))
                using (ACDDataReader acdReader = new ACDDataReader(new BinaryReader(acdFile)))
                {
                    acdData = acdReader.ReadACDData();
                }

                foreach (var item in sentencFrameUpdateList)
                {
                    // Get all candidate which are got from the specfied sentenc and sorted by frame index.
                    List<WaveCandidateInfo> endSenteceWaveInfo = indexFile.WaveCandidates.SelectMany(c => c.Value.Where(p => p.Value.SentenceId == item.Key))
                                                                                                 .Select(i => i.Value).SortBy(k => k.FrameIndexInSentence).ToList();

                    // Get the end candiate in the specifid sentence.
                    WaveCandidateInfo firstWaveCandidate = endSenteceWaveInfo[0];
                    uint frameStart = (uint)(firstWaveCandidate.FrameIndex - firstWaveCandidate.FrameIndexInSentence);
                    if (frameStart > 0 && acdData != null)
                    {
                        if (acdData.LpccData != null)
                        {
                            AddAcdTableItem(acdData.LpccData, frameStart, item.Value);
                        }

                        if (acdData.RealLpccData != null)
                        {
                            AddAcdTableItem(acdData.RealLpccData, frameStart, item.Value);
                        }

                        if (acdData.F0Data != null)
                        {
                            AddAcdTableItem(acdData.F0Data, frameStart, item.Value);
                        }

                        if (acdData.GainData != null)
                        {
                            AddAcdTableItem(acdData.GainData, frameStart, item.Value);
                        }

                        if (acdData.PowerData != null)
                        {
                            AddAcdTableItem(acdData.PowerData, frameStart, item.Value);
                        }

                        if (acdData.PitchMarkerData != null)
                        {
                            AddAcdTableItem(acdData.PitchMarkerData, frameStart, item.Value);
                        }
                    }

                    ACDDataWriter writer = new ACDDataWriter();
                    writer.Write(acdData, newAcdFilePath);
                }
            }
        }

        /// <summary>
        /// Add a frame data in acd table.
        /// </summary>
        /// <param name="acdTable">Data table.</param>
        /// <param name="frameStart">Frame start.</param>
        /// <param name="frameCount">Added frame count.</param>
        private static void AddAcdTableItem(ACDTable acdTable, uint frameStart, uint frameCount)
        {
            DataTableSetting setting = acdTable.Setting;
            int maxRowLength = acdTable.DataTable.RowCount;
            int columnLength = acdTable.DataTable.ColumnCount;
            int byteSize = 8;
            int bytesPerValue = (setting.QuantizerParas.TargetSize + 7) / byteSize;

            // 1. add the old data.
            byte[] newMatrixBytes = new byte[columnLength * (maxRowLength + frameCount) * bytesPerValue];
            int keptMatrixIndex = 0;
            int bytesPerFrame = columnLength * bytesPerValue;
            for (int i = 0; i < frameStart; i++)
            {
                Buffer.BlockCopy(acdTable.DataTable.MatrixBytes, i * bytesPerFrame, newMatrixBytes, keptMatrixIndex * bytesPerFrame, bytesPerFrame);
                keptMatrixIndex++;
            }

            // 2. insert the new data.
            byte[] invaldeMatirxBytes = new byte[] { 0 };
            for (int i = 0; i < (columnLength * frameCount * bytesPerValue); i++)
            {
                Buffer.BlockCopy(invaldeMatirxBytes, 0, newMatrixBytes, ((keptMatrixIndex * bytesPerFrame) + i), 1);
            }

            // 3. add the other old data
            for (int i = (int)frameStart; i < maxRowLength; i++)
            {
                Buffer.BlockCopy(acdTable.DataTable.MatrixBytes, i * bytesPerFrame, newMatrixBytes, ((keptMatrixIndex + (int)frameCount) * bytesPerFrame), bytesPerFrame);
                keptMatrixIndex++;
            }

            acdTable.DataTable.MatrixBytes = newMatrixBytes;
            acdTable.DataTable.RowCount += (int)frameCount;

            // 4. Update the row map data.
            if (acdTable.DataTable.IsMapRow)
            {
                int totalRowCount = acdTable.DataTable.RowMap.Length + (int)frameCount;
                ushort[] rowMap = new ushort[totalRowCount];
                int mapIndex = 0;
                for (int i = 0; i < totalRowCount; i++)
                {
                    if (i < frameStart & i >= frameStart + frameCount)
                    {
                        rowMap[i] = acdTable.DataTable.RowMap[mapIndex];
                        mapIndex++;
                    }
                    else
                    {
                        // by desinge, the added frame can't be uesed.
                        rowMap[i] = (ushort)acdTable.DataTable.RowCount;
                    }
                }

                acdTable.DataTable.RowMap = rowMap;
            }
        }

        /// <summary>
        /// Print the log.
        /// </summary>
        /// <param name="message">Message .</param>
        /// <param name="bScreen">Whether print the log on screen.</param>
        private static void PrintLog(string message, bool bScreen)
        {
            if (bScreen)
            {
                Console.WriteLine(message);
            }

            if (logWriter != null)
            {
                logWriter.WriteLine(message);
            }
        }
    }
}
