using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Collections.Generic
{
    public partial class Deque<T>
    {
        #region Node Class

        // Represents a node in the deque.
        [Serializable()]
        private class Node
        {
            private T value;

            private Node previous = null;

            private Node next = null;

            public Node(T value)
            {
                this.value = value;
            }

            public T Value
            {
                get
                {
                    return value;
                }
            }

            public Node Previous
            {
                get
                {
                    return previous;
                }
                set
                {
                    previous = value;
                }
            }

            public Node Next
            {
                get
                {
                    return next;
                }
                set
                {
                    next = value;
                }
            }
        }

        #endregion
    }
}
