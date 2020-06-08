using System;

partial class C {
  partial void M(int p);
  partial void M(int p) {
    Action a = () => { p++; M(123); };
  }
}