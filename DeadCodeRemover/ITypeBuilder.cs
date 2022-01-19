using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadCodeRemover
{
    internal interface ITypeBuilder
    {
        Task<IEnumerable<TypeInfo>> BuildTypes(Solution solution, IEnumerable<Project> projects, IKnownTypesRepository knownTypes);
    }
}
