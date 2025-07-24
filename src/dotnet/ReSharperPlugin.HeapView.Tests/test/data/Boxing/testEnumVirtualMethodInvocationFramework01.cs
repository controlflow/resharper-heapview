using System;

var e = ConsoleKey.A;
_ = e.GetHashCode();
_ = e.Equals(null);
_ = e.ToString();
_ = e.GetTypeCode();

ConsoleKey? ne = e;
_ = ne.GetHashCode();
_ = ne.Equals(null);
_ = ne.ToString();

_ = ne?.GetHashCode();
_ = ne?.Equals(null);
_ = ne?.ToString();
_ = ne?.GetTypeCode();