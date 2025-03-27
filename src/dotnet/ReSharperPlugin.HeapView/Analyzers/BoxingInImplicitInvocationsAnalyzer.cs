using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve.ExtensionMethods;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [
    typeof(IForeachStatement),
    typeof(IAwaitExpression),
    typeof(ICollectionElementInitializer),
    typeof(ITupleExpression),
    typeof(IDeconstructionPatternClause),
    typeof(IVarDeconstructionPattern),
    typeof(IDeclarationExpression)
  ],
  HighlightingTypes = [
    typeof(BoxingAllocationHighlighting),
    typeof(PossibleBoxingAllocationHighlighting)
  ])]
public class BoxingInImplicitInvocationsAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (element)
    {
      // foreach (var x in structTypeWithExtensionGetEnumerator) { }
      case IForeachStatement foreachStatement:
        CheckExtensionGetEnumeratorInvocation(foreachStatement, data, consumer);
        break;

      // new T { e } + extension Add() this parameter boxing
      case ICollectionElementInitializer collectionElementInitializer:
        CheckExtensionCollectionAddInvocation(collectionElementInitializer, data, consumer);
        break;

      // await e + extension GetAwaiter() method
      case IAwaitExpression awaitExpression:
        CheckExtensionGetAwaiterInvocation(awaitExpression, data, consumer);
        break;

      // (_, _) = e; + extension Deconstruct() this parameter boxing
      case ITupleExpression tupleExpression:
        CheckExtensionDeconstructionInvocation(tupleExpression, data, consumer);
        break;

      // is StructType (_, _) + extension Deconstruct() this parameter boxing
      case IDeconstructionPatternClause deconstructionPatternClause:
        CheckExtensionDeconstructionInvocation(deconstructionPatternClause, data, consumer);
        break;

      // is var (_, _) + extension Deconstruct() this parameter boxing
      case IVarDeconstructionPattern varDeconstructionPattern:
        CheckExtensionDeconstructionInvocation(varDeconstructionPattern, data, consumer);
        break;

      // var (_, _) = e; + extension Deconstruct() this parameter boxing
      case IDeclarationExpression declarationExpression:
        CheckExtensionDeconstructionInvocation(declarationExpression, data, consumer);
        break;
    }
  }

  private static void CheckExtensionDeconstructionInvocation(
    IDeconstructionPatternClause deconstructionPatternClause, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var recursivePattern = RecursivePatternNavigator.GetByDeconstructionPatternClause(deconstructionPatternClause);
    if (recursivePattern == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(deconstructionPatternClause.DeconstructionReference);
    if (targetType == null) return;

    var sourceExpressionType = recursivePattern.GetSourceExpressionType(new UniversalContext(recursivePattern));

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, deconstructionPatternClause,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target, isMethodGroupConversion: false),
      data, consumer);
  }

  private static void CheckExtensionDeconstructionInvocation(
    IVarDeconstructionPattern varDeconstructionPattern, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var designation = varDeconstructionPattern.Designation;
    if (designation == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(designation.DeconstructionReference);
    if (targetType == null) return;

    var dispatchType = varDeconstructionPattern.GetDispatchType();

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      dispatchType, targetType, varDeconstructionPattern.VarKeyword,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target, isMethodGroupConversion: false),
      data, consumer);
  }

  private static void CheckExtensionDeconstructionInvocation(
    IDeclarationExpression declarationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var designation = declarationExpression.Designation as IParenthesizedVariableDesignation;
    if (designation == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(designation.DeconstructionReference);
    if (targetType == null) return;

    var sourceExpressionType = declarationExpression.GetSourceExpressionType(new UniversalContext(declarationExpression));

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, declarationExpression.TypeDesignator,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target, isMethodGroupConversion: false),
      data, consumer);
  }

  private static void CheckExtensionDeconstructionInvocation(
    ITupleExpression tupleExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (!tupleExpression.IsLValueTupleExpression()) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(tupleExpression.DeconstructionReference);
    if (targetType == null) return;

    var dispatchType = tupleExpression.GetSourceExpressionType(new UniversalContext(tupleExpression));

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      dispatchType, targetType, tupleExpression,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target, isMethodGroupConversion: false),
      data, consumer);
  }

  private static void CheckExtensionCollectionAddInvocation(
    ICollectionElementInitializer collectionElementInitializer, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var collectionInitializer = CollectionInitializerNavigator.GetByElementInitializer(collectionElementInitializer);
    if (collectionInitializer == null) return;

    var invocationReference = collectionElementInitializer.Reference;
    if (invocationReference == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(invocationReference);
    if (targetType == null) return;

    var sourceExpressionType = collectionInitializer.GetConstructedType();

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, collectionElementInitializer,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target, isMethodGroupConversion: false),
      data, consumer);
  }

  private static void CheckExtensionGetEnumeratorInvocation(
    IForeachStatement foreachStatement, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var foreachHeader = foreachStatement.ForeachHeader;
    if (foreachHeader == null) return;

    var collection = foreachHeader.Collection;
    if (collection == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(foreachStatement.GetEnumeratorReference);
    if (targetType == null) return;

    var sourceExpressionType = collection.Type();

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, foreachHeader.InKeyword,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target, isMethodGroupConversion: false),
      data, consumer);
  }

  private static void CheckExtensionGetAwaiterInvocation(
    IAwaitExpression awaitExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var taskExpression = awaitExpression.Task;
    if (taskExpression == null) return;

    var getAwaiterReference = awaitExpression.GetAwaiterReference;
    if (getAwaiterReference == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(getAwaiterReference);
    if (targetType == null) return;

    var sourceExpressionType = taskExpression.Type();

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, awaitExpression.AwaitKeyword,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target, isMethodGroupConversion: false),
      data, consumer);
  }

  [Pure]
  private static IType? FindExtensionMethodWithReferenceTypeThisParameter(IReference deconstructionReference)
  {
    var resolveResult = deconstructionReference.Resolve();
    if (resolveResult.ResolveErrorType.IsAcceptable
        && resolveResult.Result.IsExtensionMethodInvocation()
        && resolveResult.DeclaredElement is IMethod { IsExtensionMethod: true } extensionsMethod)
    {
      foreach (var parameter in extensionsMethod.Parameters)
      {
        return resolveResult.Substitution[parameter.Type];
      }
    }

    return null;
  }
}
