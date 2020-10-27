using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve.ExtensionMethods;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.Stages;

/*
namespace JetBrains.ReSharper.HeapView.Analyzers
{
  // todo: do not show in initializers?

  [ElementProblemAnalyzer(typeof(ICSharpExpression), HighlightingTypes = new []{ typeof(StructCopyHighlighting) })]
  public sealed class StructCopyAnalyzer : ElementProblemAnalyzer<ICSharpExpression>
  {
    protected override void Run(ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      if (IsSemanticlessExpression(expression)) return;

      var declaredType = expression.Type() as IDeclaredType;
      if (declaredType == null) return;

      if (IsFatValueType(declaredType)) return;

      // check if expression is argument passed by ref/out-parameter (should be classified as variable)
      var argument = CSharpArgumentNavigator.GetByValue(expression);
      if (argument != null)
      {
        var argumentMode = argument.Mode;
        if (argumentMode != null)
        {
          if (argumentMode == CSharpTokenType.REF_KEYWORD || argumentMode == CSharpTokenType.OUT_KEYWORD) return;
        }
      }

      var referenceExpression = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
      if (referenceExpression != null)
      {
        if (expression.IsClassifiedAsVariable) return;

        var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
        if (invocationExpression != null)
        {
          var invocationReference = invocationExpression.Reference;
          if (invocationReference == null) return;

          var resolveResult = invocationReference.Resolve();
          if (resolveResult.ResolveErrorType != ResolveErrorType.OK) return;

          if (resolveResult.Result.IsExtensionMethod())
          {
            var description = string.Format("extension method invocation over value of type '{0}'", declaredType.GetLongPresentableName(expression.Language));

            var highlighting = new StructCopyHighlighting(expression, description);
            consumer.AddHighlighting(
              highlighting, referenceExpression.NameIdentifier.GetDocumentRange());
          }

          return;
        }
        else
        {
          // property access
        }
      }

      var elementAccessExpression = ElementAccessExpressionNavigator.GetByOperand(expression);
      if (elementAccessExpression != null)
      {
        if (expression.IsClassifiedAsVariable) return;

        consumer.AddHighlighting(
          new StructCopyHighlighting(expression, "indexer access over "),
          elementAccessExpression.LBracket.GetDocumentRange());
        return;
      }

      consumer.AddHighlighting(
        new StructCopyHighlighting(expression, "of type " + declaredType.GetLongPresentableName(expression.Language)),
        expression.GetExpressionRange());
    }

    private static bool IsSemanticlessExpression([NotNull] ICSharpExpression expression)
    {
      if (expression is IParenthesizedExpression) return true;
      if (expression is IUncheckedExpression) return true;
      if (expression is ICheckedExpression) return true;

      return false;
    }

    private static bool IsFatValueType([NotNull] IDeclaredType declaredType)
    {
      if (declaredType.Classify != TypeClassification.VALUE_TYPE) return true;

      if (declaredType.IsVoid()) return false;
      if (declaredType.IsSimplePredefined()) return false;
      //if (declaredType.IsEnumType()) return false;

      // todo: what about nullable types?

      return false;
    }

    private static bool ClassifiedAsVariable([NotNull] ICSharpExpression expression)
    {
      var referenceExpression = expression as IReferenceExpression;
      if (referenceExpression != null)
      {
        var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement;

        if (referenceExpression.IsPartOfConditionalAccess()) return false;

        if (declaredElement is ILocalVariable) return true;
        if (declaredElement is IParameter) return true;

        var field = declaredElement as IField;
        if (field != null)
          return field.IsField && IsFieldClassifiedAsVariable(referenceExpression, field);

        var eventMember = declaredElement as IEvent;
        if (eventMember != null)
          return eventMember.IsFieldLikeEvent && IsFieldClassifiedAsVariable(referenceExpression, eventMember);

        return false;
      }

      var elementAccessExpression = expression as IElementAccessExpression;
      if (elementAccessExpression != null)
      {
        if (elementAccessExpression.IsPartOfConditionalAccess()) return false;

        var operand = elementAccessExpression.Operand;
        if (operand != null)
        {
          var expressionType = operand.GetExpressionType();

          var arrayType = elementAccessExpression.GetPredefinedType().Array;
          var conversionRule = elementAccessExpression.GetTypeConversionRule();
          if (expressionType.IsImplicitlyConvertibleTo(arrayType, conversionRule)) return true;
          if (expressionType is IPointerType) return true;
        }

        return false;
      }

      var thisExpression = expression as IThisExpression;
      if (thisExpression != null)
      {
        var memberDeclaration = thisExpression.GetContainingTypeMemberDeclaration();
        if (memberDeclaration == null || memberDeclaration.IsStatic) return false;

        var typeDeclaration = memberDeclaration.GetContainingTypeDeclaration();
        return typeDeclaration is IStructDeclaration;
      }

      var parenthesizedExpression = expression as IParenthesizedExpression;
      if (parenthesizedExpression != null)
        return ClassifiedAsVariable(parenthesizedExpression.Expression);

      var checkedExpression = expression as ICheckedExpression;
      if (checkedExpression != null)
        return ClassifiedAsVariable(checkedExpression.Operand);

      var uncheckedExpression = expression as IUncheckedExpression;
      if (uncheckedExpression != null)
        return ClassifiedAsVariable(uncheckedExpression.Operand);

      if (expression is IUnsafeCodePointerIndirectionExpression) return true;
      if (expression is IUnsafeCodePointerAccessExpression) return true;

      return false;
    }

    private static bool IsFieldClassifiedAsVariable(
      [NotNull] IReferenceExpression referenceExpression, [NotNull] ITypeMember field)
    {
      var containingType = field.GetContainingType();
      if (containingType is IStruct && !field.IsStatic)
      {
        var qualifierExpression = referenceExpression.QualifierExpression;
        if (qualifierExpression != null)
          return ClassifiedAsVariable(qualifierExpression);
      }

      return true;
    }

  }
}
*/
