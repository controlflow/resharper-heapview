using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
// ReSharper disable UnusedVariable

public class StringInterpolationExpression
{
  public void Method(int value, string text)
  {
    _ = FormattableStringFactory.Create("aaa{0}", value); // params, boxing
    _ = FormattableString.Invariant($@"bbb={value,20:D}, ccc"); // alloc, params, boxing
    FormattableString string01 = @$"aaa={value}, bbb"; // alloc, params, boxing
    IFormattable string02 = $$"""aaa={{text}}"""; // alloc, params
    IFormattable string03 = $"aaa"; // alloc

    Expression<Func<IFormattable>> expr = () => $@"aaa={value}";
  }
}
