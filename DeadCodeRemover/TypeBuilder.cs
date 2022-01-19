using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;

namespace DeadCodeRemover
{
    internal class TypeBuilder : ITypeBuilder
    {
        private readonly NLog.Logger _logger;
        private readonly Dictionary<Project, Compilation> _compilations = new Dictionary<Project, Compilation>();
        public TypeBuilder(NLog.Logger logger)
        {
            _logger = logger;
        }
        public async Task<IEnumerable<TypeInfo>> BuildTypes(Solution solution, IEnumerable<Project> projects, IKnownTypesRepository knownTypes)
        {
            List<TypeInfo> types = new List<TypeInfo>();
            foreach (var project in projects)
            {
                types.AddRange(await BuildTypes(solution, project, knownTypes));
            }
            FindDeadTypes(types, knownTypes, 0);
            return types;
        }

        private async Task<IEnumerable<TypeInfo>> BuildTypes(Solution solution, Project project, IKnownTypesRepository knownTypes)
        {
            List<TypeInfo> types = new List<TypeInfo>();
            _logger.Info($"Build types for project {project.Name}");
            foreach (var doc in project.Documents)
            {
                var model = await doc.GetSemanticModelAsync();
                var declarations = (await doc.GetSyntaxRootAsync()).DescendantNodes().Where(n => n is TypeDeclarationSyntax || n is EnumDeclarationSyntax);
                foreach (var declaration in declarations)
                {
                    var lineSpan = declaration.GetLocation().GetMappedLineSpan();
                    int lines = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
                    var type = (INamedTypeSymbol)model.GetDeclaredSymbol(declaration);
                    IEnumerable<ISymbol> typesUsingMe = await GetTypesUsingMe(solution, project, type);
                    var typeInfo = new TypeInfo()
                    {
                        Symbol = type,
                        Node = declaration,
                        TypesUsingMe = typesUsingMe,
                        ContainingDocument = doc,
                        ContainingProject = project,
                        NumberOfLines = lines,
                    };
                    _logger.Info($" Add type {typeInfo.FullName}, lines of code: {typeInfo.NumberOfLines}, {typesUsingMe.Count()} references.");
                    types.Add(typeInfo);
                }
            }
            return types;
        }

        private async Task<Compilation> GetProjectCompilation(Project project)
        {
            if (!_compilations.ContainsKey(project))
            {
                _compilations[project] = await project.GetCompilationAsync();
            }
            return _compilations[project];
        }

        private async Task<IEnumerable<ISymbol>> GetTypesUsingMe(Solution solution, Project project, ISymbol type)
        {
            if (type.IsStatic)
            {
                // Check if there are any extension methods
                var extMethods = ((INamedTypeSymbol)type).GetMembers().Where(s => s.Kind == SymbolKind.Method).Where(m => ((IMethodSymbol)m).IsExtensionMethod);
                if (extMethods.Any())
                {
                    List<ISymbol> typesUsingMe = new List<ISymbol>();
                    foreach (var extMethod in extMethods)
                    {
                        typesUsingMe.AddRange(await FilterSelfReferences(solution, extMethod));
                    }
                    return typesUsingMe;
                }
            }
            return await FilterSelfReferences(solution, type);
        }
        private async Task<IEnumerable<ISymbol>> FilterSelfReferences(Solution solution, ISymbol type)
        {
            List<ISymbol> typesUsingMe = new List<ISymbol>();
            var typeRefs = await SymbolFinder.FindReferencesAsync(type, solution);
            foreach (var typeRef in typeRefs)
            {
                foreach (var loc in typeRef.Locations)
                {
                    var node = loc.Location.SourceTree?.GetRoot()?.FindNode(loc.Location.SourceSpan);
                    while (node != null &&
                    !node.IsKind(SyntaxKind.ClassDeclaration) &&
                    !node.IsKind(SyntaxKind.InterfaceDeclaration) &&
                    !node.IsKind(SyntaxKind.StructDeclaration))
                    {
                        node = node.Parent;
                    }
                    if (node == null)
                    {
                        continue;
                    }
                    Compilation compilation = await GetProjectCompilation(loc.Document.Project);
                    ISymbol nodeSymbol = compilation.GetSemanticModel(loc.Location.SourceTree).GetDeclaredSymbol(node);
                    if (nodeSymbol != null && !SymbolEqualityComparer.Default.Equals(nodeSymbol, type))
                    {
                        typesUsingMe.Add(nodeSymbol);
                        _logger.Debug($" Type {type.ContainingNamespace}.{type.Name} is referenced in {loc.Document.FilePath}.");
                    }
                }
            }
            return typesUsingMe;
        }
        private void FindDeadTypes(IEnumerable<TypeInfo> types, IKnownTypesRepository knownTypes, int depth)
        {
            IEnumerable<TypeInfo> unknownTypes = types.Where(t => !t.IsDead.HasValue);
            IEnumerable<TypeInfo> deadTypes = types.Where(t => t.IsDead == true && t.Depth == depth - 1);
            if (unknownTypes.Count() == 0)
            {
                return;
            }
            bool found = false;
            foreach (var type in unknownTypes)
            {
                if (type.TypesUsingMe.All(t => deadTypes.Select(dt => dt.Symbol).Contains(t, SymbolEqualityComparer.Default)))
                {
                    if (knownTypes.IsKnownType(type.Symbol))
                    {
                        type.IsDead = false;
                        type.RemovalAction = "None - Known Type";
                        type.Depth = depth;
                    }
                    else
                    {
                        type.IsDead = true;
                        type.Depth = type.Node.Parent.IsKind(SyntaxKind.ClassDeclaration) ? -1 : depth;
                        found = true;
                    }
                }
            }
            if (!found)
            {
                foreach (var type in unknownTypes)
                {
                    type.IsDead = false;
                    type.Depth = 0;
                    type.RemovalAction = "None - Found References";
                }
                return;
            }
            FindDeadTypes(types, knownTypes, depth + 1);
        }
    }
}