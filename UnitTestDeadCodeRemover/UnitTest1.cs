using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeadCodeRemover;

namespace UnitTestDeadCodeRemover
{
    [TestClass]
    public class UnitTest1
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static IEnumerable<TypeInfo> types;
        private static MSBuildWorkspace workspace;
        [ClassInitialize]
        public static void TestInitialize(TestContext context0)
        {
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances[0];
            //var instance = visualStudioInstances.Length == 1
            // ? visualStudioInstances[0]
            // : SelectVisualStudioInstance(visualStudioInstances);
            var knowTypes = new UnitTestKnownTypeRepository();
            try
            {
                MSBuildLocator.RegisterInstance(instance);
                workspace = MSBuildWorkspace.Create();
                workspace.WorkspaceFailed += (o, e) => Logger.Warn(e.Diagnostic.Message);
                Logger.Info($"Loading solution ClassLibrary4Roslyn.sln into workspace.");
                var solution = workspace.OpenSolutionAsync(@"C:\Users\wnms2\source\repos\DeadCodeRemover\ClassLibrary4Roslyn.sln", new ConsoleProgressReporter()).Result;
                var projects = solution.Projects.Where(p => p.FilePath == @"C:\Users\wnms2\source\repos\DeadCodeRemover\ClassLibrary4Roslyn\ClassLibrary4Roslyn.csproj");
                if (projects.Count() == 0)
                {
                    throw new ArgumentException($"Cannot find project ClassLibrary4Roslyn.csproj in solution,");
                }
                var typeBuilder = new TypeBuilder(Logger);
                types = typeBuilder.BuildTypes(solution, projects, knowTypes).Result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        [TestMethod]
        public void TestEnum()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.EnumHasReferences");
            Assert.AreEqual(3, type0?.TypesUsingMe.Count());
            var type1 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.EnumHasNoReferences");
            Assert.AreEqual(0, type1?.TypesUsingMe.Count());
        }
        [TestMethod]
        public void TestKnownType()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.TestClass");
            Assert.AreEqual(0, type0?.TypesUsingMe.Count());
            Assert.AreEqual(false, type0?.IsDead);
            Assert.AreEqual("None - Known Type", type0?.RemovalAction);
        }
        [TestMethod]
        public void TestExtensionMethod()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.StaticClassWithExtensionMethod");
            Assert.AreEqual(false, type0?.IsDead);
        }
        [TestMethod]
        public void TestClassWithOnlyConstants()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.ClassWithOnlyConstants");
            Assert.AreEqual(1, type0?.TypesUsingMe.Count());
            Assert.AreEqual(false, type0?.IsDead);
        }
        [TestMethod]
        public void TestStaticClass()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.StaticClass");
            Assert.AreEqual(1, type0?.TypesUsingMe.Count());
            Assert.AreEqual(false, type0?.IsDead);
        }
        [TestMethod]
        public void TestNestedClass()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.NestedType");
            Assert.AreEqual(3, type0?.TypesUsingMe.Count());
        }
        [TestMethod]
        public void TestAttributeReference()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.ClassUsedAsAttribute");
            Assert.AreEqual(1, type0?.TypesUsingMe.Count());
        }
        [TestMethod]
        public void TestInstanceSelfReferences()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.ClassWithSelfReferences");
            Assert.AreEqual(0, type0?.TypesUsingMe.Count());
        }
        [TestMethod]
        public void TestDesignerReference()
        {
            var type0 = types.FirstOrDefault(t => t.FullName == "ClassLibrary4Roslyn.MyUserControl");
            Assert.AreEqual(0, type0?.TypesUsingMe.Count());
        }
        [ClassCleanup]
        public static void TestCleanup()
        {
            if (workspace != null)
            {
                workspace.Dispose();
            }
        }
        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }
                Logger.Debug($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}