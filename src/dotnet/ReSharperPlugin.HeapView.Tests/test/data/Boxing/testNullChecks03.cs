// ReSharper disable RedundantPatternParentheses
// ReSharper disable ArrangeRedundantParentheses
// ReSharper disable MergeNestedPropertyPatterns

struct NullChecks<TUnconstrained, TStruct, TClass>
  where TUnconstrained : IFoo
  where TStruct : struct, IFoo
  where TClass : class, IFoo
{
  public TUnconstrained Unconstrained => default!;
  public TClass Class => default!;
  public TStruct Struct => default!;

  public void Method()
  {
    _ = this is { Unconstrained: null }; // yes in DEBUG
    _ = this is { Unconstrained: (not (null)) }; // yes in DEBUG
    _ = this is { Class: null };
    _ = this is { Class: not null };
    _ = this is { Unconstrained.Property: 42 }; // yes in DEBUG
    _ = this is { Class.Property: 42 };
    _ = this is { Struct.Property: 42 };
    _ = this is { Unconstrained: { Property: 42 } }; // yes in DEBUG
    _ = this is { Class: { Property: 42 } };
    _ = this is { Struct: { Property: 42 } };

    _ = Unconstrained is { }; // yes in DEBUG
    _ = Unconstrained is { Property: 42 }; // yes in DEBUG
  }
}

interface IFoo
{
  int Property { get; }
}