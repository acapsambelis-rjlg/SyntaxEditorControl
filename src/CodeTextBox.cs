using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CodeEditor
{
    public class CodeTextBox : Control
    {
        private TextDocument _doc = new TextDocument();
        private SyntaxRuleset _ruleset;
        private Font _editorFont;
        private float _charWidth;
        private float _lineHeight;
        private int _gutterWidth = 50;
        private const int GutterPadding = 10;
        private const int TextLeftPadding = 6;

        private TextPosition _caret;
        private TextPosition _selectionAnchor;
        private bool _hasSelection;
        private bool _mouseSelecting;
        private bool _mouseDragging;
        private TextPosition _dragStart;
        private string _dragText;

        private int _scrollX;
        private int _scrollY;
        private VScrollBar _vScrollBar;
        private HScrollBar _hScrollBar;

        private Timer _caretTimer;
        private bool _caretVisible = true;
        private int _desiredColumn = -1;
        private int _tabSize = 4;
        private bool _showLineNumbers = true;
        private bool _highlightCurrentLine = true;
        private bool _autoIndent = true;

        private Dictionary<int, List<MultiLineSpan>> _multiLineSpans;
        private bool _multiLineDirty = true;

        private struct MultiLineSpan
        {
            public int StartLine;
            public int StartCol;
            public int EndLine;
            public int EndCol;
            public Color ForeColor;
            public FontStyle Style;
        }

        private struct ColorRun
        {
            public int Start;
            public int Length;
            public Color ForeColor;
            public FontStyle Style;
        }

        public CodeTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);

            _editorFont = new Font("Courier New", 13f, FontStyle.Regular);
            _ruleset = SyntaxRuleset.CreatePlainTextRuleset();

            BackColor = _ruleset.BackgroundColor;
            ForeColor = _ruleset.DefaultForeColor;
            Cursor = Cursors.IBeam;
            TabStop = true;

            _vScrollBar = new VScrollBar();
            _vScrollBar.Dock = DockStyle.Right;
            _vScrollBar.ValueChanged += (s, e) => { _scrollY = _vScrollBar.Value; Invalidate(); };
            Controls.Add(_vScrollBar);

            _hScrollBar = new HScrollBar();
            _hScrollBar.Dock = DockStyle.Bottom;
            _hScrollBar.ValueChanged += (s, e) => { _scrollX = _hScrollBar.Value; Invalidate(); };
            Controls.Add(_hScrollBar);

            _caretTimer = new Timer();
            _caretTimer.Interval = 530;
            _caretTimer.Tick += (s, e) => { _caretVisible = !_caretVisible; InvalidateCaretLine(); };
            _caretTimer.Start();

            _doc.TextChanged += (s, e) => { _multiLineDirty = true; UpdateScrollBars(); Invalidate(); OnTextChanged(EventArgs.Empty); };

            MeasureCharSize();
            UpdateScrollBars();
            UpdateGutterWidth();
        }

        public SyntaxRuleset Ruleset
        {
            get { return _ruleset; }
            set
            {
                _ruleset = value ?? SyntaxRuleset.CreatePlainTextRuleset();
                BackColor = _ruleset.BackgroundColor;
                ForeColor = _ruleset.DefaultForeColor;
                _multiLineDirty = true;
                Invalidate();
            }
        }

        public Font EditorFont
        {
            get { return _editorFont; }
            set
            {
                _editorFont = value ?? new Font("Courier New", 13f);
                MeasureCharSize();
                UpdateScrollBars();
                UpdateGutterWidth();
                Invalidate();
            }
        }

        public override string Text
        {
            get { return _doc.Text; }
            set
            {
                _doc.Text = value;
                _caret = new TextPosition(0, 0);
                _hasSelection = false;
                _scrollX = 0;
                _scrollY = 0;
                UpdateGutterWidth();
                UpdateScrollBars();
                Invalidate();
            }
        }

        public int TabSize { get { return _tabSize; } set { _tabSize = Math.Max(1, value); } }
        public bool ShowLineNumbers { get { return _showLineNumbers; } set { _showLineNumbers = value; UpdateGutterWidth(); Invalidate(); } }
        public bool HighlightCurrentLine { get { return _highlightCurrentLine; } set { _highlightCurrentLine = value; Invalidate(); } }
        public bool AutoIndent { get { return _autoIndent; } set { _autoIndent = value; } }
        public int LineCount => _doc.LineCount;
        public TextPosition CaretPosition => _caret;

        public bool CanUndo => _doc.CanUndo;
        public bool CanRedo => _doc.CanRedo;

        public TextDocument Document => _doc;

        private void MeasureCharSize()
        {
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var size = g.MeasureString("M", _editorFont, 0, StringFormat.GenericTypographic);
                _charWidth = size.Width;
                _lineHeight = _editorFont.GetHeight(g) + 2;
                if (_charWidth < 1) _charWidth = 8;
                if (_lineHeight < 1) _lineHeight = 16;
            }
        }

        private void UpdateGutterWidth()
        {
            if (!_showLineNumbers) { _gutterWidth = 0; return; }
            int digits = Math.Max(3, _doc.LineCount.ToString().Length);
            _gutterWidth = (int)(digits * _charWidth) + GutterPadding * 2;
        }

        private void UpdateScrollBars()
        {
            int textAreaHeight = ClientSize.Height - _hScrollBar.Height;
            int textAreaWidth = ClientSize.Width - _vScrollBar.Width - _gutterWidth;
            int totalLines = _doc.LineCount;
            int visibleLines = Math.Max(1, (int)(textAreaHeight / _lineHeight));

            int maxLen = 0;
            for (int i = 0; i < _doc.LineCount; i++)
                maxLen = Math.Max(maxLen, _doc.GetLineLength(i));
            int totalWidth = (int)(maxLen * _charWidth) + 200;

            _vScrollBar.Minimum = 0;
            _vScrollBar.Maximum = Math.Max(0, totalLines + visibleLines - 2);
            _vScrollBar.LargeChange = Math.Max(1, visibleLines);
            _vScrollBar.SmallChange = 1;
            if (_scrollY > _vScrollBar.Maximum - _vScrollBar.LargeChange + 1)
                _scrollY = Math.Max(0, _vScrollBar.Maximum - _vScrollBar.LargeChange + 1);
            _vScrollBar.Value = _scrollY;

            _hScrollBar.Minimum = 0;
            _hScrollBar.Maximum = Math.Max(0, totalWidth);
            _hScrollBar.LargeChange = Math.Max(1, textAreaWidth);
            _hScrollBar.SmallChange = (int)_charWidth;
            if (_scrollX > _hScrollBar.Maximum - _hScrollBar.LargeChange + 1)
                _scrollX = Math.Max(0, _hScrollBar.Maximum - _hScrollBar.LargeChange + 1);
            _hScrollBar.Value = _scrollX;
        }

        private int TextAreaWidth => ClientSize.Width - _vScrollBar.Width - _gutterWidth;
        private int TextAreaHeight => ClientSize.Height - _hScrollBar.Height;
        private int VisibleLines => Math.Max(1, (int)(TextAreaHeight / _lineHeight) + 1);

        private void EnsureCaretVisible()
        {
            int visibleLines = (int)(TextAreaHeight / _lineHeight);
            if (_caret.Line < _scrollY)
                _scrollY = _caret.Line;
            else if (_caret.Line >= _scrollY + visibleLines)
                _scrollY = _caret.Line - visibleLines + 1;

            float caretX = _caret.Column * _charWidth;
            if (caretX - _scrollX < 0)
                _scrollX = Math.Max(0, (int)(caretX - _charWidth * 4));
            else if (caretX - _scrollX > TextAreaWidth - _charWidth * 2)
                _scrollX = (int)(caretX - TextAreaWidth + _charWidth * 6);

            _scrollX = Math.Max(0, _scrollX);
            _scrollY = Math.Max(0, Math.Min(_scrollY, Math.Max(0, _doc.LineCount - 1)));
            UpdateScrollBars();
        }

        private void ResetCaretBlink()
        {
            _caretVisible = true;
            _caretTimer.Stop();
            _caretTimer.Start();
            InvalidateCaretLine();
        }

        private void InvalidateCaretLine()
        {
            Invalidate();
        }

        private TextPosition PositionFromPoint(int x, int y)
        {
            int line = (int)((y + _scrollY * _lineHeight) / _lineHeight);
            line = Math.Max(0, Math.Min(line, _doc.LineCount - 1));
            int col = (int)Math.Round((x - _gutterWidth + _scrollX - TextLeftPadding) / _charWidth);
            col = Math.Max(0, Math.Min(col, _doc.GetLineLength(line)));
            return new TextPosition(line, col);
        }

        private void SetSelection(TextPosition anchor, TextPosition caret)
        {
            _selectionAnchor = anchor;
            _caret = caret;
            _hasSelection = !(anchor == caret);
        }

        private void ClearSelection()
        {
            _hasSelection = false;
            _selectionAnchor = _caret;
        }

        private TextRange GetSelectionRange()
        {
            if (!_hasSelection) return new TextRange(_caret, _caret);
            return new TextRange(_selectionAnchor, _caret);
        }

        private string GetSelectedText()
        {
            if (!_hasSelection) return "";
            var range = GetSelectionRange();
            return _doc.GetText(range.Start, range.End);
        }

        private void DeleteSelection()
        {
            if (!_hasSelection) return;
            var range = GetSelectionRange();
            _doc.Delete(range.Start, range.End);
            _caret = range.Start;
            ClearSelection();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = SmoothingMode.HighSpeed;

            g.Clear(_ruleset.BackgroundColor);

            int firstLine = _scrollY;
            int lastLine = Math.Min(_doc.LineCount - 1, firstLine + VisibleLines);

            if (_showLineNumbers)
            {
                using (var gutterBrush = new SolidBrush(_ruleset.LineNumberBackColor))
                    g.FillRectangle(gutterBrush, 0, 0, _gutterWidth, ClientSize.Height);

                using (var pen = new Pen(Color.FromArgb(50, 50, 50)))
                    g.DrawLine(pen, _gutterWidth - 1, 0, _gutterWidth - 1, ClientSize.Height);
            }

            if (_highlightCurrentLine && _caret.Line >= firstLine && _caret.Line <= lastLine)
            {
                float y = (_caret.Line - _scrollY) * _lineHeight;
                using (var brush = new SolidBrush(_ruleset.CurrentLineHighlight))
                    g.FillRectangle(brush, _gutterWidth, y, ClientSize.Width - _gutterWidth - _vScrollBar.Width, _lineHeight);
            }

            if (_hasSelection)
                PaintSelection(g, firstLine, lastLine);

            for (int i = firstLine; i <= lastLine; i++)
            {
                float y = (i - _scrollY) * _lineHeight;

                if (_showLineNumbers)
                {
                    string num = (i + 1).ToString();
                    using (var brush = new SolidBrush(i == _caret.Line ? Color.FromArgb(180, 180, 180) : _ruleset.LineNumberForeColor))
                    {
                        float numX = _gutterWidth - GutterPadding - g.MeasureString(num, _editorFont, 0, StringFormat.GenericTypographic).Width;
                        g.DrawString(num, _editorFont, brush, numX, y, StringFormat.GenericTypographic);
                    }
                }

                PaintLineText(g, i, y);
            }

            if (Focused && _caretVisible)
            {
                float cx = _gutterWidth + TextLeftPadding + _caret.Column * _charWidth - _scrollX;
                float cy = (_caret.Line - _scrollY) * _lineHeight;
                using (var pen = new Pen(_ruleset.CaretColor, 2f))
                    g.DrawLine(pen, cx, cy, cx, cy + _lineHeight);
            }
        }

        private void PaintSelection(Graphics g, int firstLine, int lastLine)
        {
            var range = GetSelectionRange();
            using (var brush = new SolidBrush(_ruleset.SelectionColor))
            {
                for (int i = Math.Max(range.Start.Line, firstLine); i <= Math.Min(range.End.Line, lastLine); i++)
                {
                    float y = (i - _scrollY) * _lineHeight;
                    int startCol = (i == range.Start.Line) ? range.Start.Column : 0;
                    int endCol = (i == range.End.Line) ? range.End.Column : _doc.GetLineLength(i);

                    float x1 = _gutterWidth + TextLeftPadding + startCol * _charWidth - _scrollX;
                    float x2 = _gutterWidth + TextLeftPadding + endCol * _charWidth - _scrollX;
                    if (i != range.End.Line) x2 += _charWidth;
                    g.FillRectangle(brush, x1, y, x2 - x1, _lineHeight);
                }
            }
        }

        private void PaintLineText(Graphics g, int lineIndex, float y)
        {
            string line = _doc.GetLine(lineIndex);
            if (string.IsNullOrEmpty(line)) return;

            var runs = GetColorRuns(lineIndex, line);
            float xBase = _gutterWidth + TextLeftPadding - _scrollX;

            foreach (var run in runs)
            {
                string segment = line.Substring(run.Start, run.Length);
                float x = xBase + run.Start * _charWidth;
                Font f = (run.Style != FontStyle.Regular) ? new Font(_editorFont, run.Style) : _editorFont;
                using (var brush = new SolidBrush(run.ForeColor))
                    g.DrawString(segment, f, brush, x, y, StringFormat.GenericTypographic);
                if (run.Style != FontStyle.Regular) f.Dispose();
            }
        }

        private void RebuildMultiLineSpans()
        {
            if (!_multiLineDirty) return;
            _multiLineDirty = false;
            _multiLineSpans = new Dictionary<int, List<MultiLineSpan>>();

            if (_ruleset.Rules == null || _ruleset.Rules.Count == 0) return;

            string fullText = _doc.Text;
            foreach (var rule in _ruleset.Rules)
            {
                var matches = rule.CompiledRegex.Matches(fullText);
                foreach (Match m in matches)
                {
                    int startOffset = m.Index;
                    int endOffset = m.Index + m.Length;

                    int sLine = 0, sCol = startOffset;
                    int offset = 0;
                    for (int i = 0; i < _doc.LineCount; i++)
                    {
                        int lineLen = _doc.GetLineLength(i) + 1;
                        if (offset + lineLen > startOffset)
                        {
                            sLine = i;
                            sCol = startOffset - offset;
                            break;
                        }
                        offset += lineLen;
                    }

                    int eLine = sLine, eCol = endOffset - offset;
                    offset = 0;
                    for (int i = 0; i < _doc.LineCount; i++)
                    {
                        int lineLen = _doc.GetLineLength(i) + 1;
                        if (offset + lineLen > endOffset || i == _doc.LineCount - 1)
                        {
                            eLine = i;
                            eCol = endOffset - offset;
                            break;
                        }
                        offset += lineLen;
                    }

                    var span = new MultiLineSpan
                    {
                        StartLine = sLine, StartCol = sCol,
                        EndLine = eLine, EndCol = eCol,
                        ForeColor = rule.ForeColor, Style = rule.FontStyle
                    };

                    for (int ln = sLine; ln <= eLine; ln++)
                    {
                        if (!_multiLineSpans.ContainsKey(ln))
                            _multiLineSpans[ln] = new List<MultiLineSpan>();
                        _multiLineSpans[ln].Add(span);
                    }
                }
            }
        }

        private List<ColorRun> GetColorRuns(int lineIndex, string line)
        {
            var colors = new Color[line.Length];
            var styles = new FontStyle[line.Length];
            for (int i = 0; i < line.Length; i++)
            {
                colors[i] = _ruleset.DefaultForeColor;
                styles[i] = FontStyle.Regular;
            }

            RebuildMultiLineSpans();

            if (_multiLineSpans != null && _multiLineSpans.ContainsKey(lineIndex))
            {
                foreach (var span in _multiLineSpans[lineIndex])
                {
                    int start = (lineIndex == span.StartLine) ? span.StartCol : 0;
                    int end = (lineIndex == span.EndLine) ? span.EndCol : line.Length;
                    end = Math.Min(end, line.Length);
                    start = Math.Max(0, start);
                    for (int i = start; i < end; i++)
                    {
                        colors[i] = span.ForeColor;
                        styles[i] = span.Style;
                    }
                }
            }

            var runs = new List<ColorRun>();
            if (line.Length == 0) return runs;

            int rs = 0;
            for (int i = 1; i <= line.Length; i++)
            {
                if (i == line.Length || colors[i] != colors[rs] || styles[i] != styles[rs])
                {
                    runs.Add(new ColorRun { Start = rs, Length = i - rs, ForeColor = colors[rs], Style = styles[rs] });
                    rs = i;
                }
            }
            return runs;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.Button == MouseButtons.Left)
            {
                var pos = PositionFromPoint(e.X, e.Y);

                if (_hasSelection && IsPositionInSelection(pos))
                {
                    _mouseDragging = true;
                    _dragStart = pos;
                    _dragText = GetSelectedText();
                    return;
                }

                if ((ModifierKeys & Keys.Shift) != 0)
                {
                    SetSelection(_hasSelection ? _selectionAnchor : _caret, pos);
                }
                else
                {
                    _caret = pos;
                    _selectionAnchor = pos;
                    _hasSelection = false;
                }
                _mouseSelecting = true;
                _desiredColumn = -1;
                ResetCaretBlink();
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_mouseDragging)
            {
                Cursor = Cursors.Arrow;
                return;
            }

            if (_mouseSelecting && e.Button == MouseButtons.Left)
            {
                var pos = PositionFromPoint(e.X, e.Y);
                SetSelection(_selectionAnchor, pos);
                _caret = pos;
                EnsureCaretVisible();
                ResetCaretBlink();
                Invalidate();
            }
            else
            {
                Cursor = (e.X > _gutterWidth) ? Cursors.IBeam : Cursors.Arrow;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_mouseDragging && e.Button == MouseButtons.Left)
            {
                _mouseDragging = false;
                Cursor = Cursors.IBeam;

                var dropPos = PositionFromPoint(e.X, e.Y);
                if (!IsPositionInSelection(dropPos))
                {
                    var range = GetSelectionRange();
                    _doc.BeginComposite(_caret);

                    bool dropBeforeSelection = dropPos < range.Start;
                    _doc.Delete(range.Start, range.End);
                    if (!dropBeforeSelection)
                    {
                        int deletedLen = _dragText.Length;
                        int newlines = 0;
                        foreach (char c in _dragText) if (c == '\n') newlines++;

                        if (dropPos.Line == range.End.Line && newlines == 0)
                            dropPos = new TextPosition(dropPos.Line - newlines, dropPos.Column - (range.End.Column - range.Start.Column));
                        else if (dropPos.Line > range.Start.Line && dropPos.Line <= range.End.Line)
                            dropPos = new TextPosition(dropPos.Line - newlines, dropPos.Column);
                    }

                    var endPos = _doc.Insert(dropPos, _dragText);
                    _selectionAnchor = dropPos;
                    _caret = endPos;
                    _hasSelection = true;
                    _doc.EndComposite(_caret);
                }
                _dragText = null;
                Invalidate();
                return;
            }

            _mouseSelecting = false;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;

            var pos = PositionFromPoint(e.X, e.Y);
            var line = _doc.GetLine(pos.Line);
            if (string.IsNullOrEmpty(line)) return;

            int start = pos.Column;
            int end = pos.Column;
            while (start > 0 && IsWordChar(line[start - 1])) start--;
            while (end < line.Length && IsWordChar(line[end])) end++;

            _selectionAnchor = new TextPosition(pos.Line, start);
            _caret = new TextPosition(pos.Line, end);
            _hasSelection = start != end;
            _mouseSelecting = true;
            ResetCaretBlink();
            Invalidate();
        }

        private bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        private bool IsPositionInSelection(TextPosition pos)
        {
            if (!_hasSelection) return false;
            var range = GetSelectionRange();
            return pos >= range.Start && pos <= range.End;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int delta = -(e.Delta / 120) * 3;
            _scrollY = Math.Max(0, Math.Min(_scrollY + delta, Math.Max(0, _doc.LineCount - 1)));
            UpdateScrollBars();
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Tab:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            bool shift = e.Shift;
            bool ctrl = e.Control;

            switch (e.KeyCode)
            {
                case Keys.Left: MoveCaret(0, -1, shift, ctrl); e.Handled = true; break;
                case Keys.Right: MoveCaret(0, 1, shift, ctrl); e.Handled = true; break;
                case Keys.Up: MoveCaret(-1, 0, shift, ctrl); e.Handled = true; break;
                case Keys.Down: MoveCaret(1, 0, shift, ctrl); e.Handled = true; break;
                case Keys.Home: MoveHome(shift, ctrl); e.Handled = true; break;
                case Keys.End: MoveEnd(shift, ctrl); e.Handled = true; break;
                case Keys.PageUp: MovePage(-1, shift); e.Handled = true; break;
                case Keys.PageDown: MovePage(1, shift); e.Handled = true; break;
                case Keys.Back: HandleBackspace(ctrl); e.Handled = true; break;
                case Keys.Delete:
                    if (ctrl && shift) { DuplicateLine(); e.Handled = true; }
                    else { HandleDelete(ctrl); e.Handled = true; }
                    break;
                case Keys.Enter: HandleEnter(); e.Handled = true; break;
                case Keys.Tab: HandleTab(shift); e.Handled = true; break;
                case Keys.A: if (ctrl) { SelectAll(); e.Handled = true; } break;
                case Keys.C: if (ctrl) { Copy(); e.Handled = true; } break;
                case Keys.X: if (ctrl) { Cut(); e.Handled = true; } break;
                case Keys.V: if (ctrl) { Paste(); e.Handled = true; } break;
                case Keys.Z:
                    if (ctrl && shift) { PerformRedo(); e.Handled = true; }
                    else if (ctrl) { PerformUndo(); e.Handled = true; }
                    break;
                case Keys.Y: if (ctrl) { PerformRedo(); e.Handled = true; } break;
                case Keys.D: if (ctrl) { DuplicateLine(); e.Handled = true; } break;
                case Keys.L: if (ctrl && shift) { DeleteLine(); e.Handled = true; } break;
                case Keys.U:
                    if (ctrl && shift) { TransformCase(true); e.Handled = true; }
                    else if (ctrl) { TransformCase(false); e.Handled = true; }
                    break;
                case Keys.OemOpenBrackets:
                    if (ctrl) { IndentSelection(false); e.Handled = true; }
                    break;
                case Keys.OemCloseBrackets:
                    if (ctrl) { IndentSelection(true); e.Handled = true; }
                    break;
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (e.KeyChar < 32 && e.KeyChar != '\t') return;
            if (char.IsControl(e.KeyChar)) return;

            e.Handled = true;
            InsertCharacter(e.KeyChar);
        }

        private void InsertCharacter(char c)
        {
            if (_hasSelection) DeleteSelection();
            string s = c.ToString();
            string closing = null;

            if (c == '(' ) closing = ")";
            else if (c == '[') closing = "]";
            else if (c == '{') closing = "}";
            else if (c == '"') closing = "\"";
            else if (c == '\'') closing = "'";

            if (closing != null)
            {
                _doc.BeginComposite(_caret);
                _caret = _doc.Insert(_caret, s + closing);
                _caret = new TextPosition(_caret.Line, _caret.Column - closing.Length);
                _doc.EndComposite(_caret);
            }
            else
            {
                _caret = _doc.Insert(_caret, s);
            }

            ClearSelection();
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private void HandleEnter()
        {
            string indent = "";
            if (_autoIndent)
            {
                string currentLine = _doc.GetLine(_caret.Line);
                foreach (char c in currentLine)
                {
                    if (c == ' ' || c == '\t') indent += c;
                    else break;
                }
                if (currentLine.Length > 0)
                {
                    char last = _caret.Column > 0 ? currentLine[Math.Min(_caret.Column - 1, currentLine.Length - 1)] : '\0';
                    if (last == '{' || last == '(' || last == '[' || last == ':')
                        indent += new string(' ', _tabSize);
                }
            }

            if (_hasSelection) DeleteSelection();
            _caret = _doc.Insert(_caret, "\n" + indent);
            ClearSelection();
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private void HandleBackspace(bool ctrl)
        {
            if (_hasSelection) { DeleteSelection(); EnsureCaretVisible(); ResetCaretBlink(); return; }
            if (_caret.Line == 0 && _caret.Column == 0) return;

            TextPosition delStart;
            if (ctrl)
            {
                delStart = FindWordBoundary(_caret, -1);
            }
            else if (_caret.Column == 0)
            {
                delStart = new TextPosition(_caret.Line - 1, _doc.GetLineLength(_caret.Line - 1));
            }
            else
            {
                string line = _doc.GetLine(_caret.Line);
                int spaces = 0;
                for (int i = _caret.Column - 1; i >= 0 && line[i] == ' '; i--) spaces++;
                int del = (spaces > 0 && spaces % _tabSize == 0) ? _tabSize : 1;
                del = Math.Min(del, _caret.Column);
                delStart = new TextPosition(_caret.Line, _caret.Column - del);
            }

            _doc.Delete(delStart, _caret);
            _caret = delStart;
            ClearSelection();
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private void HandleDelete(bool ctrl)
        {
            if (_hasSelection) { DeleteSelection(); EnsureCaretVisible(); ResetCaretBlink(); return; }

            TextPosition delEnd;
            if (ctrl)
            {
                delEnd = FindWordBoundary(_caret, 1);
            }
            else if (_caret.Column >= _doc.GetLineLength(_caret.Line))
            {
                if (_caret.Line >= _doc.LineCount - 1) return;
                delEnd = new TextPosition(_caret.Line + 1, 0);
            }
            else
            {
                delEnd = new TextPosition(_caret.Line, _caret.Column + 1);
            }

            _doc.Delete(_caret, delEnd);
            ClearSelection();
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private void HandleTab(bool shift)
        {
            if (_hasSelection)
            {
                IndentSelection(!shift);
                return;
            }

            if (shift)
            {
                string line = _doc.GetLine(_caret.Line);
                int removeCount = 0;
                for (int i = 0; i < Math.Min(_tabSize, line.Length); i++)
                {
                    if (line[i] == ' ') removeCount++;
                    else if (line[i] == '\t') { removeCount++; break; }
                    else break;
                }
                if (removeCount > 0)
                {
                    var start = new TextPosition(_caret.Line, 0);
                    var end = new TextPosition(_caret.Line, removeCount);
                    _doc.Delete(start, end);
                    _caret = new TextPosition(_caret.Line, Math.Max(0, _caret.Column - removeCount));
                    ClearSelection();
                }
            }
            else
            {
                if (_hasSelection) DeleteSelection();
                int spacesToInsert = _tabSize - (_caret.Column % _tabSize);
                string spaces = new string(' ', spacesToInsert);
                _caret = _doc.Insert(_caret, spaces);
                ClearSelection();
            }
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private void IndentSelection(bool indent)
        {
            var range = GetSelectionRange();
            _doc.BeginComposite(_caret);

            int startLine = range.Start.Line;
            int endLine = range.End.Line;
            if (range.End.Column == 0 && endLine > startLine) endLine--;

            for (int i = startLine; i <= endLine; i++)
            {
                if (indent)
                {
                    _doc.Insert(new TextPosition(i, 0), new string(' ', _tabSize));
                }
                else
                {
                    string line = _doc.GetLine(i);
                    int removeCount = 0;
                    for (int j = 0; j < Math.Min(_tabSize, line.Length); j++)
                    {
                        if (line[j] == ' ') removeCount++;
                        else if (line[j] == '\t') { removeCount++; break; }
                        else break;
                    }
                    if (removeCount > 0)
                        _doc.Delete(new TextPosition(i, 0), new TextPosition(i, removeCount));
                }
            }

            _doc.EndComposite(_caret);

            int adjustStart = indent ? _tabSize : -Math.Min(_tabSize, range.Start.Column);
            int adjustEnd = indent ? _tabSize : -Math.Min(_tabSize, range.End.Column);

            _selectionAnchor = new TextPosition(startLine, Math.Max(0, range.Start.Column + adjustStart));
            _caret = new TextPosition(endLine, Math.Max(0, _doc.GetLineLength(endLine)));
            _hasSelection = true;
            EnsureCaretVisible();
            ResetCaretBlink();
            Invalidate();
        }

        private void MoveCaret(int lineDir, int colDir, bool shift, bool ctrl)
        {
            TextPosition newPos = _caret;

            if (colDir != 0)
            {
                if (ctrl)
                {
                    newPos = FindWordBoundary(newPos, colDir);
                }
                else if (colDir < 0)
                {
                    if (newPos.Column > 0) newPos.Column--;
                    else if (newPos.Line > 0) { newPos.Line--; newPos.Column = _doc.GetLineLength(newPos.Line); }
                }
                else
                {
                    if (newPos.Column < _doc.GetLineLength(newPos.Line)) newPos.Column++;
                    else if (newPos.Line < _doc.LineCount - 1) { newPos.Line++; newPos.Column = 0; }
                }
                _desiredColumn = -1;

                if (!shift && _hasSelection)
                {
                    var range = GetSelectionRange();
                    _caret = colDir < 0 ? range.Start : range.End;
                    ClearSelection();
                    EnsureCaretVisible();
                    ResetCaretBlink();
                    return;
                }
            }
            else if (lineDir != 0)
            {
                if (_desiredColumn < 0) _desiredColumn = _caret.Column;

                if (ctrl && lineDir < 0)
                {
                    _scrollY = Math.Max(0, _scrollY - 1);
                    UpdateScrollBars();
                    Invalidate();
                    return;
                }
                else if (ctrl && lineDir > 0)
                {
                    _scrollY = Math.Min(Math.Max(0, _doc.LineCount - 1), _scrollY + 1);
                    UpdateScrollBars();
                    Invalidate();
                    return;
                }

                newPos.Line += lineDir;
                newPos.Line = Math.Max(0, Math.Min(newPos.Line, _doc.LineCount - 1));
                newPos.Column = Math.Min(_desiredColumn, _doc.GetLineLength(newPos.Line));

                if (!shift && _hasSelection)
                {
                    var range = GetSelectionRange();
                    _caret = lineDir < 0 ? range.Start : range.End;
                    ClearSelection();
                    EnsureCaretVisible();
                    ResetCaretBlink();
                    return;
                }
            }

            if (shift)
            {
                if (!_hasSelection) _selectionAnchor = _caret;
                _caret = newPos;
                _hasSelection = !(_selectionAnchor == _caret);
            }
            else
            {
                _caret = newPos;
                ClearSelection();
            }

            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private void MoveHome(bool shift, bool ctrl)
        {
            TextPosition newPos;
            if (ctrl)
            {
                newPos = new TextPosition(0, 0);
            }
            else
            {
                string line = _doc.GetLine(_caret.Line);
                int firstNonWhitespace = 0;
                while (firstNonWhitespace < line.Length && (line[firstNonWhitespace] == ' ' || line[firstNonWhitespace] == '\t'))
                    firstNonWhitespace++;
                newPos = new TextPosition(_caret.Line, _caret.Column == firstNonWhitespace ? 0 : firstNonWhitespace);
            }

            if (shift)
            {
                if (!_hasSelection) _selectionAnchor = _caret;
                _caret = newPos;
                _hasSelection = !(_selectionAnchor == _caret);
            }
            else
            {
                _caret = newPos;
                ClearSelection();
            }
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private void MoveEnd(bool shift, bool ctrl)
        {
            TextPosition newPos;
            if (ctrl)
                newPos = _doc.EndPosition;
            else
                newPos = new TextPosition(_caret.Line, _doc.GetLineLength(_caret.Line));

            if (shift)
            {
                if (!_hasSelection) _selectionAnchor = _caret;
                _caret = newPos;
                _hasSelection = !(_selectionAnchor == _caret);
            }
            else
            {
                _caret = newPos;
                ClearSelection();
            }
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private void MovePage(int direction, bool shift)
        {
            int pageLines = Math.Max(1, (int)(TextAreaHeight / _lineHeight) - 1);
            _desiredColumn = _desiredColumn < 0 ? _caret.Column : _desiredColumn;

            TextPosition newPos = _caret;
            newPos.Line = Math.Max(0, Math.Min(newPos.Line + direction * pageLines, _doc.LineCount - 1));
            newPos.Column = Math.Min(_desiredColumn, _doc.GetLineLength(newPos.Line));

            if (shift)
            {
                if (!_hasSelection) _selectionAnchor = _caret;
                _caret = newPos;
                _hasSelection = !(_selectionAnchor == _caret);
            }
            else
            {
                _caret = newPos;
                ClearSelection();
            }

            _scrollY += direction * pageLines;
            _scrollY = Math.Max(0, Math.Min(_scrollY, Math.Max(0, _doc.LineCount - 1)));
            UpdateScrollBars();
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        private TextPosition FindWordBoundary(TextPosition pos, int direction)
        {
            string line = _doc.GetLine(pos.Line);

            if (direction < 0)
            {
                if (pos.Column == 0)
                {
                    if (pos.Line > 0) return new TextPosition(pos.Line - 1, _doc.GetLineLength(pos.Line - 1));
                    return pos;
                }
                int col = pos.Column - 1;
                while (col > 0 && !IsWordChar(line[col])) col--;
                while (col > 0 && IsWordChar(line[col - 1])) col--;
                return new TextPosition(pos.Line, col);
            }
            else
            {
                if (pos.Column >= line.Length)
                {
                    if (pos.Line < _doc.LineCount - 1) return new TextPosition(pos.Line + 1, 0);
                    return pos;
                }
                int col = pos.Column;
                while (col < line.Length && IsWordChar(line[col])) col++;
                while (col < line.Length && !IsWordChar(line[col])) col++;
                return new TextPosition(pos.Line, col);
            }
        }

        public void SelectAll()
        {
            _selectionAnchor = new TextPosition(0, 0);
            _caret = _doc.EndPosition;
            _hasSelection = true;
            Invalidate();
        }

        public void Copy()
        {
            string text;
            if (_hasSelection)
            {
                text = GetSelectedText();
            }
            else
            {
                text = _doc.GetLine(_caret.Line) + "\n";
            }
            if (!string.IsNullOrEmpty(text))
            {
                try { Clipboard.SetText(text); } catch { }
            }
        }

        public void Cut()
        {
            if (_hasSelection)
            {
                Copy();
                DeleteSelection();
            }
            else
            {
                Copy();
                DeleteLine();
            }
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        public void Paste()
        {
            try
            {
                string text = Clipboard.GetText();
                if (string.IsNullOrEmpty(text)) return;
                text = text.Replace("\r\n", "\n").Replace("\r", "\n");
                if (_hasSelection) DeleteSelection();
                _caret = _doc.Insert(_caret, text);
                ClearSelection();
                _desiredColumn = -1;
                EnsureCaretVisible();
                ResetCaretBlink();
            }
            catch { }
        }

        public void PerformUndo()
        {
            if (!_doc.CanUndo) return;
            _caret = _doc.Undo();
            ClearSelection();
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        public void PerformRedo()
        {
            if (!_doc.CanRedo) return;
            _caret = _doc.Redo();
            ClearSelection();
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        public void DuplicateLine()
        {
            if (_hasSelection)
            {
                var range = GetSelectionRange();
                string text = _doc.GetText(range.Start, range.End);
                _doc.BeginComposite(_caret);
                _doc.Insert(range.End, text);
                _doc.EndComposite(_caret);
            }
            else
            {
                string line = _doc.GetLine(_caret.Line);
                var endOfLine = new TextPosition(_caret.Line, _doc.GetLineLength(_caret.Line));
                _doc.BeginComposite(_caret);
                _doc.Insert(endOfLine, "\n" + line);
                _caret = new TextPosition(_caret.Line + 1, _caret.Column);
                _doc.EndComposite(_caret);
            }
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        public void DeleteLine()
        {
            _doc.BeginComposite(_caret);
            if (_doc.LineCount == 1)
            {
                _doc.Delete(new TextPosition(0, 0), new TextPosition(0, _doc.GetLineLength(0)));
                _caret = new TextPosition(0, 0);
            }
            else if (_caret.Line == _doc.LineCount - 1)
            {
                var start = new TextPosition(_caret.Line - 1, _doc.GetLineLength(_caret.Line - 1));
                var end = new TextPosition(_caret.Line, _doc.GetLineLength(_caret.Line));
                _doc.Delete(start, end);
                _caret = new TextPosition(Math.Max(0, _caret.Line - 1), 0);
            }
            else
            {
                var start = new TextPosition(_caret.Line, 0);
                var end = new TextPosition(_caret.Line + 1, 0);
                _doc.Delete(start, end);
            }
            _caret = _doc.ClampPosition(_caret);
            _doc.EndComposite(_caret);
            ClearSelection();
            _desiredColumn = -1;
            EnsureCaretVisible();
            ResetCaretBlink();
        }

        public void TransformCase(bool toUpper)
        {
            if (!_hasSelection) return;
            var range = GetSelectionRange();
            string text = GetSelectedText();
            string transformed = toUpper ? text.ToUpper() : text.ToLower();
            _doc.BeginComposite(_caret);
            _doc.Delete(range.Start, range.End);
            _doc.Insert(range.Start, transformed);
            _doc.EndComposite(_caret);
            _selectionAnchor = range.Start;
            _caret = new TextPosition(range.End.Line, range.End.Column);
            _hasSelection = true;
            Invalidate();
        }

        public void GoToLine(int lineNumber)
        {
            lineNumber = Math.Max(1, Math.Min(lineNumber, _doc.LineCount));
            _caret = new TextPosition(lineNumber - 1, 0);
            ClearSelection();
            EnsureCaretVisible();
            ResetCaretBlink();
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _caretTimer.Start();
            _caretVisible = true;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _caretTimer.Stop();
            _caretVisible = false;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBars();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _caretTimer?.Dispose();
                _editorFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
