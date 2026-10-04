/*
 * Created by: Leslie Sanford 
 * 
 * Last modified: 02/23/2005
 * 
 * Contact: jabberdabber@hotmail.com
 */

using System;
using System.Collections;
using System.Diagnostics;

namespace Sanford.Collections.Immutable
{
    /// <summary>
    /// Provides functionality for iterating over an AVL tree.
    /// </summary> 
    internal class AvlEnumerator : IEnumerator
    {
        #region AvlEnumerator Members

        #region Instance Fields

        // The root of the AVL tree.
        private IAvlNode root;

        // The number of nodes in the tree.
        private readonly int count;

        // The object at the current position.
        private object current = null;

        // The current index.
        private int index;

        // Used for traversing the tree inorder.
        private System.Collections.Stack nodeStack = new System.Collections.Stack();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the AvlEnumerator class.
        /// </summary>
        /// <param name="root">
        /// The root of the AVL tree to iterate over.
        /// </param>
        public AvlEnumerator(IAvlNode root)
        {
            this.root = root;
            this.count = root.Count;

            Reset();
        }

        /// <summary>
        /// Initializes a new instance of the AvlEnumerator class.
        /// </summary>
        /// <param name="root">
        /// The root of the AVL tree to iterate over.
        /// </param>
        /// <param name="count">
        /// The number of nodes in the tree.
        /// </param>
        public AvlEnumerator(IAvlNode root, int count)
        {
            Debug.Assert(count <= root.Count);

            this.root = root;
            this.count = count;

            Reset();
        }

        #endregion

        #endregion

        #region IEnumerator Members

        /// <summary>
        /// Sets the enumerator to its initial position, which is before 
        /// the first element in the AVL tree.
        /// </summary>
        public void Reset()
        {
            index = 0;

            nodeStack.Clear();

            IAvlNode currentNode = root;

            // Push nodes on to the stack to get to the first item.
            while(currentNode != AvlNode.NullNode)
            {
                nodeStack.Push(currentNode);
                currentNode = currentNode.LeftChild;
            }
        }

        /// <summary>
        /// Gets the current element in the AVL tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The enumerator is positioned before the first element in the AVL
        /// tree or after the last element.
        /// </exception>
        public object Current
        {
            get
            {
                if(index == 0)
                {
                    throw new InvalidOperationException(
                        "The enumerator is positioned before the first " +
                        "element of the collection or after the last " +
                        "element.");
                }

                return current;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the AVL tree.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the enumerator was successfully advanced to the 
        /// next element; <b>false</b> if the enumerator has passed the end 
        /// of the collection.
        /// </returns>
        public bool MoveNext()
        {
            bool result;

            // If the end of the AVL tree has not yet been reached.
            if(index < count)
            {
                // Get the next node.
                IAvlNode currentNode = (IAvlNode)nodeStack.Pop();

                current = currentNode.Data;

                currentNode = currentNode.RightChild;

                while(currentNode != AvlNode.NullNode)
                {
                    nodeStack.Push(currentNode);
                    currentNode = currentNode.LeftChild;
                }

                index++;

                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        #endregion
    }
}