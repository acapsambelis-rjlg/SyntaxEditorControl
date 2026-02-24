using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeEditor
{
    public enum CompletionItemKind
    {
        Keyword,
        Type,
        Function,
        Variable,
        Property,
        Snippet,
        Constant,
        Module
    }

    public class CompletionItem
    {
        public string Text { get; set; }
        public CompletionItemKind Kind { get; set; }
        public string Description { get; set; }

        public CompletionItem(string text, CompletionItemKind kind, string description = "")
        {
            Text = text;
            Kind = kind;
            Description = description;
        }
    }

    public interface ICompletionProvider
    {
        List<CompletionItem> GetCompletions(string text, TextPosition caret, string partialWord);
    }

    public class CSharpCompletionProvider : ICompletionProvider
    {
        private static readonly string[] Keywords = {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "var",
            "virtual", "void", "volatile", "while", "yield", "async", "await", "dynamic", "nameof",
            "when", "where"
        };

        private static readonly string[] Types = {
            "Boolean", "Byte", "Char", "DateTime", "Decimal", "Double", "Guid", "Int16", "Int32",
            "Int64", "Object", "SByte", "Single", "String", "TimeSpan", "UInt16", "UInt32", "UInt64",
            "List", "Dictionary", "IEnumerable", "Task", "Action", "Func", "Tuple", "Array",
            "Console", "Math", "Exception", "StringBuilder", "HashSet", "Queue", "Stack",
            "IDisposable", "IComparable", "EventArgs", "Nullable"
        };

        private static readonly string[] Snippets = {
            "Console.WriteLine", "Console.ReadLine", "string.IsNullOrEmpty",
            "string.Format", "Math.Max", "Math.Min", "Math.Abs",
            "ToString", "GetType", "Equals", "GetHashCode"
        };

        public List<CompletionItem> GetCompletions(string text, TextPosition caret, string partialWord)
        {
            var items = new List<CompletionItem>();
            if (string.IsNullOrEmpty(partialWord)) return items;

            string lower = partialWord.ToLower();

            foreach (var kw in Keywords)
                if (kw.StartsWith(lower, StringComparison.OrdinalIgnoreCase))
                    items.Add(new CompletionItem(kw, CompletionItemKind.Keyword, "keyword"));

            foreach (var t in Types)
                if (t.StartsWith(lower, StringComparison.OrdinalIgnoreCase))
                    items.Add(new CompletionItem(t, CompletionItemKind.Type, "type"));

            foreach (var s in Snippets)
                if (s.StartsWith(lower, StringComparison.OrdinalIgnoreCase))
                    items.Add(new CompletionItem(s, CompletionItemKind.Function, "method"));

            AddDocumentWords(text, partialWord, items);

            return items;
        }

        private void AddDocumentWords(string text, string partial, List<CompletionItem> items)
        {
            var seen = new HashSet<string>();
            foreach (var item in items) seen.Add(item.Text);

            var wordMatches = Regex.Matches(text, @"\b[A-Za-z_]\w{2,}\b");
            foreach (Match m in wordMatches)
            {
                string w = m.Value;
                if (w != partial && !seen.Contains(w) && w.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new CompletionItem(w, CompletionItemKind.Variable, "document word"));
                    seen.Add(w);
                }
            }
        }
    }

    public class PythonCompletionProvider : ICompletionProvider
    {
        private static readonly string[] Keywords = {
            "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class",
            "continue", "def", "del", "elif", "else", "except", "finally", "for", "from", "global",
            "if", "import", "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise",
            "return", "try", "while", "with", "yield"
        };

        private static readonly string[] Builtins = {
            "abs", "all", "any", "bin", "bool", "chr", "dict", "dir", "enumerate", "eval", "exec",
            "filter", "float", "format", "getattr", "globals", "hasattr", "hash", "hex", "id",
            "input", "int", "isinstance", "issubclass", "iter", "len", "list", "locals", "map",
            "max", "min", "next", "object", "oct", "open", "ord", "pow", "print", "property",
            "range", "repr", "reversed", "round", "set", "setattr", "slice", "sorted",
            "staticmethod", "str", "sum", "super", "tuple", "type", "vars", "zip"
        };

        public List<CompletionItem> GetCompletions(string text, TextPosition caret, string partialWord)
        {
            var items = new List<CompletionItem>();
            if (string.IsNullOrEmpty(partialWord)) return items;

            foreach (var kw in Keywords)
                if (kw.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
                    items.Add(new CompletionItem(kw, CompletionItemKind.Keyword, "keyword"));

            foreach (var b in Builtins)
                if (b.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
                    items.Add(new CompletionItem(b, CompletionItemKind.Function, "builtin"));

            AddDocumentWords(text, partialWord, items);
            return items;
        }

        private void AddDocumentWords(string text, string partial, List<CompletionItem> items)
        {
            var seen = new HashSet<string>();
            foreach (var item in items) seen.Add(item.Text);

            var wordMatches = Regex.Matches(text, @"\b[A-Za-z_]\w{2,}\b");
            foreach (Match m in wordMatches)
            {
                string w = m.Value;
                if (w != partial && !seen.Contains(w) && w.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new CompletionItem(w, CompletionItemKind.Variable, "document word"));
                    seen.Add(w);
                }
            }
        }
    }

    public class JavaScriptCompletionProvider : ICompletionProvider
    {
        private static readonly string[] Keywords = {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete",
            "do", "else", "enum", "export", "extends", "finally", "for", "function", "if", "import",
            "in", "instanceof", "let", "new", "of", "return", "super", "switch", "this", "throw",
            "try", "typeof", "var", "void", "while", "with", "yield", "async", "await", "from",
            "as", "static", "get", "set"
        };

        private static readonly string[] Builtins = {
            "console", "document", "window", "Array", "Object", "String", "Number", "Boolean",
            "Function", "Symbol", "Map", "Set", "Promise", "RegExp", "Date", "Error", "JSON",
            "Math", "parseInt", "parseFloat", "isNaN", "isFinite", "setTimeout", "setInterval",
            "clearTimeout", "clearInterval", "fetch", "require", "module", "exports",
            "undefined", "null", "true", "false", "NaN", "Infinity"
        };

        private static readonly string[] Snippets = {
            "console.log", "console.error", "console.warn",
            "JSON.stringify", "JSON.parse",
            "Array.isArray", "Object.keys", "Object.values", "Object.entries",
            "Promise.all", "Promise.resolve", "Promise.reject",
            "addEventListener", "removeEventListener", "querySelector", "querySelectorAll"
        };

        public List<CompletionItem> GetCompletions(string text, TextPosition caret, string partialWord)
        {
            var items = new List<CompletionItem>();
            if (string.IsNullOrEmpty(partialWord)) return items;

            foreach (var kw in Keywords)
                if (kw.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
                    items.Add(new CompletionItem(kw, CompletionItemKind.Keyword, "keyword"));

            foreach (var b in Builtins)
                if (b.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
                    items.Add(new CompletionItem(b, CompletionItemKind.Type, "builtin"));

            foreach (var s in Snippets)
                if (s.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
                    items.Add(new CompletionItem(s, CompletionItemKind.Function, "method"));

            AddDocumentWords(text, partialWord, items);
            return items;
        }

        private void AddDocumentWords(string text, string partial, List<CompletionItem> items)
        {
            var seen = new HashSet<string>();
            foreach (var item in items) seen.Add(item.Text);

            var wordMatches = Regex.Matches(text, @"\b[A-Za-z_]\w{2,}\b");
            foreach (Match m in wordMatches)
            {
                string w = m.Value;
                if (w != partial && !seen.Contains(w) && w.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new CompletionItem(w, CompletionItemKind.Variable, "document word"));
                    seen.Add(w);
                }
            }
        }
    }
}
