//----------------------------------------------------------------------------
// <copyright file="ViterbiSearcher.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the Viterbi searcher class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Viterbi searcher
    /// Note:
    ///     1. The ILatticeProvider.GetTarget() is called in ascending order.
    ///     2. The ILatticeProvider.GetCandidate() is called in ascending order.
    ///     3. The ILatticeProvider.GetTarget() is called before ILatticeProvider.GetCandidate() for the same column.
    /// </summary>
    public class ViterbiSearcher : IUnitSelector
    {
        #region IUnitSelector members

        /// <summary>
        /// Do unit selection.
        /// </summary>
        /// <param name="searchConfig">Search configuration.</param>
        /// <returns>The best path.</returns>
        public List<PathNodeInfo> UnitSelecting(SearchConfig searchConfig)
        {
            ILatticeProvider latticeProvider = searchConfig.LatticeProvider;
            ITargetCostCalculator targetCostCalculator = searchConfig.TargetCostCalculator;
            IJoinCalculator joinCostCalculator = searchConfig.JoinCostCalculator;

            int targetCount = latticeProvider.GetColumnCount();
            if (targetCount == 0)
            {
                string message = Helper.NeutralFormat("The ILatticeProvider contains 0 targets!");
                throw new InvalidDataException(message);
            }

            List<PathNodeInfo> bestPath = new List<PathNodeInfo>(targetCount);

            TargetNode[] targets = new TargetNode[targetCount];
            LatticeNode[][] lattice = new LatticeNode[targetCount][];

            for (int i = 0; i < targetCount; i++)
            {
                int candidateCount = latticeProvider.GetRowCount(i);
                if (candidateCount == 0)
                {
                    string message = Helper.NeutralFormat("The ILatticeProvider contains 0 candidates for target {0}!", i);
                    throw new InvalidDataException(message);
                }

                targets[i] = new TargetNode(latticeProvider.GetTarget(i), candidateCount);

                lattice[i] = new LatticeNode[candidateCount];
                for (int j = 0; j < candidateCount; j++)
                {
                    PathNodeInfo pathNodeInfo = new PathNodeInfo(latticeProvider.GetCandidate(i, j), float.PositiveInfinity, float.PositiveInfinity);
                    lattice[i][j] = new LatticeNode(pathNodeInfo, float.PositiveInfinity, -1);
                }
            }

            for (int i = 0; i < targets[0].CandidateCount; i++)
            {
                LatticeNode nodeCur = lattice[0][i];

                float targetCost = targetCostCalculator.GetTargetCost(targets[0].Target, nodeCur.PathNodeInfo.Candidate);
                nodeCur.BestScore = targetCost;

                nodeCur.PathNodeInfo.TargetCost = targetCost;
            }

            for (int col = 1; col < targetCount; col++)
            {
                TargetNode targetCur = targets[col];
                TargetNode targetLeft = targets[col - 1];

                for (int i = 0; i < targetCur.CandidateCount; i++)
                {
                    LatticeNode nodeCur = lattice[col][i];

                    LatticeNode nodeLeft = lattice[col - 1][0];
                    float joinCost = joinCostCalculator.GetJoinCost(targetLeft.Target, targetCur.Target,
                        nodeLeft.PathNodeInfo.Candidate, nodeCur.PathNodeInfo.Candidate);
                    float bestPreScore = nodeLeft.BestScore + joinCost;
                    int bestPreNodeIndex = 0;
                    float bestJoinCost = joinCost;

                    for (int j = 1; j < targets[col - 1].CandidateCount; j++)
                    {
                        nodeLeft = lattice[col - 1][j];
                        joinCost = joinCostCalculator.GetJoinCost(targetLeft.Target, targetCur.Target,
                            nodeLeft.PathNodeInfo.Candidate, nodeCur.PathNodeInfo.Candidate);
                        float preCost = nodeLeft.BestScore + joinCost;
                        if (preCost < bestPreScore)
                        {
                            bestPreScore = preCost;
                            bestPreNodeIndex = j;
                            bestJoinCost = joinCost;
                        }
                    }

                    float targetCost = targetCostCalculator.GetTargetCost(targetCur.Target, nodeCur.PathNodeInfo.Candidate);
                    nodeCur.BestScore = bestPreScore + targetCost;
                    nodeCur.BestPreNodeIndex = bestPreNodeIndex;

                    nodeCur.PathNodeInfo.TargetCost = targetCost;
                    nodeCur.PathNodeInfo.JoinCost = bestJoinCost;
                }
            }

            int columnIndex = targetCount - 1;
            LatticeNode bestNode = lattice[columnIndex][0];
            float bestScore = bestNode.BestScore;
            for (int i = 1; i < targets[columnIndex].CandidateCount; i++)
            {
                LatticeNode curNode = lattice[columnIndex][i];
                if (curNode.BestScore < bestScore)
                {
                    bestNode = curNode;
                    bestScore = bestNode.BestScore;
                }
            }

            while (true)
            {
                bestPath.Add(bestNode.PathNodeInfo);
                columnIndex--;
                if (columnIndex < 0)
                {
                    break;
                }

                bestNode = lattice[columnIndex][bestNode.BestPreNodeIndex];
            }

            bestPath.Reverse();

            return bestPath;
        }

        #endregion

        #region private classes

        private class TargetNode
        {
            public object Target;
            public int CandidateCount;

            public TargetNode(object target, int candidateCount)
            {
                this.Target = target;
                this.CandidateCount = candidateCount;
            }
        }

        private class LatticeNode
        {
            public PathNodeInfo PathNodeInfo;
            public float BestScore;
            public int BestPreNodeIndex;

            public LatticeNode(PathNodeInfo pathNodeInfo, float bestScore, int bestPreNodeIndex)
            {
                this.PathNodeInfo = pathNodeInfo;
                this.BestScore = bestScore;
                this.BestPreNodeIndex = bestPreNodeIndex;
            }
        }

        #endregion
    }
}