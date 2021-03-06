﻿using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

// ReSharper disable MemberCanBePrivate.Global

namespace ReSharperPlugin.HeapView.Highlightings
{
  [RegisterConfigurableSeverity(
    SEVERITY_ID, null,
    HeapViewHighlightingsGroupIds.ID, "Closure allocation",
    "Highlights places where closure class creation happens",
    Severity.HINT)]
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class ClosureAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ClosureAllocation";
    public const string MESSAGE = "Closure allocation: {0}";

    public ClosureAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }

  [RegisterConfigurableSeverity(
    SEVERITY_ID, null,
    HeapViewHighlightingsGroupIds.ID, "Closure creation can be eliminated",
    "Highlights places where closure can be eliminated by using the overload(s) of containing method invocation, " +
    "allowing passing extra state parameter(s) to closure function",
    Severity.SUGGESTION)]
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class CanEliminateClosureCreationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.CanAvoidClosure";
    public const string MESSAGE = "Closure can be eliminated: {0}";

    public CanEliminateClosureCreationHighlighting([NotNull] ITreeNode element)
      : base(element, MESSAGE, "method has overload to avoid closure creation") { }
  }
}
