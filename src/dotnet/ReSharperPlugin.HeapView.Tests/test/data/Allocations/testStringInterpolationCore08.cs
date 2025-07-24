// ReSharper disable UnusedVariable
var unknownTarget = $"aaa{args.Length}";
string stringTarget = $"aaa{args.Length}";
object objectTarget = $"aaa{args.Length}";
GenericTarget($"aaa{args.Length}");
void GenericTarget<T>(T unused) { }