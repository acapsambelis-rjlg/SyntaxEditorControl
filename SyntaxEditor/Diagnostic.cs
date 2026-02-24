using System;
using System.Collections.Generic;

namespace CodeEditor
{
    public enum DiagnosticSeverity
    {
        Error,
        Warning,
        Info
    }

    public class Diagnostic
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string Message { get; set; }
        public DiagnosticSeverity Severity { get; set; }

        public Diagnostic(int line, int column, int length, string message, DiagnosticSeverity severity)
        {
            Line = line;
            Column = column;
            Length = length;
            Message = message;
            Severity = severity;
        }
    }

    public class DiagnosticsChangedEventArgs : EventArgs
    {
        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        public DiagnosticsChangedEventArgs(IReadOnlyList<Diagnostic> diagnostics)
        {
            Diagnostics = diagnostics;
        }
    }

    public interface IDiagnosticProvider
    {
        List<Diagnostic> Analyze(string text);
    }
}
