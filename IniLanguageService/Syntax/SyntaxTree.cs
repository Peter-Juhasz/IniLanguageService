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

            SyntaxTree syntaxTree = null;
            buffer.Properties.TryGetProperty<SyntaxTree>(typeof(SyntaxTree), out syntaxTree);

            if (syntaxTree == null ||
                syntaxTree.Snapshot != snapshot)
            {
                ILexicalParser lexicalParser = null;
                if (buffer.Properties.TryGetProperty<ILexicalParser>(typeof(ILexicalParser), out lexicalParser))
                    syntaxTree = lexicalParser.Parse(snapshot);
            }

            return syntaxTree;
        }
    }
}
