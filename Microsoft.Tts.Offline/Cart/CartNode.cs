//----------------------------------------------------------------------------
// <copyright file="CartNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements CartNode
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Cart
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    #region Logical expression data structures

    /// <summary>
    /// Logical operation type.
    /// </summary>
    internal enum Logic
    {
        /// <summary>
        /// End of the logic.
        /// </summary>
        End = 0,

        /// <summary>
        /// Value.
        /// </summary>
        Value = 1,

        /// <summary>
        /// Bracket.
        /// </summary>
        Bracket = 2,

        /// <summary>
        /// Or operator.
        /// </summary>
        Or = 3,

        /// <summary>
        /// And operator.
        /// </summary>
        And = 4,

        /// <summary>
        /// Not operator.
        /// </summary>
        Not = 5
    }

    /// <summary>
    /// Operator data structure for serialization.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OperatorSerial
    {
        #region Public fields
        /// <summary>
        /// Operation code.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Ignore.")]      
        public short Code;    
        
        /// <summary>
        /// Bit0 for l_op, bit1 for r_op.
        /// </summary>  
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Ignore.")]       
        public short Flag;     // bit0 for l_op, bit1 for r_op

        /// <summary>
        /// Left operand.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Ignore.")]
        public int LeftOp;   // left operand

        /// <summary>
        /// Right operand.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Ignore.")]
        public int RightOp;  // right operand
        #endregion

        #region Const fields
        internal const short OP_LEFT_FEATURE = 0x01;   // bit0=1 : left operand is feature, 
        // bit0=0 : position of operator
        internal const short OP_RIGHT_FEATURE = 0x02;  // bit1=1 : right operand is feature
        // bit1=0 : position of operator
        #endregion

        #region Static operatons

        /// <summary>
        /// Convert expresses as logical string, for example:
        /// <![CDATA[ 10|~20&30 ]]>.
        /// </summary>
        /// <param name="startPosition">First logical operator to take.</param>
        /// <param name="express">Express list.</param>
        /// <returns>Logic string.</returns>
        public static string ToString(int startPosition, OperatorSerial[] express)
        {
            if (express == null)
            {
                throw new ArgumentNullException("express");
            }

            if (express.Length == 0 || startPosition >= express.Length)
            {
                return string.Empty;
            }

            OperatorSerial opr = express[startPosition];

            if (opr.Code == (int)Logic.End)
            {
                return string.Empty;
            }

            if (opr.Code == (int)Logic.Value)
            {
                return opr.LeftOp.ToString(CultureInfo.InvariantCulture);
            }

            string leftOp;
            if ((opr.Flag & OP_LEFT_FEATURE) != 0)
            {
                leftOp = opr.LeftOp.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                leftOp = ToString(opr.LeftOp, express);
            }

            if (opr.Code == (int)Logic.Not)
            {
                uint temp = (uint)(opr.Flag & OP_LEFT_FEATURE);
                if (temp > 0 || (temp == 0 && express[opr.LeftOp].Code == (int)Logic.Not))
                {
                    return "~" + leftOp;
                }
                else
                {
                    return "~(" + leftOp + ")";
                }
            }

            string rightOp;
            if ((opr.Flag & OP_RIGHT_FEATURE) != 0)
            {
                rightOp = opr.RightOp.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                rightOp = ToString(opr.RightOp, express);
            }

            if (opr.Code == (int)Logic.And)
            {
                uint temp = (uint)(opr.Flag & OP_LEFT_FEATURE);
                if (temp == 0 && express[opr.LeftOp].Code == (int)Logic.Or)
                {
                    leftOp = "(" + leftOp + ")";
                }

                temp = (uint)(opr.Flag & OP_RIGHT_FEATURE);
                if (temp == 0 && (express[opr.RightOp].Code == (int)Logic.Or
                    || express[opr.RightOp].Code == (int)Logic.And))
                {
                    rightOp = "(" + rightOp + ")";
                }

                return leftOp + "&" + rightOp;
            }
            else
            {
                uint temp = (uint)(opr.Flag & OP_RIGHT_FEATURE);
                if (temp == 0 && express[opr.RightOp].Code == (int)Logic.Or)
                {
                    rightOp = "(" + rightOp + ")";
                }

                return leftOp + "|" + rightOp;
            }
        }

        /// <summary>
        /// Parse logical string to operators.
        /// </summary>
        /// <param name="logic">Logic string.</param>
        /// <returns>Operators.</returns>
        public static OperatorSerial[] Parse(string logic)
        {
            if (string.IsNullOrEmpty(logic))
            {
                throw new ArgumentNullException("logic");
            }

            List<OperatorSerial> express = new List<OperatorSerial>();
            Stack<OperatorSerial> operators = new Stack<OperatorSerial>();
            Stack<OperatorSerial> values = new Stack<OperatorSerial>();

            OperatorSerial op;
            int index = 0;
            while (logic.Length > index)
            {
                op = new OperatorSerial();
                while (logic[index] == ' ')
                {
                    index++;
                }

                if (logic[index] == '~')
                {
                    op.Code = (int)Logic.Not;
                    operators.Push(op);
                }
                else if (logic[index] == '|')
                {
                    DoHighPriorOperation(operators, values, (int)Logic.Or, express);
                    op.Code = (int)Logic.Or;
                    operators.Push(op);
                }
                else if (logic[index] == '&')
                {
                    DoHighPriorOperation(operators, values, (int)Logic.And, express);
                    op.Code = (int)Logic.And;
                    operators.Push(op);
                }
                else if (logic[index] == '(')
                {
                    op.Code = (int)Logic.Bracket;
                    operators.Push(op);
                }
                else if (logic[index] == ')')
                {
                    DoBracketOperation(operators, values, express);
                }
                else if (logic[index] >= '0' && logic[index] <= '9')
                {
                    op.Code = (int)Logic.Value;
                    op.Flag = (short)OP_LEFT_FEATURE;
                    op.LeftOp = ParseInt(logic, ref index);
                    values.Push(op);

                    DoHighPriorOperation(operators, values, (int)Logic.Not, express);

                    index--;
                }
                else
                {
                    Debug.Assert(false);
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid logic express [{0}] found.", logic[index]);
                    throw new InvalidDataException(message);
                }

                index++;
            }

            while (operators.Count > 0)
            {
                Operate(operators, values, express);
            }

            op = new OperatorSerial();

            if (values.Count > 0 && values.Peek().Flag == OP_LEFT_FEATURE)
            {
                op = values.Peek();
                express.Add(op);
            }

            return express.ToArray();
        }

        /// <summary>
        /// Read a OperatorSerial block from binary stream.
        /// </summary>
        /// <param name="br">Binary reader to read Operator.</param>
        /// <returns>Operator.</returns>
        public static OperatorSerial Read(BinaryReader br)
        {
            if (br == null)
            {
                throw new ArgumentNullException("br");
            }

            int size = Marshal.SizeOf(typeof(OperatorSerial));
            byte[] buff = br.ReadBytes(size);

            if (buff.Length != size)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found, for there is no enough data for Operator.");
                throw new InvalidDataException(message);
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(buff, 0, ptr, size);
                return (OperatorSerial)Marshal.PtrToStructure(ptr, typeof(OperatorSerial));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        #endregion

        #region Override equal operations

        /// <summary>
        /// Operator ==.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool operator ==(OperatorSerial left, OperatorSerial right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Operator !=.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if not equal, otherwise false.</returns>
        public static bool operator !=(OperatorSerial left, OperatorSerial right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Get hash code for this object.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            // use default implementation
            return base.GetHashCode();
        }

        /// <summary>
        /// Equal.
        /// </summary>
        /// <param name="obj">Other object to compare with.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is OperatorSerial))
            {
                return false;
            }

            return Equals((OperatorSerial)obj);
        }

        /// <summary>
        /// Equal.
        /// </summary>
        /// <param name="other">Other object to compare with.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equals(OperatorSerial other)
        {
            return Code == other.Code
                    && Flag == other.Flag
                    && LeftOp == other.LeftOp
                    && RightOp == other.RightOp;
        }

        #endregion

        #region Binary serialization operations
        /// <summary>
        /// Serialize this instance into binary data.
        /// </summary>
        /// <returns>Byte array.</returns>
        public byte[] ToBytes()
        {
            byte[] buff = new byte[Marshal.SizeOf(typeof(OperatorSerial))];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(buff.Length);
                Marshal.StructureToPtr(this, ptr, false);
                Marshal.Copy(ptr, buff, 0, buff.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return buff;
        }

        #endregion

        #region Private static operations

        /// <summary>
        /// Parse a int from the string, update the index to 
        /// The end of the string.
        /// </summary>
        /// <param name="str">String to parse.</param>
        /// <param name="index">Offset in the string.</param>
        /// <returns>Int parsed.</returns>
        private static int ParseInt(string str, ref int index)
        {
            int start = index;
            while (str.Length > index && str[index] >= '0' && str[index] <= '9')
            {
                index++;
            }

            string number = str.Substring(start, index - start);

            return int.Parse(number, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Execute the bracket operations on the operator stack.
        /// </summary>
        /// <param name="operators">Operator stack.</param>
        /// <param name="values">Value stack.</param>
        /// <param name="express">Express list.</param>
        private static void DoBracketOperation(Stack<OperatorSerial> operators,
            Stack<OperatorSerial> values, List<OperatorSerial> express)
        {
            if (operators.Count > 0)
            {
                OperatorSerial op = operators.Peek();

                while (op.Code != (int)Logic.Bracket)
                {
                    Operate(operators, values, express);

                    if (operators.Count == 0)
                    {
                        break;
                    }

                    op = operators.Peek();
                }

                if (op.Code == (int)Logic.Bracket)
                {
                    // to pop out the left bracket
                    operators.Pop();
                }
            }
        }

        /// <summary>
        /// Execute higher priority operation then threshold.
        /// </summary>
        /// <param name="operators">Operator stack.</param>
        /// <param name="values">Value stack.</param>
        /// <param name="threshold">Threshold of operator priority.</param>
        /// <param name="express">Express list.</param>
        private static void DoHighPriorOperation(Stack<OperatorSerial> operators,
            Stack<OperatorSerial> values, int threshold, List<OperatorSerial> express)
        {
            if (operators.Count > 0)
            {
                OperatorSerial op = operators.Peek();
                while (op.Code >= threshold)
                {
                    Operate(operators, values, express);
                    if (operators.Count == 0)
                    {
                        break;
                    }

                    op = operators.Peek();
                }
            }
        }

        /// <summary>
        /// Do operation on operator and value stacks.
        /// </summary>
        /// <param name="operators">Operator stack.</param>
        /// <param name="values">Value stack.</param>
        /// <param name="express">Express list.</param>
        private static void Operate(Stack<OperatorSerial> operators,
            Stack<OperatorSerial> values, List<OperatorSerial> express)
        {
            OperatorSerial x = operators.Pop();

            OperatorSerial a = values.Pop();

            if (x.Code == (int)Logic.Not)
            {
                x.Flag = a.Flag;
                x.LeftOp = a.LeftOp;
            }
            else
            {
                OperatorSerial b = values.Pop();

                x.Flag = (short)((ushort)b.Flag | ((ushort)a.Flag << 1));
                x.LeftOp = b.LeftOp;
                x.RightOp = a.LeftOp;
            }

            express.Add(x);

            // push result to the values stack
            OperatorSerial value = new OperatorSerial();
            value.Flag = (short)(value.Flag & ~OP_LEFT_FEATURE);
            value.LeftOp = (int)(express.Count - 1);
            values.Push(value);
        } 

        #endregion
    }

    #endregion

    /// <summary>
    /// Cart node in cart tree.
    /// </summary>
    public class CartNode
    {
        #region Fields

        private int _index;
        private int _parentIndex;
        private string _questionLogic;
        private string _setPresent;
        private SetType _setType;

        private CartNode _parent;
        private CartNode _leftChild;
        private CartNode _rightChild;

        private Question _question;
        private BitArray _unitSet = new System.Collections.BitArray(0);

        private MetaCart _metaCart;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="CartNode"/> class.
        /// </summary>
        /// <param name="metaCart">Meta CART data.</param>
        public CartNode(MetaCart metaCart)
        {
            if (metaCart == null)
            {
                throw new ArgumentNullException("metaCart");
            }

            _metaCart = metaCart;
        }

        #endregion

        #region Type for serialization

        /// <summary>
        /// Cart node type.
        /// </summary>
        internal enum NodeType
        {
            /// <summary>
            /// Leaf node.
            /// </summary>
            Leaf = 0,

            /// <summary>
            /// Not leaf node.
            /// </summary>
            NotLeaf = 1
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Index in tree first-order vister.
        /// </summary>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        /// <summary>
        /// Gets or sets Parent node index.
        /// </summary>
        public int ParentIndex
        {
            get { return _parentIndex; }
            set { _parentIndex = value; }
        }

        /// <summary>
        /// Gets or sets Parent node. null if this is root node.
        /// </summary>
        public CartNode Parent
        {
            get
            {
                return _parent;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _parent = value;
            }
        }

        /// <summary>
        /// Gets or sets Left child node.
        /// </summary>
        public CartNode LeftChild
        {
            get
            {
                return _leftChild;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _leftChild = value;
            }
        }

        /// <summary>
        /// Gets or sets Right child node.
        /// </summary>
        public CartNode RightChild
        {
            get
            {
                return _rightChild;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _rightChild = value;
            }
        }

        /// <summary>
        /// Gets or sets Question used by this node to split units into sub nodes.
        /// Null if this node is leaf node.
        /// </summary>
        public Question Question
        {
            get
            {
                return _question;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _question = value;
            }
        }

        /// <summary>
        /// Gets or sets Question logic express.
        /// This is question in string presentation. '*' for null question.
        /// </summary>
        public string QuestionLogic
        {
            get
            {
                return _questionLogic;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _questionLogic = value;
            }
        }

        /// <summary>
        /// Gets or sets Set/unit collection presentation in string line.
        /// </summary>
        public string SetPresent
        {
            get
            {
                return _setPresent;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _setPresent = value;
            }
        }

        /// <summary>
        /// Gets Units, including in this node and it's subnodes.
        /// </summary>
        public System.Collections.BitArray UnitSet
        {
            get
            {
                if (_unitSet.Length == 0 && LeftChild != null && RightChild != null)
                {
                    _unitSet = new System.Collections.BitArray(LeftChild.UnitSet);
                    _unitSet = _unitSet.Or(RightChild.UnitSet);
                    System.Diagnostics.Debug.Assert(this.UnitCount > LeftChild.UnitCount
                        && this.UnitCount > RightChild.UnitCount);
                }

                return _unitSet;
            }
        }

        /// <summary>
        /// Gets Unit count of UnitSet, set bit count.
        /// </summary>
        public int UnitCount
        {
            get 
            {
                int unitCount = 0;
                System.Collections.BitArray set = this.UnitSet;
                for (int i = 0; i < set.Length; i++)
                {
                    if (set[i])
                    {
                        ++unitCount;
                    }
                }

                return unitCount;
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Find a node, under and including this node, statisfy special feature.
        /// </summary>
        /// <param name="feature">Unit feature.</param>
        /// <returns>Cart node most closing to the feature.</returns>
        public CartNode Test(TtsUnitFeature feature)
        {            
            if (LeftChild != null)
            {
                System.Diagnostics.Debug.Assert(RightChild != null);
                bool passed = this.Question.Test(feature);
                if (passed)
                {
                    return LeftChild.Test(feature);
                }
                else
                {
                    return RightChild.Test(feature);
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(RightChild == null);
                return this;
            }
        }

        /// <summary>
        /// Join an sub-CART tree this .
        /// </summary>
        /// <param name="isLeft">Is a left child .</param>
        /// <param name="idx">Node index .</param>
        /// <param name="subRoot">New child node .</param>
        public void JoinSubTree(bool isLeft, ref int idx, CartNode subRoot)
        {
            subRoot.Parent = this;
            subRoot.Index = idx;
            if (isLeft)
            {
                LeftChild = subRoot;
                subRoot.ParentIndex = Index;
            }
            else
            {
                RightChild = subRoot;
                subRoot.ParentIndex = -Index;
            }
            
            if (subRoot.LeftChild != null)
            {
                idx++;
                subRoot.JoinSubTree(true, ref idx, subRoot.LeftChild);
            }
            
            if (subRoot.RightChild != null)
            {
                idx++;
                subRoot.JoinSubTree(false, ref idx, subRoot.RightChild);
            }
        }

        /// <summary>
        /// Reversing build unit set from the leaf nodes up to root node.
        /// </summary>
        public void ReverseBuildUnitSet()
        {
            if (LeftChild != null)
            {
                // nonleaf node
                Debug.Assert(RightChild != null);

                if (LeftChild.UnitSet.Count == 0)
                {
                    LeftChild.ReverseBuildUnitSet();
                }

                if (RightChild.UnitSet.Count == 0)
                {
                    RightChild.ReverseBuildUnitSet();
                }

                Debug.Assert(LeftChild.UnitSet.Count == RightChild.UnitSet.Count);

                _unitSet = LeftChild.UnitSet.Or(RightChild.UnitSet);
            }
            else
            {
                // leaf node
                return;
            }
        }

        /// <summary>
        /// Traverse node and refresh unit set of leaf node.
        /// </summary>
        /// <param name="positions">Position collection.</param>
        /// <param name="length">New unit set length.</param>
        public void RemappingUnitSet(Collection<int> positions, int length)
        {
            if (LeftChild != null)
            {
                // Non-leaf node
                if (RightChild == null)
                {
                    string message = Helper.NeutralFormat("Invalid Cart Tree:" +
                        "Non-leaf node without Right Child");
                    throw new InvalidDataException(message);
                }

                LeftChild.RemappingUnitSet(positions, length);
                RightChild.RemappingUnitSet(positions, length);
            }
            else
            {
                // Leaf node
                if (RightChild != null)
                {
                    string message = Helper.NeutralFormat("Invalid Cart Tree:" +
                        "Leaf node has Right Child");
                    throw new InvalidDataException(message);
                }

                // Remapping unit set
                if (_unitSet.Length != positions.Count)
                {
                    string message = Helper.NeutralFormat("Invalid mapping:" +
                        "Original Unit Count = [{0}], Mapping Unit Count = [{1}]",
                        _unitSet.Length, positions.Count);
                    throw new InvalidDataException(message);
                }

                BitArray newUnitSet = new BitArray(length);
                newUnitSet.SetAll(false);
                for (int k = 0; k < _unitSet.Length; k++)
                {
                    if (_unitSet.Get(k))
                    {
                        Debug.Assert(positions[k] < length);
                        newUnitSet.Set(positions[k], true);
                    }
                }

                _unitSet = newUnitSet;
            }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Save this CART tree node instance in the binary format.
        /// </summary>
        /// <param name="bw">Binary writer to save CART node.</param>
        public void Save(BinaryWriter bw)
        {
            if (bw == null)
            {
                throw new ArgumentNullException("bw");
            }

            int curPos = (int)bw.Seek(0, SeekOrigin.Current);
            bw.Write(-1); // Write a initialized value for yes child's offset
            bw.Write(-1); // Write a initialized value for no child's offset
            if (LeftChild != null)
            {
                Debug.Assert(RightChild != null);
                bw.Write((int)NodeType.NotLeaf);

                OperatorSerial[] express = OperatorSerial.Parse(_question.ToString());
                Debug.Assert(express.Length > 0);
                bw.Write((uint)express.Length);
                bw.Write((uint)(express.Length - 1));
                foreach (OperatorSerial op in express)
                {
                    byte[] data = op.ToBytes();
                    bw.Write(data, 0, data.Length);
                }

                int tempPos = (int)bw.Seek(0, SeekOrigin.Current);
                bw.Seek(curPos, SeekOrigin.Begin);
                bw.Write(tempPos);
                bw.Seek(0, SeekOrigin.End);
                _leftChild.Save(bw);
                tempPos = (int)bw.Seek(0, SeekOrigin.Current);
                bw.Seek(curPos + sizeof(int), SeekOrigin.Begin);
                bw.Write(tempPos);
                bw.Seek(0, SeekOrigin.End);
                _rightChild.Save(bw);
            }
            else
            {
                bw.Write((int)NodeType.Leaf);

                switch (_setType)
                {
                    case SetType.BitSet:
                        SetUtil.Write(SetType.BitSet, _unitSet, bw);
                        break;

                    case SetType.IndexSet:
                        SetUtil.Write(SetType.IndexSet, _unitSet, bw);
                        break;

                    default:
                        throw new NotSupportedException("Unsupported set type for CART node.");
                }
            }
        }

        /// <summary>
        /// Load a Cart Node from bindary stream, which is Mulan CRT compatible.
        /// </summary>
        /// <param name="br">Binary reader to load CART node.</param>
        public void Load(BinaryReader br)
        {
            if (br == null)
            {
                throw new ArgumentNullException("br");
            }

            try
            {
                br.ReadInt32(); // Skip the yes child's offset
                br.ReadInt32(); // Skip the no child's offset
                NodeType noteType = (NodeType)br.ReadInt32();

                if (noteType == NodeType.NotLeaf)
                {
                    _question = new Question(_metaCart);
                    uint size = br.ReadUInt32();
                    int startPos = br.ReadInt32();
                    OperatorSerial[] express = new OperatorSerial[size];
                    for (uint i = 0; i < size; i++)
                    {
                        express[i] = OperatorSerial.Read(br);
                    }

                    // TODO: parse it into logical presentation
                    string logic = OperatorSerial.ToString(startPos, express);
                    _question.Parse(logic);

                    _leftChild = new CartNode(_metaCart);
                    _leftChild.Load(br);
                    _leftChild.Parent = this;

                    _rightChild = new CartNode(_metaCart);
                    _rightChild.Load(br);
                    _rightChild.Parent = this;

                    QuestionLogic = _question.ToString();
                }
                else if (noteType == NodeType.Leaf)
                {
                    // AbstractSet
                    int setType = br.ReadInt32();
                    Debug.Assert(setType == (int)SetType.AbstractSet);

                    int minValue = br.ReadInt32();
                    int maxValue = br.ReadInt32();

                    Debug.Assert(maxValue >= minValue);

                    setType = br.ReadInt32();
                    _setType = (SetType)setType;
                    switch (_setType)
                    {
                        case SetType.BitSet:
                            {
                                // BitSet
                                int size = br.ReadInt32();
                                Debug.Assert(size == maxValue - minValue + 1);

                                int setCount = br.ReadInt32();

                                // calculate the number of bytes to allocate to save
                                // the bit set data. the data is INT (4 bytes) aligned
                                int bytesRequired = ((size + 31) >> 5) * 4;

                                byte[] bits = br.ReadBytes(bytesRequired);
                                if (bits.Length != bytesRequired)
                                {
                                    string message = string.Format(CultureInfo.InvariantCulture,
                                    "Malformed data found, for there is no enough data for Bit set.");
                                    throw new InvalidDataException(message);
                                }

                                _unitSet = new BitArray(bits);
                                _unitSet.Length = size;
                                int loadSetCount = 0;
                                for (int k = 0; k < _unitSet.Length; k++)
                                {
                                    loadSetCount += _unitSet.Get(k) ? 1 : 0;
                                }

                                Debug.Assert(loadSetCount == setCount);
                            }

                            break;
                        case SetType.IndexSet:
                            {
                                // Count of integer
                                int size = br.ReadInt32();

                                // Index data
                                _unitSet = new BitArray(maxValue - minValue + 1);
                                _unitSet.SetAll(false);
                                int n = 0;
                                for (int i = 0; i < size; i++)
                                {
                                    n = br.ReadInt32();
                                    if (n > _unitSet.Length - 1 || n < 0)
                                    {
                                        throw new InvalidDataException("Invalid index set data");
                                    }

                                    _unitSet.Set(n, true);
                                }
                            }

                            break;
                        default:
                            {
                                throw new InvalidDataException("Invalid set type");
                            }
                    }
                }
                else
                {
                    Debug.Assert(false);
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid node type [{0}] of CART tree node found", noteType);
                    throw new InvalidDataException(message);
                }
            }
            catch (EndOfStreamException ese)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to CART tree node from binary stream for invalid data.");
                throw new InvalidDataException(message, ese);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                        "Fail to load CART node from binary stream");
                throw new InvalidDataException(message, ide);
            }
        }

        #endregion
    }
}