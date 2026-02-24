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
        public string ExcludePattern { get; set; }

        private Regex _compiledRegex;
        private Regex _compiledExclude;

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

        public Regex CompiledExclude
        {
            get
            {
                if (ExcludePattern == null) return null;
                if (_compiledExclude == null)
                    _compiledExclude = new Regex(ExcludePattern, RegexOptions.Compiled | RegexOptions.Multiline);
                return _compiledExclude;
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
        public Color GutterSeparatorColor { get; set; }
        public Color ActiveLineNumberColor { get; set; }
        public string LineCommentToken { get; set; }
        public Color IndentGuideColor { get; set; }

        public SyntaxRuleset(string languageName)
        {
            LanguageName = languageName;
            Rules = new List<SyntaxRule>();
            DefaultForeColor = Color.FromArgb(0, 0, 0);
            BackgroundColor = Color.FromArgb(255, 255, 255);
            LineNumberForeColor = Color.FromArgb(140, 140, 140);
            LineNumberBackColor = Color.FromArgb(245, 245, 245);
            CurrentLineHighlight = Color.FromArgb(232, 242, 254);
            SelectionColor = Color.FromArgb(100, 51, 153, 255);
            CaretColor = Color.Black;
            GutterSeparatorColor = Color.FromArgb(218, 218, 218);
            ActiveLineNumberColor = Color.FromArgb(50, 50, 50);
            LineCommentToken = null;
            IndentGuideColor = Color.FromArgb(220, 220, 220);
        }

        public void AddRule(string name, string pattern, Color foreColor, FontStyle fontStyle = FontStyle.Regular)
        {
            Rules.Add(new SyntaxRule(name, pattern, foreColor, fontStyle));
        }

        public void AddRule(string name, string pattern, Color foreColor, FontStyle fontStyle, string excludePattern)
        {
            var rule = new SyntaxRule(name, pattern, foreColor, fontStyle);
            rule.ExcludePattern = excludePattern;
            Rules.Add(rule);
        }

        public static SyntaxRuleset CreateCSharpRuleset()
        {
            var rs = new SyntaxRuleset("C#");
            rs.LineCommentToken = "//";
            rs.AddRule("Comment", @"//.*$|/\*[\s\S]*?\*/", Color.FromArgb(0, 128, 0), FontStyle.Italic);
            rs.AddRule("InterpolatedString", "\\$\"(?:[^\"\\\\]|\\\\.)*\"", Color.FromArgb(163, 21, 21), FontStyle.Regular, @"\{[^}]*\}");
            rs.AddRule("String", "\"(?:[^\"\\\\]|\\\\.)*\"|@\"(?:\"\"|[^\"])*\"", Color.FromArgb(163, 21, 21));
            rs.AddRule("Char", @"'(?:[^'\\]|\\.)'", Color.FromArgb(163, 21, 21));
            rs.AddRule("Keyword",
                @"\b(?:abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield|async|await|dynamic|nameof|when|where)\b",
                Color.FromArgb(0, 0, 255), FontStyle.Bold);
            rs.AddRule("Type",
                @"\b(?:Boolean|Byte|Char|DateTime|Decimal|Double|Guid|Int16|Int32|Int64|Object|SByte|Single|String|TimeSpan|UInt16|UInt32|UInt64|List|Dictionary|IEnumerable|Task|Action|Func|Tuple|Array|Console|Math|Exception)\b",
                Color.FromArgb(43, 145, 175));
            rs.AddRule("Number", @"\b\d+\.?\d*[fFdDmMlLuU]?\b|0x[0-9a-fA-F]+\b", Color.FromArgb(9, 134, 88));
            rs.AddRule("Attribute", @"\[[\w]+(?:\(.*?\))?\]", Color.FromArgb(43, 145, 175));
            rs.AddRule("Preprocessor", @"^\s*#\s*\w+.*$", Color.FromArgb(128, 128, 128));
            return rs;
        }

        public static SyntaxRuleset CreatePythonRuleset()
        {
            var rs = new SyntaxRuleset("Python");
            rs.LineCommentToken = "#";
            rs.AddRule("Comment", @"#.*$", Color.FromArgb(0, 128, 0), FontStyle.Italic);
            rs.AddRule("DocString", "\"\"\"[\\s\\S]*?\"\"\"|'''[\\s\\S]*?'''", Color.FromArgb(163, 21, 21), FontStyle.Italic);
            rs.AddRule("FString", "[fF]\"(?:[^\"\\\\]|\\\\.)*\"|[fF]'(?:[^'\\\\]|\\\\.)*'", Color.FromArgb(163, 21, 21), FontStyle.Regular, @"\{[^}]*\}");
            rs.AddRule("String", "\"(?:[^\"\\\\]|\\\\.)*\"|'(?:[^'\\\\]|\\\\.)*'", Color.FromArgb(163, 21, 21));
            rs.AddRule("Keyword",
                @"\b(?:False|None|True|and|as|assert|async|await|break|class|continue|def|del|elif|else|except|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|raise|return|try|while|with|yield)\b",
                Color.FromArgb(0, 0, 255), FontStyle.Bold);
            rs.AddRule("Builtin",
                @"\b(?:abs|all|any|bin|bool|chr|dict|dir|enumerate|eval|exec|filter|float|format|getattr|globals|hasattr|hash|hex|id|input|int|isinstance|issubclass|iter|len|list|locals|map|max|min|next|object|oct|open|ord|pow|print|property|range|repr|reversed|round|set|setattr|slice|sorted|staticmethod|str|sum|super|tuple|type|vars|zip)\b",
                Color.FromArgb(43, 145, 175));
            rs.AddRule("Decorator", @"@\w+(?:\.\w+)*", Color.FromArgb(116, 83, 31));
            rs.AddRule("Number", @"\b\d+\.?\d*[jJ]?\b|0[xXoObB][0-9a-fA-F]+\b", Color.FromArgb(9, 134, 88));
            rs.AddRule("Self", @"\bself\b", Color.FromArgb(0, 0, 255), FontStyle.Italic);
            return rs;
        }

        public static SyntaxRuleset CreateJavaScriptRuleset()
        {
            var rs = new SyntaxRuleset("JavaScript");
            rs.LineCommentToken = "//";
            rs.AddRule("Comment", @"//.*$|/\*[\s\S]*?\*/", Color.FromArgb(0, 128, 0), FontStyle.Italic);
            rs.AddRule("TemplateString", @"`(?:[^`\\]|\\.|\$\{[^}]*\})*`", Color.FromArgb(163, 21, 21), FontStyle.Regular, @"\$\{[^}]*\}");
            rs.AddRule("String", "\"(?:[^\"\\\\]|\\\\.)*\"|'(?:[^'\\\\]|\\\\.)*'", Color.FromArgb(163, 21, 21));
            rs.AddRule("Keyword",
                @"\b(?:break|case|catch|class|const|continue|debugger|default|delete|do|else|enum|export|extends|finally|for|function|if|import|in|instanceof|let|new|of|return|super|switch|this|throw|try|typeof|var|void|while|with|yield|async|await|from|as|static|get|set)\b",
                Color.FromArgb(0, 0, 255), FontStyle.Bold);
            rs.AddRule("Boolean", @"\b(?:true|false|null|undefined|NaN|Infinity)\b", Color.FromArgb(0, 0, 255));
            rs.AddRule("Number", @"\b\d+\.?\d*(?:e[+-]?\d+)?\b|0x[0-9a-fA-F]+\b", Color.FromArgb(9, 134, 88));
            rs.AddRule("Builtin",
                @"\b(?:console|document|window|Array|Object|String|Number|Boolean|Function|Symbol|Map|Set|Promise|RegExp|Date|Error|JSON|Math|parseInt|parseFloat|isNaN|isFinite|setTimeout|setInterval|clearTimeout|clearInterval|fetch|require|module|exports)\b",
                Color.FromArgb(43, 145, 175));
            rs.AddRule("Arrow", @"=>", Color.FromArgb(0, 0, 255));
            return rs;
        }

        public static SyntaxRuleset CreatePlainTextRuleset()
        {
            var rs = new SyntaxRuleset("Plain Text");
            return rs;
        }
    }
}
