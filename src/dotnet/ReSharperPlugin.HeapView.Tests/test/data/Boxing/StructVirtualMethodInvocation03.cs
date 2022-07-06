var s = new RS();
s.ToString(); // error
s.GetHashCode(); // error
s.Equals(null); // error

ref struct RS { }