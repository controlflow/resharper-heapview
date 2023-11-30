using System.ComponentModel.Composition;
using System.Windows.Media;
using JetBrains.Annotations;
using JetBrains.Platform.VisualStudio.SinceVs10.TextControl.Markup.FormatDefinitions;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using ReSharperPlugin.HeapView.Highlightings;

#pragma warning disable 649

namespace ReSharperPlugin.HeapView.VisualStudio;

[ClassificationType(ClassificationTypeNames = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID)]
[Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
[Export(typeof(EditorFormatDefinition))]
[Name(HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID)]
[DisplayName(HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID)]
[UserVisible(true)]
internal class ReSharperBoxingOccurrenceClassificationDefinition : ClassificationFormatDefinition
{
  public ReSharperBoxingOccurrenceClassificationDefinition()
  {
    DisplayName = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID;
    ForegroundColor = Color.FromRgb(52, 175, 229);
  }

  [Export, Name(HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID), BaseDefinition("formal language")]
  [UsedImplicitly]
  internal ClassificationTypeDefinition? ClassificationTypeDefinition;
}

[ClassificationType(ClassificationTypeNames = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID)]
[Order(After = VsSyntaxPriorityClassificationDefinition.Name, Before = VsAnalysisPriorityClassificationDefinition.Name)]
[Export(typeof(EditorFormatDefinition))]
[Name(HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID)]
[DisplayName(HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID)]
[UserVisible(true)]
internal class ReSharperHeapAllocationClassificationDefinition : ClassificationFormatDefinition
{
  public ReSharperHeapAllocationClassificationDefinition()
  {
    DisplayName = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID;
    ForegroundColor = Color.FromRgb(52, 175, 229);
  }

  [Export, Name(HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID), BaseDefinition("formal language")]
  [UsedImplicitly]
  internal ClassificationTypeDefinition? ClassificationTypeDefinition;
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = "HackOrderReSharperAndVisualStudioClassificationDefinition")]
[Order(After = "class name", Before = "ReSharper Class Identifier")]
[Order(After = "class name", Before = "ReSharper Static Class Identifier")]
[Order(After = "struct name", Before = "ReSharper Struct Identifier")]
[Order(After = "enum name", Before = "ReSharper Enum Identifier")]
[Order(After = "interface name", Before = "ReSharper Interface Identifier")]
[Order(After = "delegate name", Before = "ReSharper Delegate Identifier")]
[Order(After = "type parameter name", Before = "ReSharper Type Parameter Identifier")]
[Name("HackOrderReSharperAndVisualStudioClassificationDefinition")]
[UserVisible(false)]
internal class HackOrderReSharperAndVisualStudioClassificationDefinition : ClassificationFormatDefinition;