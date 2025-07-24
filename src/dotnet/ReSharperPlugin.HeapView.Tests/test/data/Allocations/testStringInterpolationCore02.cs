// ReSharper disable UnusedVariable
// ReSharper disable RedundantStringInterpolation
using System;

[StringInterpolationExpression(42, $"text={ConstPart}")]
public class StringInterpolationExpression : Attribute
{
  private const string ConstPart = "const";

  public StringInterpolationExpression(int value, string text)
  {
    var string00 = $"empty{{";
    var string01 = $"value={value}"; // alloc, default handler
    var string02 = $"text={text}"; // alloc, concat
    var string03 = $"text={text,-1}"; // alloc, default handler
    var string04 = $"text={ConstPart}";
  }
}