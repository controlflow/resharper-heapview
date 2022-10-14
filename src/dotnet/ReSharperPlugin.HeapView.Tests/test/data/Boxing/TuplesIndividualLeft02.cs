// ReSharper disable RedundantAssignment
// ReSharper disable NotAccessedVariable
// ReSharper disable UnusedVariable

var t = ((1, 2), 3);
(object a1, int b1) = t;
(var (a2, b2), int b22) = t;
(_, b2) = t;
((_, _), b2) = t;
((_, _), int b3) = t;
((object _, int _), int b33) = t;
((object a4, int b4), int b5) = t;
((_, a4), _) = t;