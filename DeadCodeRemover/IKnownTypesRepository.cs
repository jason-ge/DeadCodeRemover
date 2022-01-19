using Microsoft.CodeAnalysis;

namespace DeadCodeRemover
{
    internal interface IKnownTypesRepository
    {
        bool IsKnownType(INamedTypeSymbol type);
        void LoadKnownTypes(string path);
    }
}
