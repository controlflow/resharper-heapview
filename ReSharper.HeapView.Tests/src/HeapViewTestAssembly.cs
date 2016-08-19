using JetBrains.ReSharper.HeapView;
using NUnit.Framework;
using JetBrains.TestFramework;

#pragma warning disable 618
[assembly: TestDataPathBase(@".\..\..\..\ReSharper.HeapView.Tests\data")]
#pragma warning restore 618

// ReSharper disable once CheckNamespace
[SetUpFixture]
public class HeapViewTestsAssembly : ExtensionTestEnvironmentAssembly<HeapViewTestEnvironmentZone> { }