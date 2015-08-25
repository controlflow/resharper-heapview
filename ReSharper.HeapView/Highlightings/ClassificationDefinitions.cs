using System.ComponentModel.Composition;
using System.Windows.Media;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.TextControl.DocumentMarkup;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
// ReSharper disable UnassignedField.Global

[assembly: RegisterHighlighter(
  id: HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
  EffectColor = "Red",
  EffectType = EffectType.SOLID_UNDERLINE,
  Layer = HighlighterLayer.SYNTAX,
  VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
  HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
  EffectColor = "Yellow",
  EffectType = EffectType.SOLID_UNDERLINE,
  Layer = HighlighterLayer.SYNTAX,
  VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
  id: HeapViewAttributeIds.STRUCT_COPY_ID,
  EffectColor = "Yellow",
  EffectType = EffectType.SOLID_UNDERLINE,
  Layer = HighlighterLayer.SYNTAX,
  VSPriority = VSPriority.IDENTIFIERS)]

// todo: extract constants

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ClassificationType(ClassificationTypeNames = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID)]
  [Order(After = "Formal Language Priority", Before = "Natural Language Priority")]
  [Export(typeof(EditorFormatDefinition))]
  [Name(HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID)]
  [DisplayName(HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID)]
  [UserVisible(true)]
  internal class ReSharperBoxingOccurrenceClassificationDefinition : ClassificationFormatDefinition
  {
    public ReSharperBoxingOccurrenceClassificationDefinition()
    {
      DisplayName = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID;
      ForegroundColor = Colors.Red;
    }

    [Export, Name(HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID), BaseDefinition("formal language")]
    internal ClassificationTypeDefinition ClassificationTypeDefinition;
  }

  [ClassificationType(ClassificationTypeNames = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID)]
  [Order(After = "Formal Language Priority", Before = "Natural Language Priority")]
  [Export(typeof(EditorFormatDefinition))]
  [Name(HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID)]
  [DisplayName(HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID)]
  [UserVisible(true)]
  internal class ReSharperHeapAllocationClassificationDefinition : ClassificationFormatDefinition
  {
    public ReSharperHeapAllocationClassificationDefinition()
    {
      DisplayName = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID;
      ForegroundColor = Colors.Orange;
    }

    [Export, Name(HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID), BaseDefinition("formal language")]
    internal ClassificationTypeDefinition ClassificationTypeDefinition;
  }

  [ClassificationType(ClassificationTypeNames = HeapViewAttributeIds.STRUCT_COPY_ID)]
  [Order(After = "Formal Language Priority", Before = "Natural Language Priority")]
  [Export(typeof(EditorFormatDefinition))]
  [Name(HeapViewAttributeIds.STRUCT_COPY_ID)]
  [DisplayName(HeapViewAttributeIds.STRUCT_COPY_ID)]
  [UserVisible(true)]
  internal class ReSharperStructCopyClassificationDefinition : ClassificationFormatDefinition
  {
    public ReSharperStructCopyClassificationDefinition()
    {
      DisplayName = HeapViewAttributeIds.STRUCT_COPY_ID;
      ForegroundColor = Colors.SkyBlue;
    }

    [Export, Name(HeapViewAttributeIds.STRUCT_COPY_ID), BaseDefinition("formal language")]
    internal ClassificationTypeDefinition ClassificationTypeDefinition;
  }
}