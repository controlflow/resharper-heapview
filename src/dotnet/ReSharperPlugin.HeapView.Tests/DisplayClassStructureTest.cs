using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.HeapView.Analyzers;

namespace ReSharperPlugin.HeapView.Tests;

[TestFixture]
[TestNet60]
public class DisplayClassStructureTest : BaseTestWithSingleProject
{
  protected override string RelativeTestDataPath => @"DisplayClass";

  [Test] public void Test01() { DoNamedTest(); }
  [Test] public void Test02() { DoNamedTest(); }
  [Test] public void Test03() { DoNamedTest(); }

  [Test] public void TestClosures01() { DoNamedTest(); }
  [Test] public void TestClosures02() { DoNamedTest(); }
  [Test] public void TestClosures03() { DoNamedTest(); }
  [Test] public void TestClosures04() { DoNamedTest(); }
  [Test] public void TestClosures05() { DoNamedTest(); }
  [Test] public void TestClosures06() { DoNamedTest(); }
  [Test] public void TestClosures07() { DoNamedTest(); }

  [Test] public void TestParameters01() { DoNamedTest(); }
  [Test] public void TestParameters02() { DoNamedTest(); }
  [Test] public void TestParameters03() { DoNamedTest(); }
  [Test] public void TestParameters04() { DoNamedTest(); }
  [Test] public void TestParameters05() { DoNamedTest(); }
  [Test] public void TestParameters06() { DoNamedTest(); }
  [Test] public void TestParameters07() { DoNamedTest(); }
  [Test] public void TestParameters08() { DoNamedTest(); }

  [Test] public void TestRecords01() { DoNamedTest(); }
  [Test] public void TestRecords02() { DoNamedTest(); }
  [Test] public void TestRecords03() { DoNamedTest(); }

  [Test] public void TestQuery01() { DoNamedTest(); }

  [Test] public void TestLocalFunctions01() { DoNamedTest(); }
  [Test] public void TestLocalFunctions02() { DoNamedTest(); }
  [Test] public void TestLocalFunctions03() { DoNamedTest(); }

  protected override void DoTest(Lifetime lifetime, IProject testProject)
  {
    var psiFiles = Solution.GetPsiServices().Files;
    psiFiles.CommitAllDocuments();

    foreach (var projectFile in testProject.GetAllProjectFiles())
    {
      ExecuteWithGold(projectFile, writer =>
      {
        var sourceFile = projectFile.ToSourceFiles().Single();
        var psiFile = (ICSharpFile) sourceFile.GetPsiFiles(CSharpLanguage.Instance).Single();

        var enumerator = psiFile.ThisAndDescendants();
        while (enumerator.MoveNext())
        {
          var current = enumerator.Current;

          switch (current)
          {
            case IClassLikeDeclaration:
            case ICSharpFunctionDeclaration:
            case IExpressionBodyOwnerDeclaration:
            case ITopLevelCode:
            {
              var displayClassStructure = DisplayClassStructure.Build(current);
              if (displayClassStructure != null)
              {
                writer.WriteLine(displayClassStructure.Dump());
                writer.WriteLine("========");
              }

              break;
            }
          }

          switch (current)
          {
            case IBlock:
            case IArrowExpressionClause:
            case IVariableInitializer:
            case ITopLevelCode:
            {
              enumerator.SkipThisNode();
              break;
            }
          }
        }
      });
    }
  }
}