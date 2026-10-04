/*
 * Created by: Leslie Sanford 
 * 
 * Last modified: 02/23/2005
 * 
 * Contact: jabberdabber@hotmail.com
 */

using System;
using System.ComponentModel;

namespace Sanford.Collections.Immutable
{
	/// <summary>
	/// Represents a null AVL node.
	/// </summary>
	[ImmutableObject(true)]
	internal class NullAvlNode : IAvlNode
	{
        #region IAvlNode Members

        /// <summary>
        /// Removes the current node from the AVL tree.
        /// </summary>
        /// <returns>
        /// The node to in the tree to replace the current node.
        /// </returns>
        public IAvlNode Remove()
        {
            return this;
        }

        /// <summary>
        /// Balances the subtree represented by the node.
        /// </summary>
        /// <returns>
        /// The root node of the balanced subtree.
        /// </returns>
        public IAvlNode Balance()
        {
            return this;
        }

        /// <summary>
        /// Indicates whether or not the subtree the node represents is in 
        /// balance.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the subtree is in balance; otherwise, <b>false</b>.
        /// </returns>
        public bool IsBalanced()
        {
            return true;
        }

        /// <summary>
        /// Gets the balance factor of the subtree the node represents.
        /// </summary>
        public int BalanceFactor
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the number of nodes in the subtree.
        /// </summary>
        public int Count
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the node's data.
        /// </summary>
        public object Data
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the height of the subtree the node represents.
        /// </summary>
        public int Height
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the node's left child.
        /// </summary>
        public IAvlNode LeftChild
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Gets the node's right child.
        /// </summary>
        public IAvlNode RightChild
        {
            get
            {
                return this;
            }
        }

        #endregion
    }
}
