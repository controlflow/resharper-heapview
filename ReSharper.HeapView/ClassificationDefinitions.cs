using System.ComponentModel.Composition;
using System.Windows.Media;
using JetBrains.ReSharper.HeapView;
using JetBrains.TextControl.DocumentMarkup;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
// ReSharper disable UnassignedField.Global

[assembly: RegisterHighlighter(
  Compatibility.BOXING_HIGHLIGHTING_ID,
  EffectColor = "Red", EffectType = EffectType.SOLID_UNDERLINE,
  Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
  Compatibility.ALLOCATION_HIGHLIGHTING_ID,
  EffectColor = "Yellow", EffectType = EffectType.SOLID_UNDERLINE,
  Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

namespace JetBrains.ReSharper.HeapView
{
  [ClassificationType(ClassificationTypeNames = Compatibility.BOXING_HIGHLIGHTING_ID)]
  [Order(After = "Formal Language Priority", Before = "Natural Language Priority")]
  [Export(typeof(EditorFormatDefinition))]
  [Name(Compatibility.BOXING_HIGHLIGHTING_ID)]
  [DisplayName(Compatibility.BOXING_HIGHLIGHTING_ID)]
  [UserVisible(true)]
  internal class ReSharperBoxingOccurrenceClassificationDefinition : ClassificationFormatDefinition
  {
    public ReSharperBoxingOccurrenceClassificationDefinition()
    {
      DisplayName = Compatibility.BOXING_HIGHLIGHTING_ID;
      ForegroundColor = Colors.Red;
    }

    [Export]
    [Name(Compatibility.BOXING_HIGHLIGHTING_ID)]
    [BaseDefinition("formal language")]
    internal ClassificationTypeDefinition ClassificationTypeDefinition;
  }

  [ClassificationType(ClassificationTypeNames = Compatibility.ALLOCATION_HIGHLIGHTING_ID)]
  [Order(After = "Formal Language Priority", Before = "Natural Language Priority")]
  [Export(typeof(EditorFormatDefinition))]
  [Name(Compatibility.ALLOCATION_HIGHLIGHTING_ID)]
  [DisplayName(Compatibility.ALLOCATION_HIGHLIGHTING_ID)]
  [UserVisible(true)]
  internal class ReSharperHeapAllocationClassificationDefinition : ClassificationFormatDefinition
  {
    public ReSharperHeapAllocationClassificationDefinition()
    {
      DisplayName = Compatibility.ALLOCATION_HIGHLIGHTING_ID;
      ForegroundColor = Colors.Orange;
    }

    [Export]
    [Name(Compatibility.ALLOCATION_HIGHLIGHTING_ID)]
    [BaseDefinition("formal language")]
    internal ClassificationTypeDefinition ClassificationTypeDefinition;
  }
}