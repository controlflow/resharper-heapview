{
  // display class #1
  int i = 0;
  void Local1() => i++;
  async void Local2() => Local1();
  Local1();
}

{
  // display class #2, optimized
  int a = 0;
    
  void Local1() { a++; Local2(); Local1(); }
  void Local2() { a--; Local1(); }
  Local2();
}