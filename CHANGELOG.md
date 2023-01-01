# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## 2022.3.0
- Added support for ReSharper and Rider 2022.3
- Detect boxing allocations because of not implemented default interface members
- "Closure can be eliminated" inspection rewrite, simplification, fix a few issues
- Report allocations in C# string interpolation expressions
- Report allocations in delegate/events operators
- Rewritten analysis of string concatenations
- Support for C# 10 and C# 11 constructs
- Handle C# 11 static method delegate caching
- Rewritten 'params' allocation analyzer

## 2022.2.0
- Boxing allocations analysis was rewritten to match the recent compiler/runtime
- Boxing is now detected in implicit extension method invocations (in deconstruction, foreach statement, await expression, collection initializers)
- Boxing detection inside tuple conversions was completely rewritten (fix a few unrecognized allocations, better allocation reports, better highlighting placements when possible)
- Boxing is now detected in Enumerable.Cast<T>() method invocations (including query expressions)
- Boxing detection in type tests was completely rewritten, more boxing cases recognized in .NET Framework projects
- Boxing is now detected when variables of unconstrained generic type are being checked for null via patterns
- New options page to control the analysis mode - allows to hide or show the allocations that only happen in Release build with optimizations enabled
- Added much more tests, currently reported boxing cases are verified in various runtimes/configurations
- Added support for ReSharper and Rider 2022.2

## 2022.1.0
- Added support for ReSharper and Rider 2022.1

## 2021.3.0
- Added support for ReSharper and Rider 2021.3

## 2021.2.0
- Added support for ReSharper and Rider 2021.2

## 2021.1.0
- Added support for ReSharper and Rider 2021.1
- Fixed zoning for usage in InspectCode

## 2020.3.0
- Added support for ReSharper and Rider 2020.3

## 2020.2.5
- Fixed plugin to signal that IDE restart is required

## 2020.2.4
- Updated for ReSharper and Rider 2020.2 EAP8

## 2020.2.3
- Updated for ReSharper and Rider 2020.2 EAP7

## 2020.2.2
- Updated for ReSharper and Rider 2020.2 EAP4

## 2020.2.1
- Updated for ReSharper and Rider 2020.2 EAP3

## 2020.2.0
- Updated for ReSharper and Rider 2020.2 EAP2

## 1.2.21
- Updated for ReSharper and Rider 2020.1

## 1.2.20
- Updated for ReSharper and Rider 2019.3

## 1.2.19
- Updated icon

## 1.2.18
- Updated for ReSharper and Rider 2019.2

## 1.2.17
- Updated for ReSharper and Rider 2019.2

## 1.2.13
- Updated for ReSharper and Rider 2019.1

## 1.2.3
- ReSharper 2017.1 EAP5 support.

## 1.2.2
- Upgrade to latest ReSharper 2016.3 SDK build;
- ReSharper 2017.1 EAP2 support.

## 1.2
- ReSharper 2016.2 RTM and ReSharper 2016.3 EAP support.

## 1.1.0
- ReSharper 10 RTM support;
- New inspection 'Closure can be eliminated' to detect overloads with additional state parameters helping avoid closure allocations.

## 0.9.9
- Drop support for ReSharper 8.1, 8.2 and 9.0;
- Drop 'Slow delegate creation' inspection: not useful, no way to fix, behavior differs in x86/x64/RyuJIT.

## 0.9.8
- ReSharper 9.2 RTM support.

## 0.9.7
- ReSharper 9.1 RTM support.

## 0.9.6
- Split object allocation highlighting in groups (evident/hidden/possible), so user can separately turn them on or off;
- Suppress boxing warnings inside attributes;
- Generalize 'parameters array creation' inspection to object creation expressions, indexer access, constructor and collection initializers;
- Handle some of Roslyn changes (when C# 6.0 language level is turned on);
- C# 6.0: anonymous methods without closure inside generic methods now can be cached;
- C# 6.0: support for expression-bodied members and property/event initializers;
- C# 6.0: support for cached empty parameter arrays when Array.Empty<T>() is available.

## 0.9.5
- ReSharper 9.0 RTM support;
- In R# 9.0 build inspection colors are available with 'ReSharper HeapView Boxing' and 'ReSharper HeapView Allocation' ids in Visual Studio fonts & colors settings;
- 'Slow delegate creation' downgraded to warning severity.

## 0.9.3-beta
- Initial ReSharper 9.0 EAP support.

## 0.9.2-beta
- Build against R# 9.0 signed binaries.

## 0.9.1
- Better highlighting ranges for formal parameters and parameter arrays;
- Allocation highlighting splitted into separate delegate/closure/object allocation highlightings;
- Highlightings now configurable (can change severity, can search for similar issues);
- Tests for slow delegate creation.

## 0.9
- Initial version, support for ReSharper >8.1.
