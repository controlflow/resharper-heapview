using System;
using System.Linq;

public class SomeClass {
  public void Ordinary() {
    var local = "local";
    const string constantLocal = "constant";
    F(() => {
      var local2 = "local2";
      F(() => local2 + local);
      return local + constantLocal;
    });
  }

  public unsafe void Unsafe(int[] array) {
    ref var refLocal = ref array[0];
    F(() => refLocal.ToString());

    fixed (int* ptr = &array[0]) {
      F(() => ptr->ToString());
    }
  }

  public void Catch() {
    try { }
    catch (Exception exception) when (DeclaresVar(out var outVar) && C(() => outVar)) {
      F(() => exception.ToString());
    }
  }

  public void OutVars() {
    DeclaresVar(out var statement);
    var declaration = DeclaresVar(out var declarationStatement);
    F(() => statement + declarationStatement);

    do { }
    while (DeclaresVar(out var doVariable) && C(() => doVariable));

    foreach ((var iterator1, var (iterator2, _))
        in DeclaresVar(out var foreachCollection)
             .ToString().Select(x => ("aa", ("bb", "cc")))) {
      F(() => iterator1 + iterator2 + foreachCollection);
    }

    for (DeclaresVar(out var forInitializer), F(() => forInitializer);
      DeclaresVar(out var forCondition);
      DeclaresVar(out var forIterator), F(() => forIterator))
    {
      F(() => forInitializer + forCondition);
    }

    Func<bool> lambda = () => DeclaresVar(out var lambdaVar) && C(() => lambdaVar);

    bool LocalFunc() => DeclaresVar(out var localFuncVar) && C(() => localFuncVar);

    switch ((object) statement) {
      case string caseVar when DeclaresVar(out var switchSection) && C(() => switchSection + caseVar):
        var line1 = Console.ReadLine();
        F(() => line1);
        break;

      case string caseVar2:
        var line2 = Console.ReadLine();
        F(() => caseVar2 + line2);
        break;
    }

    var switchExpression = (object) statement switch {
      string armVar => DeclaresVar(out var armOutVar) && C(() => armOutVar + armVar),
      _ => false
    };

    for (var forVariable = DeclaresVar(out var forInitializer) && C(() => forInitializer);
      C(() => forVariable + forInitializer);)
    {
      F(() => forVariable + forInitializer);
    }
  }

  public SomeClass(out string t) { t = "aa"; }

  public SomeClass()
    : this(out var ctorInitializer)
  {
    F(() => ctorInitializer);
  }

  public bool EmbeddedScope(bool flag)
  {
    if (flag)
      if (DeclaresVar(out var ifEmbedded) && C(() => ifEmbedded)) { }

    if (flag)
      _ = DeclaresVar(out var expressionEmbedded) && C(() => expressionEmbedded);

    if (flag)
      switch (DeclaresVar(out var switchEmbedded), C(() => switchEmbedded)) { }

    if (flag.GetHashCode() == 42)
    {
      if (flag)
        throw new MyException(DeclaresVar(out var throwStandalone) && C(() => throwStandalone));

      throw new MyException(DeclaresVar(out var throwEmbedded) && C(() => throwEmbedded));
    }

    if (flag)
      return DeclaresVar(out var returnEmbedded) && C(() => returnEmbedded);

    if (DeclaresVar(out var ifStandalone) && C(() => ifStandalone)) { }
    _ = DeclaresVar(out var expressionStandalone) && C(() => expressionStandalone);
    switch (DeclaresVar(out var switchStandalone), C(() => switchStandalone)) { }
    return DeclaresVar(out var returnStandalone) && C(() => returnStandalone);
  }

  public static extern bool DeclaresVar(out string t);
  public static string F(Func<string> func) => func();
  public static bool C(Func<string> func) => true;

  public class MyException : Exception {
    public MyException(bool b) { }
  }
}