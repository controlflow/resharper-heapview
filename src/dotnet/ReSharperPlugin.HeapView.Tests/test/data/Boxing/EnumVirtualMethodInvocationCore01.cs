using System;

var e = ConsoleKey.A;
_ = e.GetHashCode(); // optimized in Core
_ = e.Equals(null);
_ = e.ToString();
_ = e.GetTypeCode();

ConsoleKey? ne = e;
_ = ne.GetHashCode(); // optimized in Core
_ = ne.Equals(null);
_ = ne.ToString();

_ = ne?.GetHashCode(); // optimized in Core
_ = ne?.Equals(null);
_ = ne?.ToString();
_ = ne?.GetTypeCode();