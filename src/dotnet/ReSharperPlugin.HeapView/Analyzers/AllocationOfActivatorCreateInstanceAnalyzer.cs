using System;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.RichText;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(IInvocationExpression) ],
  HighlightingTypes = [
    typeof(ObjectAllocationHighlighting),
    typeof(ObjectAllocationPossibleHighlighting)
  ])]
public class AllocationOfActivatorCreateInstanceAnalyzer : HeapAllocationAnalyzerBase<IInvocationExpression>
{
  private static readonly ClrTypeName ActivatorTypeName = new(typeof(Activator).FullName!);

  protected override void Run(
    IInvocationExpression invocationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var invokedReferenceExpression = invocationExpression.InvokedExpression.GetOperandThroughParenthesis() as IReferenceExpression;
    if (invokedReferenceExpression == null) return;

    var nameIdentifier = invokedReferenceExpression.NameIdentifier;
    if (nameIdentifier is not { Name: nameof(Activator.CreateInstance) }) return;

    var resolveResult = invokedReferenceExpression.Reference.Resolve();
    if (!resolveResult.ResolveErrorType.IsAcceptable) return;

    var method = resolveResult.DeclaredElement as IMethod;
    if (method is not { ContainingType: IClass { ShortName: nameof(Activator) } activatorType }) return;

    if (!activatorType.GetClrName().Equals(ActivatorTypeName)) return;

    if (invocationExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var typeParameters = method.TypeParameters;
    if (typeParameters.Count == 0)
    {
      // object o = Activator.CreateInstance(typeof(T));
      if (method.ReturnType.IsObject())
      {
        ReportAllocationInNetFramework();
      }
    }
    else if (typeParameters.Count == 1)
    {
      // T o = Activator.CreateInstance<T>();
      if (method.ReturnType.IsTypeParameterType())
      {
        var returnType = resolveResult.Substitution.Apply(method.ReturnType);
        if (!returnType.IsUnknownOrUnresolvedTypeElementType())
        {
          if (data.GetTargetRuntime() == TargetRuntime.NetCore)
          {
            CheckAllocationsInCore();
          }
          else
          {
            ReportAllocationInNetFramework();
          }
        }

        void CheckAllocationsInCore()
        {
          switch (returnType.Classify)
          {
            case TypeClassification.UNKNOWN:
            {
              var typeName = returnType.GetPresentableName(invocationExpression.Language, CommonUtils.DefaultTypePresentationStyle);

              consumer.AddHighlighting(new ObjectAllocationPossibleHighlighting(
                nameIdentifier, new RichText(
                  $"new instance creation if '{typeName}' type parameter will be substituted with the reference type")));
              break;
            }

            case TypeClassification.REFERENCE_TYPE:
            {
              var typeName = returnType.GetPresentableName(invocationExpression.Language, CommonUtils.DefaultTypePresentationStyle);

              consumer.AddHighlighting(new ObjectAllocationHighlighting(
                nameIdentifier, new RichText(
                  $"new '{typeName}' instance creation")));
              break;
            }
          }
        }
      }
    }

    return;

    void ReportAllocationInNetFramework()
    {
      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(
          nameIdentifier, "new instance creation or boxing of the value type"));
    }
  }
}