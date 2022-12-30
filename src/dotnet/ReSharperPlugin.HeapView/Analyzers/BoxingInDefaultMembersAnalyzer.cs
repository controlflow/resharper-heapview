using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

// [ElementProblemAnalyzer(
//   ElementTypes: new[]
//   {
//     typeof(ICSharpFunctionDeclaration),
//     typeof(IExpressionBodyOwnerDeclaration),
//   },
//   HighlightingTypes = new[]
//   {
//     typeof(BoxingAllocationHighlighting),
//     typeof(PossibleBoxingAllocationHighlighting)
//   })]
public class BoxingInDefaultMembersAnalyzer : HeapAllocationAnalyzerBase<ICSharpDeclaration>
{
  protected override bool ShouldRun(IFile file, ElementProblemAnalyzerData data)
  {
    return file.IsDefaultInterfaceImplementationSupported();
  }

  protected override void Run(
    ICSharpDeclaration declaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var typeDeclaration = declaration.GetContainingTypeDeclaration();
    if (typeDeclaration is not IInterfaceDeclaration) return;

    var typeMemberDeclaration = declaration.GetContainingTypeMemberDeclarationIgnoringClosures(returnThis: true);
    if (typeMemberDeclaration is not { IsStatic: false }) return;

    var codeBody = declaration.GetCodeBody();
    if (codeBody.GetAnyTreeNode() is not { } memberBody) return;

    if (memberBody is IConditionalAccessExpression conditionalAccessExpression1)
    {
      CheckInstanceMethodInvocationFromDefaultMember(conditionalAccessExpression1, data, consumer);
    }

    foreach (var descendant in memberBody.CompositeDescendants())
    {
      if (descendant is IConditionalAccessExpression conditionalAccessExpression)
      {
        CheckInstanceMethodInvocationFromDefaultMember(conditionalAccessExpression, data, consumer);
      }
    }
  }

  private static void CheckInstanceMethodInvocationFromDefaultMember(
    IConditionalAccessExpression conditionalAccessExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (conditionalAccessExpression)
    {
      case IReferenceExpression referenceExpression
        when referenceExpression.QualifierExpression.IsThisOrNull()
             && !referenceExpression.IsSubpatternMemberAccessPart():
      {
        var resolveResult = referenceExpression.Reference.Resolve();
        if (!resolveResult.ResolveErrorType.IsAcceptable) return;

        if (resolveResult.DeclaredElement is ITypeMember { IsStatic: false, ContainingType: IInterface } and (IProperty or IMethod or IEvent))
        {
          consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(
            referenceExpression.NameIdentifier, "aa"));
        }

        break;
      }

      case IElementAccessExpression elementAccessExpression
        when elementAccessExpression.Operand.GetOperandThroughParenthesis() is IThisExpression thisExpression:
      {
        var resolveResult = elementAccessExpression.Reference.Resolve();
        if (!resolveResult.ResolveErrorType.IsAcceptable) return;

        if (resolveResult.DeclaredElement is IProperty { IsStatic: false, ContainingType: IInterface })
        {
          consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(
            thisExpression, "bb"));
        }

        break;
      }
    }
  }
}