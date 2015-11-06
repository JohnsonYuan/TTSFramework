//----------------------------------------------------------------------------
// <copyright file="PhoneEventCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Phone Event Data Compiler
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Phone Event Data Compiler Error definition.
    /// </summary>
    public enum PhoneEventCompilerError
    {
        /// <summary>
        /// Invalid Phone Set.
        /// </summary>
        [ErrorAttribute(Message = "Invalid phoneset for compiling phone event.")]
        InvalidPhoneSet
    }

    /// <summary>
    /// Interface of PhoneConverter, Remove of dependency on the ServiceProvider.
    /// </summary>
    public interface IPhoneConverter
    {
        /// <summary>
        /// Convert TTS into SAPI phones.
        /// </summary>
        /// <param name="ttsPhone">The tts phone id string.</param>
        /// <returns>The sapi phone id string.</returns>
        string TTS2SAPI(string ttsPhone);

        /// <summary>
        /// Convert TTS into UPS phones.
        /// </summary>
        /// <param name="ttsPhone">The tts phone id string.</param>
        /// <returns>The ups phone id string.</returns>
        string TTS2UPS(string ttsPhone);
    }

    /// <summary>
    /// Phone Event Data Compiler.
    /// </summary>
    public class PhoneEventCompiler
    {
        /// <summary>
        /// For a TTS phone, the maximum number of SAPI phone mapped.
        /// </summary>
        private const int MAX_SAPI_PHONE_PER_TTS = 7;

        /// <summary>
        /// For a TTS phone, the maximum number of ups phone mapped.
        /// </summary>
        private const int MAX_UPS_PHONE_PER_TTS = 7;

        /// <summary>
        /// Ups id for "+" symbol.
        /// </summary>
        private const int VISEME_0 = 0;

        /// <summary>
        /// UPS viseme map.
        /// </summary>
        private static int[,] visemeMap = new int[,]
        {
            /*UPS label, UPS Id, Viseme*/
            { /*_!*/1, 0 },
            { /*_&*/2, 0 },
            { /*_,*/3, 0 },
            { /*_s*/4, 0 },
            { /*NCK*/33, 0 },
            { /*.*/46, 0 },
            { /*A*/97, 2 },
            { /*B*/98, 21 },
            { /*CT*/99, 16 },
            { /*D*/100, 19 },
            { /*E*/101, 4 },
            { /*F*/102, 18 },
            { /*G*/103, 20 },
            { /*H*/104, 12 },
            { /*I*/105, 6 },
            { /*J*/106, 6 },
            { /*K*/107, 20 },
            { /*L*/108, 14 },
            { /*M*/109, 21 },
            { /*N*/110, 19 },
            { /*O*/111, 8 },
            { /*P*/112, 21 },
            { /*QT*/113, 20 },
            { /*RR*/114, 13 },
            { /*S*/115, 15 },
            { /*T*/116, 19 },
            { /*U*/117, 7 },
            { /*V*/118, 18 },
            { /*W*/119, 7 },
            { /*X*/120, 12 },
            { /*Y*/121, 4 },
            { /*Z*/122, 15 },
            { /*_|*/124, 0 },
            { /*AE*/230, 1 },
            { /*C*/231, 12 },
            { /*DH*/240, 17 },
            { /*EU*/248, 1 },
            { /*HH*/295, 12 },
            { /*NG*/331, 20 },
            { /*OE*/339, 4 },
            { /*TCK*/448, 0 },
            { /*LCK*/449, 0 },
            { /*CCK*/450, 0 },
            { /*NCK2*/451, 0 },
            { /*AEX*/592, 4 },
            { /*AA*/593, 2 },
            { /*Q*/594, 2 },
            { /*BIM*/595, 21 },
            { /*AO*/596, 3 },
            { /*SC*/597, 16 },
            { /*DR*/598, 19 },
            { /*DIM*/599, 19 },
            { /*EX*/600, 1 },
            { /*AX*/601, 1 },
            { /*AXR*/602, 1 },
            { /*EH*/603, 4 },
            { /*ER*/604, 5 },
            { /*ERR*/605, 5 },
            { /*UR*/606, 5 },
            { /*JD*/607, 16 },
            { /*GIM*/608, 20 },
            { /*G2*/609, 20 },
            { /*QD*/610, 20 },
            { /*GH*/611, 20 },
            { /*OU*/612, 1 },
            { /*WJ*/613, 7 },
            { /*HZ*/614, 12 },
            { /*SHX*/615, 16 },
            { /*IX*/616, 6 },
            { /*IH*/618, 6 },
            { /*LG*/619, 14 },
            { /*LSH*/620, 14 },
            { /*LR*/621, 14 },
            { /*LH*/622, 6 },
            { /*UU*/623, 4 },
            { /*GA*/624, 20 },
            { /*MF*/625, 21 },
            { /*NJ*/626, 19 },
            { /*NR*/627, 19 },
            { /*QN*/628, 19 },
            { /*OX*/629, 1 },
            { /*AOE*/630, 8 },
            { /*PH*/632, 18 },
            { /*RA*/633, 13 },
            { /*LT*/634, 14 },
            { /*R*/635, 13 },
            { /*DXR*/637, 13 },
            { /*DX*/638, 19 },
            { /*QQ*/640, 13 },
            { /*RH*/641, 13 },
            { /*SR*/642, 15 },
            { /*SH*/643, 16 },
            { /*JIM*/644, 16 },
            { /*SHC*/646, 16 },
            { /*TCK2*/647, 0 },
            { /*TR*/648, 19 },
            { /*YX*/649, 6 },
            { /*UH*/650, 4 },
            { /*VA*/651, 18 },
            { /*AH*/652, 1 },
            { /*WH*/653, 7 },
            { /*LJ*/654, 14 },
            { /*YH*/655, 7 },
            { /*ZR*/656, 15 },
            { /*ZC*/657, 16 },
            { /*ZH*/658, 16 },
            { /*ZHJ*/659, 16 },
            { /*GT*/660, 19 },
            { /*HG*/661, 12 },
            { /*LCK2*/662, 0 },
            { /*NCK3*/663, 0 },
            { /*PCK*/664, 0 },
            { /*BB*/665, 21 },
            { /*QIM*/667, 20 },
            { /*ESH*/668, 12 },
            { /*CJ*/669, 12 },
            { /*GL*/671, 14 },
            { /*QOM*/672, 20 },
            { /*ET*/673, 20 },
            { /*EZH*/674, 12 },
            { /*DZ2*/675, 15 },
            { /*JH2*/676, 16 },
            { /*JC2*/677, 16 },
            { /*TS2*/678, 15 },
            { /*CH2*/679, 16 },
            { /*CC2*/680, 16 },
            { /*asp*/688, 0 },
            { /*bva*/689, 0 },
            { /*pal*/690, 0 },
            { /*rhz*/692, 0 },
            { /*lab*/695, 0 },
            { /*ejc*/700, 0 },
            { /*S1*/712, 0 },
            { /*S2*/716, 0 },
            { /*lng*/720, 0 },
            { /*hlg*/721, 0 },
            { /*xsh*/728, 0 },
            { /*rho*/734, 0 },
            { /*vel*/736, 0 },
            { /*lar*/737, 0 },
            { /*phr*/740, 0 },
            { /*T2*/768, 0 },
            { /*T4*/769, 0 },
            { /*nas*/771, 0 },
            { /*T3*/772, 0 },
            { /*xst*/774, 0 },
            { /*cen*/776, 0 },
            { /*vls*/778, 0 },
            { /*T5*/779, 0 },
            { /*T1*/783, 0 },
            { /*atr*/792, 0 },
            { /*rtr*/793, 0 },
            { /*nar*/794, 0 },
            { /*lrd*/796, 0 },
            { /*rai*/797, 0 },
            { /*low*/798, 0 },
            { /*adv*/799, 0 },
            { /*rte*/800, 0 },
            { /*bvd*/804, 0 },
            { /*vsl*/805, 0 },
            { /*syl*/809, 0 },
            { /*den*/810, 0 },
            { /*vcd*/812, 0 },
            { /*nsy*/815, 0 },
            { /*cvd*/816, 0 },
            { /*ret*/817, 0 },
            { /*vph*/820, 0 },
            { /*mrd*/825, 0 },
            { /*api*/826, 0 },
            { /*lam*/827, 0 },
            { /*lla*/828, 0 },
            { /*mcn*/829, 0 },
            { /*+*/865, 0 },
            { /*BH*/946, 21 },
            { /*TH*/952, 19 },
            { /*QH*/967, 12 },
            { /*_||*/8214, 0 },
            { /*_^*/8255, 0 },
            { /*nsr*/8319, 0 },
            { /*T+*/8593, 0 },
            { /*T=*/8594, 0 },
            { /*T-*/8595, 0 },
            { /*_?*/8599, 0 },
            { /*_.*/8600, 0 }
        };

        /// <summary>
        /// Compiler entrance.
        /// </summary>
        /// <param name="phoneSet">Phone set object.</param>
        /// <param name="phoneConverter">PhoneConverter.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(TtsPhoneSet phoneSet,
            IPhoneConverter phoneConverter, Stream outputStream)
        {
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            phoneSet.Validate();
            if (phoneSet.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                errorSet.Add(PhoneEventCompilerError.InvalidPhoneSet);
            }
            else
            {
                BinaryWriter bw = new BinaryWriter(outputStream);
                int inUpsCount = visemeMap.Length / 2;
                Dictionary<char, byte> dicVisemeMap = new Dictionary<char, byte>(inUpsCount);
                for (int i = 0; i < inUpsCount; i++)
                {
                    dicVisemeMap.Add((char)visemeMap[i, 0], (byte)visemeMap[i, 1]);
                }

                foreach (Phone phm in phoneSet.Phones)
                {
                    bw.Write((short)phm.Id);
                    string strViseme = string.Empty;
                    string ttsPhoneID = new string(Convert.ToChar(phm.Id), 1);

                    char[] upsIds;

                    try
                    {
                        upsIds = phoneConverter.TTS2UPS(ttsPhoneID).ToCharArray();
                    }
                    catch (Exception ex)
                    {
                        upsIds = string.Empty.ToCharArray();
                        errorSet.Add(DataCompilerError.CompilingLog, 
                            string.Format("Failed to convert TTS phone to UPS, Id={0}. {1}", phm.Id, Helper.BuildExceptionMessage(ex)));
                    }

                    // check upsid's length
                    if (upsIds.Length > MAX_UPS_PHONE_PER_TTS)
                    {
                        throw new NotSupportedException("Too many UPS phones for one TTS phone.");
                    }

                    // write viseme
                    int k = 0;
                    for (int j = 0; j < upsIds.Length; j++)
                    {
                        byte visemeVal = dicVisemeMap[upsIds[j]];
                        if (visemeVal != VISEME_0)
                        {
                            // ignore those silence viseme in-between one ups phone's viseme series since 
                            // it's not natural to close mouth in one phone.
                            bw.Write(visemeVal);
                            k++;
                        }
                    }

                    // pad zero. If all viseme are silence, then save one silence viseme.
                    // otherwise, zero represent the end of viseme id series.
                    for (; k < MAX_UPS_PHONE_PER_TTS + 1; k++)
                    {
                        bw.Write((byte)0);
                    }

                    if (phoneConverter != null)
                    {
                        string sapiPhoneId = phoneConverter.TTS2SAPI(ttsPhoneID);
                        if (sapiPhoneId.Length > MAX_SAPI_PHONE_PER_TTS)
                        {
                            throw new NotSupportedException("Too many SAPI phones for one TTS phone.");
                        }

                        char[] phoneIdArray = sapiPhoneId.ToCharArray();
                        int i = 0;
                        for (i = 0; i < phoneIdArray.Length; i++)
                        {
                            bw.Write((ushort)phoneIdArray[i]);
                        }

                        for (; i < MAX_SAPI_PHONE_PER_TTS; i++)
                        {
                            bw.Write((ushort)0);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < MAX_SAPI_PHONE_PER_TTS; ++i)
                        {
                            bw.Write((ushort)0);
                        }
                    }

                    bw.Write((ushort)0);
                }
            }

            return errorSet;
        }
    }
}