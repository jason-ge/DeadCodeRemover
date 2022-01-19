using DeadCodeRemover;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestDeadCodeRemover
{
    internal class UnitTestKnownTypeRepository : IKnownTypesRepository
    {
        public void LoadKnownTypes(string path)
        {
            throw new NotImplementedException();
        }

        public bool IsKnownType(INamedTypeSymbol symbol)
        {
            return symbol.Name == "TestClass";
        }
    }
}
