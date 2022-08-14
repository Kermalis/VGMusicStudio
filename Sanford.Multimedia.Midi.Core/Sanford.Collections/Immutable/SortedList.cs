/*
 * Created by: Leslie Sanford 
 * 
 * Last modified: 02/23/2005
 * 
 * Contact: jabberdabber@hotmail.com
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

namespace Sanford.Collections.Immutable
{
    /// <summary>
    /// Represents a collection of key-and-value pairs that are sorted by the 
    /// keys and are accessible by key.
    /// </summary>
    [ImmutableObject(true)]
    public class SortedList : IEnumerable
    {
        #region SortedList Members

        #region Class Fields

        /// <summary>
        /// An empty SortedList.
        /// </summary>
        public static readonly SortedList Empty = new SortedList();

        #endregion

        #region Instance Fields

        // The compare object used for making comparisions.
        private IComparer comparer = null;

        // The root of the AVL tree.
        private IAvlNode root = AvlNode.NullNode;

        // Represents the method responsible for comparing keys.
        private delegate int CompareHandler(object x, object y);

        // The actual delegate to use for comparing keys.
        private CompareHandler compareHandler;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the SortedList class that is empty 
        /// and is sorted according to the IComparable interface implemented by 
        /// each key added to the SortedList.
        /// </summary>
        public SortedList()
        {
            InitializeCompareHandler();
        }

        /// <summary>
        /// Initializes a new instance of the SortedList class that is empty 
        /// and is sorted according to the specified IComparer interface.
        /// </summary>
        /// <param name="comparer">
        /// The IComparer implementation to use when comparing keys, or a null 
        /// reference to use the IComparable implementation of each key. 
        /// </param>
        public SortedList(IComparer comparer)
        {
            this.comparer = comparer;

            InitializeCompareHandler();
        }

        /// <summary>
        /// Initializes a new instance of the SortedList class with the 
        /// specified root node and the IComparer interface to use for sorting
        /// keys.
        /// </summary>
        /// <param name="root">
        /// The root of the AVL tree.
        /// </param>
        /// <param name="comparer">
        /// The IComparer implementation to use when comparing keys, or a null 
        /// reference to use the IComparable implementation of each key.
        /// </param>
        private SortedList(IAvlNode root, IComparer comparer)
        {
            this.root = root;
            this.comparer = comparer;

            InitializeCompareHandler();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds an element with the specified key and value to the SortedList.
        /// </summary>
        /// <param name="key">
        /// The key of the element to add.
        /// </param>
        /// <param name="value">
        /// The value of the element to add. The value can be a null reference.
        /// </param>
        /// <returns>
        /// A new SortedList with the specified key and value added to the 
        /// previous SortedList.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <i>key</i> is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An element with the specified key already exists in the SortedList,
        /// or The SortedList is set to use the IComparable interface, and key 
        /// does not implement the IComparable interface.
        /// </exception>
        public SortedList Add(object key, object value)
        {
            // Preconditions.
            if(key == null)
            {
                throw new ArgumentNullException("key", 
                    "Key cannot be null.");
            }
            else if(comparer == null && !(key is IComparable))
            {
                throw new ArgumentException(
                    "Key does not implement IComparable interface.");
            }

            return new SortedList(
                Add(key, value, root),
                comparer);
        }

        /// <summary>
        /// Determines whether the SortedList contains a specific key.
        /// </summary>
        /// <param name="key">
        /// The key to locate in the SortedList. 
        /// </param>
        /// <returns>
        /// <b>true</b> if the SortedList contains an element with the 
        /// specified <i>key</i>; otherwise, <b>false</b>.
        /// </returns>
        public bool Contains(object key)
        {
            return this[key] != null;            
        }

        /// <summary>
        /// Returns an IDictionaryEnumerator that can iterate through the 
        /// SortedList.
        /// </summary>
        /// <returns>
        /// An IDictionaryEnumerator for the SortedList.
        /// </returns>
        public IDictionaryEnumerator GetEnumerator()
        {
            return new SortedListEnumerator(root);
        }

        /// <summary>
        /// Removes the element with the specified key from SortedList.
        /// </summary>
        /// <param name="key">
        /// </param>
        /// <returns>
        /// The <i>key</i> of the element to remove. 
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <i>key</i> is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The SortedList is set to use the IComparable interface, and key 
        /// does not implement the IComparable interface.
        /// </exception>
        public SortedList Remove(object key)
        {
            // Preconditions.
            if(key == null)
            {
                throw new ArgumentNullException("key", 
                    "Key cannot be null.");
            }
            else if(comparer == null && !(key is IComparable))
            {
                throw new ArgumentException(
                    "Key does not implement IComparable interface.");
            }

            return new SortedList(Remove(key, root), comparer);
        }        

        // Initializes the delegate to use for making key comparisons.
        private void InitializeCompareHandler()
        {
            if(comparer == null)
            {
                compareHandler = new CompareHandler(CompareWithoutComparer);
            }
            else
            {
                compareHandler = new CompareHandler(CompareWithComparer);
            }
        }

        // Method for comparing keys using the IComparable interface.
        private int CompareWithoutComparer(object x, object y)
        {
            return ((IComparable)x).CompareTo(y);
        }

        // Method for comparing keys using the provided comparer.
        private int CompareWithComparer(object x, object y)
        {
            return comparer.Compare(x, y);
        }

        // Adds key/value pair to the internal AVL tree.
        private IAvlNode Add(object key, object value, IAvlNode node)
        {
            IAvlNode result;

            // If the bottom of the tree has been reached.
            if(node == AvlNode.NullNode)
            {
                // Create new node representing the new key/value pair.
                result = new AvlNode(
                    new DictionaryEntry(key, value),
                    AvlNode.NullNode,
                    AvlNode.NullNode);
            }
            // Else the bottom of the tree has not been reached.
            else
            {
                DictionaryEntry entry = (DictionaryEntry)node.Data;
                int compareResult = compareHandler(key, entry.Key);

                // If the specified key is less than the current key.
                if(compareResult < 0)
                {
                    // Create new node and continue searching to the left.
                    result = new AvlNode(
                        node.Data,
                        Add(key, value, node.LeftChild),
                        node.RightChild);
                }
                // Else the specified key is greater than the current key.
                else if(compareResult > 0)
                {
                    // Create new node and continue searching to the right.
                    result = new AvlNode(
                        node.Data,
                        node.LeftChild,
                        Add(key, value, node.RightChild));
                }
                // Else the specified key is equal to the current key.
                else
                {
                    // Throw exception. Duplicate keys are not allowed.
                    throw new ArgumentException(
                        "Item is already in the collection.");
                }
            }

            // If the current node is not balanced.
            if(!result.IsBalanced())
            {
                // Balance node.
                result = result.Balance();
            }

            return result;
        }

        // Search for the node with the specified key.
        private object Search(object key, IAvlNode node)
        {
            object result;

            // If the key is not in the SortedList.
            if(node == AvlNode.NullNode)
            {
                // Result is null.
                result = null;
            }
            // Else the key has not yet been found.
            else
            {
                DictionaryEntry entry = (DictionaryEntry)node.Data;
                int compareResult = compareHandler(key, entry.Key);

                // If the specified key is less than the current key.
                if(compareResult < 0)
                {
                    // Search to the left.
                    result = Search(key, node.LeftChild);
                }
                    // Else if the specified key is greater than the current key.
                else if(compareResult > 0)
                {
                    // Search to the right.
                    result = Search(key, node.RightChild);
                }
                // Else the key has been found.
                else
                {
                    // Get value.
                    result = entry.Value;
                }
            }

            return result;
        }

        // Remove the node with the specified key.
        private IAvlNode Remove(object key, IAvlNode node)
        {
            IAvlNode result;

            // The the key does not exist in the SortedList.
            if(node == AvlNode.NullNode)
            {
                // Result is null.
                result = node;
            }
            // Else the key has not yet been found.
            else
            {
                DictionaryEntry entry = (DictionaryEntry)node.Data;
                int compareResult = compareHandler(key, entry.Key);

                // If the specified key is less than the current key.
                if(compareResult < 0)
                {
                    // Create node and continue searching to the left.
                    result = new AvlNode(
                        node.Data,
                        Remove(key, node.LeftChild),
                        node.RightChild);
                }
                // Else if the specified key is greater than the current key.
                else if(compareResult > 0)
                {
                    // Create node and continue searching to the right.
                    result = new AvlNode(
                        node.Data,
                        node.LeftChild,
                        Remove(key, node.RightChild));
                }
                // Else the node to remove has been found.
                else
                {
                    // Remove node.
                    result = node.Remove();                    
                }
            }

            // If the node is out of balance.
            if(!result.IsBalanced())
            {
                // Rebalance node.
                result = result.Balance();
            }

            // Postconditions.
            Debug.Assert(result.IsBalanced());

            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public object this[object key]
        {
            get
            {
                return Search(key, root);
            }
        }  
      
        /// <summary>
        /// Gets the number of elements contained in the SortedList.
        /// </summary>
        public int Count
        {
            get
            {
                return root.Count;
            }
        }

        #endregion

        #region SortedListEnumerator Class

        /// <summary>
        /// Provides functionality for iterating through a SortedList.
        /// </summary>
        private class SortedListEnumerator : IDictionaryEnumerator
        {
            #region SortedListEnumerator Members

            #region Instance Fields

            private AvlEnumerator enumerator;

            #endregion

            #region Construction

            /// <summary>
            /// Initializes a new instance of the SortedListEnumerator class 
            /// with the specified root of the AVL tree to iterate over.
            /// </summary>
            /// <param name="root">
            /// The root of the AVL tree the SortedList uses internally.
            /// </param>
            public SortedListEnumerator(IAvlNode root)
            {
                enumerator = new AvlEnumerator(root);
            }

            #endregion

            #endregion

            #region IDictionaryEnumerator Members

            public object Key
            {
                get
                {
                    DictionaryEntry entry = (DictionaryEntry)enumerator.Current;

                    return entry.Key;
                }
            }

            public object Value
            {
                get
                {
                    DictionaryEntry entry = (DictionaryEntry)enumerator.Current;

                    return entry.Value;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    DictionaryEntry entry = (DictionaryEntry)enumerator.Current;

                    return entry;
                }
            }

            #endregion

            #region IEnumerator Members

            public void Reset()
            {
                enumerator.Reset();
            }

            public object Current
            {
                get
                {
                    return enumerator.Current;
                }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();                    
            }

            #endregion
        }

        #endregion

        #endregion

        #region IEnumerable Members

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new AvlEnumerator(root);
        }

        #endregion
    }
}