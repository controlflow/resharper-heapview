using JetBrains.TestFramework;
using NUnit.Framework;

//#pragma warning disable 618
//[assembly: TestDataPathBase(@".\..\..\..\ReSharper.HeapView.Tests\data")]
//#pragma warning restore 618

namespace ReSharperPlugin.HeapView.Tests;

[SetUpFixture]
public class HeapViewTestsAssembly : ExtensionTestEnvironmentAssembly<HeapViewTestEnvironmentZone>;