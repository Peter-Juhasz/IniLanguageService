using Microsoft.VisualStudio.Text;
using System;

namespace IniLanguageService.Syntax
{
    public interface ISyntacticParser
    {
        SyntaxTree Parse(ITextSnapshot snapshot);
    }

    public static class CommonScanner
    {
        public static SnapshotSpan ReadWhiteSpace(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadToLineEndWhile(ref point, Char.IsWhiteSpace, rewindWhiteSpace: false);
        }

        public static SnapshotSpan ReadToLineEndWhile(this ITextSnapshot snapshot, ref SnapshotPoint point, Predicate<char> predicate, bool rewindWhiteSpace = true)
        {
            SnapshotPoint start = point;

            while (
                point.Position < snapshot.Length &&
                point.GetChar() != '\n' && point.GetChar() != '\r' &&
                predicate(point.GetChar())
            )
                point = point + 1;

            if (rewindWhiteSpace)
            {
                while (
                    point - 1 >= start &&
                    Char.IsWhiteSpace((point - 1).GetChar())
                )
                    point = point - 1;
            }

            return new SnapshotSpan(start, point);
        }
    }
}