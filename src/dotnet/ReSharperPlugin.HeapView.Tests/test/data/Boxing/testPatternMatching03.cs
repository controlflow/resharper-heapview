using System;
// ReSharper disable PatternAlwaysMatches
// ReSharper disable UnusedVariable
// ReSharper disable MergeAndPattern
// ReSharper disable RedundantDiscardDesignation
#pragma warning disable CS8794

struct Patterns : I, I2
{
  public int Property => 0;

  public bool Pattern0() => this is I _;
  public bool Pattern1() => this is I _ and var o; // yes
  public bool Pattern2() => this is { } and I and var o; // yes
  public bool Pattern3() => this is I or I2;
}

interface I { int Property { get; } }
interface I2 { }