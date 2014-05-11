using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Threading;
using JetBrains.Util;
using NUnit.Framework;

[assembly: TestDataPathBase(@".\Data\Daemon")]

// ReSharper disable once CheckNamespace
[SetUpFixture]
public class HeapViewTestsAssembly : ReSharperTestEnvironmentAssembly
{
  [NotNull]
  private static IEnumerable<Assembly> GetAssembliesToLoad()
  {
    //yield return typeof(PostfixTemplatesManager).Assembly;
    yield return Assembly.GetExecutingAssembly();
  }

  public override void SetUp()
  {
    base.SetUp();
    ReentrancyGuard.Current.Execute("LoadAssemblies", () => {
      var assemblyManager = Shell.Instance.GetComponent<AssemblyManager>();
      assemblyManager.LoadAssemblies(GetType().Name, GetAssembliesToLoad());
    });
  }

  public override void TearDown()
  {
    ReentrancyGuard.Current.Execute("UnloadAssemblies", () => {
      var assemblyManager = Shell.Instance.GetComponent<AssemblyManager>();
      assemblyManager.UnloadAssemblies(GetType().Name, GetAssembliesToLoad());
    });

    base.TearDown();
  }
}