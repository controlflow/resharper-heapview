using JetBrains.TextControl.DocumentMarkup;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Highlightings
{
  public class HeapViewSettingsNamesProvider : PrefixBasedSettingsNamesProvider
  {
    public HeapViewSettingsNamesProvider()
      : base("ReSharper HeapView", "HEAP_VIEW") { }
  }
}
