using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

// ReSharper disable RedundantExplicitParamsArrayCreation
// TODO: "fake" string concatenations between interpolated string handlers - do not allocates
// TODO: "fake" C# 11 utf-8 string concatenations
// TODO: string interpolation allocations (in the case of "default interpolation handler")
// TODO: string interpolations + custom non-struct handlers
// todo: string interpolations + awaits in holes?
// todo: string interpolations + FormattableString
// todo: string interpolation can compile into string.Concat

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  new[] {
    typeof(IReferenceExpression),
    typeof(IObjectCreationExpression),
    typeof(IArrayCreationExpression),
    typeof(IInvocationExpression),
    typeof(IForeachStatement),
    typeof(IAdditiveExpression),
    typeof(IAssignmentExpression),
    typeof(IElementAccessExpression),
    typeof(IConstructorInitializer),
    typeof(ICollectionElementInitializer)
  },
  HighlightingTypes = new[] {
    typeof(ObjectAllocationHighlighting),
    typeof(ObjectAllocationEvidentHighlighting),
    typeof(ObjectAllocationPossibleHighlighting),
    typeof(DelegateAllocationHighlighting)
  })]
public sealed class HeapAllocationAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (element)
    {
      // F(); when F is iterator
      case IInvocationExpression invocationExpression:
        CheckInvocationExpression(invocationExpression, consumer);
        return;

      // var xs = Iterator;
      case IReferenceExpression referenceExpression:
        CheckReferenceExpression(referenceExpression, consumer);
        return;

      // string s = "abc" + x + "def";
      // case IAdditiveExpression additiveExpression:
      //   CheckStringConcatenation(additiveExpression, consumer);
      //   return;

      // str += "abc";
      // case IAssignmentExpression { IsCompoundAssignment: true } assignmentExpression when IsStringConcatenation(assignmentExpression):
      //   consumer.AddHighlighting(
      //     new ObjectAllocationHighlighting(assignmentExpression.OperatorSign, "string concatenation"),
      //     assignmentExpression.OperatorSign.GetDocumentRange());
      //   break;

      // foreach (var x in xs); when xs.GetEnumerator() is ref-type
      // note: produces false-positive for LocalList<T>-produced IList<T>
      case IForeachStatement foreachStatement:
        CheckForeachDeclaration(foreachStatement, consumer);
        return;
    }
  }

  private static void CheckInvocationExpression([NotNull] IInvocationExpression invocationExpression, [NotNull] IHighlightingConsumer consumer)
  {
    var invokedExpression = invocationExpression.InvokedExpression;
    if (invokedExpression == null) return;

    var invocationReference = invocationExpression.InvocationExpressionReference.NotNull();

    var (declaredElement, _, resolveErrorType) = invocationReference.Resolve();
    if (resolveErrorType != ResolveErrorType.OK) return;

    var method = declaredElement as IMethod;
    if (method == null) return;

    if (method.IsIterator)
    {
      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(invocationExpression, "iterator method call"),
        invokedExpression.GetExpressionRange());
    }
    else if (method.ReturnType.Classify == TypeClassification.REFERENCE_TYPE)
    {
      var annotationsCache = invocationExpression.GetPsiServices().GetCodeAnnotationsCache();
      var linqTunnelAnnotationProvider = annotationsCache.GetProvider<LinqTunnelAnnotationProvider>();
      var pureAnnotationProvider = annotationsCache.GetProvider<PureAnnotationProvider>();

      if (pureAnnotationProvider.GetInfo(method) && linqTunnelAnnotationProvider.GetInfo(method))
      {
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(invocationExpression, "LINQ method call"),
          invokedExpression.GetExpressionRange());
      }
    }
  }

  private static void CheckReferenceExpression([NotNull] IReferenceExpression referenceExpression, [NotNull] IHighlightingConsumer consumer)
  {
    var (declaredElement, _) = referenceExpression.Reference.Resolve();

    switch (declaredElement)
    {
      case IProperty { Getter: { } getter }:
      {
        var languageService = referenceExpression.Language.LanguageServiceNotNull();

        var accessType = languageService.GetReferenceAccessType(referenceExpression.Reference);
        if (accessType == ReferenceAccessType.READ && getter.IsIterator)
        {
          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(referenceExpression, "iterator property access"),
            referenceExpression.NameIdentifier.GetDocumentRange());
        }

        break;
      }
    }
  }

  private static void CheckStringConcatenation([NotNull] IAdditiveExpression concatenation, [NotNull] IHighlightingConsumer consumer)
  {
    if (!IsStringConcatenation(concatenation)) return;

    // do not inspect inner concatenations
    var parent = concatenation.GetContainingParenthesizedExpression();
    var parentConcatenation = AdditiveExpressionNavigator.GetByLeftOperand(parent)
                              ?? AdditiveExpressionNavigator.GetByRightOperand(parent);
    if (parentConcatenation != null && IsStringConcatenation(parentConcatenation)) return;

    // collect all operands
    var allConstants = true;
    var concatenations = new List<IAdditiveExpression>();
    if (CollectStringConcatenation(concatenation, concatenations, ref allConstants)
        && !allConstants
        && concatenations.Count > 0)
    {
      var mostLeftConcatSign = concatenations
        .Select(x => x.OperatorSign.GetDocumentRange())
        .OrderBy(x => x.TextRange.StartOffset).First();

      var operandsCount = concatenations.Count + 1;
      var description = "string concatenation"
                        + (operandsCount <= 2 ? null : $" ({operandsCount} operands)")
                        + (operandsCount <= 4 ? null : " + params array allocation");

      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(concatenation, description),
        mostLeftConcatSign);
    }
  }

  private static bool CollectStringConcatenation(
    [NotNull] IAdditiveExpression concatenation, [NotNull] List<IAdditiveExpression> parts, ref bool allConstants)
  {
    var lhsOperand = concatenation.LeftOperand;
    var rhsOperand = concatenation.RightOperand;
    if (lhsOperand == null || rhsOperand == null ||
        concatenation.OperatorSign == null) return false;

    var lhsConstant = lhsOperand.ConstantValue;
    if (!lhsConstant.IsString() && !lhsConstant.IsPureNull(concatenation.Language))
    {
      allConstants = false;

      if (lhsOperand.GetOperandThroughParenthesis() is IAdditiveExpression left && IsStringConcatenation(concatenation))
      {
        if (!CollectStringConcatenation(left, parts, ref allConstants)) return false;
      }
    }

    var rhsConstant = rhsOperand.ConstantValue;
    if (!rhsConstant.IsString() && !rhsConstant.IsPureNull(concatenation.Language))
    {
      allConstants = false;

      if (rhsOperand.GetOperandThroughParenthesis() is IAdditiveExpression right && IsStringConcatenation(concatenation))
      {
        if (!CollectStringConcatenation(right, parts, ref allConstants)) return false;
      }
    }

    parts.Add(concatenation);
    return true;
  }

  private static bool IsStringConcatenation([NotNull] IAdditiveExpression concatenation)
  {
    var leftOperand = concatenation.LeftOperand;
    if (leftOperand == null) return false;

    var rightOperand = concatenation.RightOperand;
    if (rightOperand == null) return false;

    return concatenation.OperatorReference.IsStringConcatOperator();
  }

  private static bool IsStringConcatenation([NotNull] IAssignmentExpression concatenation)
  {
    var sourceOperand = concatenation.Source;
    if (sourceOperand == null) return false;

    var destinationOperand = concatenation.Dest;
    if (destinationOperand == null) return false;

    return concatenation.OperatorReference.IsStringConcatOperator();
  }

  private static void CheckForeachDeclaration([NotNull] IForeachStatement foreachStatement, [NotNull] IHighlightingConsumer consumer)
  {
    var collection = foreachStatement.Collection;

    var collectionType = collection?.Type() as IDeclaredType;
    if (collectionType == null || collectionType.IsUnknown) return;

    // no allocations because of compiler support (just like arrays)
    if (collectionType.IsString()) return;

    var typeElement = collectionType.GetTypeElement();
    if (typeElement == null) return;

    // search for GetEnumerator() method
    var symbolTable = ResolveUtil.GetSymbolTableByTypeElement(typeElement, SymbolTableMode.FULL, typeElement.Module);

    foreach (var symbolInfo in symbolTable.GetSymbolInfos("GetEnumerator"))
    {
      var method = symbolInfo.GetDeclaredElement() as IMethod;
      if (method == null) continue;

      if (!CSharpDeclaredElementUtil.IsForeachEnumeratorPatternMember(method)) continue;

      // with ref-return
      if (method.ReturnType.Classify == TypeClassification.REFERENCE_TYPE)
      {
        DocumentRange range;
        var inToken = collection.GetPreviousMeaningfulToken();
        if (inToken != null && inToken.GetTokenType().IsKeyword)
        {
          range = inToken.GetDocumentRange();
        }
        else
        {
          range = collection.GetExpressionRange();
        }

        var highlighting = new ObjectAllocationPossibleHighlighting(
          foreachStatement, "enumerator allocation (except iterators or collection with cached enumerator)");
        consumer.AddHighlighting(highlighting, range);
      }

      break;
    }
  }

  // todo: cache in problem analyzer data
  private static bool IsCachedEmptyArrayAvailable([NotNull] ITreeNode context)
  {
    if (!context.IsCSharp6Supported()) return false;

    var systemArrayType = context.GetPredefinedType().Array;
    var classType = systemArrayType.GetTypeElement() as IClass;
    if (classType == null) return false;

    foreach (var typeMember in classType.EnumerateMembers("Empty", caseSensitive: false))
    {
      var method = typeMember as IMethod;
      if (method?.Parameters.Count == 0
          && method.TypeParameters.Count == 1
          && method.GetAccessRights() == AccessRights.PUBLIC) return true;
    }

    return false;
  }
}