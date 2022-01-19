using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary4Roslyn
{
    public sealed class ClassWithSelfReferences
    {
        private readonly string _name;
        public ClassWithSelfReferences() : this("DefaultClass")
        {
        }
        public ClassWithSelfReferences(string name)
        {
            _name = name;
        }

        public ClassWithSelfReferences GetNewInstance()
        {
            return new ClassWithSelfReferences("MyClass");
        }
    }
}
