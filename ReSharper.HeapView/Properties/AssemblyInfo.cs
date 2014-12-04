using System.Reflection;
#if RESHARPER8
using JetBrains.Application.PluginSupport;
#endif

[assembly: AssemblyTitle("ReSharper.HeapView")]
[assembly: AssemblyDescription("Heap Allocations Viewer plugin for ReSharper")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Shvedov Alexander")]
[assembly: AssemblyProduct("ReSharper Heap Allocations Viewer")]
[assembly: AssemblyCopyright("Copyright © Shvedov Alexander, 2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("0.9.3")]
[assembly: AssemblyFileVersion("1.0.0.0")]

#if RESHARPER8

[assembly: PluginTitle("ReSharper Heap Allocations Viewer")]
[assembly: PluginDescription("Heap Allocations Viewer plugin for ReSharper")]
[assembly: PluginVendor("Shvedov Alexander")]

#endif