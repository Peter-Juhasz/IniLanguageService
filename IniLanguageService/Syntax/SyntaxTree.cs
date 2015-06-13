using Microsoft.VisualStudio.Text;

namespace IniLanguageService.Syntax
{
    public class SyntaxTree
    {
        public SyntaxTree(ITextSnapshot snapshot, SyntaxNode root)
        {
            this.Snapshot = snapshot;
            this.Root = root;
        }

        public ITextSnapshot Snapshot { get; private set; }

        public SyntaxNode Root { get; private set; }
    }

    internal class IniSyntaxTree : SyntaxTree
    {
        public IniSyntaxTree(IniDocumentSyntax document)
            : base(document.Snapshot, document)
        { }

        public new SyntaxNode Root
        {
            get { return base.Root as IniDocumentSyntax; }
        }
    }

    public static class TextBufferExtensions
    {
        public static SyntaxTree GetSyntaxTree(this ITextBuffer buffer)
        {
            return buffer.CurrentSnapshot.GetSyntaxTree();
        }
    }

    public static class TextSnapshotExtensions
    {
        public static SyntaxTree GetSyntaxTree(this ITextSnapshot snapshot)
        {
            ITextBuffer buffer = snapshot.TextBuffer;

            return buffer.Properties.GetOrCreateSingletonProperty<SyntaxTree>("Syntax",
                () => buffer.Properties.GetProperty<ILexicalParser>("Parser").Parse(snapshot)
            );
        }
    }
}
