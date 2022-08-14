/*
 * Created by: Leslie Sanford 
 * 
 * Last modified: 02/23/2005
 * 
 * Contact: jabberdabber@hotmail.com
 */

using System;

namespace Sanford.Collections.Immutable
{
	/// <summary>
	/// Represents the functionality and properties of AVL nodes.
	/// </summary>
	internal interface IAvlNode
	{
        /// <summary>
        /// Removes the current node from the AVL tree.
        /// </summary>
        /// <returns>
        /// The node to in the tree to replace the current node.
        /// </returns>
        IAvlNode Remove();

        /// <summary>
        /// Balances the subtree represented by the node.
        /// </summary>
        /// <returns>
        /// The root node of the balanced subtree.
        /// </returns>
        IAvlNode Balance();

        /// <summary>
        /// Indicates whether or not the subtree the node represents is in 
        /// balance.
        /// </summary>
        /// <returns>
        /// <b>true</b> if the subtree is in balance; otherwise, <b>false</b>.
        /// </returns>
        bool IsBalanced();

        /// <summary>
        /// Gets the balance factor of the subtree the node represents.
        /// </summary>
        int BalanceFactor
        {
            get;
        }

        /// <summary>
        /// Gets the number of nodes in the subtree.
        /// </summary>
        int Count
        {
            get;
        }

        /// <summary>
        /// Gets the node's data.
        /// </summary>
        object Data
        {
            get;
        }

        /// <summary>
        /// Gets the height of the subtree the node represents.
        /// </summary>
        int Height
        {
            get;
        }

        /// <summary>
        /// Gets the node's left child.
        /// </summary>
        IAvlNode LeftChild
        {
            get;
        }

        /// <summary>
        /// Gets the node's right child.
        /// </summary>
        IAvlNode RightChild
        {
            get;
        }
	}
}
