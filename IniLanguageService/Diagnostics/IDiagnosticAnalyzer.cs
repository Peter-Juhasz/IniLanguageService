using System.ComponentModel.Composition;

namespace IniLanguageService.Diagnostics
{
    public interface IDiagnosticAnalyzer
    {
    }

    public class ExportDiagnosticAnalyzer : ExportAttribute
    {
        public ExportDiagnosticAnalyzer()
            : base(typeof(IDiagnosticAnalyzer))
        { }
    }
}
