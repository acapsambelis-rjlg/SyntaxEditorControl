using System;
using System.Collections.Generic;

namespace CodeEditor
{
    public class FoldRegion
    {
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public bool IsCollapsed { get; set; }

        public FoldRegion(int startLine, int endLine)
        {
            StartLine = startLine;
            EndLine = endLine;
            IsCollapsed = false;
        }
    }

    public interface IFoldingProvider
    {
        List<FoldRegion> GetFoldRegions(string text);
    }

    public class BraceFoldingProvider : IFoldingProvider
    {
        public List<FoldRegion> GetFoldRegions(string text)
        {
            var regions = new List<FoldRegion>();
            var lines = text.Split('\n');
            var stack = new Stack<int>();
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

                    if (c == '/' && j + 1 < lines[i].Length && lines[i][j + 1] == '/')
                    { inLineComment = true; continue; }
                    if (c == '/' && j + 1 < lines[i].Length && lines[i][j + 1] == '*')
                    { inBlockComment = true; continue; }
                    if (c == '"' || c == '\'' || c == '`')
                    { inString = true; stringChar = c; continue; }

                    if (c == '{')
                    {
                        stack.Push(i);
                    }
                    else if (c == '}' && stack.Count > 0)
                    {
                        int startLine = stack.Pop();
                        if (i > startLine)
                        {
                            regions.Add(new FoldRegion(startLine, i));
                        }
                    }
                }
            }

            regions.Sort((a, b) => a.StartLine.CompareTo(b.StartLine));
            return regions;
        }
    }

    public class IndentFoldingProvider : IFoldingProvider
    {
        public List<FoldRegion> GetFoldRegions(string text)
        {
            var regions = new List<FoldRegion>();
            var lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                bool isBlockStart = trimmed.EndsWith(":") ||
                    (trimmed.StartsWith("def ") || trimmed.StartsWith("class ") ||
                     trimmed.StartsWith("if ") || trimmed.StartsWith("elif ") ||
                     trimmed.StartsWith("else:") || trimmed.StartsWith("for ") ||
                     trimmed.StartsWith("while ") || trimmed.StartsWith("try:") ||
                     trimmed.StartsWith("except") || trimmed.StartsWith("finally:") ||
                     trimmed.StartsWith("with ") || trimmed.StartsWith("async "));

                if (!isBlockStart) continue;

                int baseIndent = lines[i].Length - trimmed.Length;
                int endLine = i;

                for (int j = i + 1; j < lines.Length; j++)
                {
                    string nextTrimmed = lines[j].TrimStart();
                    if (string.IsNullOrWhiteSpace(nextTrimmed)) continue;

                    int nextIndent = lines[j].Length - nextTrimmed.Length;
                    if (nextIndent <= baseIndent) break;
                    endLine = j;
                }

                if (endLine > i)
                {
                    regions.Add(new FoldRegion(i, endLine));
                }
            }

            return regions;
        }
    }
}
