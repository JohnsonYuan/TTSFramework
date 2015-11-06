//----------------------------------------------------------------------------
// <copyright file="SingleMachineComputation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is the class to invoke multi thread parallel compuation
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// The class of single machine method.
    /// </summary>
    public struct SingleMachineMethod
    {
        /// <summary>
        /// The delegate method without pass in object.
        /// </summary>
        public delegate void SingleMethod();

        /// <summary>
        /// The delegate method with pass in object.
        /// </summary>
        /// <param name="state">The passed in object.</param>
        public delegate void SingleMethodWithState(object state);
    }

    /// <summary>
    /// The class of MultiThreadParallelComputation.
    /// </summary>
    public class SingleMachineComputation : ParallelComputation
    {
        #region Fields
        private Queue<Pair<object, object>> _queDelegateMethod = null;

        #endregion

        #region Properties

        #endregion

        #region constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleMachineComputation"/> class.
        /// </summary>
        public SingleMachineComputation()
        {
            _queDelegateMethod = new Queue<Pair<object, object>>();
        }
        #endregion

        /// <summary>
        /// The enqueue mthod is to enqueue the delegate method with state.
        /// </summary>
        /// <param name="delegateMethod">The delegate function.</param>
        /// <param name="state">The pass in object.</param>
        public void EnqueueMethod(object delegateMethod, object state)
        {
            if (delegateMethod is Delegate)
            {
                _queDelegateMethod.Enqueue(new Pair<object, object>((SingleMachineMethod.SingleMethodWithState)delegateMethod, state));
            }
            else
            {
                throw new ArgumentException("The passed in object should be delegate method");
            }
        }

        /// <summary>
        /// The enqueue mthod is to enqueue the delegate method without state.
        /// </summary>
        /// <param name="delegateMethod">The delegate function.</param>
        public void EnqueueMethod(object delegateMethod)
        {
            if (delegateMethod is Delegate)
            {
                _queDelegateMethod.Enqueue(new Pair<object, object>((SingleMachineMethod.SingleMethod)delegateMethod, null));
            }
            else
            {
                throw new ArgumentException("The passed in object should be delegate method");
            }
        }

        /// <summary>
        /// The Initialize() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool Initialize()
        {
            return IsInitialized();
        }

        /// <summary>
        /// The BroadCast() methods to initalize the necessary configuration.
        /// </summary>
        /// <returns>Bool.</returns>
        protected override bool BroadCast()
        {
            bool isSuccess = true;
            while (_queDelegateMethod.Count != 0)
            {
                var delegateMethodAndState = _queDelegateMethod.Dequeue();
                if (delegateMethodAndState.Right == null)
                {
                    var method = (SingleMachineMethod.SingleMethod)delegateMethodAndState.Left;
                    method();
                }
                else
                {
                    var method = (SingleMachineMethod.SingleMethodWithState)delegateMethodAndState.Left;
                    var state = delegateMethodAndState.Right;
                    method(state);
                }
            }
            
            return isSuccess;
        }

        private bool IsInitialized()
        {
            if (_queDelegateMethod.Count() == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}