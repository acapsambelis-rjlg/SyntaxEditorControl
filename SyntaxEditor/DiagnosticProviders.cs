using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeEditor
{
    public class CSharpDiagnosticProvider : IDiagnosticProvider
    {
        public List<Diagnostic> Analyze(string text)
        {
            return Analyze(text, null);
        }

        public List<Diagnostic> Analyze(string text, AnalysisContext context)
        {
            var diagnostics = new List<Diagnostic>();
            var lines = text.Split('\n');

            CheckBracketBalance(lines, diagnostics);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();

                if (IsInsideBlockComment(lines, i)) continue;
                if (trimmed.StartsWith("//")) continue;

                CheckSemicolonMissing(lines, i, line, trimmed, diagnostics);
                CheckEmptyCatchBlock(lines, i, trimmed, diagnostics);
                CheckEmptyBlock(lines, i, trimmed, diagnostics);
                CheckUnusedVariable(lines, i, line, text, diagnostics);
                CheckConsoleWriteLineArgs(lines, i, line, diagnostics);
                CheckComparisonInsteadOfAssignment(lines, i, line, trimmed, diagnostics);
                CheckThisQualifier(lines, i, line, diagnostics);
                CheckStringTypeName(lines, i, line, trimmed, diagnostics);
                CheckCommonTypos(lines, i, line, diagnostics);
            }

            CheckUnterminatedStrings(lines, diagnostics);
            CheckDuplicateDeclarations(lines, diagnostics);
            CheckUnreachableCode(lines, diagnostics);

            return diagnostics;
        }

        private void CheckSemicolonMissing(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(trimmed)) return;
            if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*")) return;
            if (trimmed.StartsWith("using ") && !trimmed.Contains("(")) 
            {
                if (!trimmed.EndsWith(";"))
                {
                    diagnostics.Add(new Diagnostic(i, line.Length, 1, "Expected ';' at end of using directive", DiagnosticSeverity.Error));
                }
                return;
            }

            var statementPattern = new Regex(@"^\s*(?:return|throw|var|int|string|bool|double|float|char|long|byte|short|decimal|object|dynamic)\s+.+[^;{}\s]\s*$");
            if (statementPattern.IsMatch(line))
            {
                if (!trimmed.EndsWith(";") && !trimmed.EndsWith("{") && !trimmed.EndsWith("}") && !trimmed.EndsWith(","))
                {
                    diagnostics.Add(new Diagnostic(i, line.Length, 1, "Possible missing semicolon", DiagnosticSeverity.Warning));
                }
            }
        }

        private void CheckEmptyCatchBlock(string[] lines, int i, string trimmed, List<Diagnostic> diagnostics)
        {
            if (!trimmed.StartsWith("catch")) return;

            for (int j = i + 1; j < lines.Length; j++)
            {
                string next = lines[j].Trim();
                if (next == "{") continue;
                if (next == "}")
                {
                    int col = lines[i].IndexOf("catch");
                    diagnostics.Add(new Diagnostic(i, col, 5, "Empty catch block; consider logging the exception", DiagnosticSeverity.Warning));
                }
                break;
            }
        }

        private void CheckUnusedVariable(string[] lines, int i, string line, string fullText, List<Diagnostic> diagnostics)
        {
            var localVarMatch = Regex.Match(line, @"\b(?:var|int|string|bool|double|float)\s+(\w+)\s*=");
            if (!localVarMatch.Success) return;

            string varName = localVarMatch.Groups[1].Value;
            int afterDecl = fullText.IndexOf(line) + line.Length;
            if (afterDecl >= fullText.Length) return;

            string rest = fullText.Substring(afterDecl);
            if (!Regex.IsMatch(rest, @"\b" + Regex.Escape(varName) + @"\b"))
            {
                int col = localVarMatch.Groups[1].Index;
                diagnostics.Add(new Diagnostic(i, col, varName.Length,
                    $"Variable '{varName}' is assigned but its value is never used", DiagnosticSeverity.Warning));
            }
        }

        private void CheckConsoleWriteLineArgs(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"Console\.Write(Line)?\(\s*\)");
            if (match.Success)
            {
                if (match.Value.Contains("WriteLine"))
                    return;
                diagnostics.Add(new Diagnostic(i, match.Index, match.Length,
                    "Console.Write() called with no arguments", DiagnosticSeverity.Info));
            }
        }

        private void CheckComparisonInsteadOfAssignment(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(trimmed, @"^if\s*\(.*[^=!<>]=[^=].*\)");
            if (match.Success)
            {
                int col = line.IndexOf("if");
                diagnostics.Add(new Diagnostic(i, col, match.Length,
                    "Possible accidental assignment in condition; did you mean '=='?", DiagnosticSeverity.Warning));
            }
        }

        private void CheckThisQualifier(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"\bthis\.(\w+)");
            if (match.Success)
            {
                diagnostics.Add(new Diagnostic(i, match.Index, 5,
                    "'this.' qualifier is unnecessary", DiagnosticSeverity.Hint));
            }
        }

        private void CheckStringTypeName(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"\bString\b");
            if (match.Success && !trimmed.StartsWith("//") && !trimmed.StartsWith("using"))
            {
                diagnostics.Add(new Diagnostic(i, match.Index, 6,
                    "Use keyword 'string' instead of 'String'", DiagnosticSeverity.Hint));
            }
            match = Regex.Match(line, @"\bInt32\b");
            if (match.Success && !trimmed.StartsWith("//"))
            {
                diagnostics.Add(new Diagnostic(i, match.Index, 5,
                    "Use keyword 'int' instead of 'Int32'", DiagnosticSeverity.Hint));
            }
            match = Regex.Match(line, @"\bBoolean\b");
            if (match.Success && !trimmed.StartsWith("//"))
            {
                diagnostics.Add(new Diagnostic(i, match.Index, 7,
                    "Use keyword 'bool' instead of 'Boolean'", DiagnosticSeverity.Hint));
            }
        }

        private void CheckEmptyBlock(string[] lines, int i, string trimmed, List<Diagnostic> diagnostics)
        {
            if (trimmed.StartsWith("catch")) return;
            var match = Regex.Match(trimmed, @"^(if|else if|else|for|foreach|while|switch)\b");
            if (!match.Success) return;

            for (int j = i + 1; j < lines.Length; j++)
            {
                string next = lines[j].Trim();
                if (next == "{") continue;
                if (next == "}")
                {
                    int col = lines[i].IndexOf(match.Groups[1].Value);
                    diagnostics.Add(new Diagnostic(i, col, match.Groups[1].Length,
                        $"Empty '{match.Groups[1].Value}' block", DiagnosticSeverity.Warning));
                }
                break;
            }
        }

        private void CheckCommonTypos(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var typos = new Dictionary<string, string>
            {
                { @"\bConsle\b", "Console" }, { @"\bConsoel\b", "Console" },
                { @"\bWritLine\b", "WriteLine" }, { @"\bWriteLien\b", "WriteLine" },
                { @"\bReadLien\b", "ReadLine" },
                { @"\bStirng\b", "String" }, { @"\bStrign\b", "String" },
                { @"\bLsit\b", "List" }, { @"\bDictinoary\b", "Dictionary" },
                { @"\bDicitonary\b", "Dictionary" },
                { @"\bLenght\b", "Length" }, { @"\bLegnth\b", "Length" },
                { @"\bCoutn\b", "Count" },
                { @"\bTostirng\b", "ToString" }, { @"\bToStirng\b", "ToString" },
                { @"\bNamepsace\b", "Namespace" },
                { @"\bretrun\b", "return" }, { @"\breture\b", "return" },
            };

            foreach (var kv in typos)
            {
                var m = Regex.Match(line, kv.Key);
                if (m.Success)
                {
                    diagnostics.Add(new Diagnostic(i, m.Index, m.Length,
                        $"Did you mean '{kv.Value}'?", DiagnosticSeverity.Error));
                }
            }
        }

        private void CheckUnterminatedStrings(string[] lines, List<Diagnostic> diagnostics)
        {
            bool inBlockComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();

                for (int j = 0; j < line.Length - 1; j++)
                {
                    if (!inBlockComment && line[j] == '/' && line[j + 1] == '*') { inBlockComment = true; j++; continue; }
                    if (inBlockComment && line[j] == '*' && line[j + 1] == '/') { inBlockComment = false; j++; continue; }
                }
                if (inBlockComment) continue;
                if (trimmed.StartsWith("//")) continue;

                bool inStr = false;
                bool isVerbatim = false;
                char strCh = '"';
                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];
                    if (inStr)
                    {
                        if (isVerbatim)
                        {
                            if (c == '"' && j + 1 < line.Length && line[j + 1] == '"') { j++; continue; }
                            if (c == '"') { inStr = false; continue; }
                        }
                        else
                        {
                            if (c == '\\' && j + 1 < line.Length) { j++; continue; }
                            if (c == strCh) { inStr = false; continue; }
                        }
                        continue;
                    }
                    if (c == '/' && j + 1 < line.Length && line[j + 1] == '/') break;
                    if (c == '@' && j + 1 < line.Length && line[j + 1] == '"') { inStr = true; isVerbatim = true; strCh = '"'; j++; continue; }
                    if (c == '$' && j + 1 < line.Length && line[j + 1] == '"') { inStr = true; isVerbatim = false; strCh = '"'; j++; continue; }
                    if (c == '"' || c == '\'') { inStr = true; isVerbatim = false; strCh = c; continue; }
                }
                if (inStr && !isVerbatim)
                {
                    diagnostics.Add(new Diagnostic(i, line.Length - 1, 1,
                        "Unterminated string literal", DiagnosticSeverity.Error));
                }
            }
        }

        private void CheckDuplicateDeclarations(string[] lines, List<Diagnostic> diagnostics)
        {
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (IsInsideBlockComment(lines, i)) continue;
                if (trimmed.StartsWith("//")) continue;

                var methodMatch = Regex.Match(trimmed, @"^(?:public|private|protected|internal|static|\s)*\s+(?:void|int|string|bool|double|float|Task|async\s+Task)\s+(\w+)\s*\(");
                if (methodMatch.Success)
                {
                    string name = methodMatch.Groups[1].Value;
                    string key = "method:" + name;
                    if (seen.ContainsKey(key))
                    {
                        int col = lines[i].IndexOf(name);
                        diagnostics.Add(new Diagnostic(i, col, name.Length,
                            $"Method '{name}' is already declared on line {seen[key] + 1} (if not an overload, this is a duplicate)", DiagnosticSeverity.Info));
                    }
                    else
                    {
                        seen[key] = i;
                    }
                }

                var varMatch = Regex.Match(trimmed, @"^(?:var|int|string|bool|double|float|char|long|byte|short|decimal|object|dynamic)\s+(\w+)\s*[=;]");
                if (varMatch.Success)
                {
                    string name = varMatch.Groups[1].Value;
                    int indent = lines[i].Length - trimmed.Length;
                    string key = "var:" + name + ":" + indent;
                    if (seen.ContainsKey(key))
                    {
                        int col = lines[i].IndexOf(name, lines[i].Length - trimmed.Length);
                        diagnostics.Add(new Diagnostic(i, col, name.Length,
                            $"Variable '{name}' is already declared on line {seen[key] + 1}", DiagnosticSeverity.Warning));
                    }
                    else
                    {
                        seen[key] = i;
                    }
                }
            }
        }

        private void CheckUnreachableCode(string[] lines, List<Diagnostic> diagnostics)
        {
            for (int i = 0; i < lines.Length - 1; i++)
            {
                if (IsInsideBlockComment(lines, i)) continue;
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//")) continue;

                bool isTerminator = Regex.IsMatch(trimmed, @"^(return\b|break\s*;|continue\s*;|throw\b)");
                if (!isTerminator) continue;

                int indent = lines[i].Length - trimmed.Length;

                for (int j = i + 1; j < lines.Length; j++)
                {
                    string nextTrimmed = lines[j].TrimStart();
                    if (string.IsNullOrWhiteSpace(nextTrimmed)) continue;
                    if (nextTrimmed.StartsWith("//") || nextTrimmed.StartsWith("/*") || nextTrimmed.StartsWith("*")) continue;
                    if (nextTrimmed == "}" || nextTrimmed == "{") break;

                    int nextIndent = lines[j].Length - nextTrimmed.Length;
                    if (nextIndent <= indent) break;

                    if (nextTrimmed.StartsWith("case ") || nextTrimmed.StartsWith("default:")) break;

                    diagnostics.Add(new Diagnostic(j, nextIndent, nextTrimmed.Length,
                        "Unreachable code detected", DiagnosticSeverity.Warning));
                    break;
                }
            }
        }

        private void CheckBracketBalance(string[] lines, List<Diagnostic> diagnostics)
        {
            var stack = new Stack<Tuple<char, int, int>>();
            bool inString = false;
            bool inLineComment = false;
            bool inBlockComment = false;
            char stringChar = '"';

            for (int i = 0; i < lines.Length; i++)
            {
                inLineComment = false;
                for (int j = 0; j < lines[i].Length; j++)
                {
                    char c = lines[i][j];
                    char prev = j > 0 ? lines[i][j - 1] : '\0';

                    if (inBlockComment)
                    {
                        if (c == '/' && prev == '*') inBlockComment = false;
                        continue;
                    }
                    if (inLineComment) continue;
                    if (inString)
                    {
                        if (c == stringChar && prev != '\\') inString = false;
                        continue;
                    }

                    if (c == '/' && j + 1 < lines[i].Length && lines[i][j + 1] == '/') { inLineComment = true; continue; }
                    if (c == '/' && j + 1 < lines[i].Length && lines[i][j + 1] == '*') { inBlockComment = true; continue; }
                    if (c == '"' || c == '\'') { inString = true; stringChar = c; continue; }

                    if (c == '(' || c == '[' || c == '{')
                        stack.Push(Tuple.Create(c, i, j));
                    else if (c == ')' || c == ']' || c == '}')
                    {
                        char expected = c == ')' ? '(' : c == ']' ? '[' : '{';
                        if (stack.Count == 0)
                        {
                            diagnostics.Add(new Diagnostic(i, j, 1,
                                $"Unmatched closing '{c}'", DiagnosticSeverity.Error));
                        }
                        else if (stack.Peek().Item1 != expected)
                        {
                            var top = stack.Pop();
                            diagnostics.Add(new Diagnostic(i, j, 1,
                                $"Mismatched bracket: expected closing for '{top.Item1}' from line {top.Item2 + 1}", DiagnosticSeverity.Error));
                        }
                        else
                        {
                            stack.Pop();
                        }
                    }
                }
            }

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                diagnostics.Add(new Diagnostic(item.Item2, item.Item3, 1,
                    $"Unclosed '{item.Item1}'", DiagnosticSeverity.Error));
            }
        }

        private bool IsInsideBlockComment(string[] lines, int targetLine)
        {
            bool inBlock = false;
            for (int i = 0; i < targetLine; i++)
            {
                for (int j = 0; j < lines[i].Length; j++)
                {
                    if (!inBlock && j + 1 < lines[i].Length && lines[i][j] == '/' && lines[i][j + 1] == '*')
                    { inBlock = true; j++; }
                    else if (inBlock && j + 1 < lines[i].Length && lines[i][j] == '*' && lines[i][j + 1] == '/')
                    { inBlock = false; j++; }
                }
            }
            return inBlock;
        }
    }

    public class PythonDiagnosticProvider : IDiagnosticProvider
    {
        public List<Diagnostic> Analyze(string text)
        {
            return Analyze(text, null);
        }

        public List<Diagnostic> Analyze(string text, AnalysisContext context)
        {
            var diagnostics = new List<Diagnostic>();
            var lines = text.Split('\n');

            CheckIndentation(lines, diagnostics);
            CheckBracketBalance(lines, diagnostics);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();

                if (trimmed.StartsWith("#")) continue;
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                CheckColonMissing(lines, i, line, trimmed, diagnostics);
                CheckMutableDefaultArgument(lines, i, line, diagnostics);
                CheckBareExcept(lines, i, trimmed, diagnostics);
                CheckTabMixing(lines, i, line, diagnostics);
                CheckComparisonToNone(lines, i, line, diagnostics);
                CheckBoolComparison(lines, i, line, diagnostics);
                CheckTypeComparison(lines, i, line, diagnostics);
                CheckCommonTypos(lines, i, line, diagnostics);
                CheckEmptyBlock(lines, i, line, trimmed, diagnostics);
                CheckDuplicateKeys(lines, i, line, trimmed, diagnostics);
            }

            CheckUnterminatedStrings(lines, diagnostics);
            CheckDuplicateDeclarations(lines, diagnostics);
            CheckUnreachableCode(lines, diagnostics);

            return diagnostics;
        }

        private void CheckColonMissing(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            var blockPattern = new Regex(@"^\s*(def|class|if|elif|else|for|while|try|except|finally|with|async\s+def|async\s+for|async\s+with)\b");
            var match = blockPattern.Match(line);
            if (!match.Success) return;

            if (!trimmed.EndsWith(":") && !trimmed.EndsWith(":\\") && !trimmed.EndsWith(","))
            {
                if (!trimmed.Contains("#") || trimmed.IndexOf("#") < trimmed.Length - 1)
                {
                    string beforeComment = trimmed;
                    int hashIdx = FindUnquotedHash(trimmed);
                    if (hashIdx >= 0) beforeComment = trimmed.Substring(0, hashIdx).TrimEnd();

                    if (!beforeComment.EndsWith(":") && !beforeComment.EndsWith(",") && !beforeComment.EndsWith("\\"))
                    {
                        diagnostics.Add(new Diagnostic(i, line.Length, 1,
                            $"Expected ':' at end of '{match.Groups[1].Value}' statement", DiagnosticSeverity.Error));
                    }
                }
            }
        }

        private int FindUnquotedHash(string s)
        {
            bool inSingle = false, inDouble = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\'' && !inDouble) inSingle = !inSingle;
                else if (c == '"' && !inSingle) inDouble = !inDouble;
                else if (c == '#' && !inSingle && !inDouble) return i;
            }
            return -1;
        }

        private void CheckMutableDefaultArgument(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"def\s+\w+\(.*?(\w+)\s*=\s*(\[\]|\{\}|\[\s*\]|\{\s*\})");
            if (match.Success)
            {
                int col = match.Groups[1].Index;
                diagnostics.Add(new Diagnostic(i, col, match.Groups[1].Length + match.Groups[2].Length + 1,
                    $"Mutable default argument '{match.Groups[2].Value}'; use None and assign inside the function",
                    DiagnosticSeverity.Warning));
            }
        }

        private void CheckBareExcept(string[] lines, int i, string trimmed, List<Diagnostic> diagnostics)
        {
            if (Regex.IsMatch(trimmed, @"^except\s*:"))
            {
                int col = lines[i].IndexOf("except");
                diagnostics.Add(new Diagnostic(i, col, 6,
                    "Bare 'except:' catches all exceptions including SystemExit and KeyboardInterrupt; specify an exception type",
                    DiagnosticSeverity.Warning));
            }
        }

        private void CheckTabMixing(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            if (line.Length > 0 && line[0] == ' ')
            {
                if (line.Contains("\t"))
                {
                    diagnostics.Add(new Diagnostic(i, 0, line.Length - line.TrimStart().Length,
                        "Mixed tabs and spaces in indentation", DiagnosticSeverity.Error));
                }
            }
        }

        private void CheckComparisonToNone(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"(\w+)\s*==\s*None");
            if (match.Success)
            {
                diagnostics.Add(new Diagnostic(i, match.Index, match.Length,
                    "Use 'is None' instead of '== None'", DiagnosticSeverity.Info));
            }

            match = Regex.Match(line, @"(\w+)\s*!=\s*None");
            if (match.Success)
            {
                diagnostics.Add(new Diagnostic(i, match.Index, match.Length,
                    "Use 'is not None' instead of '!= None'", DiagnosticSeverity.Info));
            }
        }

        private void CheckBoolComparison(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"==\s*True\b");
            if (match.Success)
            {
                diagnostics.Add(new Diagnostic(i, match.Index, match.Length,
                    "Comparison to True; simplify to just the expression", DiagnosticSeverity.Hint));
            }
            match = Regex.Match(line, @"==\s*False\b");
            if (match.Success)
            {
                diagnostics.Add(new Diagnostic(i, match.Index, match.Length,
                    "Comparison to False; use 'not' instead", DiagnosticSeverity.Hint));
            }
        }

        private void CheckTypeComparison(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"type\((\w+)\)\s*==");
            if (match.Success)
            {
                diagnostics.Add(new Diagnostic(i, match.Index, match.Length,
                    "Use 'isinstance()' instead of comparing type()", DiagnosticSeverity.Hint));
            }
        }

        private void CheckCommonTypos(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var typos = new Dictionary<string, string>
            {
                { @"\bpirnt\b", "print" }, { @"\bpritn\b", "print" }, { @"\bptint\b", "print" },
                { @"\bimpotr\b", "import" }, { @"\bimoprt\b", "import" },
                { @"\bfrom\s+\w+\s+impotr\b", "import" },
                { @"\bretrun\b", "return" }, { @"\breture\b", "return" },
                { @"\bflase\b", "False" }, { @"\btrue\b(?!\s*=)", "True" },
                { @"\bnoen\b", "None" },
                { @"\blenght\b", "length" }, { @"\blegnth\b", "length" },
                { @"\bappned\b", "append" }, { @"\bextned\b", "extend" },
                { @"\binsret\b", "insert" },
                { @"\bdefualt\b", "default" }, { @"\bdefautl\b", "default" },
            };

            foreach (var kv in typos)
            {
                var m = Regex.Match(line, kv.Key);
                if (m.Success)
                {
                    diagnostics.Add(new Diagnostic(i, m.Index, m.Length,
                        $"Did you mean '{kv.Value}'?", DiagnosticSeverity.Error));
                }
            }
        }

        private void CheckEmptyBlock(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(trimmed, @"^(if|elif|else|for|while|with|try|except|finally)\b.*:\s*$");
            if (!match.Success) return;

            for (int j = i + 1; j < lines.Length; j++)
            {
                string nextTrimmed = lines[j].TrimStart();
                if (string.IsNullOrWhiteSpace(nextTrimmed)) continue;
                if (nextTrimmed.StartsWith("#")) continue;

                int blockIndent = line.Length - trimmed.Length;
                int nextIndent = lines[j].Length - nextTrimmed.Length;

                if (nextIndent <= blockIndent)
                {
                    int col = line.IndexOf(match.Groups[1].Value);
                    diagnostics.Add(new Diagnostic(i, col, match.Groups[1].Length,
                        $"Empty '{match.Groups[1].Value}' block", DiagnosticSeverity.Warning));
                }
                break;
            }
        }

        private void CheckDuplicateKeys(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            var matches = Regex.Matches(line, @"['""](\w+)['""]\s*:");
            var keys = new Dictionary<string, int>();
            foreach (Match m in matches)
            {
                string key = m.Groups[1].Value;
                if (keys.ContainsKey(key))
                {
                    diagnostics.Add(new Diagnostic(i, m.Index, m.Length,
                        $"Duplicate key '{key}' in dictionary literal", DiagnosticSeverity.Warning));
                }
                else
                {
                    keys[key] = m.Index;
                }
            }
        }

        private void CheckUnterminatedStrings(string[] lines, List<Diagnostic> diagnostics)
        {
            bool inTriple = false;
            char tripleChar = '"';

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();
                if (!inTriple && trimmed.StartsWith("#")) continue;

                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];
                    if (inTriple)
                    {
                        if (c == tripleChar && j + 2 < line.Length && line[j + 1] == tripleChar && line[j + 2] == tripleChar)
                        { inTriple = false; j += 2; }
                        continue;
                    }
                    if ((c == '"' || c == '\'') && j + 2 < line.Length && line[j + 1] == c && line[j + 2] == c)
                    { inTriple = true; tripleChar = c; j += 2; continue; }

                    if (c == '"' || c == '\'')
                    {
                        bool closed = false;
                        for (int k = j + 1; k < line.Length; k++)
                        {
                            if (line[k] == '\\') { k++; continue; }
                            if (line[k] == c) { closed = true; j = k; break; }
                        }
                        if (!closed)
                        {
                            diagnostics.Add(new Diagnostic(i, j, 1,
                                "Unterminated string literal", DiagnosticSeverity.Error));
                            break;
                        }
                    }
                }
            }
        }

        private void CheckDuplicateDeclarations(string[] lines, List<Diagnostic> diagnostics)
        {
            var seen = new Dictionary<string, int>();

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("#")) continue;
                int indent = lines[i].Length - trimmed.Length;

                var defMatch = Regex.Match(trimmed, @"^def\s+(\w+)\s*\(");
                if (defMatch.Success)
                {
                    string name = defMatch.Groups[1].Value;
                    string key = "def:" + indent + ":" + name;
                    if (seen.ContainsKey(key))
                    {
                        int col = lines[i].IndexOf(name);
                        diagnostics.Add(new Diagnostic(i, col, name.Length,
                            $"Function '{name}' is already defined on line {seen[key] + 1}", DiagnosticSeverity.Warning));
                    }
                    else { seen[key] = i; }
                }

                var classMatch = Regex.Match(trimmed, @"^class\s+(\w+)");
                if (classMatch.Success)
                {
                    string name = classMatch.Groups[1].Value;
                    string key = "class:" + indent + ":" + name;
                    if (seen.ContainsKey(key))
                    {
                        int col = lines[i].IndexOf(name);
                        diagnostics.Add(new Diagnostic(i, col, name.Length,
                            $"Class '{name}' is already defined on line {seen[key] + 1}", DiagnosticSeverity.Warning));
                    }
                    else { seen[key] = i; }
                }
            }
        }

        private void CheckUnreachableCode(string[] lines, List<Diagnostic> diagnostics)
        {
            for (int i = 0; i < lines.Length - 1; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                bool isTerminator = Regex.IsMatch(trimmed, @"^(return\b|break\s*$|continue\s*$|raise\b)");
                if (!isTerminator) continue;

                int indent = lines[i].Length - trimmed.Length;

                for (int j = i + 1; j < lines.Length; j++)
                {
                    string nextTrimmed = lines[j].TrimStart();
                    if (string.IsNullOrWhiteSpace(nextTrimmed)) continue;
                    if (nextTrimmed.StartsWith("#")) continue;

                    int nextIndent = lines[j].Length - nextTrimmed.Length;
                    if (nextIndent <= indent) break;

                    diagnostics.Add(new Diagnostic(j, nextIndent, nextTrimmed.Length,
                        "Unreachable code detected", DiagnosticSeverity.Warning));
                    break;
                }
            }
        }

        private void CheckIndentation(string[] lines, List<Diagnostic> diagnostics)
        {
            int expectedIndent = 0;
            int indentSize = 4;
            bool detectedSize = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

                int spaces = line.Length - line.TrimStart().Length;

                if (!detectedSize && spaces > 0)
                {
                    indentSize = spaces;
                    detectedSize = true;
                }

                if (detectedSize && spaces % indentSize != 0)
                {
                    diagnostics.Add(new Diagnostic(i, 0, spaces,
                        $"Unexpected indentation; expected a multiple of {indentSize} spaces", DiagnosticSeverity.Warning));
                }
            }
        }

        private void CheckBracketBalance(string[] lines, List<Diagnostic> diagnostics)
        {
            var stack = new Stack<Tuple<char, int, int>>();
            bool inString = false;
            char stringChar = '"';
            bool inTriple = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (!inString && !inTriple && trimmed.StartsWith("#")) continue;

                for (int j = 0; j < lines[i].Length; j++)
                {
                    char c = lines[i][j];

                    if (inTriple)
                    {
                        if (j + 2 < lines[i].Length &&
                            c == stringChar && lines[i][j + 1] == stringChar && lines[i][j + 2] == stringChar)
                        { inTriple = false; j += 2; }
                        continue;
                    }

                    if (inString)
                    {
                        if (c == stringChar && (j == 0 || lines[i][j - 1] != '\\'))
                            inString = false;
                        continue;
                    }

                    if ((c == '"' || c == '\'') && j + 2 < lines[i].Length &&
                        lines[i][j + 1] == c && lines[i][j + 2] == c)
                    { inTriple = true; stringChar = c; j += 2; continue; }

                    if (c == '"' || c == '\'') { inString = true; stringChar = c; continue; }

                    if (c == '(' || c == '[' || c == '{')
                        stack.Push(Tuple.Create(c, i, j));
                    else if (c == ')' || c == ']' || c == '}')
                    {
                        char expected = c == ')' ? '(' : c == ']' ? '[' : '{';
                        if (stack.Count == 0)
                        {
                            diagnostics.Add(new Diagnostic(i, j, 1,
                                $"Unmatched closing '{c}'", DiagnosticSeverity.Error));
                        }
                        else if (stack.Peek().Item1 != expected)
                        {
                            var top = stack.Pop();
                            diagnostics.Add(new Diagnostic(i, j, 1,
                                $"Mismatched bracket: expected closing for '{top.Item1}' from line {top.Item2 + 1}",
                                DiagnosticSeverity.Error));
                        }
                        else { stack.Pop(); }
                    }
                }
            }

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                diagnostics.Add(new Diagnostic(item.Item2, item.Item3, 1,
                    $"Unclosed '{item.Item1}'", DiagnosticSeverity.Error));
            }
        }
    }

    public class JavaScriptDiagnosticProvider : IDiagnosticProvider
    {
        public List<Diagnostic> Analyze(string text)
        {
            return Analyze(text, null);
        }

        public List<Diagnostic> Analyze(string text, AnalysisContext context)
        {
            var diagnostics = new List<Diagnostic>();
            var lines = text.Split('\n');

            CheckBracketBalance(lines, diagnostics);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();

                if (IsInsideBlockComment(lines, i)) continue;
                if (trimmed.StartsWith("//")) continue;

                CheckVarUsage(lines, i, line, trimmed, diagnostics);
                CheckTripleEquals(lines, i, line, diagnostics);
                CheckSemicolonMissing(lines, i, line, trimmed, diagnostics);
                CheckConsoleLog(lines, i, line, diagnostics);
                CheckUndefinedComparison(lines, i, line, diagnostics);
                CheckUndefinedInit(lines, i, line, diagnostics);
                CheckArrowFunction(lines, i, line, trimmed, diagnostics);
                CheckCommonTypos(lines, i, line, diagnostics);
                CheckEmptyBlock(lines, i, trimmed, diagnostics);
                CheckAssignmentInCondition(lines, i, line, trimmed, diagnostics);
                CheckDuplicateKeys(lines, i, line, diagnostics);
            }

            CheckUnterminatedStrings(lines, diagnostics);
            CheckDuplicateDeclarations(lines, diagnostics);
            CheckUnreachableCode(lines, diagnostics);

            return diagnostics;
        }

        private void CheckVarUsage(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(trimmed, @"^var\s+(\w+)");
            if (match.Success)
            {
                int col = line.IndexOf("var");
                diagnostics.Add(new Diagnostic(i, col, 3,
                    "Use 'let' or 'const' instead of 'var'", DiagnosticSeverity.Warning));
            }
        }

        private void CheckTripleEquals(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            bool inStr = false;
            char strCh = '"';
            for (int j = 0; j < line.Length - 1; j++)
            {
                char c = line[j];
                if (inStr) { if (c == strCh && (j == 0 || line[j - 1] != '\\')) inStr = false; continue; }
                if (c == '"' || c == '\'' || c == '`') { inStr = true; strCh = c; continue; }

                if (c == '=' && line[j + 1] == '=' && (j + 2 >= line.Length || line[j + 2] != '='))
                {
                    if (j > 0 && (line[j - 1] == '!' || line[j - 1] == '<' || line[j - 1] == '>' || line[j - 1] == '=')) continue;
                    diagnostics.Add(new Diagnostic(i, j, 2,
                        "Use '===' instead of '==' for strict equality comparison", DiagnosticSeverity.Warning));
                }
                else if (c == '!' && j + 2 < line.Length && line[j + 1] == '=' && line[j + 2] != '=')
                {
                    diagnostics.Add(new Diagnostic(i, j, 2,
                        "Use '!==' instead of '!=' for strict inequality comparison", DiagnosticSeverity.Warning));
                }
            }
        }

        private void CheckSemicolonMissing(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(trimmed)) return;
            if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*")) return;
            if (trimmed.StartsWith("import ") || trimmed.StartsWith("export "))
            {
                if (!trimmed.EndsWith(";") && !trimmed.EndsWith("{") && !trimmed.Contains(" from "))
                    return;
            }

            var statementPattern = new Regex(@"^\s*(?:return|throw|const|let|var)\s+.+[^;{},\s]\s*$");
            if (statementPattern.IsMatch(line))
            {
                if (!trimmed.EndsWith(";") && !trimmed.EndsWith("{") && !trimmed.EndsWith("}") && !trimmed.EndsWith(",") && !trimmed.EndsWith("=>"))
                {
                    diagnostics.Add(new Diagnostic(i, line.Length, 1,
                        "Possible missing semicolon", DiagnosticSeverity.Info));
                }
            }
        }

        private void CheckConsoleLog(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"console\.log\(");
            if (match.Success)
            {
                diagnostics.Add(new Diagnostic(i, match.Index, 11,
                    "console.log() statement found; consider removing before production", DiagnosticSeverity.Info));
            }
        }

        private void CheckUndefinedComparison(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"typeof\s+\w+\s*===?\s*""undefined""");
            if (!match.Success)
            {
                match = Regex.Match(line, @"===?\s*undefined");
                if (match.Success)
                {
                    diagnostics.Add(new Diagnostic(i, match.Index, match.Length,
                        "Consider using 'typeof x === \"undefined\"' for safer undefined checks", DiagnosticSeverity.Info));
                }
            }
        }

        private void CheckUndefinedInit(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"\b(?:let|const|var)\s+(\w+)\s*=\s*undefined\s*;");
            if (match.Success)
            {
                int eqIdx = line.IndexOf("= undefined", match.Index);
                if (eqIdx >= 0)
                {
                    diagnostics.Add(new Diagnostic(i, eqIdx, 12,
                        "Unnecessary initialization to undefined; 'let' variables are undefined by default", DiagnosticSeverity.Hint));
                }
            }
        }

        private void CheckArrowFunction(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(line, @"\bfunction\s*\(");
            if (match.Success && !trimmed.StartsWith("function ") && !trimmed.StartsWith("export function") && !trimmed.StartsWith("async function"))
            {
                diagnostics.Add(new Diagnostic(i, match.Index, 8,
                    "Consider using an arrow function instead", DiagnosticSeverity.Hint));
            }
        }

        private void CheckCommonTypos(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var typos = new Dictionary<string, string>
            {
                { @"\bconsle\b", "console" }, { @"\bconosle\b", "console" },
                { @"\bdocuemnt\b", "document" }, { @"\bdocumnet\b", "document" },
                { @"\bfuntcion\b", "function" }, { @"\bfucntion\b", "function" },
                { @"\bretrun\b", "return" }, { @"\breture\b", "return" },
                { @"\blenght\b", "length" }, { @"\blegnth\b", "length" },
                { @"\bflase\b", "false" }, { @"\bture\b", "true" },
                { @"\bnlul\b", "null" },
                { @"\bparseINt\b", "parseInt" }, { @"\bpraseInt\b", "parseInt" },
                { @"\bsetTimoet\b", "setTimeout" }, { @"\bsetTimout\b", "setTimeout" },
                { @"\bsetInteval\b", "setInterval" },
                { @"\baddEvenListener\b", "addEventListener" }, { @"\baddEventListner\b", "addEventListener" },
                { @"\bquerySelectro\b", "querySelector" },
            };

            foreach (var kv in typos)
            {
                var m = Regex.Match(line, kv.Key);
                if (m.Success)
                {
                    diagnostics.Add(new Diagnostic(i, m.Index, m.Length,
                        $"Did you mean '{kv.Value}'?", DiagnosticSeverity.Error));
                }
            }
        }

        private void CheckEmptyBlock(string[] lines, int i, string trimmed, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(trimmed, @"^(if|else if|else|for|while|switch)\b");
            if (!match.Success) return;

            for (int j = i + 1; j < lines.Length; j++)
            {
                string next = lines[j].Trim();
                if (next == "{") continue;
                if (next == "}")
                {
                    int col = lines[i].IndexOf(match.Groups[1].Value);
                    diagnostics.Add(new Diagnostic(i, col, match.Groups[1].Length,
                        $"Empty '{match.Groups[1].Value}' block", DiagnosticSeverity.Warning));
                }
                break;
            }
        }

        private void CheckAssignmentInCondition(string[] lines, int i, string line, string trimmed, List<Diagnostic> diagnostics)
        {
            var match = Regex.Match(trimmed, @"^(?:if|while)\s*\(.*[^=!<>]=[^=].*\)");
            if (match.Success)
            {
                int col = lines[i].Length - trimmed.Length;
                diagnostics.Add(new Diagnostic(i, col, match.Length,
                    "Possible accidental assignment in condition; did you mean '===' or '=='?", DiagnosticSeverity.Warning));
            }
        }

        private void CheckDuplicateKeys(string[] lines, int i, string line, List<Diagnostic> diagnostics)
        {
            var matches = Regex.Matches(line, @"(?:^|[,{])\s*(\w+)\s*:");
            var keys = new Dictionary<string, int>();
            foreach (Match m in matches)
            {
                string key = m.Groups[1].Value;
                if (keys.ContainsKey(key))
                {
                    diagnostics.Add(new Diagnostic(i, m.Groups[1].Index, m.Groups[1].Length,
                        $"Duplicate key '{key}' in object literal", DiagnosticSeverity.Warning));
                }
                else
                {
                    keys[key] = m.Groups[1].Index;
                }
            }
        }

        private void CheckUnterminatedStrings(string[] lines, List<Diagnostic> diagnostics)
        {
            bool inBlockComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();

                for (int j = 0; j < line.Length - 1; j++)
                {
                    if (!inBlockComment && line[j] == '/' && line[j + 1] == '*') { inBlockComment = true; j++; continue; }
                    if (inBlockComment && line[j] == '*' && line[j + 1] == '/') { inBlockComment = false; j++; continue; }
                }
                if (inBlockComment) continue;
                if (trimmed.StartsWith("//")) continue;

                bool inStr = false;
                bool inTemplate = false;
                char strCh = '"';
                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];
                    if (inTemplate)
                    {
                        if (c == '\\' && j + 1 < line.Length) { j++; continue; }
                        if (c == '`') { inTemplate = false; continue; }
                        continue;
                    }
                    if (inStr)
                    {
                        if (c == '\\' && j + 1 < line.Length) { j++; continue; }
                        if (c == strCh) { inStr = false; continue; }
                        continue;
                    }
                    if (c == '/' && j + 1 < line.Length && line[j + 1] == '/') break;
                    if (c == '`') { inTemplate = true; continue; }
                    if (c == '"' || c == '\'') { inStr = true; strCh = c; continue; }
                }
                if (inStr)
                {
                    diagnostics.Add(new Diagnostic(i, line.Length - 1, 1,
                        "Unterminated string literal", DiagnosticSeverity.Error));
                }
            }
        }

        private void CheckDuplicateDeclarations(string[] lines, List<Diagnostic> diagnostics)
        {
            var seen = new Dictionary<string, int>();
            int braceDepth = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (IsInsideBlockComment(lines, i)) continue;
                if (trimmed.StartsWith("//")) continue;

                foreach (char c in trimmed)
                {
                    if (c == '{') braceDepth++;
                    else if (c == '}') { braceDepth = Math.Max(0, braceDepth - 1); }
                }

                var funcMatch = Regex.Match(trimmed, @"^(?:function|async\s+function)\s+(\w+)\s*\(");
                if (funcMatch.Success)
                {
                    string name = funcMatch.Groups[1].Value;
                    string key = "func:" + braceDepth + ":" + name;
                    if (seen.ContainsKey(key))
                    {
                        int col = lines[i].IndexOf(name);
                        diagnostics.Add(new Diagnostic(i, col, name.Length,
                            $"Function '{name}' is already declared on line {seen[key] + 1}", DiagnosticSeverity.Warning));
                    }
                    else { seen[key] = i; }
                }

                var varMatch = Regex.Match(trimmed, @"^(?:let|const)\s+(\w+)\s*[=;]");
                if (varMatch.Success)
                {
                    string name = varMatch.Groups[1].Value;
                    string key = "var:" + braceDepth + ":" + name;
                    if (seen.ContainsKey(key))
                    {
                        int col = lines[i].IndexOf(name, lines[i].Length - trimmed.Length);
                        diagnostics.Add(new Diagnostic(i, col, name.Length,
                            $"Variable '{name}' is already declared on line {seen[key] + 1}", DiagnosticSeverity.Warning));
                    }
                    else { seen[key] = i; }
                }
            }
        }

        private void CheckUnreachableCode(string[] lines, List<Diagnostic> diagnostics)
        {
            for (int i = 0; i < lines.Length - 1; i++)
            {
                if (IsInsideBlockComment(lines, i)) continue;
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//")) continue;

                bool isTerminator = Regex.IsMatch(trimmed, @"^(return\b|break\s*;|continue\s*;|throw\b)");
                if (!isTerminator) continue;

                for (int j = i + 1; j < lines.Length; j++)
                {
                    string nextTrimmed = lines[j].TrimStart();
                    if (string.IsNullOrWhiteSpace(nextTrimmed)) continue;
                    if (nextTrimmed.StartsWith("//") || nextTrimmed.StartsWith("/*") || nextTrimmed.StartsWith("*")) continue;
                    if (nextTrimmed == "}" || nextTrimmed == "{") break;
                    if (nextTrimmed.StartsWith("case ") || nextTrimmed.StartsWith("default:")) break;

                    int nextIndent = lines[j].Length - nextTrimmed.Length;
                    diagnostics.Add(new Diagnostic(j, nextIndent, nextTrimmed.Length,
                        "Unreachable code detected", DiagnosticSeverity.Warning));
                    break;
                }
            }
        }

        private void CheckBracketBalance(string[] lines, List<Diagnostic> diagnostics)
        {
            var stack = new Stack<Tuple<char, int, int>>();
            bool inString = false;
            bool inLineComment = false;
            bool inBlockComment = false;
            bool inTemplate = false;
            char stringChar = '"';

            for (int i = 0; i < lines.Length; i++)
            {
                inLineComment = false;
                for (int j = 0; j < lines[i].Length; j++)
                {
                    char c = lines[i][j];
                    char prev = j > 0 ? lines[i][j - 1] : '\0';

                    if (inBlockComment)
                    {
                        if (c == '/' && prev == '*') inBlockComment = false;
                        continue;
                    }
                    if (inLineComment) continue;
                    if (inTemplate)
                    {
                        if (c == '`' && prev != '\\') inTemplate = false;
                        continue;
                    }
                    if (inString)
                    {
                        if (c == stringChar && prev != '\\') inString = false;
                        continue;
                    }

                    if (c == '/' && j + 1 < lines[i].Length && lines[i][j + 1] == '/') { inLineComment = true; continue; }
                    if (c == '/' && j + 1 < lines[i].Length && lines[i][j + 1] == '*') { inBlockComment = true; continue; }
                    if (c == '`') { inTemplate = true; continue; }
                    if (c == '"' || c == '\'') { inString = true; stringChar = c; continue; }

                    if (c == '(' || c == '[' || c == '{')
                        stack.Push(Tuple.Create(c, i, j));
                    else if (c == ')' || c == ']' || c == '}')
                    {
                        char expected = c == ')' ? '(' : c == ']' ? '[' : '{';
                        if (stack.Count == 0)
                        {
                            diagnostics.Add(new Diagnostic(i, j, 1,
                                $"Unmatched closing '{c}'", DiagnosticSeverity.Error));
                        }
                        else if (stack.Peek().Item1 != expected)
                        {
                            var top = stack.Pop();
                            diagnostics.Add(new Diagnostic(i, j, 1,
                                $"Mismatched bracket: expected closing for '{top.Item1}' from line {top.Item2 + 1}",
                                DiagnosticSeverity.Error));
                        }
                        else { stack.Pop(); }
                    }
                }
            }

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                diagnostics.Add(new Diagnostic(item.Item2, item.Item3, 1,
                    $"Unclosed '{item.Item1}'", DiagnosticSeverity.Error));
            }
        }

        private bool IsInsideBlockComment(string[] lines, int targetLine)
        {
            bool inBlock = false;
            for (int i = 0; i < targetLine; i++)
            {
                for (int j = 0; j < lines[i].Length; j++)
                {
                    if (!inBlock && j + 1 < lines[i].Length && lines[i][j] == '/' && lines[i][j + 1] == '*')
                    { inBlock = true; j++; }
                    else if (inBlock && j + 1 < lines[i].Length && lines[i][j] == '*' && lines[i][j + 1] == '/')
                    { inBlock = false; j++; }
                }
            }
            return inBlock;
        }
    }
}
