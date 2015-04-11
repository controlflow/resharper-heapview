using JetBrains.ReSharper.HeapView;
using NUnit.Framework;
using JetBrains.TestFramework;

[assembly: TestDataPathBase(@".\Data")]

// ReSharper disable once CheckNamespace
[SetUpFixture]
public class HeapViewTestsAssembly : ExtensionTestEnvironmentAssembly<IHeapViewTestEnvironmentZone> { }