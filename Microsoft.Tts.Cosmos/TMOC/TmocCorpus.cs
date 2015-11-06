namespace Microsoft.Tts.Cosmos.TMOC
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using ScopeRuntime;

    /// <summary>
    /// The statistic of corpus.
    /// The defintion of a typical tts corpus is the data setused for training.
    /// This class is used to decide the number of token used in VMOC and it inlcudes the data size, number of utterance and speech duration.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class CorpusStatistic
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CorpusStatistic" /> class. The default constructor.
        /// </summary>
        public CorpusStatistic()
        {
            CorpusName = string.Empty;
            UtteranceNum = 0;
            SpeechDuration = 0;
            WordNum = 0;
            SpkNum = 0;
            MaleSpkNum = 0;
            FemaleSpkNum = 0;
            DataSize = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorpusStatistic" /> class. The constructor.
        /// </summary>
        /// <param name="line">The line.</param>
        public CorpusStatistic(string line)
        {
            var f = line.Split(new char[] { ' ', '\t' });
            if (f.Length != 8)
            {
                throw new FormatException("wrong format of statistis");
            }

            CorpusName = f[0];
            UtteranceNum = Convert.ToInt32(f[1]);
            SpeechDuration = Convert.ToDouble(f[2]);
            WordNum = Convert.ToInt32(f[3]);
            SpkNum = Convert.ToInt32(f[4]);
            MaleSpkNum = Convert.ToInt32(f[5]);
            FemaleSpkNum = Convert.ToInt32(f[6]);
            DataSize = Convert.ToInt64(f[7]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorpusStatistic" /> class. The constructor.
        /// </summary>
        /// <param name="row">The row.</param>
        public CorpusStatistic(Row row)
        {
            CorpusName = row[0].String;
            UtteranceNum = row[1].Integer;
            SpeechDuration = row[2].Double;
            WordNum = row[3].Integer;
            SpkNum = row[4].Integer;
            MaleSpkNum = row[5].Integer;
            FemaleSpkNum = row[6].Integer;
            DataSize = row[7].Long;
        }

        /// <summary>
        /// Gets or sets the name of corpus.
        /// </summary>
        public string CorpusName { get; set; }

        /// <summary>
        /// Gets or sets number of utterance.
        /// </summary>
        public int UtteranceNum { get; set; }

        /// <summary>
        /// Gets or sets data size.
        /// </summary>
        public long DataSize { get; set; }

        /// <summary>
        /// Gets or sets duration in seconds.
        /// </summary>
        public double SpeechDuration { get; set; }

        /// <summary>
        /// Gets or sets number of words in corpus.
        /// </summary>
        public int WordNum { get; set; }

        /// <summary>
        /// Gets or sets number of speaker in corpus.
        /// </summary>
        public int SpkNum { get; set; }

        /// <summary>
        /// Gets or sets number of male speakr in corpus.
        /// </summary>
        public int MaleSpkNum { get; set; }

        /// <summary>
        /// Gets or sets number of female speakr in corpus.
        /// </summary>
        public int FemaleSpkNum { get; set; }

        /// <summary>
        /// Gets or sets add up another statistics to this.
        /// </summary>
        /// <param name="other">Other.</param>
        public void Accumulate(CorpusStatistic other)
        {
            CorpusName = other.CorpusName;
            UtteranceNum += other.UtteranceNum;
            SpeechDuration += other.SpeechDuration;
            WordNum += other.WordNum;
            DataSize += other.DataSize;

            // This can not be accumulate correctly.
            SpkNum = 0;
            MaleSpkNum = 0;
            FemaleSpkNum = 0;
        }

        /// <summary>
        /// To row method.
        /// </summary>
        /// <param name="dstrow">The dataset row.</param>
        public void ToRow(Row dstrow)
        {
            dstrow[0].Set(CorpusName);
            dstrow[1].Set(UtteranceNum);
            dstrow[2].Set(SpeechDuration);
            dstrow[3].Set(WordNum);
            dstrow[4].Set(SpkNum);
            dstrow[5].Set(MaleSpkNum);
            dstrow[6].Set(FemaleSpkNum);
            dstrow[7].Set(DataSize);
        }
    }

    /// <summary>
    /// This infomation are pared from input xml file.
    /// All the name are abosolute path containing the VC path.
    /// </summary>
    public class TmocCorpus
    {
        /// <summary>
        /// Corpus Name.
        /// </summary>
        public string CorpusName;

        /// <summary>
        /// Whether this is a subCorpus.
        /// </summary>
        public int SubCorpusID;

        /// <summary>
        /// Corpus weight.
        /// </summary>
        public double Weight;

        /// <summary>
        /// Fbl stream.
        /// </summary>
        public string FblStream;

        /// <summary>
        /// The hypfile in plain text format.
        /// Used for corpus I/O.
        /// </summary>
        public string HypFile;

        /// <summary>
        /// The hypfile in SStream format.
        /// Using within training.
        /// </summary>
        public string HypFileSS;

        /// <summary>
        /// The corpus stream created from fblstream and hypFile.
        /// </summary>
        public string CorpusStream;

        /// <summary>
        /// The sstream to reuse alignment.
        /// </summary>
        public string AlignmentStream;

        /// <summary>
        /// The sstream to reuse lattice.
        /// </summary>
        public string LatticeStream;

        /// <summary>
        /// The corpus statistics.
        /// </summary>
        [CLSCompliant(false)]
        public CorpusStatistic Stats;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmocCorpus" /> class. The default constructor.
        /// </summary>
        public TmocCorpus()
        {
            SubCorpusID = -1;
            CorpusName = string.Empty;
            CorpusStream = string.Empty;
        }

        /// <summary>
        /// Gets the normalized corpus name.
        /// </summary>
        public string CorpusNameNormalize
        {
            get { return Regex.Replace(CorpusName, @"[^a-zA-z0-9]", "_"); }
        }

        /// <summary>
        /// Sum up the duration of all the corpus.
        /// </summary>
        /// <param name="corpusarr">Corpus array.</param>
        /// <returns>Duration.</returns>
        public static double CalSpeechDuration(TmocCorpus[] corpusarr)
        {
            double duration = 0;
            foreach (var c in corpusarr)
            {
                duration += c.Stats.SpeechDuration;
            }

            return duration;
        }

        /// <summary>
        /// Sum up the duration of all the corpus.
        /// </summary>
        /// <param name="corpusarr">Corpus array.</param>
        /// <returns>Data size.</returns>
        public static long CalDataSize(TmocCorpus[] corpusarr)
        {
            long datasize = 0;
            foreach (var c in corpusarr)
            {
                datasize += c.Stats.DataSize;
            }

            return datasize;
        }

        /// <summary>
        /// Sum up the utterance number of all the corpus.
        /// </summary>
        /// <param name="corpusarr">Corpus array.</param>
        /// <returns>The number of total utt.</returns>
        public static int CalUtteranceNum(TmocCorpus[] corpusarr)
        {
            int totalUttNum = 0;
            foreach (var c in corpusarr)
            {
                totalUttNum += c.Stats.UtteranceNum;
            }

            return totalUttNum;
        }

        /// <summary>
        /// Sum up the cluster number of all the corpus.
        /// </summary>
        /// <param name="corpusarr">Corpus array.</param>
        /// <returns>The number of total cluster.</returns>
        public static int CalTotalFblStreamClusterNum(TmocCorpus[] corpusarr)
        {
            int totalClusterNum = 0;
            foreach (var c in corpusarr)
            {
                totalClusterNum += TmocCorpus.GetFblStreamClusterNum(c.Stats.UtteranceNum);
            }

            return totalClusterNum;
        }

        /// <summary>
        /// Sum up the fbl stream size of all the corpus.
        /// </summary>
        /// <param name="corpusarr">Corpus array.</param>
        /// <returns>Total stream size.</returns>
        public static long CalTotalFblStreamSize(TmocCorpus[] corpusarr)
        {
            long totalStreamSize = 0;
            foreach (var c in corpusarr)
            {
                totalStreamSize += TmocFile.FileSizeWithRetry(c.FblStream);
            }

            return totalStreamSize;
        }

        /// <summary>
        /// Sum up the corpus stream size of all the corpus.
        /// </summary>
        /// <param name="corpusarr">Corpus array.</param>
        /// <returns>Total stream size.</returns>
        public static long CalTotalCorpusStreamSize(TmocCorpus[] corpusarr)
        {
            long totalStreamSize = 0;
            foreach (var c in corpusarr)
            {
                totalStreamSize += TmocFile.FileSizeWithRetry(c.CorpusStream);
            }

            return totalStreamSize;
        }

        /// <summary>
        /// Get the cluster number of the structurred stream by utterance number. defaultly, 2000 utterances make a cluster.
        /// </summary>
        /// <param name="utterNum">Utter numbers.</param>
        /// <returns>The cluster number.</returns>
        public static int GetFblStreamClusterNum(int utterNum)
        {
            return (utterNum / 2000) + 1;
        }

        /// <summary>
        /// Get the name of subcorpus when split a big corpus into smaller ones.
        /// </summary>
        /// <param name="corpusName">The corpus name.</param>
        /// <param name="subCorpusID">The sub corpus ID.</param>
        /// <returns>The new corpus name.</returns>
        public static string SubCorpusName(string corpusName, int subCorpusID)
        {
            if (subCorpusID >= 0)
            {
                return string.Format("{0}_partion{1:D6}", corpusName, subCorpusID);
            }
            else
            {
                return corpusName;
            }
        }

        /// <summary>
        /// Check the existence of the files.
        /// </summary>
        public void CheckInput()
        {
            if (!TmocFile.Exists(FblStream))
            {
                throw new Exception(string.Format(" the file stream {0} does not exist", FblStream));
            }
        }

        /// <summary>
        /// Shallow clone method.
        /// </summary>
        /// <returns>Tmoc corpus.</returns>
        public TmocCorpus ShallowClone()
        {
            return (TmocCorpus)this.MemberwiseClone();
        }
    }
}
