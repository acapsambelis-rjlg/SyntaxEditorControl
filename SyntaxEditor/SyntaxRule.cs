using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace CodeEditor
{
    public class SyntaxRule
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        public Color ForeColor { get; set; }
        public FontStyle FontStyle { get; set; }

        private Regex _compiledRegex;

        public SyntaxRule(string name, string pattern, Color foreColor, FontStyle fontStyle = FontStyle.Regular)
        {
            Name = name;
            Pattern = pattern;
            ForeColor = foreColor;
            FontStyle = fontStyle;
        }

        public Regex CompiledRegex
        {
            get
            {
                if (_compiledRegex == null)
                    _compiledRegex = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.Multiline);
                return _compiledRegex;
            }
        }
    }

    public class SyntaxRuleset
    {
        public string LanguageName { get; set; }
        public List<SyntaxRule> Rules { get; private set; }
        public Color DefaultForeColor { get; set; }
        public Color BackgroundColor { get; set; }
        public Color LineNumberForeColor { get; set; }
        public Color LineNumberBackColor { get; set; }
        public Color CurrentLineHighlight { get; set; }
        public Color SelectionColor { get; set; }
        public Color CaretColor { get; set; }

        public SyntaxRuleset(string languageName)
        {
            LanguageName = languageName;
            Rules = new List<SyntaxRule>();
            DefaultForeColor = Color.FromArgb(212, 212, 212);
            BackgroundColor = Color.FromArgb(30, 30, 30);
            LineNumberForeColor = Color.FromArgb(110, 118, 129);
            LineNumberBackColor = Color.FromArgb(30, 30, 30);
            CurrentLineHighlight = Color.FromArgb(40, 40, 40);
            SelectionColor = Color.FromArgb(100, 60, 120, 200);
            CaretColor = Color.FromArgb(220, 220, 220);
        }

        public void AddRule(string name, string pattern, Color foreColor, FontStyle fontStyle = FontStyle.Regular)
        {
            Rules.Add(new SyntaxRule(name, pattern, foreColor, fontStyle));
        }

        public static SyntaxRuleset CreateCSharpRuleset()
        {
            var rs = new SyntaxRuleset("C#");
            rs.AddRule("Comment", @"//.*$|/\*[\s\S]*?\*/", Color.FromArgb(106, 153, 85), FontStyle.Italic);
            rs.AddRule("String", "\"(?:[^\"\\\\]|\\\\.)*\"|@\"(?:\"\"|[^\"])*\"", Color.FromArgb(206, 145, 120));
            rs.AddRule("Char", @"'(?:[^'\\]|\\.)'", Color.FromArgb(206, 145, 120));
            rs.AddRule("Keyword",
                @"\b(?:abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield|async|await|dynamic|nameof|when|where)\b",
                Color.FromArgb(86, 156, 214), FontStyle.Bold);
            rs.AddRule("Type",
                @"\b(?:Boolean|Byte|Char|DateTime|Decimal|Double|Guid|Int16|Int32|Int64|Object|SByte|Single|String|TimeSpan|UInt16|UInt32|UInt64|List|Dictionary|IEnumerable|Task|Action|Func|Tuple|Array|Console|Math|Exception)\b",
                Color.FromArgb(78, 201, 176));
            rs.AddRule("Number", @"\b\d+\.?\d*[fFdDmMlLuU]?\b|0x[0-9a-fA-F]+\b", Color.FromArgb(181, 206, 168));
            rs.AddRule("Attribute", @"\[[\w]+(?:\(.*?\))?\]", Color.FromArgb(78, 201, 176));
            rs.AddRule("Preprocessor", @"^\s*#\s*\w+.*$", Color.FromArgb(155, 155, 155));
            return rs;
        }

        public static SyntaxRuleset CreatePythonRuleset()
        {
            var rs = new SyntaxRuleset("Python");
            rs.AddRule("Comment", @"#.*$", Color.FromArgb(106, 153, 85), FontStyle.Italic);
            rs.AddRule("DocString", "\"\"\"[\\s\\S]*?\"\"\"|'''[\\s\\S]*?'''", Color.FromArgb(206, 145, 120), FontStyle.Italic);
            rs.AddRule("String", "\"(?:[^\"\\\\]|\\\\.)*\"|'(?:[^'\\\\]|\\\\.)*'", Color.FromArgb(206, 145, 120));
            rs.AddRule("Keyword",
                @"\b(?:False|None|True|and|as|assert|async|await|break|class|continue|def|del|elif|else|except|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|raise|return|try|while|with|yield)\b",
                Color.FromArgb(86, 156, 214), FontStyle.Bold);
            rs.AddRule("Builtin",
                @"\b(?:abs|all|any|bin|bool|chr|dict|dir|enumerate|eval|exec|filter|float|format|getattr|globals|hasattr|hash|hex|id|input|int|isinstance|issubclass|iter|len|list|locals|map|max|min|next|object|oct|open|ord|pow|print|property|range|repr|reversed|round|set|setattr|slice|sorted|staticmethod|str|sum|super|tuple|type|vars|zip)\b",
                Color.FromArgb(220, 220, 170));
            rs.AddRule("Decorator", @"@\w+(?:\.\w+)*", Color.FromArgb(220, 220, 170));
            rs.AddRule("Number", @"\b\d+\.?\d*[jJ]?\b|0[xXoObB][0-9a-fA-F]+\b", Color.FromArgb(181, 206, 168));
            rs.AddRule("Self", @"\bself\b", Color.FromArgb(86, 156, 214), FontStyle.Italic);
            return rs;
        }

        public static SyntaxRuleset CreateJavaScriptRuleset()
        {
            var rs = new SyntaxRuleset("JavaScript");
            rs.AddRule("Comment", @"//.*$|/\*[\s\S]*?\*/", Color.FromArgb(106, 153, 85), FontStyle.Italic);
            rs.AddRule("TemplateString", @"`(?:[^`\\]|\\.|\$\{[^}]*\})*`", Color.FromArgb(206, 145, 120));
            rs.AddRule("String", "\"(?:[^\"\\\\]|\\\\.)*\"|'(?:[^'\\\\]|\\\\.)*'", Color.FromArgb(206, 145, 120));
            rs.AddRule("Keyword",
                @"\b(?:break|case|catch|class|const|continue|debugger|default|delete|do|else|enum|export|extends|finally|for|function|if|import|in|instanceof|let|new|of|return|super|switch|this|throw|try|typeof|var|void|while|with|yield|async|await|from|as|static|get|set)\b",
                Color.FromArgb(86, 156, 214), FontStyle.Bold);
            rs.AddRule("Boolean", @"\b(?:true|false|null|undefined|NaN|Infinity)\b", Color.FromArgb(86, 156, 214));
            rs.AddRule("Number", @"\b\d+\.?\d*(?:e[+-]?\d+)?\b|0x[0-9a-fA-F]+\b", Color.FromArgb(181, 206, 168));
            rs.AddRule("Builtin",
                @"\b(?:console|document|window|Array|Object|String|Number|Boolean|Function|Symbol|Map|Set|Promise|RegExp|Date|Error|JSON|Math|parseInt|parseFloat|isNaN|isFinite|setTimeout|setInterval|clearTimeout|clearInterval|fetch|require|module|exports)\b",
                Color.FromArgb(78, 201, 176));
            rs.AddRule("Arrow", @"=>", Color.FromArgb(86, 156, 214));
            return rs;
        }

        public static SyntaxRuleset CreatePlainTextRuleset()
        {
            var rs = new SyntaxRuleset("Plain Text");
            rs.DefaultForeColor = Color.FromArgb(212, 212, 212);
            return rs;
        }
    }
}
