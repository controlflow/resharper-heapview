(int, (bool, byte))[] array = null;

foreach (var (a, (b, c)) in array) { }
foreach ((_, (_, _)) in array) { }
foreach ((object _, (object _, object _)) in array) { }
foreach ((object d, (object e, object f)) in array) { }
foreach ((object g, var (h, i)) in array) { }