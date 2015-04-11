using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
#if RESHARPER8
using JetBrains.ReSharper.Daemon.Stages;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

namespace JetBrains.ReSharper.HeapView.Analyzers
{
  [ElementProblemAnalyzer(typeof(IReferenceExpression), HighlightingTypes = new[] { typeof(SlowDelegateCreationHighlighting) })]
  public class SlowDelegateCreationAnalyzer : ElementProblemAnalyzer<ICSharpExpression>
  {
    protected override void Run(
      ICSharpExpression element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      var referenceExpression = element as IReferenceExpression;
      if (referenceExpression == null) return;

      if (referenceExpression.Parent is IInvocationExpression) return;

      var resolveResult = referenceExpression.Reference.Resolve();
      var method = resolveResult.DeclaredElement as IMethod;
      if (method != null)
      {
        CheckMethodGroup(referenceExpression, method, consumer);
      }
    }

    private static void CheckMethodGroup(
      [NotNull] IReferenceExpression methodReference,
      [NotNull] IMethod method, [NotNull] IHighlightingConsumer consumer)
    {
      string message = null;
      var methodType = method.GetContainingType();
      if (methodType is IInterface)
      {
        message = string.Format("from interface '{0}' method", methodType.ShortName);
        var highlighting = new SlowDelegateCreationHighlighting(methodReference, message);
        consumer.AddHighlighting(highlighting, methodReference.GetExpressionRange());
        return;
      }

      // there is not lags if method is instance method
      if (!method.IsStatic)
      {
        var qualifierExpression = methodReference.QualifierExpression;
        if (qualifierExpression != null && qualifierExpression.IsClassifiedAsVariable) return;
      }

      var substitution = methodReference.Reference.Resolve().Result.Substitution;
      var typeParameters = new JetHashSet<ITypeParameter>();

      // collect all the type parameters from the method reference
      if (!substitution.IsEmpty())
      {
        foreach (var typeParameter in substitution.Domain)
        {
          var substitutionType = substitution.Apply(typeParameter);
          var targs = TypeParametersCollectingVisitor.Collect(substitutionType);
          typeParameters.UnionWith(targs);
        }
      }

      // get the delegate creation owner type, if member is not static
      var delegateCreationMember = methodReference.GetContainingTypeMemberDeclaration();
      if (delegateCreationMember == null) return;

      var typeMember = delegateCreationMember.DeclaredElement;
      if (typeMember == null) return;

      ITypeElement delegateCreationOwnerType = null;
      if (!typeMember.IsStatic)
      {
        delegateCreationOwnerType = typeMember.GetContainingType();
      }

      // look for implicit qualification with the type parameters
      ITypeElement lastType = null;
      for (var qualifier = methodReference; qualifier != null; qualifier = qualifier.QualifierExpression as IReferenceExpression)
      {
        if (qualifier.IsClassifiedAsVariable)
        {
          lastType = null;
          break;
        }

        var resolveResult = qualifier.Reference.Resolve();
        lastType = resolveResult.DeclaredElement as ITypeElement;
      }

      if (lastType != null)
      {
        for (var hidden = lastType.GetContainingType(); hidden != null; hidden = hidden.GetContainingType())
        {
          if (hidden.TypeParameters.Count > 0)
          {
            foreach (var typeParameter in hidden.TypeParameters)
              typeParameters.Add(typeParameter);
          }
        }
      }

      foreach (var parameter in typeParameters)
      {
        if (parameter.IsValueType) continue;

        if (delegateCreationOwnerType != null &&
            delegateCreationOwnerType.TypeParameters.Contains(parameter)) continue;

        if (message == null) message = "method group parametrized with type parameter ";

        if (parameter.OwnerType != null)
        {
          message += string.Format("'{0}' of type '{1}'", parameter.ShortName, parameter.OwnerType.ShortName);
        }
        else if (parameter.OwnerMethod != null)
        {
          message += string.Format("'{0}' of method '{1}'", parameter.ShortName, parameter.OwnerMethod.ShortName);
        }
      }

      if (message != null)
      {
        var highlighting = new SlowDelegateCreationHighlighting(methodReference, message);
        consumer.AddHighlighting(highlighting, methodReference.GetExpressionRange());
      }
    }
  }
}