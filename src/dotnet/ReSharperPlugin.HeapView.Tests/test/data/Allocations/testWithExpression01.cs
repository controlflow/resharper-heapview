var rc = new RecordClass(42, true);
var rcClone1 = rc with { Property = 1 }; // yes
var rcClone2 = rc with { }; // yes

var rs = new RecordStruct(42, true);
var rsClone1 = rs with { Property = 1 };
var rsClone2 = rs with { };

var s = new Struct { Property = 1 };
var sClone1 = s with { Property = 1 };
var sClone2 = s with { };

var ao = new { Name = "Alex", Age = 42 };
var aoClone1 = ao with { }; // yes
var aoClone2 = ao with { Age = 1 }; // yes

if (args.Length > 0)
{
  var aoClone3 = ao with { Age = 2 };
  throw new System.Exception();
}

public record RecordClass(int Property, bool Flag);
public record struct RecordStruct(int Property, bool Flag);
public struct Struct { public int Property; public bool Flag; }