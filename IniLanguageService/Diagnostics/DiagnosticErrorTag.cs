using Microsoft.VisualStudio.Text.Tagging;

namespace IniLanguageService
{
    public interface IDiagnosticErrorTag : IErrorTag
    {
        string Id { get; }
    }

    internal class DiagnosticErrorTag : ErrorTag
    {
        public DiagnosticErrorTag(string errorType, string id, object toolTipContent)
            : base(errorType, toolTipContent)
        {
            this.Id = id;
        }

        public string Id { get; private set; }
    }
}
