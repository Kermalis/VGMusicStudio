/*
 * Created by: Leslie Sanford 
 * 
 * Last modified: 02/28/2005
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
	/// Represents a collection of elements accessible by index and supports
	/// insertion and deletion.
	/// </summary>
	[ImmutableObject(true)]
	public class ArrayList
	{
        #region ArrayList Members

        #region Constants

        // The height of the tree pool.
        private const int TreePoolHeight = 30;

        // The default height of the initial tree.
        private const int DefaultCapacityHeight = 5;

        #endregion

        #region Readonly

        /* 
         * The tree pool is a tree made up of null nodes. It is completely 
         * balanced and is used to form a template of nodes for use in the
         * ArrayList. Initially, a small subtree is taken from the tree pool
         * when an ArrayList is created. New nodes replace the null nodes as
         * new versions of the ArrayList are created. Once the tree has been 
         * filled, another subtree of equal height is taken from the tree pool
         * to enlarge the tree for the next version of the ArrayList.
         * 
         * The reasoning behind this approach is that the Add method of the 
         * ArrayList will probably be the most widely used operation. By having
         * a prefabricated balanced tree, no rebalancing has to take place as 
         * new nodes are added to the tree. Their position in the tree has 
         * already been determined by the existing null tree. This improves 
         * performance.
         */

        private static readonly IAvlNode TreePool;

        #endregion

        #region Fields

        // The number of items in the ArrayList.
        private int count = 0;

        // The root of the tree.
        private IAvlNode root;

        #endregion

        #region Contstruction

        /// <summary>
        /// Initializes the ArrayList class.
        /// </summary>
        static ArrayList()
        {
            IAvlNode parent = AvlNode.NullNode;
            IAvlNode child = AvlNode.NullNode;

            // Create the tree pool.
            for(int i = 0; i < TreePoolHeight; i++)
            {
                parent = new AvlNode(null, child, child);
                child = parent;
            }

            TreePool = parent;

            // Postconditions.
            Debug.Assert(TreePool.Height == TreePoolHeight);
        }

        /// <summary>
        /// Initializes a new instance of the ArrayList class.
        /// </summary>
		public ArrayList()
		{
            root = GetSubTree(DefaultCapacityHeight);
        }

        /// <summary>
        /// Initializes a new instance of the ArrayList class that contains 
        /// elements copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The ICollection whose elements are copied to the new list. 
        /// </param>
        public ArrayList(ICollection collection)
        {
            if(collection.Count > 0)
            {
                int height = (int)Math.Log(collection.Count, 2) + 1;

                root = CollectionToTree(collection.GetEnumerator(), height);
            }
            else
            {
                root = GetSubTree(DefaultCapacityHeight);
            }

            count = collection.Count;
        }

        /// <summary>
        /// Initializes a new instance of the ArrayList class with the 
        /// specified root and count.
        /// </summary>
        /// <param name="root">
        /// The root of the tree.
        /// </param>
        /// <param name="count">
        /// The number of items in the ArrayList.
        /// </param>
        private ArrayList(IAvlNode root, int count)
        {
            this.root = root;
            this.count = count;
        }

        #endregion        

        #region Methods

        /// <summary>
        /// Adds an object to the end of the ArrayList.
        /// </summary>
        /// <param name="value">
        /// The Object to be added to the end of the ArrayList. 
        /// </param>
        /// <returns>
        /// A new ArrayList object with the specified value added at the end.
        /// </returns>
        public ArrayList Add(object value)
        {
            ArrayList result;

            // If the tree has been filled.
            if(count == root.Count)
            {
                // Create a new ArrayList while enlarging the tree. The 
                // current count serves as an index for setting the specified
                // value.
                result = new ArrayList(
                    SetValue(count, value, EnlargeTree()), 
                    count + 1);
            }
            // Else the tree has not been filled.
            else
            {
                // Create a new ArrayList. The current count serves as an index 
                // for setting the specified value.
                result = new ArrayList(
                    SetValue(count, value, root), 
                    count + 1);
            }

            // Postconditions.
            Debug.Assert(result.Count == Count + 1);
            Debug.Assert(result.GetValue(result.Count - 1) == value);

            return result;
        }

        /// <summary>
        /// Determines whether an element is in the ArrayList.
        /// </summary>
        /// <param name="value">
        /// The Object to locate in the ArrayList. 
        /// </param>
        /// <returns>
        /// <b>true</b> if item is found in the ArrayList; otherwise, 
        /// <b>false</b>.
        /// </returns>
        public bool Contains(object value)
        {
            return IndexOf(value) > -1;
        }

        /// <summary>
        /// Returns the zero-based index of the first occurrence of a value in 
        /// the ArrayList.
        /// </summary>
        /// <param name="value">
        /// The Object to locate in the ArrayList.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of value within the 
        /// ArrayList, if found; otherwise, -1.
        /// </returns>
        public int IndexOf(object value)
        {
            int index = 0;

            // Iterate through the ArrayList and compare each value with the
            // specified value. If they match, return the index of the value.
            foreach(object v in this)
            {
                if(value.Equals(v))
                {
                    return index;
                }

                index++;
            }

            // The specified value is not in the ArrayList.
            return -1;
        }

        /// <summary>
        /// Inserts an element into the ArrayList at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which value should be inserted. 
        /// </param>
        /// <param name="value">
        /// The Object to insert.
        /// </param>
        /// <returns>
        /// A new ArrayList with the specified object inserted at the specified 
        /// index.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than zero or index is greater than Count.
        /// </exception>
        public ArrayList Insert(int index, object value)
        {
            // Preconditions.
            if(index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException(
                    "ArrayList index out of range.");
            }            

            // Create new ArrayList with the value inserted at the specified index.
            ArrayList result = new ArrayList(Insert(index, value, root), count + 1);

            // Post conditions.
            Debug.Assert(result.GetValue(index) == value);

            return result;
        }

        /// <summary>
        /// Removes the first occurrence of a specified object from the 
        /// ArrayList.
        /// </summary>
        /// <param name="value">
        /// The Object to remove from the ArrayList. 
        /// </param>
        /// <returns>
        /// A new ArrayList with the first occurrent of the specified object 
        /// removed.
        /// </returns>
        public ArrayList Remove(object value)
        {
            ArrayList result;
            int index = IndexOf(value);

            // If the object is in the ArrayList.
            if(index > -1)
            {
                // Remove the object.
                result = RemoveAt(index);

                // Postcondition.
                Debug.Assert(result.Count == Count - 1);
            }
            // Else the object is not in the ArrayList.
            else
            {
                result = this;
            }

            return result;
        }

        /// <summary>
        /// Removes the element at the specified index of the ArrayList.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to remove. 
        /// </param>
        /// <returns>
        /// A new ArrayList with the element at the specified index removed.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than zero or index is equal to or greater than Count.
        /// </exception>
        public ArrayList RemoveAt(int index)
        {
            // Preconditions.
            if(index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException("index", index,
                    "ArrayList index out of range.");
            } 
           
            // Create a new ArrayList with the element at the specified index 
            // removed.
            ArrayList result = new ArrayList(RemoveAt(index, root), count - 1);

            // Postconditions.
            Debug.Assert(result.Count == Count - 1);

            return result;
        }

        /// <summary>
        /// Gets the value at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to get.
        /// </param>
        /// <returns>
        /// The value at the specified index.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than zero or index is equal to or greater than Count.
        /// </exception>
        public object GetValue(int index)
        {
            // Preconditions.
            if(index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException("index", index,
                    "Index out of range.");
            }

            return GetValue(index, root);
        }

        /// <summary>
        /// Sets the value at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to set.
        /// </param>
        /// <param name="value">
        /// The value to set at the specified index.
        /// </param>
        /// <returns>
        /// A new ArrayList with the specified value set at the specified index.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than zero or index is equal to or greater than Count.
        /// </exception>
        public ArrayList SetValue(int index, object value)
        {
            // Preconditions.
            if(index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(
                    "ArrayList index out of range.");
            }

            // Create a new ArrayList with the specified value set at the 
            // specified index.
            ArrayList result = new ArrayList(SetValue(index, value, root), count);

            // Postconditions.
            Debug.Assert(result.GetValue(index) == value);

            return result;
        }

        private IAvlNode CollectionToTree(IEnumerator enumerator, int height)
        {
            IAvlNode result;
            
            if(height == 0)
            {
                object data = null;

                if(enumerator.MoveNext())
                {
                    data = enumerator.Current;
                }

                result = new AvlNode(
                    data, 
                    AvlNode.NullNode,
                    AvlNode.NullNode);
            }
            else
            {
                IAvlNode leftChild, rightChild;
                object data = null;

                leftChild = CollectionToTree(enumerator, height - 1);

                if(enumerator.MoveNext())
                {
                    data = enumerator.Current;

                    rightChild = CollectionToTree(enumerator, height - 1);
                }
                else
                {
                    rightChild = GetSubTree(height - 1);
                }

                result = new AvlNode(
                    data,
                    leftChild,                    
                    rightChild);
            }

            Debug.Assert(result.IsBalanced());

            return result;
        }

        // Enlarges the tree used by the ArrayList.
        private IAvlNode EnlargeTree()
        {
            // Preconditions.
            Debug.Assert(root.Height <= TreePool.Height);

            // Create new root for the enlarged tree.
            IAvlNode result = new AvlNode(null, root, GetSubTree(root.Height));

            // Postconditions.
            Debug.Assert(result.BalanceFactor == 0);
            Debug.Assert(result.Height == root.Height + 1);

            return result;
        }

        // Recursive GetValue helper method.
        private object GetValue(int index, IAvlNode node)
        {
            // Preconditions.
            Debug.Assert(index >= 0 && index < Count);
            Debug.Assert(node != AvlNode.NullNode);

            object result;
            int leftCount =  node.LeftChild.Count;

            // If the node has been found.
            if(index == leftCount)
            {
                // Get value.
                result = node.Data;
            }
            // Else if the node is in the left tree.
            else if(index < leftCount)
            {
                // Move search to left child.
                result = GetValue(index, node.LeftChild);
            }
            // Else if the node is in the right tree.
            else
            {
                // Move search to the right child.
                result = GetValue(index - (leftCount + 1), node.RightChild);
            }

            return result;
        }
        
        // Recursive SetValue helper method.
        private IAvlNode SetValue(int index, object value, IAvlNode node)
        {
            // Preconditions.
            Debug.Assert(index >= 0 && index < node.Count);
            Debug.Assert(node != AvlNode.NullNode);

            IAvlNode result;
            int leftCount = node.LeftChild.Count;

            // If the node has been found.
            if(index == leftCount)
            {
                // Create new node with the new value.
                result = new AvlNode(value, node.LeftChild, node.RightChild);
            }
            // Else if the node is in the left tree.
            else if(index < leftCount)
            {
                // Create new node and move search to the left child. The new 
                // node will reuse the right child subtree.
                result = new AvlNode(
                    node.Data, 
                    SetValue(index, value, node.LeftChild),
                    node.RightChild);
            }
            // Else if the node is in the right tree.
            else
            {
                // Create new node and move search to the right child. The new 
                // node will reuse the left child subtree.
                result = new AvlNode(
                    node.Data,
                    node.LeftChild,
                    SetValue(index - (leftCount + 1), value, node.RightChild));
            }

            return result;
        }

        // Gets a subtree from the tree pool at the specified height.
        private static IAvlNode GetSubTree(int height)
        {
            // Preconditions.
            Debug.Assert(height >= 0 && height <= TreePool.Height);

            IAvlNode result = TreePool;

            // How far to descend into the tree pool to get the subtree.
            int d = TreePool.Height - height;

            // Descend down the tree pool until arriving at the root of the 
            // subtree.
            for(int i = 0; i < d; i++)
            {
                result = result.LeftChild;
            }

            // Postconditions.
            Debug.Assert(result.Height == height);
      
            return result;
        }        

        // Recursive Insert helper method.
        private IAvlNode Insert(int index, object value, IAvlNode node)
        {
            // Preconditions.
            Debug.Assert(index >= 0 && index <= Count);
            Debug.Assert(node != null);            

            /*
             * The insertion algorithm searches for the correct place to add a
             * new node at the bottom of the tree using the specified index.
             */

            IAvlNode result;

            // If the bottom of the tree has not yet been reached.
            if(node != AvlNode.NullNode)
            {
                int leftCount = node.LeftChild.Count;

                // If we need to descend to the left.
                if(index <= leftCount)
                {
                    // Create new node and move search to the left child. The 
                    // new node will reuse the right child subtree.
                    result = new AvlNode(
                        node.Data,
                        Insert(index, value, node.LeftChild),
                        node.RightChild);
                }
                // Else we need to descend to the right.
                else
                {
                    // Create new node and move search to the right child. The 
                    // new node will reuse the left child subtree.
                    result = new AvlNode(
                        node.Data,
                        node.LeftChild,
                        Insert(index - (leftCount + 1), 
                            value, 
                            node.RightChild));
                }
            }
            // Else the bottom of the tree has been reached.
            else
            {
                // Create new node at the specified index.
                result = new AvlNode(value, AvlNode.NullNode, AvlNode.NullNode);
            }

            /*
             * This check isn't necessary if a node has already been rebalanced 
             * after an insertion. AVL tree insertions never require more than
             * one rebalancing. However, it's easier to go ahead and check at 
             * this point since we're using recursion. This may need optimizing
             * in the future.
             */

            // If the node is not balanced.
            if(!result.IsBalanced())
            {                
                // Rebalance node.
                result = result.Balance();
            }

            // Postconditions.
            Debug.Assert(result.IsBalanced());

            return result;
        }

        // Recursive RemoveAt helper method.
        private IAvlNode RemoveAt(int index, IAvlNode node)
        {
            // Preconditions.
            Debug.Assert(index >= 0 && index < Count);
            Debug.Assert(node != AvlNode.NullNode);

            IAvlNode newNode = AvlNode.NullNode;

            int leftCount = node.LeftChild.Count;

            // If the node has been found.
            if(index == leftCount)
            {
                newNode = node.Remove();
            }
            // Else if the node is in the left tree.
            else if(index < leftCount)
            {
                // Create new node and move search to the left child. The new 
                // node will reuse the right child subtree.
                newNode = new AvlNode(
                    node.Data,
                    RemoveAt(index, node.LeftChild),
                    node.RightChild);
            }
            // Else if the node is in the right tree.
            else
            {
                // Create new node and move search to the right child. The new 
                // node will reuse the left child subtree.
                newNode = new AvlNode(
                    node.Data,
                    node.LeftChild,
                    RemoveAt(index - (leftCount + 1), node.RightChild));
            }

            // If the node is out of balance.
            if(!newNode.IsBalanced())
            {
                // Rebalance node.
                newNode = newNode.Balance();
            }

            // Postconditions.
            Debug.Assert(newNode.IsBalanced());

            return newNode;
        }        

        #endregion

        /// <summary>
        /// Gets the number of elements contained in the ArrayList.
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that can iterate through the ArrayList.
        /// </summary>
        /// <returns>
        /// An IEnumerator that can be used to iterate through the ArrayList.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return new AvlEnumerator(root, Count);
        }

        #endregion
    }
}
