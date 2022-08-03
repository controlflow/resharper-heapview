int i = 0;
void Local1() { }
void Local2() { Local1(); i++; }
void Local3() { Local2(); }
void Local4() { Local3(); }

System.Action f = Local4;