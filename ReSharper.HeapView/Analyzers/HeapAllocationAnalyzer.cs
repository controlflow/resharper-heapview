using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
#if RESHARPER8
using JetBrains.ReSharper.Daemon.Stages;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

namespace JetBrains.ReSharper.HeapView.Analyzers
{
  [ElementProblemAnalyzer(
    typeof(IReferenceExpression),
    typeof(IObjectCreationExpression),
    typeof(IAnonymousObjectCreationExpression),
    typeof(IArrayCreationExpression),
    typeof(IInvocationExpression),
    typeof(IArrayInitializer),
    typeof(IForeachStatement),
    typeof(IAdditiveExpression),
    typeof(IAssignmentExpression),
    HighlightingTypes = new[] {
      typeof(ObjectAllocationHighlighting),
      typeof(DelegateAllocationHighlighting)
    })]
  public sealed class HeapAllocationAnalyzer : ElementProblemAnalyzer<ITreeNode>
  {
    protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      // var t = new object();
      var objectCreation = element as IObjectCreationExpression;
      if (objectCreation != null)
      {
        CheckObjectCreation(objectCreation, consumer);
        return;
      }

      // var t = new { Foo = 123 };
      var anonymousObjectCreation = element as IAnonymousObjectCreationExpression;
      if (anonymousObjectCreation != null)
      {
        CheckAnonymousObjectCreation(anonymousObjectCreation, consumer);
        return;
      }

      // var xs = new[] {1, 2, 3};
      var arrayCreation = element as IArrayCreationExpression;
      if (arrayCreation != null)
      {
        CheckArrayCreation(arrayCreation, consumer);
        return;
      }

      // int[] xs = {1, 2, 3};
      var arrayInitializer = element as IArrayInitializer;
      if (arrayInitializer != null)
      {
        CheckArrayInitializer(arrayInitializer, consumer);
        return;
      }

      // F(); when F(params T[] xs);
      // F(); when F is iterator
      var invocation = element as IInvocationExpression;
      if (invocation != null)
      {
        CheckInvocation(invocation, consumer);
        return;
      }

      // Action f = F;
      // var xs = Iterator;
      var referenceExpression = element as IReferenceExpression;
      if (referenceExpression != null)
      {
        CheckReferenceExpression(referenceExpression, consumer);
        return;
      }

      // string s = "abc" + x + "def";
      var additiveExpression = element as IAdditiveExpression;
      if (additiveExpression != null)
      {
        CheckStringConcatenation(additiveExpression, consumer);
        return;
      }

      // str += "abc";
      var assignmentExpression = element as IAssignmentExpression;
      if (assignmentExpression != null
          && assignmentExpression.IsCompoundAssignment
          && IsStringConcatenation(assignmentExpression))
      {
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(assignmentExpression.OperatorSign, "string concatenation"),
          assignmentExpression.OperatorSign.GetDocumentRange());
      }

      // foreach (var x in xs); when xs.GetEnumerator() is ref-type
      // note: produces false-positive for LocalList<T>-produced IList<T>
      var foreachStatement = element as IForeachStatement;
      if (foreachStatement != null)
      {
        CheckForeachDeclaration(foreachStatement, consumer);
        // ReSharper disable once RedundantJumpStatement
        return;
      }
    }

    private static void CheckObjectCreation([NotNull] IObjectCreationExpression objectCreation, [NotNull] IHighlightingConsumer consumer)
    {
      var typeReference = objectCreation.TypeReference;
      if (typeReference == null) return;

      if (objectCreation.GetContainingNode<IAttribute>() != null) return;

      var newKeyword = objectCreation.NewKeyword.NotNull();

      var typeElement = typeReference.Resolve().DeclaredElement as ITypeElement;
      var typeParameter = typeElement as ITypeParameter;
      if (typeElement is IClass || (typeParameter != null && typeParameter.IsClassType))
      {
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(newKeyword, "reference type creation"),
          newKeyword.GetDocumentRange());
      }
      else if (typeParameter != null && !typeParameter.IsValueType)
      {
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(newKeyword, "possible reference type creation"),
          newKeyword.GetDocumentRange());
      }
    }

    private static void CheckAnonymousObjectCreation([NotNull] ICreationExpression objectCreation, [NotNull] IHighlightingConsumer consumer)
    {
      var newKeyword = objectCreation.NewKeyword.NotNull();

      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(newKeyword, "reference type instantiation"),
        newKeyword.GetDocumentRange());
    }

    private static void CheckArrayCreation([NotNull] IArrayCreationExpression arrayCreation, [NotNull] IHighlightingConsumer consumer)
    {
      if (arrayCreation.GetContainingNode<IAttribute>() == null)
      {
        var newKeyword = arrayCreation.NewKeyword.NotNull();

        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(newKeyword, "array creation"),
          newKeyword.GetDocumentRange());
      }
    }

    private static void CheckArrayInitializer([NotNull] IArrayInitializer arrayInitializer, [NotNull] IHighlightingConsumer consumer)
    {
      ITreeNode start = null, end = null;
      var variableDeclaration = LocalVariableDeclarationNavigator.GetByInitial(arrayInitializer);
      if (variableDeclaration != null && variableDeclaration.EquivalenceSign() != null)
      {
        start = variableDeclaration.NameIdentifier;
        end = variableDeclaration.EquivalenceSign();
      }
      else
      {
        var fieldDeclaration = FieldDeclarationNavigator.GetByInitial(arrayInitializer);
        if (fieldDeclaration != null && fieldDeclaration.EquivalenceSign != null)
        {
          start = fieldDeclaration.NameIdentifier;
          end = fieldDeclaration.EquivalenceSign;
        }
      }

      if (start != null && end != null)
      {
        var endOffset = end.GetDocumentRange().TextRange.EndOffset;
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(arrayInitializer, "array instantiation"),
          start.GetDocumentRange().SetEndTo(endOffset));
      }
    }

    private static void CheckInvocation([NotNull] IInvocationExpression invocation, [NotNull] IHighlightingConsumer consumer)
    {
      var reference = invocation.InvocationExpressionReference.NotNull("reference != null");
      var invokedExpression = invocation.InvokedExpression;

      var declaredElement = reference.Resolve().DeclaredElement;
      var parametersOwner = declaredElement as IParametersOwner;
      if (parametersOwner != null)
      {
        var parameters = parametersOwner.Parameters;
        if (parameters.Count > 0)
        {
          var lastParameter = parameters[parameters.Count - 1];
          if (lastParameter.IsParameterArray)
          {
            ICSharpExpression paramsArgument = null;
            foreach (var argument in invocation.ArgumentsEnumerable)
            {
              var parameter = argument.MatchingParameter;
              if (parameter != null && Equals(parameter.Element, lastParameter))
              {
                var parameterType = parameter.Substitution[parameter.Element.Type];
                var convertedTo = argument.GetImplicitlyConvertedTo();
                if (!convertedTo.IsUnknown)
                {
                  if (convertedTo.Equals(parameterType)) return;
                  paramsArgument = argument.Value;
                }

                break;
              }
            }

            var anchor = invokedExpression as IReferenceExpression ?? paramsArgument ?? invokedExpression;
            consumer.AddHighlighting(
              new ObjectAllocationHighlighting(
                anchor, string.Format("parameters array '{0}' creation", lastParameter.ShortName)),
              anchor.GetExpressionRange());
          }
        }
      }

      var method = declaredElement as IMethod;
      if (method != null)
      {
        if (method.IsIterator) // todo: may be perf issue
        {
          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(invocation, "iterator method call"),
            invokedExpression.GetExpressionRange());
        }
        else if (method.ReturnType.Classify == TypeClassification.REFERENCE_TYPE)
        {
          var annotationsCache = invocation.GetPsiServices().GetCodeAnnotationsCache();
          if (annotationsCache.IsPure(method) && annotationsCache.GetLinqTunnel(method))
          {
            consumer.AddHighlighting(
              new ObjectAllocationHighlighting(invocation, "LINQ method call"),
              invokedExpression.GetExpressionRange());
          }
        }
      }
    }

    private static void CheckReferenceExpression(
      [NotNull] IReferenceExpression referenceExpression, [NotNull] IHighlightingConsumer consumer)
    {
      var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement;

      var property = declaredElement as IProperty;
      if (property != null && property.Getter != null && property.Getter.IsIterator)
      {
        var service = referenceExpression.Language.LanguageServiceNotNull();
        var accessType = service.GetReferenceAccessType(referenceExpression.Reference);
        if (accessType == ReferenceAccessType.READ)
        {
          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(referenceExpression, "iterator property access"),
            referenceExpression.NameIdentifier.GetDocumentRange());
        }
      }

      if (declaredElement is IMethod)
      {
        var type = referenceExpression.GetImplicitlyConvertedTo() as IDeclaredType;
        if (type != null && !type.IsUnknown && type.GetTypeElement() is IDelegate)
        {
          consumer.AddHighlighting(
            new DelegateAllocationHighlighting(referenceExpression, "from method group"),
            referenceExpression.NameIdentifier.GetDocumentRange());
        }
      }
    }

    private static void CheckStringConcatenation(
      [NotNull] IAdditiveExpression concatenation, [NotNull] IHighlightingConsumer consumer)
    {
      if (!IsStringConcatenation(concatenation)) return;

      // do not inspect inner concatenations
      var parent = concatenation.GetContainingParenthesizedExpression();
      var parentConcatention = AdditiveExpressionNavigator.GetByLeftOperand(parent)
                               ?? AdditiveExpressionNavigator.GetByRightOperand(parent);
      if (parentConcatention != null && IsStringConcatenation(parentConcatention)) return;

      // collect all operands
      var allConstants = true;
      var concatenations = new List<IAdditiveExpression>();
      if (CollectStringConcatenation(concatenation, concatenations, ref allConstants)
          && !allConstants && concatenations.Count > 0)
      {
        var mostLeftConcatSign = concatenations
          .Select(x => x.OperatorSign.GetDocumentRange())
          .OrderBy(x => x.TextRange.StartOffset).First();

        var operandsCount = concatenations.Count + 1;
        var description = "string concatenation"
          + (operandsCount <= 2 ? null : string.Format(" ({0} operands)", operandsCount))
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
        var left = lhsOperand.GetOperandThroughParenthesis() as IAdditiveExpression;
        if (left != null && IsStringConcatenation(concatenation))
        {
          if (!CollectStringConcatenation(left, parts, ref allConstants)) return false;
        }
      }

      var rhsConstant = rhsOperand.ConstantValue;
      if (!rhsConstant.IsString() && !rhsConstant.IsPureNull(concatenation.Language))
      {
        allConstants = false;
        var right = rhsOperand.GetOperandThroughParenthesis() as IAdditiveExpression;
        if (right != null && IsStringConcatenation(concatenation))
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
      var rightOperand = concatenation.RightOperand;
      if (leftOperand == null || rightOperand == null) return false;

      return IsStringConcatOperatorReference(concatenation.OperatorReference);
    }

    private static bool IsStringConcatenation([NotNull] IAssignmentExpression concatenation)
    {
      var sourceOperand = concatenation.Source;
      var destOperand = concatenation.Dest;
      if (sourceOperand == null || destOperand == null) return false;

      return IsStringConcatOperatorReference(concatenation.OperatorReference);
    }

    private static bool IsStringConcatOperatorReference([CanBeNull] IReference reference)
    {
      if (reference == null) return false;

      var @operator = reference.Resolve().DeclaredElement as IOperator;
      if (@operator == null || !@operator.IsPredefined) return false;

      var parameters = @operator.Parameters;
      if (parameters.Count != 2) return false;

      IType lhsType = parameters[0].Type, rhsType = parameters[1].Type;
      if (lhsType.IsString()) return rhsType.IsString() || rhsType.IsObject();
      if (rhsType.IsString()) return lhsType.IsString() || lhsType.IsObject();

      return false;
    }

    private static void CheckForeachDeclaration(
      [NotNull] IForeachStatement foreachStatement, [NotNull] IHighlightingConsumer consumer)
    {
      var collection = foreachStatement.Collection;
      if (collection == null) return;

      var collectionType = collection.Type() as IDeclaredType;
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
            range = inToken.GetDocumentRange();
          else
            range = collection.GetExpressionRange();

          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(foreachStatement,
              "possible enumerator allocation (except iterators or collection with cached enumerator)"),
            range);
        }

        break;
      }
    }
  }
}