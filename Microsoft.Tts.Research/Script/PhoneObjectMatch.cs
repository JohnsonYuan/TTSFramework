//----------------------------------------------------------------------------
// <copyright file="PhoneObjectMatch.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements PhoneObjectMatch, get the matched phone 
// in engine to the certain script phone.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// PhoneObjectMatch class.
    /// </summary>
    public class PhoneObjectMatch
    {
        private Collection<ScriptPhone> _scriptPronouncedPhones = new Collection<ScriptPhone>();
        private Collection<TtsPhone> _enginePronouncedPhones = new Collection<TtsPhone>();
        private TtsPhone _engineFirstPhone;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PhoneObjectMatch class.
        /// </summary>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="utt">Tts utterance.</param>
        public PhoneObjectMatch(ScriptSentence scriptSentence, SP.TtsUtterance utt)
        {
            EngineMatchToScript.CheckPronouncedWordsMatched(scriptSentence, utt);
            BuildPronouncedPhones(scriptSentence, utt);
            if (utt.Phones.Count > 0)
            {
                _engineFirstPhone = utt.Phones[0];
            }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Get matched engine phone.
        /// </summary>
        /// <param name="scriptPhone">Script phone.</param>
        /// <returns>The matched engine phone.</returns>
        public TtsPhone ToTtsPhone(ScriptPhone scriptPhone)
        {
            TtsPhone targetPhone = null;
            if (!Phoneme.IsSilenceFeature(scriptPhone.Name))
            {
                int phoneIndex = _scriptPronouncedPhones.IndexOf(scriptPhone);
                targetPhone = _enginePronouncedPhones[phoneIndex];
            }
            else
            {
                int phoneIndex = scriptPhone.Syllable.Word.Sentence.ScriptPhones.IndexOf(scriptPhone);
                if (phoneIndex == 0)
                {
                    if (_engineFirstPhone != null && _engineFirstPhone.IsSilence)
                    {
                        targetPhone = _engineFirstPhone;
                    }
                }
                else
                {
                    phoneIndex = _scriptPronouncedPhones.IndexOf(
                        scriptPhone.Syllable.Word.Sentence.ScriptPhones[phoneIndex - 1]);
                    TtsPhone tempPhone = _enginePronouncedPhones[phoneIndex].Next;
                    if (tempPhone.IsSilence)
                    {
                        targetPhone = tempPhone;
                    }
                }
            }

            return targetPhone;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Build script and engine's pronounced phones.
        /// </summary>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="utt">Tts utterance.</param>
        private void BuildPronouncedPhones(ScriptSentence scriptSentence, SP.TtsUtterance utt)
        {
            foreach (ScriptPhone scriptPhone in scriptSentence.ScriptPhones)
            {
                if (!Phoneme.IsSilenceFeature(scriptPhone.Name))
                {
                    _scriptPronouncedPhones.Add(scriptPhone);
                }
            }

            foreach (TtsPhone uttPhone in utt.Phones)
            {
                if (!uttPhone.IsSilence)
                {
                    _enginePronouncedPhones.Add(uttPhone);
                }
            }

            if (_scriptPronouncedPhones.Count != _enginePronouncedPhones.Count)
            {
                throw new InvalidDataException("Runtime's pronounced phones' count has no " +
                    "consistence with script's.");
            }
        }

        #endregion
    }
}