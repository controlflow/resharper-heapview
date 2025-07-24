// ReSharper disable UnusedVariable
// ReSharper disable RedundantStringInterpolation
using System;

public class StringInterpolationExpression
{
  private const string ConstPart = "const";

  public StringInterpolationExpression(int value, string text)
  {
    var string00 = $"empty{{";
    var string01 = $"value={value}"; // alloc, format, boxing
    var string02 = $"text={text}"; // alloc, concat
    var string03 = $"text={text,-1}"; // alloc, format
    var string04 = $"text={ConstPart}"; // alloc, concat
  }
}