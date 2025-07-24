using System.Collections.Immutable;

ImmutableArray<string> xs1 = [];
ImmutableArray<string> xs2 = ["aaa"]; // heap array
ImmutableArray<string> xs3 = [..args];
ImmutableArray<string> xs4 = [..(System.Collections.Generic.IEnumerable<string>)args];
ImmutableArray<string> xs5 = [..args, "aaa"];