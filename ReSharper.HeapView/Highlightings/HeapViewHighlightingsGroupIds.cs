using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Feature.Services.Daemon;
// ReSharper disable RedundantArgumentName
// ReSharper disable RedundantArgumentNameForLiteralExpression

[assembly: RegisterConfigurableHighlightingsGroup(
  key: HeapViewHighlightingsGroupIds.ID,
  title: "[Heap Allocations Plugin] Allocation hints")]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  public static class HeapViewHighlightingsGroupIds
  {
    public const string ID = "AllocationHints";
  }
}