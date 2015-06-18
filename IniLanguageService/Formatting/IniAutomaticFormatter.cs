using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using IniLanguageService.Syntax;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace IniLanguageService.Formatting
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(IniContentTypeNames.Ini)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class IniAutomaticFormatter : IVsTextViewCreationListener
    {
#pragma warning disable 169

        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory;

        [Import]
        private ITextDocumentFactoryService TextDocumentFactoryService;

#pragma warning restore 169


        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);
            
            // register command filter
            CommandFilter filter = new CommandFilter(view);

            IOleCommandTarget next;
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(filter, out next));
            filter.Next = next;
        }


        private sealed class CommandFilter : IOleCommandTarget
        {
            public CommandFilter(ITextView view)
            {
                _textView = view;
            }

            private readonly ITextView _textView;


            public void OnCharTyped(char @char)
            {
                // complete brace '['
                if (@char == '[')
                {
                    ITextBuffer buffer = _textView.TextBuffer;
                    
                    SyntaxTree syntaxTree = buffer.GetSyntaxTree();
                    IniDocumentSyntax root = syntaxTree.Root as IniDocumentSyntax;

                    var caret = _textView.Caret.Position.BufferPosition;
                    IniSectionSyntax section = root.Sections
                        .FirstOrDefault(s => s.OpeningBracketToken.Span.Span.End == caret);

                    if (section != null)
                    {
                        if (section.NameToken.IsMissing &&
                            section.ClosingBracketToken.IsMissing)
                        {
                            // complete pair
                            using (ITextEdit edit = buffer.CreateEdit())
                            {
                                // TODO: Do not move caret
                                //edit.Insert(change.NewSpan.End, "]");

                                edit.Apply();
                            }
                        }
                    }
                }

                // format on ']'
                else if (@char == ']')
                {
                    ITextBuffer buffer = _textView.TextBuffer;

                    SyntaxTree syntaxTree = buffer.GetSyntaxTree();
                    IniDocumentSyntax root = syntaxTree.Root as IniDocumentSyntax;

                    var caret = _textView.Caret.Position.BufferPosition;
                    IniSectionSyntax section = root.Sections
                        .FirstOrDefault(s => s.ClosingBracketToken.Span.Span.End == caret);

                    if (section != null)
                    {
                        // remove unnecessary whitespace
                        using (ITextEdit format = buffer.CreateEdit())
                        {
                            if (section.OpeningBracketToken.Span.Span.End != section.NameToken.Span.Span.Start)
                                format.Delete(new SnapshotSpan(section.OpeningBracketToken.Span.Span.End, section.NameToken.Span.Span.Start));

                            if (section.NameToken.Span.Span.End != section.ClosingBracketToken.Span.Span.Start)
                                format.Delete(new SnapshotSpan(section.NameToken.Span.Span.End, section.ClosingBracketToken.Span.Span.Start));

                            format.Apply();
                        }
                    }
                }

                // format on '='
                else if (@char == '=')
                {
                    ITextBuffer buffer = _textView.TextBuffer;

                    SyntaxTree syntaxTree = buffer.GetSyntaxTree();
                    IniDocumentSyntax root = syntaxTree.Root as IniDocumentSyntax;

                    var caret = _textView.Caret.Position.BufferPosition;
                    IniPropertySyntax property = root.Sections
                        .SelectMany(s => s.Properties)
                        .FirstOrDefault(p => p.DelimiterToken.Span.Span.End == caret);

                    if (property != null)
                    {
                        // reference point is section opening '['
                        SnapshotPoint referencePoint = property.Section.OpeningBracketToken.Span.Span.Start;

                        // find property before
                        IniPropertySyntax before = property.Section.Properties
                            .TakeWhile(p => p != property)
                            .LastOrDefault();

                        // override reference point if found property before
                        if (before != null)
                            referencePoint = before.NameToken.Span.Span.Start;

                        // compare
                        ITextSnapshotLine referenceLine = referencePoint.GetContainingLine();
                        ITextSnapshotLine line = property.DelimiterToken.Span.Span.End.GetContainingLine();

                        SnapshotSpan referenceIndent = new SnapshotSpan(referenceLine.Start, referencePoint);
                        SnapshotSpan indent = new SnapshotSpan(line.Start, property.NameToken.Span.Span.Start);

                        if (referenceIndent.GetText() != indent.GetText())
                        {
                            using (ITextEdit edit = buffer.CreateEdit())
                            {
                                edit.Replace(indent, referenceIndent.GetText());

                                edit.Apply();
                            }
                        }
                    }
                }
            }


            public IOleCommandTarget Next { get; set; }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                int hresult = VSConstants.S_OK;

                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                if (ErrorHandler.Succeeded(hresult))
                {
                    if (pguidCmdGroup == VSConstants.VSStd2K)
                    {
                        switch ((VSConstants.VSStd2KCmdID)nCmdID)
                        {
                            case VSConstants.VSStd2KCmdID.TYPECHAR:
                                char @char = GetTypeChar(pvaIn);
                                OnCharTyped(@char);
                                break;
                        }
                    }
                }

                return hresult;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            private static char GetTypeChar(IntPtr pvaIn)
            {
                return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }
        }
    }
}
