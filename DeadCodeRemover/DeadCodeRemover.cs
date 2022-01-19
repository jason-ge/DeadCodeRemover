using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MSBuild;
using System.Xml.Linq;

namespace DeadCodeRemover
{
    internal class DeadTypeRemover
    {
        private readonly NLog.Logger _logger;
        public DeadTypeRemover(NLog.Logger logger)
        {
            _logger = logger;
        }

        public async Task<int> RemoveDeadTypes(MSBuildWorkspace workspace, IEnumerable<TypeInfo> types)
        {
            int count = 0;
            foreach (var document in types.Select(t => t.ContainingDocument).Where(t => !t.FilePath.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)).Distinct())
            {
                _logger.Info($"Processing document {document.Name} in project {document.Project.Name}.");
                DocumentEditor docEditor = await DocumentEditor.CreateAsync(document);
                int typesRemoved = RemoveTypesFromDocument(document, docEditor, types);
                count += typesRemoved;
                if (typesRemoved > 0)
                {
                    var newProject = await SaveChanges(document, docEditor);
                    if (newProject != null)
                    {
                        RemoveDocument(document);
                        // workspace.TryApplyChanges(newProject.Solution);
                        foreach (var type in types.Where(t => t.IsDead == true && t.ContainingDocument == document))
                        {
                            type.RemovalAction = "Document Removed";
                        }
                    }
                }
            }
            return count;
        }
        private int RemoveTypesFromDocument(Document document, DocumentEditor docEditor, IEnumerable<TypeInfo> types)
        {
            int count = 0;
            foreach (var typeInfo in types.Where(t => t.ContainingDocument == document).OrderBy(t => t.Depth))
            {
                _logger.Info($" Remove {typeInfo.Node.Kind()} {typeInfo.Symbol.Name} at depth {typeInfo.Depth}.");
                docEditor.RemoveNode(typeInfo.Node);
                typeInfo.RemovalAction = "Type Removed";
                count++;
            }
            return count;
        }
        private async Task<Project> SaveChanges(Document document, DocumentEditor docEditor)
        {
            var newDoc = docEditor.GetChangedDocument();
            var declarations = newDoc
            .GetSyntaxRootAsync()?
            .Result?
            .DescendantNodes();
            if (!declarations.Any(d =>
            d.Kind() == SyntaxKind.ClassDeclaration ||
            d.Kind() == SyntaxKind.StructDeclaration ||
            d.Kind() == SyntaxKind.InterfaceDeclaration ||
            d.Kind() == SyntaxKind.EnumDeclaration))
            {
                return document.Project.RemoveDocument(document.Id);
            }
            else
            {
                var newContent = (await newDoc.GetSyntaxTreeAsync())?
                .GetCompilationUnitRoot()
                .NormalizeWhitespace()
                .GetText()
                .ToString();
                using (var fs = new StreamWriter(newDoc.FilePath))
                {
                    fs.Write(newContent);
                }
                return null;
            }
        }

        private void RemoveDocument(Document document)
        {
            if (File.Exists(document.FilePath))
            {
                _logger.Info($" Remove file {document.FilePath}");
                File.Delete(document.FilePath);
                var fileName = Path.GetFileName(document.FilePath);
                XDocument doc = XDocument.Load(document.Project.FilePath);
                var resourceElements = doc.Descendants("EmbeddedResource").Where(el => el.Element("DependentUpon") != null && el.Element("DependentUpon").Value == fileName);
                foreach (var resxElement in resourceElements)
                {
                    string resFileName = null;
                    if (resxElement.Attribute("Update") != null)
                    {
                        resFileName = resxElement.Attribute("Update").Value;
                    }
                    else if (resxElement.Attribute("Include") != null)
                    {
                        resFileName = resxElement.Attribute("Include").Value;
                    }
                    else
                    {
                        _logger.Error($" Unexpected EmbeddedResource element {resxElement}");
                        continue;
                    }
                    var resxFile = Path.Combine(Path.GetDirectoryName(document.Project.FilePath), resFileName);
                    if (File.Exists(resxFile))
                    {
                        _logger.Info($" Remove resource file {resxFile}");
                        File.Delete(resxFile);
                    }
                }
                if (resourceElements.Any())
                {
                    resourceElements.Remove();
                    doc.Save(document.Project.FilePath);
                }
            }
            var designerFile = $"{Path.GetDirectoryName(document.FilePath)}\\{Path.GetFileNameWithoutExtension(document.FilePath)}.Designer{Path.GetExtension(document.FilePath)}";
            if (File.Exists(designerFile))
            {
                _logger.Info($" Remove designer file {designerFile}");
                File.Delete(designerFile);
            }
        }
    }
}
