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
    [Test] public void TestClosures04() { DoNamedTest2(); }
    [Test] public void TestClosures05() { DoNamedTest2(); }
    [Test] public void TestClosures06() { DoNamedTest2(); }
    [Test] public void TestClosures07() { DoNamedTest2(); }

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

            inspector.Run();

            if (inspector.DisplayClasses.Count == 0 && inspector.CapturelessClosures.Count == 0) continue;

            writer.WriteLine(PresentElement(declaration.DeclaredElement));
            writer.WriteLine($"> display classes: {inspector.DisplayClasses.Count}");

            foreach (var (localScope, displayClass) in inspector.DisplayClasses.OrderBy(x => x.Value.Index))
            {
              writer.WriteLine($"  class #{displayClass.Index}: '{PresentScope(localScope)}'");

              writer.WriteLine($"    kind: {displayClass.Kind.ToString().Decapitalize()}");

              var members = displayClass.ScopeMembers.Select(PresentCaptureElement).ToList();

              if (displayClass.ParentDisplayClass is { Index: var index })
                members.Add($"parent display class #{index}");

              writer.WriteLine($"    members: {members.AggregateString(separator: ", ")}");
              writer.WriteLine($"    closures: {displayClass.Closures.Count}");

              foreach (var (closure, caps) in displayClass.Closures)
              {
                writer.WriteLine($"      {PresentClosure(closure)}");
                writer.WriteLine($"        captures: {caps.Select(PresentCaptureElement).AggregateString(separator: ", ")}");
              }
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

      static string PresentCaptureElement(IDeclaredElement declaredElement)
      {
        if (declaredElement is ITypeMember) return "'this' reference";

        return PresentElement(declaredElement);
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