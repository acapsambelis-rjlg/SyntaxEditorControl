using System;
using System.Collections.Generic;
using System.Text;

namespace CodeEditor
{
    public struct TextPosition : IComparable<TextPosition>, IEquatable<TextPosition>
    {
        public int Line;
        public int Column;

        public TextPosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public int CompareTo(TextPosition other)
        {
            int c = Line.CompareTo(other.Line);
            return c != 0 ? c : Column.CompareTo(other.Column);
        }

        public bool Equals(TextPosition other) => Line == other.Line && Column == other.Column;
        public override bool Equals(object obj) => obj is TextPosition p && Equals(p);
        public override int GetHashCode() => Line * 100003 + Column;

        public static bool operator ==(TextPosition a, TextPosition b) => a.Equals(b);
        public static bool operator !=(TextPosition a, TextPosition b) => !a.Equals(b);
        public static bool operator <(TextPosition a, TextPosition b) => a.CompareTo(b) < 0;
        public static bool operator >(TextPosition a, TextPosition b) => a.CompareTo(b) > 0;
        public static bool operator <=(TextPosition a, TextPosition b) => a.CompareTo(b) <= 0;
        public static bool operator >=(TextPosition a, TextPosition b) => a.CompareTo(b) >= 0;

        public override string ToString() => $"({Line},{Column})";
    }

    public struct TextRange
    {
        public TextPosition Start;
        public TextPosition End;

        public TextRange(TextPosition start, TextPosition end)
        {
            if (start <= end) { Start = start; End = end; }
            else { Start = end; End = start; }
        }

        public bool IsEmpty => Start == End;
        public TextRange Normalized => new TextRange(Start, End);
    }

    internal abstract class UndoAction
    {
        public TextPosition CaretBefore;
        public TextPosition CaretAfter;
        public abstract void Undo(TextDocument doc);
        public abstract void Redo(TextDocument doc);
    }

    internal class InsertAction : UndoAction
    {
        public TextPosition Position;
        public string Text;

        public override void Undo(TextDocument doc) { doc.InternalDelete(Position, Text); }
        public override void Redo(TextDocument doc) { doc.InternalInsert(Position, Text); }
    }

    internal class DeleteAction : UndoAction
    {
        public TextPosition Position;
        public string Text;

        public override void Undo(TextDocument doc) { doc.InternalInsert(Position, Text); }
        public override void Redo(TextDocument doc) { doc.InternalDelete(Position, Text); }
    }

    internal class CompositeAction : UndoAction
    {
        public List<UndoAction> Actions = new List<UndoAction>();
        public override void Undo(TextDocument doc)
        {
            for (int i = Actions.Count - 1; i >= 0; i--)
                Actions[i].Undo(doc);
        }
        public override void Redo(TextDocument doc)
        {
            for (int i = 0; i < Actions.Count; i++)
                Actions[i].Redo(doc);
        }
    }

    public class TextDocument
    {
        private List<string> _lines = new List<string> { "" };
        private Stack<UndoAction> _undoStack = new Stack<UndoAction>();
        private Stack<UndoAction> _redoStack = new Stack<UndoAction>();
        private CompositeAction _compositeAction;
        private bool _inUndoRedo;

        public event EventHandler TextChanged;

        public int LineCount => _lines.Count;

        public string GetLine(int index)
        {
            if (index < 0 || index >= _lines.Count) return "";
            return _lines[index];
        }

        public int GetLineLength(int index)
        {
            if (index < 0 || index >= _lines.Count) return 0;
            return _lines[index].Length;
        }

        public string Text
        {
            get { return string.Join("\n", _lines); }
            set
            {
                _lines.Clear();
                if (string.IsNullOrEmpty(value))
                {
                    _lines.Add("");
                }
                else
                {
                    var split = value.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                    _lines.AddRange(split);
                }
                _undoStack.Clear();
                _redoStack.Clear();
                OnTextChanged();
            }
        }

        public TextPosition Insert(TextPosition pos, string text)
        {
            pos = ClampPosition(pos);
            var result = InternalInsert(pos, text);
            if (!_inUndoRedo)
            {
                var action = new InsertAction { Position = pos, Text = text, CaretBefore = pos, CaretAfter = result };
                PushUndo(action);
            }
            OnTextChanged();
            return result;
        }

        public string Delete(TextPosition start, TextPosition end)
        {
            var range = new TextRange(start, end);
            start = ClampPosition(range.Start);
            end = ClampPosition(range.End);
            if (start == end) return "";
            string deleted = GetText(start, end);
            InternalDelete(start, deleted);
            if (!_inUndoRedo)
            {
                var action = new DeleteAction { Position = start, Text = deleted, CaretBefore = end, CaretAfter = start };
                PushUndo(action);
            }
            OnTextChanged();
            return deleted;
        }

        public string GetText(TextPosition start, TextPosition end)
        {
            var range = new TextRange(start, end);
            start = range.Start;
            end = range.End;

            if (start.Line == end.Line)
            {
                var line = GetLine(start.Line);
                int s = Math.Min(start.Column, line.Length);
                int e = Math.Min(end.Column, line.Length);
                return line.Substring(s, e - s);
            }

            var sb = new StringBuilder();
            var firstLine = GetLine(start.Line);
            sb.Append(firstLine.Substring(Math.Min(start.Column, firstLine.Length)));
            for (int i = start.Line + 1; i < end.Line; i++)
            {
                sb.Append('\n');
                sb.Append(GetLine(i));
            }
            sb.Append('\n');
            var lastLine = GetLine(end.Line);
            sb.Append(lastLine.Substring(0, Math.Min(end.Column, lastLine.Length)));
            return sb.ToString();
        }

        public void BeginComposite(TextPosition caretBefore)
        {
            _compositeAction = new CompositeAction { CaretBefore = caretBefore };
        }

        public void EndComposite(TextPosition caretAfter)
        {
            if (_compositeAction != null && _compositeAction.Actions.Count > 0)
            {
                _compositeAction.CaretAfter = caretAfter;
                _undoStack.Push(_compositeAction);
                _redoStack.Clear();
            }
            _compositeAction = null;
        }

        internal TextPosition InternalInsert(TextPosition pos, string text)
        {
            pos = ClampPosition(pos);
            string line = _lines[pos.Line];
            string before = line.Substring(0, pos.Column);
            string after = line.Substring(pos.Column);

            var insertLines = text.Split('\n');
            if (insertLines.Length == 1)
            {
                _lines[pos.Line] = before + insertLines[0] + after;
                return new TextPosition(pos.Line, pos.Column + insertLines[0].Length);
            }

            _lines[pos.Line] = before + insertLines[0];
            for (int i = 1; i < insertLines.Length - 1; i++)
                _lines.Insert(pos.Line + i, insertLines[i]);
            int lastIdx = pos.Line + insertLines.Length - 1;
            _lines.Insert(lastIdx, insertLines[insertLines.Length - 1] + after);
            return new TextPosition(lastIdx, insertLines[insertLines.Length - 1].Length);
        }

        internal void InternalDelete(TextPosition pos, string text)
        {
            int newlineCount = 0;
            foreach (char c in text)
                if (c == '\n') newlineCount++;

            if (newlineCount == 0)
            {
                string line = _lines[pos.Line];
                _lines[pos.Line] = line.Substring(0, pos.Column) + line.Substring(pos.Column + text.Length);
                return;
            }

            string firstLine = _lines[pos.Line];
            int endLine = pos.Line + newlineCount;
            string lastLine = _lines[endLine];
            int endCol = text.Length - text.LastIndexOf('\n') - 1;

            _lines[pos.Line] = firstLine.Substring(0, pos.Column) + lastLine.Substring(endCol);
            for (int i = 0; i < newlineCount; i++)
                _lines.RemoveAt(pos.Line + 1);
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public TextPosition Undo()
        {
            if (_undoStack.Count == 0) return new TextPosition(0, 0);
            var action = _undoStack.Pop();
            _inUndoRedo = true;
            action.Undo(this);
            _inUndoRedo = false;
            _redoStack.Push(action);
            OnTextChanged();
            return action.CaretBefore;
        }

        public TextPosition Redo()
        {
            if (_redoStack.Count == 0) return new TextPosition(0, 0);
            var action = _redoStack.Pop();
            _inUndoRedo = true;
            action.Redo(this);
            _inUndoRedo = false;
            _undoStack.Push(action);
            OnTextChanged();
            return action.CaretAfter;
        }

        public TextPosition ClampPosition(TextPosition pos)
        {
            if (pos.Line < 0) return new TextPosition(0, 0);
            if (pos.Line >= _lines.Count) return new TextPosition(_lines.Count - 1, _lines[_lines.Count - 1].Length);
            pos.Column = Math.Max(0, Math.Min(pos.Column, _lines[pos.Line].Length));
            return pos;
        }

        public TextPosition EndPosition => new TextPosition(_lines.Count - 1, _lines[_lines.Count - 1].Length);

        private void PushUndo(UndoAction action)
        {
            if (_compositeAction != null)
                _compositeAction.Actions.Add(action);
            else
            {
                _undoStack.Push(action);
                _redoStack.Clear();
            }
        }

        private void OnTextChanged()
        {
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
