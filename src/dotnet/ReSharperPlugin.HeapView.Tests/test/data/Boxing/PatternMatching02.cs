#pragma warning disable 183
using System;
// ReSharper disable PatternAlwaysMatches
// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable UnusedVariable
// ReSharper disable RedundantPropertyPatternClause

struct Patterns : I
{
  public int Property => 0;

  public bool Pattern1() => this is I;
  public bool Pattern2() => this is I i; // yes
  public bool Pattern3() => this is object;
  public bool Pattern4() => this is Object;
  public bool Pattern5() => this is object o; // yes
  public bool Pattern6() => this is Object o; // yes
  public bool Pattern7() => this is I { } _;
  public bool Pattern8() => this is I { } i; // yes
  public bool Pattern9() => this is I { Property: 42 }; // yes
}

interface I { int Property { get; } }