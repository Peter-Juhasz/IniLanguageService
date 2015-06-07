using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace IniLanguageService.Syntax
{
    public class SnapshotToken
    {
        public SnapshotToken(ClassificationSpan classificationSpan)
        {
            this.Span = classificationSpan;
        }
        public SnapshotToken(SnapshotSpan span, IClassificationType type)
            : this(new ClassificationSpan(span, type))
        { }

        public ClassificationSpan Span { get; private set; }

        public bool IsMissing
        {
            get { return this.Span.Span.IsEmpty; }
        }
    }
}
