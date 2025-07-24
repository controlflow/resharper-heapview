record Base(params int[] Items);
record Derived1(int X) : Base;
record Derived2(int Y) : Base();
record Derived3(int Z) : Base(Z); // alloc