//----------------------------------------------------------------------------
// <copyright file="DictionaryReference.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     DictionaryReference class Map online dictionary http://dictionary.reference.com.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Dictionary.EnUS
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;

    /// <summary>
    /// DictionaryReference class. Map online dictionary http://dictionary.reference.com.
    /// </summary>
    public class DictionaryReference : DictionaryModel
    {
        /// <summary>
        /// Language flag of this dictionary .
        /// </summary>
        public static Language Language = Language.EnUS;

        #region Readonly Fields

        /// <summary>
        /// Define a delimiter array .
        /// </summary>
        private readonly string[] _nonProns = new string[] { ",", ";" };

        #endregion 

        /// <summary>
        /// If webclient object is disposed.
        /// </summary>
        private bool ifWebClientDisposed = false;

        /// <summary>
        /// Initializes a new instance of the DictionaryReference class .
        /// </summary>
        public DictionaryReference()
        {
            resourceName = "Microsoft.Tts.Offline.Dictionary.Data.en_US.DictionaryReference.xml";
            LoadResource(resourceName);
            wordDelimiter = "·";
            syllable = '-';
            primaryStress = 'ˈ';
            secondaryStress = 'ˌ';

            this.InitializeWebClient();
        }

        /// <summary>
        /// Initialize Webclient object.
        /// </summary>
        public void InitializeWebClient()
        {
            if (!this.ifWebClientDisposed)
            {
                this.Dispose(true);
            }

            this.webClient = new WebClient();
            this.webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            //// pretend to be an IE browser, by set IE request header
            this.webClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; InfoPath.2; .NET4.0C; .NET4.0E)");
            this.webClient.Encoding = Encoding.UTF8;
            this.webClient.Disposed += new EventHandler(this.IfWebClientDisposed);
            this.ifWebClientDisposed = false;
        }

        /// <summary>
        /// Lookup word pronunciation from online .
        /// </summary>
        /// <param name="word">Word parameter .</param>
        /// <returns>Look-up pronunciations .</returns>
        public override Collection<string> Lookup(string word)
        {
            try
            {
                src = webClient.DownloadString(new Uri(url + word));
                IsOOV = !IsFind();
                if (!IsOOV)
                {
                    if (src.IndexOf("class=\"pronounce\"") != -1)
                    {
                        Process();
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Message.Contains("(404)") && ex.Message.Contains("Not Found"))
                {
                    IsOOV = true;
                    return Pronunciations;
                }
                else
                {
                    this.Dispose(true);
                    this.InitializeWebClient();

                    // It's web error, will be retry.
                    throw new WebException("Failed to look up word \"" + word + "\" from " + url + ".", ex);
                }
            }
            catch (Exception ex)
            {
                this.Dispose(true);

                // it's other error, will be find in the other error list.
                throw new Exception("Error happened when looking up \"" + word + "\" from " + url, ex);
            }

            return Pronunciations;
        }

        /// <summary>
        /// Process the raw pronunciation .
        /// </summary>
        /// <param name="rawPron">Raw pron .</param>
        protected override void PostProcess(string rawPron)
        {
            rawPron = CleanPron(rawPron);

            string[] rawPronArray = rawPron.Split(_nonProns, StringSplitOptions.RemoveEmptyEntries);

            Pronunciations.Clear();

            string lastPron = string.Empty;
            foreach (string match in rawPronArray)
            {
                // Skip non pron match
                if (_nonProns.Contains(match))
                {
                    continue;
                }

                string pron = match.Trim();

                if (string.IsNullOrEmpty(pron))
                {
                    continue;
                }

                // If the first pron is a partial pronunciation like "-ˈmɒroʊ" or "ˈmɒroʊ-". We can't decide another part. Just skip it.
                if (pron[0].Equals(syllable) || pron[pron.Length - 1].Equals(syllable))
                {
                    continue;
                }

                pron = EnsureSyllable(pron);
                pron = HandlePartialPron(pron, lastPron);
                if (string.IsNullOrEmpty(pron))
                {
                    continue;
                }

                lastPron = pron;
                ArrayList phoneList = SplitPhones(pron);
                pron = string.Empty;
                foreach (string phone in phoneList)
                {
                    pron += phone + " ";
                }

                string ttsPron = ConvertToTTSPron(pron, phoneMappping);
                ttsPron = EnsureStress(ttsPron);
                if (!Pronunciations.Contains(ttsPron))
                {
                    Pronunciations.Add(ttsPron);
                }
            }

            ThrowExceptionIfPronsNull();
        }

        /// <summary>
        /// Ensure word pronunciation is syllabled .
        /// </summary>
        /// <param name="pron">Pronunciation .</param>
        /// <returns>Pron with syllable .</returns>
        private string EnsureSyllable(string pron)
        {
            string[] array = pron.Split(syllable);
            for (int i = 0; i < array.Length; i++)
            {
                if (!string.IsNullOrEmpty(array[i]))
                {
                    // This is site specific. If the stress is in pronunciation. There should be a syllabe there .
                    if (array[i].Contains(secondaryStress) && !array[i].StartsWith(secondaryStress.ToString()))
                    {
                        array[i] = array[i].Replace(secondaryStress.ToString(), syllable.ToString() + secondaryStress.ToString());
                    }

                    if (array[i].Contains(primaryStress) && !array[i].StartsWith(primaryStress.ToString()))
                    {
                        array[i] = array[i].Replace(primaryStress.ToString(), syllable.ToString() + primaryStress.ToString());
                    }
                }
            }

            string syllablizedPron = string.Empty;
            foreach (string str in array)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    syllablizedPron += str + syllable.ToString();
                }
            }

            if (!pron[pron.Length - 1].Equals(syllable))
            {
                // If original pronunciation is like "-ˈmɒroʊ", add the syllable back .
                if (pron[0].Equals(syllable))
                {
                    syllablizedPron = syllable.ToString() + syllablizedPron.Substring(0, syllablizedPron.Length - 1);
                }
                else
                {
                    syllablizedPron = syllablizedPron.Substring(0, syllablizedPron.Length - 1);
                }
            }

            return syllablizedPron;
        }

        /// <summary>
        /// Clean some useless tags .
        /// </summary>
        /// <param name="rawPron">Raw pron parameter .</param>
        /// <returns>Cleaned raw pron .</returns>
        private string CleanPron(string rawPron)
        {
            string result = string.Empty;

            // Delete Non English pronunciation tag .
            int nonEnglishIndex = rawPron.IndexOf(@"<span class=""dbox-italic"">");

            if (nonEnglishIndex >= 0)
            {
                result = rawPron.Substring(0, nonEnglishIndex);
            }
            else
            {
                result = rawPron;
            }

            // Delete HTML tag and its attribute .
            result = Regex.Replace(result, @"(<[\w\ \=\""\'\.\-\/\d\:]*/?>|<[\w\ \=\""\'\.\-\/\d\:]*>)", string.Empty);
            result = Regex.Replace(result, @"also", ";");

            // Get string between \ and \
            int lastIndex = result.LastIndexOf("\\");

            if (lastIndex >= 0)
            {
                result = result.Substring(0, lastIndex + 1);
            }

            result = HttpUtility.HtmlDecode(Regex.Replace(result, @"[\\\(\)\ ]", string.Empty));

            // delete all of chars after "for ..."
            int index = result.IndexOf("for");

            if (index >= 0)
            {
                result = result.Substring(0, index).Trim();
            }
            else
            {
                result.Trim();
            }

            return result;
        }

        /// <summary>
        /// A event function, will be called when webclient's dispose() is called.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="eventArgs">Event Args.</param>
        private void IfWebClientDisposed(object sender, EventArgs eventArgs)
        {
            this.ifWebClientDisposed = true;
        }
    }
}