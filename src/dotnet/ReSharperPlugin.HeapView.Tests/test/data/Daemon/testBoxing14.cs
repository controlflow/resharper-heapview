class BoxingUnboxing<T> {
  public void M(T t) {
    if (typeof(T) == typeof(int)) {
      var integer = (int) (object) t;
    }
    else if (typeof(T) == typeof(byte)) {
      var byteValue = (byte) (object) t;
      var noBoxHere = (int) (object) t; // will throw ICE
    }
  }

  public void M2<T2>(T2 t) where T2 : struct {
    if (typeof(T2) == typeof(int)) {
      var integer = (int) (object) t;
    }
    else if (typeof(T2) == typeof(byte)) {
      var byteValue = (byte) (object) t;
      var noBoxHere = (int) (object) t; // will throw ICE
    }
  }

  public void G<U>(T t) {
    if (typeof(T) == typeof(U)) {
      var value = (U) (object) t;
    }
  }
}