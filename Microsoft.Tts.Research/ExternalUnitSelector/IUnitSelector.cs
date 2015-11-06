//----------------------------------------------------------------------------
// <copyright file="IUnitSelector.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements IUnitSelector interface
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for target cost calculator.
    /// </summary>
    public interface ITargetCostCalculator
    {
        /// <summary>
        /// Get the target cost of a candidate against a target.
        /// </summary>
        /// <param name="target">Target specification.</param>
        /// <param name="candidate">Candidate specification.</param>
        /// <returns>Target cost.</returns>
        float GetTargetCost(object target, object candidate);
    }

    /// <summary>
    /// Interface for join cost calculator.
    /// </summary>
    public interface IJoinCalculator
    {
        /// <summary>
        /// Get the join cost of two candidates (belong to 2 adjacent target).
        /// </summary>
        /// <param name="targetLeft">Left target specification.</param>
        /// <param name="targetRight">Right target specification.</param>
        /// <param name="candidateLeft">Left candidate specification.</param>
        /// <param name="candidateRight">Right candidate specification.</param>
        /// <returns>Join cost.</returns>
        float GetJoinCost(object targetLeft, object targetRight,
            object candidateLeft, object candidateRight);
    }

    /// <summary>
    /// Interface for lattice provider.
    /// </summary>
    public interface ILatticeProvider
    {
        /// <summary>
        /// Get the column count of the lattice.
        /// </summary>
        /// <returns>Column count.</returns>
        int GetColumnCount();

        /// <summary>
        /// Get the row count of a specific column in the lattice.
        /// </summary>
        /// <param name="column">The belonging column.</param>
        /// <returns>Row count of the column.</returns>
        int GetRowCount(int column);

        /// <summary>
        /// Get the target specification.
        /// </summary>
        /// <param name="column">The belonging column.</param>
        /// <returns>Target specification.</returns>
        object GetTarget(int column);

        /// <summary>
        /// Get the candidate specification.
        /// </summary>
        /// <param name="column">The belonging column.</param>
        /// <param name="row">Row cell of the candidate.</param>
        /// <returns>Candidate specification.</returns>
        object GetCandidate(int column, int row);
    }

    /// <summary>
    /// Interface for unit selector.
    /// </summary>
    public interface IUnitSelector
    {
        /// <summary>
        /// Do unit selection.
        /// </summary>
        /// <param name="searchConfig">Search configuration.</param>
        /// <returns>The best path.</returns>
        List<PathNodeInfo> UnitSelecting(SearchConfig searchConfig);
    }

    /// <summary>
    /// Path node in the lattice.
    /// </summary>
    public class PathNodeInfo
    {
        /// <summary>
        /// Candidate specification.
        /// </summary>
        public object Candidate;

        /// <summary>
        /// Target cost of the candidate.
        /// </summary>
        public float TargetCost;

        /// <summary>
        /// Join cost between the candidate and the left candidate in the partial best path.
        /// </summary>
        public float JoinCost;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathNodeInfo"/> class.
        /// </summary>
        /// <param name="candidate">Candidate specification.</param>
        /// <param name="targetCost">Target cost.</param>
        /// <param name="joinCost">Join cost.</param>
        public PathNodeInfo(object candidate, float targetCost, float joinCost)
        {
            this.Candidate = candidate;
            this.TargetCost = targetCost;
            this.JoinCost = joinCost;
        }

        /// <summary>
        /// Override Equals().
        /// Whether is this obejct equal to another.
        /// </summary>
        /// <param name="obj">The object to be compared.</param>
        /// <returns>True if equal, false if not.</returns>
        public override bool Equals(object obj)
        {
            PathNodeInfo rhs = obj as PathNodeInfo;

            if (TargetCost != rhs.TargetCost)
            {
                return false;
            }
            else if (JoinCost != rhs.JoinCost)
            {
                return false;
            }
            else if (!Candidate.Equals(rhs.Candidate))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Override GetHashCode().
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return TargetCost.GetHashCode() ^ JoinCost.GetHashCode() ^ Candidate.GetHashCode();
        }
    }

    /// <summary>
    /// Search configuration.
    /// </summary>
    public class SearchConfig
    {
        /// <summary>
        /// Lattice provider.
        /// </summary>
        public ILatticeProvider LatticeProvider;

        /// <summary>
        /// Target cost calculator.
        /// </summary>
        public ITargetCostCalculator TargetCostCalculator;

        /// <summary>
        /// Join cost calculator.
        /// </summary>
        public IJoinCalculator JoinCostCalculator;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchConfig"/> class.
        /// </summary>
        /// <param name="latticeProvider">Lattice provider.</param>
        /// <param name="targetCostCalculator">Target cost calculator.</param>
        /// <param name="joinCostCalculator">Join cost calculator.</param>
        public SearchConfig(ILatticeProvider latticeProvider, ITargetCostCalculator targetCostCalculator, IJoinCalculator joinCostCalculator)
        {
            this.LatticeProvider = latticeProvider;
            this.TargetCostCalculator = targetCostCalculator;
            this.JoinCostCalculator = joinCostCalculator;
        }
    }
}