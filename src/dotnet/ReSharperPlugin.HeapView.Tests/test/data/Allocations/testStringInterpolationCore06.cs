// ReSharper disable UnusedVariable
// ReSharper disable RedundantStringInterpolation
#pragma warning disable CS0219

public class StringInterpolationExpression
{
  public void Method(int value)
  {
    var string01 = "aaa" + $"bbb{value}"; // concat, alloc
    var string02 = $"aaa" + $"bbb{value}"; // alloc
    var string03 = $"bbb{value}" + "aaa"; // alloc, concat
    var string04 = $"bbb{value}" + $"aaa"; // alloc
    var string05 = $"aaa{value}" + $"bbb{value}";
    var string06 = ("A" + $"aaa{value}") + ($"bbb{value}" + "B"); // concat 4x, no fuse
    var string07 = ($"aaa{value}" + $"bbb{value}") + $"ccc{value}"; // alloc
    var string08 = $"aaa{value}" + ($"bbb{value}" + $"ccc{value}"); // alloc

    var const01 = "aaa" + $"bbb";
    var const02 = $"aaa" + "bbb";
    var const03 = "aaa" + "bbb";
    var const04 = $"aaa" + $"bbb";
  }
}