using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary4Roslyn
{
    internal class ClassUsedAsAttribute : Attribute
    {
        private string name;
        public double version;

        public ClassUsedAsAttribute(string name)
        {
            this.name = name;
            version = 1.0;
        }
    }
}
