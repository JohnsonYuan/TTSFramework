//----------------------------------------------------------------------------
// <copyright file="MerriamWebster.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     MerriamWebster class Map online dictionary http://www.merriam-webster.com.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Dictionary.EnUS
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;

    /// <summary>
    /// MerriamWebster class. Map online dictionary http://www.merriam-webster.com.
    /// </summary>
    public class MerriamWebster : DictionaryModel
    {
        /// <summary>
        /// Language flag of this dictionary .
        /// </summary>
        public static Language Language = Language.EnUS;

        /// <summary>
        /// Define a delimiter array.
        /// </summary>
        private static char[] delimiter = new char[] { ',', ';', '÷' };

        /// <summary>
        /// If webclient object is disposed.
        /// </summary>
        private bool ifWebClientDisposed = false;

        /// <summary>
        /// Initializes a new instance of the MerriamWebster class .
        /// </summary>
        public MerriamWebster()
        {
            resourceName = "Microsoft.Tts.Offline.Dictionary.Data.en_US.MerriamWebster.xml";
            LoadResource(resourceName);
            wordDelimiter = "&#183;";
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
                    if (src.IndexOf("class=\"pr\"") != -1)
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
        /// <param name="rawPron">Raw pron parameter .</param>
        protected override void PostProcess(string rawPron)
        {
            rawPron = CleanPron(rawPron);
            string[] prons = rawPron.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            string lastPron = string.Empty;
            for (int i = 0; i < prons.Length; i++)
            {
                string pron = prons[i].Trim();
                if (!string.IsNullOrEmpty(pron))
                {
                    if (pron[0].Equals(syllable) || pron[pron.Length - 1].Equals(syllable))
                    {
                        continue;
                    }

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
            }

            ThrowExceptionIfPronsNull();
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

            if (lastIndex > 0)
            {
                result = result.Substring(0, lastIndex + 1);
            }

            result = HttpUtility.HtmlDecode(Regex.Replace(result, @"[\\\(\)\ ]", string.Empty));

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