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

namespace Sanford.Collections.Immutable
{
	/// <summary>
    /// Represents a simple last-in-first-out collection of objects.
	/// </summary>
	[ImmutableObject(true)]
	public class Stack : IEnumerable
	{
        #region Stack Members

        #region Class Fields

        /// <summary>
        /// An empty Stack.
        /// </summary>
        public static readonly Stack Empty = new Stack();

        #endregion

        #region Instance Fields

        // The number of elements in the stack.
        private readonly int count;

        // The top node in the stack.
        private Node top = null;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the Stack class.
        /// </summary>
		public Stack()
		{
            count = 0;
		}

        /// <summary>
        /// Initializes a new instance of the Stack class with the 
        /// specified top node and the number of elements in the stack.
        /// </summary>
        /// <param name="top">
        /// The top node in the stack.
        /// </param>
        /// <param name="count">
        /// The number of elements in the stack.
        /// </param>        
        private Stack(Node top, int count)
        {
            this.top = top;
            this.count = count;            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts an object at the top of the Stack.
        /// </summary>
        /// <param name="obj">
        /// The Object to push onto the Stack.
        /// </param>
        /// <returns>
        /// A new stack with the specified object on the top of the stack.
        /// </returns>
        public Stack Push(object obj)
        {
            Node newTop = new Node(obj, top);

            return new Stack(newTop, Count + 1);
        }

        /// <summary>
        /// Removes the object at the top of the Stack.
        /// </summary>
        /// <returns>
        /// A new stack with top of the previous stack removed.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The Stack is empty.
        /// </exception>
        public Stack Pop()
        { 
            // Preconditions.
            if(Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot pop an empty stack.");
            }

            Stack result;

            if(Count - 1 == 0)
            {
                result = Empty;
            }
            else
            {
                result = new Stack(top.Next, Count - 1);
            }

            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of elements in the Stack.
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// Gets the top of the stack.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The Stack is empty.
        /// </exception>
        public object Top
        {
            get
            {
                if(Count == 0)
                {
                    throw new InvalidOperationException(
                        "Cannot access the top when the stack is empty.");
                }

                return top.Value;
            }
        }

        #endregion

        #region Node Class

        /// <summary>
        /// Represents a node in the stack.
        /// </summary>
        private class Node
        {
            private Node next = null;

            private object value;

            public Node(object value, Node next)
            {
                this.value = value;
                this.next = next;
            }

            public Node Next
            {
                get
                {
                    return next;
                }
            }

            public object Value
            {
                get
                {
                    return value;
                }
            }
        }

        #endregion 

        #region StackEnumerator Class

        /// <summary>
        /// Provides functionality for iterating over the Stack class.
        /// </summary>
        private class StackEnumerator : IEnumerator
        {
            #region StackEnumerator Members

            #region Instance Fields

            // The stack to iterate over.
            private Stack owner;

            // The current index into the stack.
            private int index;

            // The current node.
            private Node current;

            // The next node in the stack.
            private Node next;

            #endregion

            #region Construction

            /// <summary>
            /// Initializes a new instance of the StackEnumerator class with 
            /// the specified stack to iterate over.
            /// </summary>
            /// <param name="owner">
            /// The Stack to iterate over.
            /// </param>
            public StackEnumerator(Stack owner)
            {
                this.owner = owner;

                Reset();
            }

            #endregion

            #region IEnumerator Members

            /// <summary>
            /// Sets the enumerator to its initial position, which is before 
            /// the first element in the Stack.
            /// </summary>
            public void Reset()
            {
                index = -1;

                next = owner.top;
            }

            /// <summary>
            /// Gets the current element in the Stack.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// The enumerator is positioned before the first element of the 
            /// Stack or after the last element.
            /// </exception>
            public object Current
            {
                get
                {
                    // Preconditions.
                    if(index < 0 || index >= owner.Count)
                    {
                        throw new InvalidOperationException(
                            "The enumerator is positioned before the first " +
                            "element of the collection or after the last element.");
                    }

                    return current.Value;
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the Stack.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                index++;

                if(index >= owner.Count)
                {
                    return false;
                }

                current = next;
                next = next.Next;

                return true;
            }

            #endregion
        }

        #endregion

        #endregion

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an IEnumerator for the Stack.
        /// </summary>
        /// <returns>
        /// An IEnumerator for the Stack.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return new StackEnumerator(this);
        }

        #endregion
    }
}
