//----------------------------------------------------------------------------
// <copyright file="Kmeans.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines k-means clustering.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Calculates sample distance and center.
    /// Each derived sample class should have its own strategy class.
    /// </summary>
    public interface ISampleStrategy
    {
        /// <summary>
        /// Calculates the distance between two samples.
        /// </summary>
        /// <param name="left">Left sample.</param>
        /// <param name="right">Righ sample.</param>
        /// <returns>The distance.</returns>
        float Diff(Sample left, Sample right);

        /// <summary>
        /// Calculates the center of a sample collection.
        /// </summary>
        /// <param name="samples">Sample collection.</param>
        /// <returns>Center of the samples.</returns>
        Sample GetCenter(IList<Sample> samples);
    }

    /// <summary>
    /// Base sample class.
    /// </summary>
    [Serializable]
    public class Sample
    {
        /// <summary>
        /// Gets or sets centroid this sample associated with.
        /// </summary>
        public Sample Centroid { get; set; }
    }

    /// <summary>
    /// Implements k-means clustering algorithm.
    /// </summary>
    [Serializable]
    public class Kmeans
    {
        private const int SamplePerThread = 1000;
        private const double IterStopRatio = 0.00005;

        private ISampleStrategy _sampleStrategy = null;
        private IList<Sample> _centroids = null;
        private IList<Sample> _representativeCentroids = null;
        private Dictionary<Sample, List<Sample>> _representativeCentroidsMap = null;

        private float _totalDistance = float.MaxValue;
        
        [NonSerialized]
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the Kmeans class.
        /// </summary>
        /// <param name="sampleStrategy">Sample strategy object.</param>
        /// <param name="clusterCount">Target cluster count.</param>
        public Kmeans(ISampleStrategy sampleStrategy, int clusterCount)
        {
            // Sets default values
            MaxIteration = 30;

            _sampleStrategy = sampleStrategy;
            ClusterCount = clusterCount;
        }

        /// <summary>
        /// Gets the cluster result centroids.
        /// </summary>
        public IList<Sample> Centroids
        {
            get
            {
                return _centroids;
            }
        }

        /// <summary>
        /// Gets or sets the maximum iteration number to stop.
        /// </summary>
        public int MaxIteration { get; set; }

        /// <summary>
        /// Gets the target cluster number.
        /// </summary>
        public int ClusterCount { get; private set; }

        /// <summary>
        /// Gets or sets the logger object.
        /// </summary>
        public ILogger Logger 
        { 
            get
            {
                return _logger;
            }

            set 
            {
                _logger = value;
            }
        }

        /// <summary>
        /// Cluster the samples into groups.
        /// </summary>
        /// <param name="samples">Samples to be clustered.</param>
        public void Cluster(IList<Sample> samples)
        {
            _centroids = Initialize(samples);
            _representativeCentroids = null;

            int iter = 1;

            while (true)
            {
                float lastDistance = _totalDistance;

                _totalDistance = Assign(samples);

                Log("Iter {0}, total distance {1}", iter, _totalDistance);

                ProcessEmptyCluster(samples);

                if (_totalDistance == 0 || (lastDistance - _totalDistance < lastDistance * IterStopRatio))
                {
                    Log("Converge with total distance {0}", _totalDistance);
                    break;
                }

                if (++iter > MaxIteration)
                {
                    Log("Max iteration reached {0}", MaxIteration);
                    break;
                }

                Update(samples);
            }
        }

        /// <summary>
        /// Build centroid cluster map.
        /// </summary>
        public void BuildMapForCentroids()
        {
            Kmeans centroidsKeams = new Kmeans(_sampleStrategy, ClusterCount / 100);
            centroidsKeams.Cluster(_centroids);
            _representativeCentroids = centroidsKeams.Centroids;
            _representativeCentroidsMap = new Dictionary<Sample, List<Sample>>();
            _representativeCentroids.ForEach(s => _representativeCentroidsMap.Add(s, new List<Sample>()));

            foreach (Sample centroid in _centroids)
            {
                Sample backup = centroid.Centroid;
                Assign(centroid, _representativeCentroids);
                _representativeCentroidsMap[centroid.Centroid].Add(centroid);
                centroid.Centroid = backup;
            }
        }

        /// <summary>
        /// Re-assign each sample to its nearest cluster center.
        /// </summary>
        /// <param name="samples">Samples to be clustered.</param>
        /// <returns>The total inner cluster distance.</returns>
        public float Assign(IList<Sample> samples)
        {
            float totalDistance = 0;

            foreach (var sampleChunk in samples.Split(SamplePerThread))
            {
                WaitCallback proc = delegate(object chunk)
                {
                    float chunkDistance = 0;

                    foreach (var sample in (Sample[])chunk)
                    {
                        // if cluster centroids is null, go by whole centroids
                        if (_representativeCentroids == null)
                        {
                            chunkDistance += Assign(sample, Centroids);
                        }
                        else
                        {
                            // look for near centroids by clustering, then assign
                            var nearCentroids = SearchCentroids(sample);
                            chunkDistance += Assign(sample, nearCentroids);
                        }
                    }

                    lock (samples)
                    {
                        totalDistance += chunkDistance;
                    }
                };

                ManagedThreadPool.QueueUserWorkItem(proc, sampleChunk);
            }

            ManagedThreadPool.WaitForDone();

            return totalDistance;
        }

        /// <summary>
        /// Initializes the centers.
        /// </summary>
        /// <param name="samples">All samples.</param>
        /// <returns>The centers to start cluster with.</returns>
        protected virtual IList<Sample> Initialize(IList<Sample> samples)
        {
            var interval = samples.Count / ClusterCount;

            return Enumerable.Range(0, ClusterCount).Select(i => samples[i * interval]).ToList();
        }

        /// <summary>
        /// Look for the similar centroids by representative centroids.
        /// </summary>
        /// <param name="sample">Sample.</param>
        /// <returns>Similar centroids.</returns>
        private IList<Sample> SearchCentroids(Sample sample)
        {
            List<Sample> nearCentroids = new List<Sample>();
            foreach (Sample clusterCentroid in _representativeCentroids.OrderBy(s => _sampleStrategy.Diff(s, sample)))
            {
                nearCentroids.AddRange(_representativeCentroidsMap[clusterCentroid]);
                if (nearCentroids.Count >= _centroids.Count * 0.1)
                {
                    break;
                }
            }

            return nearCentroids;
        }

        /// <summary>
        /// Re-assign sample to its nearest cluster center.
        /// </summary>
        /// <param name="sample">Sample to be clustered.</param>
        /// <param name="centroids">Centroids to be assigned.</param>
        /// <returns>The inner cluster distance.</returns>
        private float Assign(Sample sample, IList<Sample> centroids)
        {
            var smallestDistance = float.MaxValue;
            var lastCentroid = sample.Centroid;

            foreach (var centroid in centroids)
            {
                var distance = _sampleStrategy.Diff(sample, centroid);

                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    sample.Centroid = centroid;
                }
            }

            return smallestDistance;
        }

        /// <summary>
        /// Update the center of each cluster.
        /// </summary>
        /// <param name="samples">Samples to be clustered.</param>
        private void Update(IList<Sample> samples)
        {
            for (int i = 0; i < _centroids.Count; i++)
            {
                _centroids[i] = _sampleStrategy.GetCenter(
                    samples.Where(s => s.Centroid == _centroids[i]).ToArray());
            }
        }

        /// <summary>
        /// Process empty clusters according to empty action.
        /// </summary>
        /// <param name="samples">Samples after reassignment.</param>
        private void ProcessEmptyCluster(IList<Sample> samples)
        {
            var nonEmptyCentroids = samples.Select(s => s.Centroid).Distinct().ToList();
            var emptyCentroidCount = _centroids.Count - nonEmptyCentroids.Count;

            if (emptyCentroidCount > 0)
            {
                Log("{0} empty centroids found", emptyCentroidCount);
            }

            _centroids = nonEmptyCentroids;
        }

        /// <summary>
        /// Find the furthest sample to reference.
        /// </summary>
        /// <param name="reference">Reference sample.</param>
        /// <param name="samples">Sample list.</param>
        /// <returns>The furthest sample to reference.</returns>
        private Sample FindFurthest(Sample reference, IList<Sample> samples)
        {
            float largestDistance = 0;
            Sample ret = null;

            foreach (var sample in samples)
            {
                var distance = _sampleStrategy.Diff(sample, reference);

                if (distance > largestDistance)
                {
                    largestDistance = distance;
                    ret = sample;
                }
            }

            return ret;
        }

        /// <summary>
        /// Log messages.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Argument list.</param>
        private void Log(string format, params object[] args)
        {
            Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));

            if (Logger != null)
            {
                Logger.LogLine(format, args);
            }
        }
    }
}