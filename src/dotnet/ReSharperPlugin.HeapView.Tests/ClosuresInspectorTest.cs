using System;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;
using ReSharperPlugin.HeapView.Analyzers;

namespace ReSharperPlugin.HeapView.Tests
{
  [TestNetFramework46]
  [TestPackages(Packages = new[] {SYSTEM_VALUE_TUPLE_PACKAGE})]
  [TestReferences("System", "System.Core", "Microsoft.CSharp")]
  [CSharpLanguageLevel(CSharpLanguageLevel.Latest)]
  public class ClosuresInspectorTest : BaseTestWithTextControl
  {
    protected override string RelativeTestDataPath => "Closures";

    [Test] public void TestClosures01() { DoNamedTest2(); }
    [Test] public void TestClosures02() { DoNamedTest2(); }
    [Test] public void TestClosures03() { DoNamedTest2(); }

    protected override void DoTest(Lifetime lifetime, IProject testProject)
    {
      var textControl = OpenTextControl(lifetime);
      var sourceFile = textControl.Document.GetPsiSourceFile(Solution).NotNull();
      var psiFile = sourceFile.GetTheOnlyPsiFile<CSharpLanguage>().NotNull();

      ExecuteWithGold(
        testName: TestName2,
        writer =>
        {
          foreach (var declaration in psiFile.ThisAndDescendants<ICSharpDeclaration>())
          {
            var inspector = ClosuresInspector.TryBuild(declaration);
            if (inspector == null) continue;

            writer.WriteLine(PresentElement(declaration.DeclaredElement));

            inspector.Run();

            writer.WriteLine($"> display classes: {inspector.DisplayClasses.Count}");

            foreach (var (localScope, displayClass) in inspector.DisplayClasses.OrderBy(x => x.Value.Index))
            {
              writer.WriteLine($"    display class #{displayClass.Index}: '{PresentScope(localScope)}'");
              writer.WriteLine($"       captures: {displayClass.Captures.Select(PresentElement).AggregateString(separator: ", ")}");
            }

            writer.WriteLine($"> captures: {inspector.Captures.Count}");

            foreach (var (closure, captures) in inspector.Captures)
            {
              writer.WriteLine($"    {PresentClosure(closure)}");
              writer.WriteLine($"       captures: {captures.Select(PresentElement).AggregateString(separator: ", ")}");
            }

            writer.WriteLine($"> captureless: {inspector.CapturelessClosures.Count}");
            foreach (var closure in inspector.CapturelessClosures)
            {
              writer.WriteLine($"  {PresentClosure(closure)}");
            }
          }
        });

      static string PresentClosure(ICSharpClosure closure)
      {
        var code = closure.GetText().ToSingleLineAndTrim(maxLength: 40);

        switch (closure)
        {
          case IAnonymousMethodExpression _:
            return $"anon method  '{code}'";
          case ILambdaExpression _:
            return $"lambda expr  '{code}'";
          case ILocalFunctionDeclaration _:
            return $"local func   '{code}'";
          case IQueryParameterPlatform _:
            return $"query clause '{code}'";
          default:
            throw new ArgumentOutOfRangeException(nameof(closure));
        }
      }

      static string PresentElement(IDeclaredElement declaredElement)
      {
        if (declaredElement is null) return "<null>";

        return DeclaredElementPresenter.Format(
          CSharpLanguage.Instance, DeclaredElementPresenter.KIND_QUOTED_NAME_PRESENTER, declaredElement).Text;
      }

      static string PresentScope(IScope localScope)
      {
        return localScope.GetText().ToSingleLineAndTrim(maxLength: 40);
      }
    }
  }
}