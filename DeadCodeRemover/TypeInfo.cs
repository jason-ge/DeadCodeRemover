using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadCodeRemover
{
    internal class TypeInfo
    {
        public string FullName
        {
            get
            {
                return $"{Symbol.ContainingNamespace}.{Symbol.Name}";
            }
        }
        public Project ContainingProject { get; set; }
        public Document ContainingDocument { get; set; }
        public INamedTypeSymbol Symbol { get; set; }
        public SyntaxNode Node { get; set; }
        public int NumberOfLines { get; set; }
        public IEnumerable<ISymbol> TypesUsingMe { get; set; }
        public bool? IsDead { get; set; }
        public string RemovalAction { get; set; }
        public int Depth { get; set; }
    }
}
