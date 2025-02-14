using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;
using JetBrains.Util.DataStructures.Specialized;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(IClassLikeDeclaration) ],
  HighlightingTypes = [ typeof(PossibleBoxingAllocationHighlighting) ])]
public class BoxingInDefaultMembersAnalyzer : HeapAllocationAnalyzerBase<IClassLikeDeclaration>
{
  protected override bool ShouldRun(IFile file, ElementProblemAnalyzerData data)
  {
    return file.IsDefaultInterfaceImplementationSupported();
  }

  protected override void Run(
    IClassLikeDeclaration declaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (declaration.DeclaredElement is not IStruct structType) return;

    var extendsList = declaration.ExtendsList;
    if (extendsList == null) return;

    using var visitedInterfaceTypes = VisitedTypesPool.Allocate();
    using var defaultMembers = DefaultMembersPool.Allocate();

    PooledHashSet<OverridableMemberInstance>? implementedMembers = null;
    try
    {
      foreach (var typeUsage in extendsList.ExtendedInterfacesEnumerable)
      {
        if (typeUsage is not IUserTypeUsage
            {
              ScalarTypeName: { NameIdentifier: { } typeNameIdentifier, Reference: { } reference }
            })
          continue;

        if (reference.Resolve() is not (IInterface interfaceType, var substitution))
          continue;

        defaultMembers.Clear();

        var extendedType = TypeFactory.CreateType(interfaceType, substitution, NullableAnnotation.Unknown);

        if (visitedInterfaceTypes.Add(extendedType))
        {
          AppendDefaultMembersFrom(interfaceType, substitution);
        }

        foreach (var superType in extendedType.GetAllSuperTypes())
        {
          if (superType is (IInterface superInterface, var superSubstitution) && visitedInterfaceTypes.Add(superType))
          {
            AppendDefaultMembersFrom(superInterface, superSubstitution);
          }
        }

        if (defaultMembers.Count > 0)
        {
          implementedMembers ??= ComputeAllImplementedMembers(structType);

          defaultMembers.ExceptWith(implementedMembers);

          if (defaultMembers.Count > 0)
          {
            ReportNotImplementedDefaultMembers(defaultMembers, structType, extendedType, typeNameIdentifier, consumer);
          }
        }

        continue;

        void AppendDefaultMembersFrom(IInterface iface, ISubstitution sub)
        {
          foreach (var defaultMember in GetAllInstanceDefaultMembers(iface, data))
          {
            defaultMembers.Add(new OverridableMemberInstance(defaultMember, sub));
          }
        }
      }
    }
    finally
    {
      implementedMembers?.Dispose();
    }
  }

  private static void ReportNotImplementedDefaultMembers(
    HashSet<OverridableMemberInstance> defaultMembers, IStruct structType, IDeclaredType superInterfaceType,
    ICSharpIdentifier implementedInterfaceTypeName, IHighlightingConsumer consumer)
  {
    var kindAndName = DeclaredElementPresenter.Format(CSharpLanguage.Instance, DeclaredElementPresenter.KIND_QUOTED_NAME_PRESENTER, structType);

    var richText = new RichText();
    richText
      .Append(kindAndName.Clone().Capitalize())
      .AppendLine(" do not provides the implementations for the following interface members with default bodies:");

    foreach (var defaultMember in defaultMembers)
    {
      richText
        .Append("  ")
        .Append(DeclaredElementPresenter.Format(CSharpLanguage.Instance, MemberWithInterfaceQualificationPresenter, defaultMember))
        .AppendLine();
    }

    richText.AppendLine();
    richText.Append("Using the default implementations of the interface members may result in boxing of the ");
    richText.Append(kindAndName);
    richText.Append(" values at runtime in generic code (");
    richText.Append("where", DeclaredElementPresenterTextStyles.Generic[DeclaredElementPresentationPartKind.Keyword]);
    richText.Append(' ');
    richText.Append("T", DeclaredElementPresenterTextStyles.Generic[DeclaredElementPresentationPartKind.Type]);
    richText.Append(" : ");
    richText.Append(superInterfaceType.GetPresentableName(implementedInterfaceTypeName.Language, CommonUtils.DefaultTypePresentationStyle));
    richText.Append(")");

    consumer.AddHighlighting(
      new PossibleBoxingAllocationHighlighting(implementedInterfaceTypeName, richText));
  }

  private static readonly DeclaredElementPresenterStyle MemberWithInterfaceQualificationPresenter = new()
  {
    ShowName = NameStyle.SHORT,
    ShowParameterTypes = true,
    ShowParameterNames = false,
    ShowTypesQualified = false,
    ShowExplicitInterfaceQualification = true,
    ShowType = TypeStyle.DEFAULT
  };

  private static readonly ObjectPool<PooledHashSet<IType>> VisitedTypesPool = PooledHashSet<IType>.CreatePool(TypeEqualityComparer.Default);
  private static readonly ObjectPool<PooledHashSet<OverridableMemberInstance>> DefaultMembersPool = PooledHashSet<OverridableMemberInstance>.CreatePool();

  private static readonly Key<Dictionary<IInterface, IReadOnlyList<IOverridableMember>>> DefaultMembersInInterfaceKey = new(nameof(DefaultMembersInInterfaceKey));

  [Pure]
  private static PooledHashSet<OverridableMemberInstance> ComputeAllImplementedMembers(IStruct structType)
  {
    var implementedMembers = DefaultMembersPool.Allocate();
    try
    {
      foreach (var typeMember in structType.GetMembers())
      {
        if (typeMember is IOverridableMember { IsStatic: false } overridableMember)
        {
          foreach (var implementedMember in overridableMember.GetAllSuperMembers())
          {
            if (implementedMember.Member.ContainingType is IInterface)
            {
              implementedMembers.Add(implementedMember);
            }
          }
        }
      }
    }
    catch
    {
      implementedMembers.Dispose();
      throw;
    }

    return implementedMembers;
  }

  [Pure]
  private static IReadOnlyList<IOverridableMember> GetAllInstanceDefaultMembers(
    IInterface interfaceElement, ElementProblemAnalyzerData data)
  {
    var cache = data.GetOrCreateDataUnderLock(DefaultMembersInInterfaceKey,
      static () => new Dictionary<IInterface, IReadOnlyList<IOverridableMember>>(
        DeclaredElementEqualityComparer.TypeElementComparer));

    lock (cache)
    {
      if (!cache.TryGetValue(interfaceElement, out var defaultMembers))
      {
        cache[interfaceElement] = defaultMembers = CollectDefaultMembers();
      }

      return defaultMembers;
    }

    [Pure]
    IReadOnlyList<IOverridableMember> CollectDefaultMembers()
    {
      var defaultMembers = new LocalList<IOverridableMember>();

      foreach (var typeMember in interfaceElement.GetMembers())
      {
        if (typeMember is IOverridableMember { IsStatic: false, IsVirtual: true } overridableMember and not (IProperty or IEvent))
        {
          defaultMembers.Add(overridableMember);
        }
      }

      return defaultMembers.ReadOnlyList();
    }
  }
}