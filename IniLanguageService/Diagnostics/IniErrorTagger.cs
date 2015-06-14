using IniLanguageService.Diagnostics;
using IniLanguageService.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace IniLanguageService
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType(IniContentTypeNames.Ini)]
    internal sealed class IniErrorTaggerProvider : ITaggerProvider
    {
#pragma warning disable 649

        [ImportMany]
        private IEnumerable<IDiagnosticAnalyzer> analyzers;

#pragma warning restore 649


        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new IniErrorTagger(analyzers)
            ) as ITagger<T>;
        }


        private sealed class IniErrorTagger : ITagger<IErrorTag>
        {
            public IniErrorTagger(IEnumerable<IDiagnosticAnalyzer> analyzers)
            {
                _analyzers = analyzers;
            }

            private readonly IEnumerable<IDiagnosticAnalyzer> _analyzers;
            

            public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                ITextBuffer buffer = spans.First().Snapshot.TextBuffer;
                SyntaxTree syntax = buffer.GetSyntaxTree();
                IniDocumentSyntax root = syntax.Root as IniDocumentSyntax;

                return
                    // find intersecting nodes
                    from node in root.DescendantsAndSelf()
                    where spans.IntersectsWith(node.Span)
                    let type = node.GetType()
                    
                    // find analyzers for node
                    from analyzer in _analyzers
                    from @interface in analyzer.GetType().GetInterfaces()
                    where @interface.IsGenericType
                       && @interface.GetGenericTypeDefinition() == typeof(ISyntaxNodeAnalyzer<>)
                    let analyzerNodeType = @interface.GetGenericArguments().Single()
                    where analyzerNodeType.IsAssignableFrom(type)

                    // analyze node
                    from diagnostic in typeof(ISyntaxNodeAnalyzer<>)
                        .MakeGenericType(analyzerNodeType)
                        .GetMethod("Analyze")
                        .Invoke(analyzer, new [] { node }) as IEnumerable<ITagSpan<IErrorTag>>
                    where spans.IntersectsWith(diagnostic.Span)
                    select diagnostic
                ;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }
    }
}
