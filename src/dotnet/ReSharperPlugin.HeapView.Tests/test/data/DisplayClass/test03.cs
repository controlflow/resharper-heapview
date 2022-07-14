void Local1() { }
void Local2() { Local1() { } }
void Local3() { Local2() { } }
void Local4() { Local3() { } }

var f = Local4;