using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary4Roslyn
{
    internal class SingletonClass
    {
        public static readonly SingletonClass Instance = new SingletonClass();

        private SingletonClass() { }
    }
}
