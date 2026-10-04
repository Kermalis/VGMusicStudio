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
    /// Provides functionality for enumerating a RandomAccessList.
    /// </summary>
    internal class RalEnumerator : IEnumerator
    {
        #region Enumerator Members

        #region Instance Fields

        // The object at the current position.
        private object current = null;

        // The current index position.
        private int index;

        // For storing and traversing the nodes in the tree.
        private System.Collections.Stack treeStack = new System.Collections.Stack();

        // The first top node in the list.
        private RalTopNode head;

        // The current top node in the list.
        private RalTopNode currentTopNode;

        // The number of nodes in the list.
        private int count;

        #endregion 

        #region Construction

        /// <summary>
        /// Initializes a new instance of the Enumerator with the specified 
        /// head of the list and the number of nodes in the list.
        /// </summary>
        /// <param name="head">
        /// The head of the list.
        /// </param>
        /// <param name="count">
        /// The number of nodes in the list.
        /// </param>
        public RalEnumerator(RalTopNode head, int count)
        {
            this.head = head;
            this.count = count;

            if(count > 0)
            {
                Debug.Assert(head != null);
            }
           
            Reset();
        }

        #endregion

        #endregion

        #region IEnumerator Members

        /// <summary>
        /// Sets the enumerator to its initial position, which is before 
        /// the first element in the random access list.
        /// </summary>
        public void Reset()
        {
            index = -1;
            currentTopNode = head;
            treeStack.Clear();

            //  If the list is not empty.
            if(count > 0)
            {
                // Push the first node in the list onto the stack.
                treeStack.Push(head.Root);
            }
        }

        /// <summary>
        /// Gets the current element in the random access list.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The enumerator is positioned before the first element in the 
        /// random access list or after the last element.
        /// </exception>
        public object Current
        {
            get
            {    
                // Preconditions.
                if(index < 0 || index >= count)
                {
                    throw new InvalidOperationException(
                        "The enumerator is positioned before the first " +
                        "element of the collection or after the last element.");
                }

                return current;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element in the random access 
        /// list.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the enumerator was successfully advanced to the 
        /// next element; <b>false</b> if the enumerator has passed the end 
        /// of the collection.
        /// </returns>
        public bool MoveNext()
        {
            // Move index to the next position.
            index++;

            // If the index has moved beyond the end of the list, return false.
            if(index >= count)
                return false;

            RalTreeNode currentNode; 

            // Get the node at the top of the stack.
            currentNode = (RalTreeNode)treeStack.Peek();

            // Get the value at the top of the stack.
            current = currentNode.Value;

            // If there are still left children to traverse.
            if(currentNode.LeftChild != null)
            {
                // If the left child is not null, the right child should not be
                // null either.
                Debug.Assert(currentNode.RightChild != null);

                // Push left child onto stack.
                treeStack.Push(currentNode.LeftChild); 
            }
            // Else the bottom of the tree has been reached.
            else
            {
                // If the left child is null, the right child should be null, 
                // too.
                Debug.Assert(currentNode.RightChild == null);

                // Move back up in the tree to the parent node.
                treeStack.Pop();
                    
                RalTreeNode previousNode;

                // Whild the stack is not empty.
                while(treeStack.Count > 0)
                {
                    // Get the previous node.
                    previousNode = (RalTreeNode)treeStack.Peek();

                    // If the bottom of the left tree has been reached.
                    if(currentNode == previousNode.LeftChild)
                    {
                        // Push the right child onto the stack so that the 
                        // right tree will now be traversed.
                        treeStack.Push(previousNode.RightChild);

                        // Finished.
                        break;
                    }
                    // Else the bottom of the right tree has been reached.
                    else
                    {
                        // Keep track of the current node.
                        currentNode = previousNode;

                        // Pop the stack to move back up the tree.
                        treeStack.Pop();
                    }
                }

                // If the stack is empty.
                if(treeStack.Count == 0)
                {
                    // Move to the next tree in the list.
                    currentTopNode = currentTopNode.NextNode;

                    // If the end of the list has not yet been reached.
                    if(currentTopNode != null)
                    {
                        // Begin with the next tree.
                        treeStack.Push(currentTopNode.Root);
                    }
                }                    
            }

            return true;
        }

        #endregion
    }
}