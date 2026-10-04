using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Collections.Generic
{
    internal interface ICommand
    {
        void Execute();
        void Undo();
    }
}
