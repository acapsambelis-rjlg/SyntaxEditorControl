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

    public enum SymbolKind
    {
        Class,
        Struct,
        Interface,
        Enum,
        Function,
        Method,
        Property,
        Field,
        Variable,
        Constant,
        Namespace,
        Module,
        Event,
        Delegate,
        TypeAlias
    }

    public class SymbolInfo
    {
        public string Name { get; set; }
        public SymbolKind Kind { get; set; }
        public string ReturnType { get; set; }
        public List<string> ParameterTypes { get; set; }
        public bool IsPublic { get; set; }
        public bool IsStatic { get; set; }
        public string ParentSymbol { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public SymbolInfo(string name, SymbolKind kind)
        {
            Name = name;
            Kind = kind;
            IsPublic = true;
            ParameterTypes = new List<string>();
            Metadata = new Dictionary<string, object>();
        }
    }

    public class FileSymbols
    {
        public string FilePath { get; set; }
        public string LanguageName { get; set; }
        public List<SymbolInfo> ExportedSymbols { get; set; }

        public FileSymbols(string filePath, string languageName)
        {
            FilePath = filePath;
            LanguageName = languageName;
            ExportedSymbols = new List<SymbolInfo>();
        }

        public bool HasSymbol(string name)
        {
            for (int i = 0; i < ExportedSymbols.Count; i++)
            {
                if (ExportedSymbols[i].Name == name) return true;
            }
            return false;
        }

        public SymbolInfo GetSymbol(string name)
        {
            for (int i = 0; i < ExportedSymbols.Count; i++)
            {
                if (ExportedSymbols[i].Name == name) return ExportedSymbols[i];
            }
            return null;
        }

        public List<SymbolInfo> GetSymbolsByKind(SymbolKind kind)
        {
            var result = new List<SymbolInfo>();
            for (int i = 0; i < ExportedSymbols.Count; i++)
            {
                if (ExportedSymbols[i].Kind == kind) result.Add(ExportedSymbols[i]);
            }
            return result;
        }
    }

    public class AnalysisContext
    {
        public string CurrentFilePath { get; set; }
        public Dictionary<string, FileSymbols> FileSymbolMap { get; private set; }

        public AnalysisContext()
        {
            FileSymbolMap = new Dictionary<string, FileSymbols>();
        }

        public AnalysisContext(string currentFilePath) : this()
        {
            CurrentFilePath = currentFilePath;
        }

        public void AddFileSymbols(FileSymbols symbols)
        {
            FileSymbolMap[symbols.FilePath] = symbols;
        }

        public void RemoveFileSymbols(string filePath)
        {
            FileSymbolMap.Remove(filePath);
        }

        public bool ResolveSymbol(string name, out SymbolInfo symbol, out string sourceFile)
        {
            foreach (var kvp in FileSymbolMap)
            {
                if (kvp.Key == CurrentFilePath) continue;
                var found = kvp.Value.GetSymbol(name);
                if (found != null)
                {
                    symbol = found;
                    sourceFile = kvp.Key;
                    return true;
                }
            }
            symbol = null;
            sourceFile = null;
            return false;
        }

        public List<string> GetAllExportedSymbolNames()
        {
            var names = new List<string>();
            foreach (var kvp in FileSymbolMap)
            {
                if (kvp.Key == CurrentFilePath) continue;
                foreach (var sym in kvp.Value.ExportedSymbols)
                {
                    if (!names.Contains(sym.Name))
                        names.Add(sym.Name);
                }
            }
            return names;
        }

        public List<SymbolInfo> GetAllExportedSymbols()
        {
            var symbols = new List<SymbolInfo>();
            foreach (var kvp in FileSymbolMap)
            {
                if (kvp.Key == CurrentFilePath) continue;
                symbols.AddRange(kvp.Value.ExportedSymbols);
            }
            return symbols;
        }
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
        List<Diagnostic> Analyze(string text, AnalysisContext context);
    }
}
