/*
 * Created by: Leslie Sanford 
 * 
 * Last modified: 02/23/2005
 * 
 * Contact: jabberdabber@hotmail.com
 */

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Sanford.Collections.Immutable
{
	/// <summary>
	/// Represents a node in an AVL tree.
	/// </summary>
	[ImmutableObject(true)]
	internal class AvlNode : IAvlNode
	{
        #region AvlNode Members

        #region Class Fields

        // For use as a null node.
        public static readonly NullAvlNode NullNode = new NullAvlNode();

        #endregion

        #region Instance Fields

        // The data represented by this node.
        private readonly object data;

        // The number of nodes in the subtree.
        private readonly int count;

        // The height of this node.
        private readonly int height;

        // Left and right children.
        private readonly IAvlNode leftChild;
        private readonly IAvlNode rightChild;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the AvlNode class with the specified 
        /// data and left and right children.
        /// </summary>
        /// <param name="data">
        /// The data for the node.
        /// </param>
        /// <param name="leftChild">
        /// The left child.
        /// </param>
        /// <param name="rightChild">
        /// The right child.
        /// </param>
		public AvlNode(object data, IAvlNode leftChild, IAvlNode rightChild)
		{
            // Preconditions.
            Debug.Assert(leftChild != null && rightChild != null);

            this.data = data;
            this.leftChild = leftChild;
            this.rightChild = rightChild;

            count = 1 + leftChild.Count + rightChild.Count;
            height = 1 + Math.Max(leftChild.Height, rightChild.Height);
		}

        #endregion

        #region Methods
        
        #region Rotation Methods

        // Left - left single rotation.
        private IAvlNode DoLLRotation(IAvlNode node)
        {
            /*
             *  An LL rotation looks like the following:  
             * 
             *             A          B    
             *            /          / \
             *           B    --->  C   A
             *          /
             *         C 
             */

            // Create right child of the new root.
            IAvlNode a = new AvlNode(
                node.Data, 
                node.LeftChild.RightChild, 
                node.RightChild);

            IAvlNode b = new AvlNode(
                node.LeftChild.Data, 
                node.LeftChild.LeftChild, 
                a);

            // Postconditions.
            Debug.Assert(b.Data == node.LeftChild.Data);
            Debug.Assert(b.LeftChild == node.LeftChild.LeftChild);
            Debug.Assert(b.RightChild.Data == node.Data);

            return b;
        }

        // Left - right double rotation.
        private IAvlNode DoLRRotation(IAvlNode node)
        {
            /*
             *  An LR rotation looks like the following: 
             * 
             *       Perform an RR rotation at B:
             *   
             *           A              A
             *          /              /
             *         B      --->    C
             *          \            / 
             *           C          B
             * 
             *       Perform an LL rotation at A:
             *     
             *             A          C    
             *            /          / \
             *           C    --->  B   A
             *          /
             *         B 
             */

            IAvlNode a = new AvlNode(
                node.Data, 
                DoRRRotation(node.LeftChild), 
                node.RightChild);

            IAvlNode c = DoLLRotation(a);

            // Postconditions.
            Debug.Assert(c.Data == node.LeftChild.RightChild.Data);
            Debug.Assert(c.LeftChild.Data == node.LeftChild.Data);
            Debug.Assert(c.RightChild.Data == node.Data);

            return c;
        }

        // Right - right single rotation.
        private IAvlNode DoRRRotation(IAvlNode node)
        { 
            /*
             *  An RR rotation looks like the following:  
             * 
             *        A              B    
             *         \            / \
             *          B    --->  A   C
             *           \
             *            C 
             */

            // Create left child of the new root.
            IAvlNode a = new AvlNode(
                node.Data, 
                node.LeftChild, 
                node.RightChild.LeftChild);

            IAvlNode b = new AvlNode(
                node.RightChild.Data, 
                a, 
                node.RightChild.RightChild);

            // Postconditions.
            Debug.Assert(b.Data == node.RightChild.Data);
            Debug.Assert(b.RightChild == node.RightChild.RightChild);
            Debug.Assert(b.LeftChild.Data == node.Data);

            return b;
        }

        // Right - left double rotation.
        private IAvlNode DoRLRotation(IAvlNode node)
        {
            /*
             *  An RL rotation looks like the following: 
             * 
             *       Perform an LL rotation at B:
             *   
             *         A            A
             *          \            \ 
             *           B    --->    C
             *          /              \ 
             *         C                B
             * 
             *       Perform an RR rotation at A:
             *     
             *         A              C    
             *          \            / \
             *           C    --->  A   B
             *            \
             *             B 
             */

            IAvlNode a = new AvlNode(
                node.Data, 
                node.LeftChild,
                DoLLRotation(node.RightChild));

            IAvlNode c = DoRRRotation(a);

            // Postconditions.
            Debug.Assert(c.Data == node.RightChild.LeftChild.Data);
            Debug.Assert(c.LeftChild.Data == node.Data);
            Debug.Assert(c.RightChild.Data == node.RightChild.Data);                

            return c;
        }

        #endregion

        #endregion

        #endregion

        #region IAvlNode Members

        /// <summary>
        /// Removes the current node from the AVL tree.
        /// </summary>
        /// <returns>
        /// The node to in the tree to replace the current node.
        /// </returns>
        public IAvlNode Remove()
        {
            IAvlNode result; 

            /*
             * Deal with the three cases for removing a node from a binary tree.
             */

            // If the node has no right children.
            if(this.RightChild == AvlNode.NullNode)
            {  
                // The replacement node is the node's left child.
                result = this.LeftChild;
            }
                // Else if the node's right child has no left children.
            else if(this.RightChild.LeftChild == AvlNode.NullNode)
            {
                // The replacement node is the node's right child.
                result = new AvlNode(
                    this.RightChild.Data,
                    this.LeftChild,
                    this.RightChild.RightChild);
            }
                // Else the node's right child has left children.
            else
            {
                /*
                 * Go to the node's right child and descend as far left as 
                 * possible. The node found at this point will replace the 
                 * node to be removed.
                 */

                IAvlNode replacement = AvlNode.NullNode;
                IAvlNode rightChild = RemoveReplacement(this.RightChild, ref replacement);

                // Create new node with the replacement node and the new
                // right child.
                result = new AvlNode(
                    replacement.Data,
                    this.LeftChild,
                    rightChild);
            }

            return result;
        }

        // Finds and removes replacement node for deletion (third case).
        private IAvlNode RemoveReplacement(IAvlNode node, ref IAvlNode replacement)
        {
            IAvlNode newNode;

            // If the bottom of the left tree has been found.
            if(node.LeftChild == AvlNode.NullNode)
            {
                // The replacement node is the node found at this point.
                replacement = node;

                // Get the node's right child. This will be needed as we 
                // ascend back up the tree.
                newNode = node.RightChild;
            }
                // Else the bottom of the left tree has not been found.
            else
            {
                // Create new node and continue descending down the left tree.
                newNode = new AvlNode(node.Data,
                    RemoveReplacement(node.LeftChild, ref replacement),
                    node.RightChild);

                // If the node is out of balance.
                if(!newNode.IsBalanced())
                {
                    // Rebalance the node.
                    newNode = newNode.Balance();
                }
            }

            // Postconditions.
            Debug.Assert(newNode.IsBalanced());

            return newNode;
        }

        /// <summary>
        /// Balances the subtree represented by the node.
        /// </summary>
        /// <returns>
        /// The root node of the balanced subtree.
        /// </returns>
        public IAvlNode Balance()
        {
            IAvlNode result;

            if(BalanceFactor < -1)
            {
                if(leftChild.BalanceFactor < 0)
                {
                    result = DoLLRotation(this);
                }
                else
                {
                    result = DoLRRotation(this);
                }
            }
            else if(BalanceFactor > 1)
            {
                if(rightChild.BalanceFactor > 0)
                {
                    result = DoRRRotation(this);
                }
                else
                {
                    result = DoRLRotation(this);
                }
            } 
            else
            {
                result = this;
            }

            Debug.Assert(result.IsBalanced());

            return result;
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
            return BalanceFactor >= -1 && BalanceFactor <= 1;
        }

        /// <summary>
        /// Gets the balance factor of the subtree the node represents.
        /// </summary>
        public int BalanceFactor
        {
            get
            {
                return rightChild.Height - leftChild.Height;
            }
        }

        /// <summary>
        /// Gets the number of nodes in the subtree.
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets the node's data.
        /// </summary>
        public object Data
        {
            get
            {
                return data;
            }
        }

        /// <summary>
        /// Gets the height of the subtree the node represents.
        /// </summary>
        public int Height
        {
            get
            {
                return height;
            }
        }

        /// <summary>
        /// Gets the node's left child.
        /// </summary>
        public IAvlNode LeftChild
        {
            get
            {
                return leftChild;
            }
        }

        /// <summary>
        /// Gets the node's right child.
        /// </summary>
        public IAvlNode RightChild
        {
            get
            {
                return rightChild;
            }
        }

        #endregion
    }
}
