using System;
// ReSharper disable SuggestDiscardDeclarationVarStyle
// ReSharper disable UnusedVariable

var copy1 = args[..]; // yes

if (args is []) { }
if (args is ["aa"]) { }
if (args is ["aa", "bb"]) { }
if (args is [..]) { }
if (args is [.., ..]) { }
if (args is [.._]) { }
if (args is [..var _]) { }
if (args is [..var slice1]) { } // yes
if (args is [..["aa", "bb"]]) { } // yes
if (args[0] is ['a', 'b', ..var t]) { } // yes

if (args.Length > 0)
{
  var copy2 = args[..];
  if (args is [.. var slice2]) { }
  throw new Exception();
}