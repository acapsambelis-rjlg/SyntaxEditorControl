using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CodeEditor
{
    public partial class CodeTextBox : Control
    {
        private TextDocument _doc = new TextDocument();
        private SyntaxRuleset _ruleset;
        private Font _editorFont;
        private float _baseFontSize = 13f;
        private const float MinFontSize = 6f;
        private const float MaxFontSize = 48f;
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
        private TextPosition _dragDropPos;

        private int _scrollX;
        private int _scrollY;

        private bool _caretVisible = true;
        private int _desiredColumn = -1;
        private int _tabSize = 4;
        private bool _showLineNumbers = true;
        private bool _highlightCurrentLine = true;
        private bool _autoIndent = true;
        private bool _initialized;

        private Dictionary<int, List<MultiLineSpan>> _multiLineSpans;
        private bool _multiLineDirty = true;

        private IDiagnosticProvider _diagnosticProvider;
        private AnalysisContext _analysisContext;
        private List<Diagnostic> _diagnostics = new List<Diagnostic>();
        private Dictionary<int, List<Diagnostic>> _diagnosticsByLine = new Dictionary<int, List<Diagnostic>>();
        private Timer _diagnosticTimer;
        private ToolTip _diagnosticTooltip;
        private int _lastTooltipLine = -1;
        private int _lastTooltipCol = -1;

        private TextPosition? _matchBracketA;
        private TextPosition? _matchBracketB;

        private Panel _findPanel;
        private TextBox _findInput;
        private TextBox _replaceInput;
        private Button _findNextBtn;
        private Button _findPrevBtn;
        private Button _replaceBtn;
        private Button _replaceAllBtn;
        private Button _findCloseBtn;
        private CheckBox _caseSensitiveChk;
        private Label _findCountLabel;
        private bool _findVisible;
        private bool _replaceVisible;
        private List<TextRange> _findMatches = new List<TextRange>();
        private int _currentMatchIndex = -1;

        private IFoldingProvider _foldingProvider;
        private List<FoldRegion> _foldRegions = new List<FoldRegion>();
        private Dictionary<int, bool> _collapsedLines = new Dictionary<int, bool>();
        private List<int> _visibleLinesList = new List<int>();
        private Dictionary<int, int> _lineToVisibleIndex = new Dictionary<int, int>();
        private const int FoldMarginWidth = 14;
        private bool _foldingDirty = true;

        private ICompletionProvider _completionProvider;
        private ListBox _completionList;
        private List<CompletionItem> _completionItems = new List<CompletionItem>();
        private string _completionPartial = "";
        private bool _completionVisible;

        private List<TextPosition> _carets = new List<TextPosition>();
        private List<TextPosition> _selectionAnchors = new List<TextPosition>();
        private List<bool> _hasSelections = new List<bool>();

        private struct MultiLineSpan
        {
            public int StartLine;
            public int StartCol;
            public int EndLine;
            public int EndCol;
            public Color ForeColor;
            public FontStyle Style;
            public bool IsExclude;
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

            _baseFontSize = 13f;
            _editorFont = new Font("Courier New", _baseFontSize, FontStyle.Regular);
            _ruleset = SyntaxRuleset.CreatePlainTextRuleset();

            BackColor = _ruleset.BackgroundColor;
            ForeColor = _ruleset.DefaultForeColor;
            Cursor = Cursors.IBeam;
            TabStop = true;

            InitializeComponent();

            _caretTimer.Start();

            _doc.TextChanged += (s, e) => {
                _multiLineDirty = true;
                _foldingDirty = true;
                RebuildVisibleLinesList();
                if (_initialized) { UpdateScrollBars(); Invalidate(); ScheduleDiagnosticUpdate(); UpdateFindHighlights(); }
                OnTextChanged(EventArgs.Empty);
            };

            _diagnosticTimer = new Timer();
            _diagnosticTimer.Interval = 500;
            _diagnosticTimer.Tick += DiagnosticTimer_Tick;

            _diagnosticTooltip = new ToolTip();
            _diagnosticTooltip.InitialDelay = 200;
            _diagnosticTooltip.ReshowDelay = 100;
            _diagnosticTooltip.AutoPopDelay = 15000;
            _diagnosticTooltip.UseAnimation = false;
            _diagnosticTooltip.UseFading = false;

            InitFindPanel();
            InitCompletionList();

            MeasureCharSize();
            UpdateGutterWidth();
            _initialized = true;
            UpdateScrollBars();
        }

        #region Properties

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
                _baseFontSize = _editorFont.Size;
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

        public IDiagnosticProvider DiagnosticProvider
        {
            get { return _diagnosticProvider; }
            set
            {
                _diagnosticProvider = value;
                RunDiagnostics();
            }
        }

        public AnalysisContext AnalysisContext
        {
            get { return _analysisContext; }
            set
            {
                _analysisContext = value;
                RunDiagnostics();
            }
        }

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();

        public event EventHandler<DiagnosticsChangedEventArgs> DiagnosticsChanged;

        public void SetDiagnostics(List<Diagnostic> diagnostics)
        {
            _diagnostics = diagnostics ?? new List<Diagnostic>();
            RebuildDiagnosticIndex();
            OnDiagnosticsChanged();
            Invalidate();
        }

        public void ClearDiagnostics()
        {
            _diagnostics.Clear();
            _diagnosticsByLine.Clear();
            OnDiagnosticsChanged();
            Invalidate();
        }

        public IFoldingProvider FoldingProvider
        {
            get { return _foldingProvider; }
            set
            {
                _foldingProvider = value;
                _foldingDirty = true;
                _collapsedLines.Clear();
                RebuildFoldRegions();
                UpdateGutterWidth();
                Invalidate();
            }
        }

        public ICompletionProvider CompletionProvider
        {
            get { return _completionProvider; }
            set { _completionProvider = value; }
        }

        #endregion

        #region Designer Event Handlers

        private void VScrollBar_ValueChanged(object sender, EventArgs e)
        {
            _scrollY = _vScrollBar.Value;
            Invalidate();
        }

        private void HScrollBar_ValueChanged(object sender, EventArgs e)
        {
            _scrollX = _hScrollBar.Value;
            Invalidate();
        }

        private void CaretTimer_Tick(object sender, EventArgs e)
        {
            _caretVisible = !_caretVisible;
            InvalidateCaretLine();
        }

        #endregion

        #region Measurement and Scrolling

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
            if (!_showLineNumbers) { _gutterWidth = _foldingProvider != null ? FoldMarginWidth : 0; return; }
            int digits = Math.Max(3, _doc.LineCount.ToString().Length);
            _gutterWidth = (int)(digits * _charWidth) + GutterPadding * 2;
            if (_foldingProvider != null)
                _gutterWidth += FoldMarginWidth;
        }

        private bool _updatingScrollBars;

        private void UpdateScrollBars()
        {
            if (!_initialized || _updatingScrollBars) return;
            _updatingScrollBars = true;
            try
            {
                UpdateScrollBarsInternal();
            }
            finally
            {
                _updatingScrollBars = false;
            }
        }

        private void UpdateScrollBarsInternal()
        {
            int textAreaHeight = ClientSize.Height - _hScrollBar.Height;
            int textAreaWidth = ClientSize.Width - _vScrollBar.Width - _gutterWidth;
            int totalLines = GetVisibleLineCount();
            int visibleLines = Math.Max(1, (int)(textAreaHeight / _lineHeight));

            int maxLen = 0;
            for (int i = 0; i < _doc.LineCount; i++)
            {
                if (IsLineVisible(i))
                    maxLen = Math.Max(maxLen, _doc.GetLineLength(i));
            }
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
            int caretVi = ActualToVisibleLine(_caret.Line);
            if (caretVi < _scrollY)
                _scrollY = caretVi;
            else if (caretVi >= _scrollY + visibleLines)
                _scrollY = caretVi - visibleLines + 1;

            float caretX = _caret.Column * _charWidth;
            if (caretX - _scrollX < 0)
                _scrollX = Math.Max(0, (int)(caretX - _charWidth * 4));
            else if (caretX - _scrollX > TextAreaWidth - _charWidth * 2)
                _scrollX = (int)(caretX - TextAreaWidth + _charWidth * 6);

            _scrollX = Math.Max(0, _scrollX);
            _scrollY = Math.Max(0, Math.Min(_scrollY, Math.Max(0, GetVisibleLineCount() - 1)));
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

        #endregion

        #region Position and Selection

        private TextPosition PositionFromPoint(int x, int y)
        {
            int visibleRow = (int)(y / _lineHeight) + _scrollY;
            int line = VisibleToActualLine(visibleRow);
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

        private bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        private bool IsPositionInSelection(TextPosition pos)
        {
            if (!_hasSelection) return false;
            var range = GetSelectionRange();
            return pos >= range.Start && pos <= range.End;
        }

        #endregion

        #region Painting

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = SmoothingMode.HighSpeed;

            RebuildFoldRegions();

            g.Clear(_ruleset.BackgroundColor);

            int visibleCount = GetVisibleLineCount();
            int firstVisIdx = _scrollY;
            int lastVisIdx = Math.Min(visibleCount - 1, firstVisIdx + VisibleLines);

            var textClip = new Rectangle(_gutterWidth, 0, ClientSize.Width - _gutterWidth, ClientSize.Height);
            g.SetClip(textClip);

            if (_highlightCurrentLine && IsLineVisible(_caret.Line))
            {
                float y = ScreenYForLine(_caret.Line);
                if (y >= -_lineHeight && y < ClientSize.Height)
                    using (var brush = new SolidBrush(_ruleset.CurrentLineHighlight))
                        g.FillRectangle(brush, _gutterWidth, y, ClientSize.Width - _gutterWidth - _vScrollBar.Width, _lineHeight);
            }

            if (_hasSelection)
                PaintSelection(g, firstVisIdx, lastVisIdx);

            PaintWordOccurrenceHighlights(g, firstVisIdx, lastVisIdx);
            PaintFindHighlights(g, firstVisIdx, lastVisIdx);
            PaintBracketMatching(g);
            PaintExtraCursors(g);
            PaintIndentGuides(g, firstVisIdx, lastVisIdx);

            for (int vi = firstVisIdx; vi <= lastVisIdx; vi++)
            {
                int actualLine = VisibleToActualLine(vi);
                float y = (vi - _scrollY) * _lineHeight;
                PaintLineText(g, actualLine, y);
            }

            PaintDiagnostics(g, firstVisIdx, lastVisIdx);

            if (Focused && _caretVisible && IsLineVisible(_caret.Line))
            {
                float cx = _gutterWidth + TextLeftPadding + _caret.Column * _charWidth - _scrollX;
                float cy = ScreenYForLine(_caret.Line);
                using (var pen = new Pen(_ruleset.CaretColor, 2f))
                    g.DrawLine(pen, cx, cy, cx, cy + _lineHeight);
            }

            if (_mouseDragging)
            {
                float dx = _gutterWidth + TextLeftPadding + _dragDropPos.Column * _charWidth - _scrollX;
                float dy = ScreenYForLine(_dragDropPos.Line);
                using (var pen = new Pen(Color.FromArgb(160, _ruleset.CaretColor), 1.5f))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawLine(pen, dx, dy, dx, dy + _lineHeight);
                }
            }

            g.ResetClip();

            if (_showLineNumbers)
            {
                int foldOffset = _foldingProvider != null ? FoldMarginWidth : 0;
                int lineNumWidth = _gutterWidth - foldOffset;

                using (var gutterBrush = new SolidBrush(_ruleset.LineNumberBackColor))
                    g.FillRectangle(gutterBrush, 0, 0, _gutterWidth, ClientSize.Height);
                using (var pen = new Pen(_ruleset.GutterSeparatorColor))
                    g.DrawLine(pen, _gutterWidth - 1, 0, _gutterWidth - 1, ClientSize.Height);

                for (int vi = firstVisIdx; vi <= lastVisIdx; vi++)
                {
                    int actualLine = VisibleToActualLine(vi);
                    float y = (vi - _scrollY) * _lineHeight;
                    string num = (actualLine + 1).ToString();
                    using (var brush = new SolidBrush(actualLine == _caret.Line ? _ruleset.ActiveLineNumberColor : _ruleset.LineNumberForeColor))
                    {
                        float numX = lineNumWidth - GutterPadding - g.MeasureString(num, _editorFont, 0, StringFormat.GenericTypographic).Width;
                        g.DrawString(num, _editorFont, brush, numX, y, StringFormat.GenericTypographic);
                    }
                    PaintFoldMargin(g, (int)y, actualLine);
                }
            }
            else if (_foldingProvider != null)
            {
                using (var gutterBrush = new SolidBrush(_ruleset.LineNumberBackColor))
                    g.FillRectangle(gutterBrush, 0, 0, _gutterWidth, ClientSize.Height);

                for (int vi = firstVisIdx; vi <= lastVisIdx; vi++)
                {
                    int actualLine = VisibleToActualLine(vi);
                    float y = (vi - _scrollY) * _lineHeight;
                    PaintFoldMargin(g, (int)y, actualLine);
                }
            }
        }

        private void PaintSelection(Graphics g, int firstVisIdx, int lastVisIdx)
        {
            var range = GetSelectionRange();
            using (var brush = new SolidBrush(_ruleset.SelectionColor))
            {
                for (int i = range.Start.Line; i <= range.End.Line; i++)
                {
                    if (!IsLineVisible(i)) continue;
                    float y = ScreenYForLine(i);
                    if (y + _lineHeight < 0 || y > ClientSize.Height) continue;

                    int startCol = (i == range.Start.Line) ? range.Start.Column : 0;
                    int endCol = (i == range.End.Line) ? range.End.Column : _doc.GetLineLength(i);

                    float x1 = _gutterWidth + TextLeftPadding + startCol * _charWidth - _scrollX;
                    float x2 = _gutterWidth + TextLeftPadding + endCol * _charWidth - _scrollX;
                    if (i != range.End.Line) x2 += _charWidth;
                    g.FillRectangle(brush, x1, y, x2 - x1, _lineHeight);
                }
            }
        }

        private void PaintWordOccurrenceHighlights(Graphics g, int firstVisIdx, int lastVisIdx)
        {
            if (!_hasSelection) return;
            string selected = GetSelectedText();
            if (string.IsNullOrEmpty(selected)) return;
            if (selected.Contains("\n") || selected.Contains("\r")) return;

            bool isWord = true;
            foreach (char c in selected)
            {
                if (!char.IsLetterOrDigit(c) && c != '_') { isWord = false; break; }
            }
            if (!isWord) return;

            var range = GetSelectionRange();
            if (range.Start.Line != range.End.Line) return;

            using (var brush = new SolidBrush(Color.FromArgb(60, 180, 210, 255)))
            using (var borderPen = new Pen(Color.FromArgb(100, 120, 170, 220), 1f))
            {
                for (int vi = firstVisIdx; vi <= lastVisIdx; vi++)
                {
                    int actualLine = VisibleToActualLine(vi);
                    float y = (vi - _scrollY) * _lineHeight;
                    string line = _doc.GetLine(actualLine);

                    int idx = 0;
                    while ((idx = line.IndexOf(selected, idx, StringComparison.Ordinal)) >= 0)
                    {
                        if (actualLine == range.Start.Line && idx == range.Start.Column)
                        { idx += selected.Length; continue; }

                        bool wordStart = idx == 0 || !IsWordChar(line[idx - 1]);
                        bool wordEnd = idx + selected.Length >= line.Length || !IsWordChar(line[idx + selected.Length]);
                        if (wordStart && wordEnd)
                        {
                            float x = _gutterWidth + TextLeftPadding + idx * _charWidth - _scrollX;
                            float w = selected.Length * _charWidth;
                            g.FillRectangle(brush, x, y, w, _lineHeight);
                            g.DrawRectangle(borderPen, x, y, w, _lineHeight);
                        }
                        idx += selected.Length;
                    }
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

        private void PaintIndentGuides(Graphics g, int firstVisIdx, int lastVisIdx)
        {
            if (_tabSize <= 0) return;

            if (_ruleset.IndentGuideAlign == IndentGuideAlignment.Center)
                PaintBraceIndentGuides(g, firstVisIdx, lastVisIdx);
            else
                PaintIndentationGuides(g, firstVisIdx, lastVisIdx);
        }

        private void PaintBraceIndentGuides(Graphics g, int firstVisIdx, int lastVisIdx)
        {
            if (_foldRegions.Count == 0) return;

            float centerOffset = _charWidth / 2f;

            using (var pen = new Pen(_ruleset.IndentGuideColor, 1f))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                foreach (var region in _foldRegions)
                {
                    if (region.IsCollapsed) continue;

                    int braceLineIdx = -1;
                    for (int i = region.StartLine; i <= region.EndLine; i++)
                    {
                        string lt = _doc.GetLine(i).TrimStart();
                        if (lt.Length > 0 && lt[0] == '{') { braceLineIdx = i; break; }
                    }
                    if (braceLineIdx < 0) continue;

                    string braceLine = _doc.GetLine(braceLineIdx);
                    int braceCol = 0;
                    for (int i = 0; i < braceLine.Length; i++)
                    {
                        if (braceLine[i] == '{') { braceCol = i; break; }
                    }

                    float x = _gutterWidth + TextLeftPadding + braceCol * _charWidth - _scrollX + centerOffset;
                    if (x <= _gutterWidth || x >= ClientSize.Width - _vScrollBar.Width) continue;

                    int drawStart = braceLineIdx + 1;
                    int drawEnd = region.EndLine - 1;

                    for (int vi = firstVisIdx; vi <= lastVisIdx; vi++)
                    {
                        int actualLine = VisibleToActualLine(vi);
                        if (actualLine < drawStart || actualLine > drawEnd) continue;

                        float y = (vi - _scrollY) * _lineHeight;
                        g.DrawLine(pen, x, y, x, y + _lineHeight);
                    }
                }
            }
        }

        private void PaintIndentationGuides(Graphics g, int firstVisIdx, int lastVisIdx)
        {
            float guideSpacing = _tabSize * _charWidth;
            using (var pen = new Pen(_ruleset.IndentGuideColor, 1f))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                for (int vi = firstVisIdx; vi <= lastVisIdx; vi++)
                {
                    int actualLine = VisibleToActualLine(vi);
                    float y = (vi - _scrollY) * _lineHeight;
                    string line = _doc.GetLine(actualLine);
                    string trimmed = line.TrimStart();
                    bool isEmpty = trimmed.Length == 0;

                    int guideLevels;
                    if (isEmpty)
                    {
                        int nextIndent = GetNextNonEmptyLineIndent(actualLine);
                        int prevIndent = GetPrevNonEmptyLineIndent(actualLine);
                        guideLevels = Math.Min(nextIndent, prevIndent) / _tabSize;
                    }
                    else
                    {
                        int lineIndent = line.Length - trimmed.Length;
                        guideLevels = lineIndent / _tabSize - 1;
                    }

                    for (int level = 1; level <= guideLevels; level++)
                    {
                        float x = _gutterWidth + TextLeftPadding + level * guideSpacing - _scrollX;
                        if (x > _gutterWidth && x < ClientSize.Width - _vScrollBar.Width)
                            g.DrawLine(pen, x, y, x, y + _lineHeight);
                    }
                }
            }
        }

        private int GetNextNonEmptyLineIndent(int fromLine)
        {
            for (int i = fromLine + 1; i < _doc.LineCount; i++)
            {
                string line = _doc.GetLine(i);
                string trimmed = line.TrimStart();
                if (trimmed.Length > 0)
                    return line.Length - trimmed.Length;
            }
            return 0;
        }

        private int GetPrevNonEmptyLineIndent(int fromLine)
        {
            for (int i = fromLine - 1; i >= 0; i--)
            {
                string line = _doc.GetLine(i);
                string trimmed = line.TrimStart();
                if (trimmed.Length > 0)
                    return line.Length - trimmed.Length;
            }
            return 0;
        }

        #endregion

        #region Syntax Highlighting

        private void OffsetToLineCol(int charOffset, int[] lineOffsets, out int line, out int col)
        {
            line = 0;
            col = charOffset;
            for (int i = 0; i < lineOffsets.Length; i++)
            {
                if (i == lineOffsets.Length - 1 || lineOffsets[i + 1] > charOffset)
                {
                    line = i;
                    col = charOffset - lineOffsets[i];
                    break;
                }
            }
        }

        private void AddSpan(MultiLineSpan span)
        {
            for (int ln = span.StartLine; ln <= span.EndLine; ln++)
            {
                if (!_multiLineSpans.ContainsKey(ln))
                    _multiLineSpans[ln] = new List<MultiLineSpan>();
                _multiLineSpans[ln].Add(span);
            }
        }

        private void RebuildMultiLineSpans()
        {
            if (!_multiLineDirty) return;
            _multiLineDirty = false;
            _multiLineSpans = new Dictionary<int, List<MultiLineSpan>>();

            if (_ruleset.Rules == null || _ruleset.Rules.Count == 0) return;

            string fullText = _doc.Text;

            int[] lineOffsets = new int[_doc.LineCount];
            int off = 0;
            for (int i = 0; i < _doc.LineCount; i++)
            {
                lineOffsets[i] = off;
                off += _doc.GetLineLength(i) + 1;
            }

            foreach (var rule in _ruleset.Rules)
            {
                var matches = rule.CompiledRegex.Matches(fullText);
                foreach (Match m in matches)
                {
                    int sLine, sCol, eLine, eCol;
                    OffsetToLineCol(m.Index, lineOffsets, out sLine, out sCol);
                    OffsetToLineCol(m.Index + m.Length, lineOffsets, out eLine, out eCol);

                    AddSpan(new MultiLineSpan
                    {
                        StartLine = sLine, StartCol = sCol,
                        EndLine = eLine, EndCol = eCol,
                        ForeColor = rule.ForeColor, Style = rule.FontStyle,
                        IsExclude = false
                    });

                    if (rule.CompiledExclude != null)
                    {
                        string matchedText = m.Value;
                        var excludeMatches = rule.CompiledExclude.Matches(matchedText);
                        foreach (Match em in excludeMatches)
                        {
                            int exStart = m.Index + em.Index;
                            int exEnd = exStart + em.Length;
                            int exSLine, exSCol, exELine, exECol;
                            OffsetToLineCol(exStart, lineOffsets, out exSLine, out exSCol);
                            OffsetToLineCol(exEnd, lineOffsets, out exELine, out exECol);

                            AddSpan(new MultiLineSpan
                            {
                                StartLine = exSLine, StartCol = exSCol,
                                EndLine = exELine, EndCol = exECol,
                                ForeColor = _ruleset.DefaultForeColor, Style = FontStyle.Regular,
                                IsExclude = true
                            });
                        }
                    }
                }
            }
        }

        private List<ColorRun> GetColorRuns(int lineIndex, string line)
        {
            var colors = new Color[line.Length];
            var styles = new FontStyle[line.Length];
            var claimed = new bool[line.Length];
            for (int i = 0; i < line.Length; i++)
            {
                colors[i] = _ruleset.DefaultForeColor;
                styles[i] = FontStyle.Regular;
            }

            RebuildMultiLineSpans();

            if (_multiLineSpans != null && _multiLineSpans.ContainsKey(lineIndex))
            {
                var spans = _multiLineSpans[lineIndex];

                foreach (var span in spans)
                {
                    if (span.IsExclude) continue;
                    int start = (lineIndex == span.StartLine) ? span.StartCol : 0;
                    int end = (lineIndex == span.EndLine) ? span.EndCol : line.Length;
                    end = Math.Min(end, line.Length);
                    start = Math.Max(0, start);
                    for (int i = start; i < end; i++)
                    {
                        if (!claimed[i])
                        {
                            colors[i] = span.ForeColor;
                            styles[i] = span.Style;
                            claimed[i] = true;
                        }
                    }
                }

                foreach (var span in spans)
                {
                    if (!span.IsExclude) continue;
                    int start = (lineIndex == span.StartLine) ? span.StartCol : 0;
                    int end = (lineIndex == span.EndLine) ? span.EndCol : line.Length;
                    end = Math.Min(end, line.Length);
                    start = Math.Max(0, start);
                    for (int i = start; i < end; i++)
                    {
                        colors[i] = _ruleset.DefaultForeColor;
                        styles[i] = FontStyle.Regular;
                        claimed[i] = false;
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

        #endregion

        #region Mouse Handling

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (_completionVisible && !_completionList.Bounds.Contains(e.Location))
                HideCompletion();

            if (e.Button == MouseButtons.Left)
            {
                if (_foldingProvider != null && e.X >= _gutterWidth - FoldMarginWidth && e.X < _gutterWidth)
                {
                    RebuildFoldRegions();
                    int visRow = (int)(e.Y / _lineHeight) + _scrollY;
                    int line = VisibleToActualLine(visRow);
                    if (line >= 0 && line < _doc.LineCount)
                    {
                        var region = GetFoldRegionForLine(line);
                        if (region != null) { ToggleFold(line); return; }
                    }
                }

                var pos = PositionFromPoint(e.X, e.Y);

                if ((ModifierKeys & Keys.Control) != 0 && e.X > _gutterWidth)
                {
                    AddCursorAtPosition(pos);
                    return;
                }

                ClearExtraCursors();

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
                UpdateBracketMatching();
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_mouseDragging)
            {
                Cursor = Cursors.Arrow;
                _dragDropPos = PositionFromPoint(e.X, e.Y);
                Invalidate();
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
                if (e.X > _gutterWidth)
                    UpdateDiagnosticTooltip(e.X, e.Y);
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
                if (IsPositionInSelection(dropPos))
                {
                    _caret = dropPos;
                    ClearSelection();
                    _desiredColumn = -1;
                    ResetCaretBlink();
                }
                else
                {
                    var range = GetSelectionRange();
                    _doc.BeginComposite(_caret);

                    bool dropBeforeSelection = dropPos < range.Start;
                    _doc.Delete(range.Start, range.End);
                    if (!dropBeforeSelection)
                    {
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if ((ModifierKeys & Keys.Control) != 0)
            {
                float step = e.Delta > 0 ? 1f : -1f;
                float newSize = Math.Max(MinFontSize, Math.Min(MaxFontSize, _baseFontSize + step));
                if (newSize != _baseFontSize)
                {
                    _baseFontSize = newSize;
                    _editorFont = new Font(_editorFont.FontFamily, _baseFontSize, _editorFont.Style);
                    MeasureCharSize();
                    UpdateGutterWidth();
                    _multiLineDirty = true;
                    UpdateScrollBars();
                    Invalidate();
                }
                return;
            }

            if ((ModifierKeys & Keys.Shift) != 0)
            {
                int hDelta = -(e.Delta / 120) * (int)(_charWidth * 3);
                _scrollX = Math.Max(0, _scrollX + hDelta);
                UpdateScrollBars();
                Invalidate();
                return;
            }

            int delta = -(e.Delta / 120) * 3;
            _scrollY = Math.Max(0, Math.Min(_scrollY + delta, Math.Max(0, GetVisibleLineCount() - 1)));
            UpdateScrollBars();
            Invalidate();
        }

        #endregion

        #region Keyboard Handling

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

            if (_completionVisible)
            {
                if (e.KeyCode == Keys.Up)
                {
                    if (_completionList.SelectedIndex > 0) _completionList.SelectedIndex--;
                    e.Handled = true; e.SuppressKeyPress = true; return;
                }
                if (e.KeyCode == Keys.Down)
                {
                    if (_completionList.SelectedIndex < _completionItems.Count - 1) _completionList.SelectedIndex++;
                    e.Handled = true; e.SuppressKeyPress = true; return;
                }
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                {
                    AcceptCompletion(); e.Handled = true; e.SuppressKeyPress = true; return;
                }
                if (e.KeyCode == Keys.Escape)
                {
                    HideCompletion(); e.Handled = true; e.SuppressKeyPress = true; return;
                }
            }

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (_findVisible) { HideFind(); e.Handled = true; }
                    else if (HasMultipleCursors) { ClearExtraCursors(); e.Handled = true; }
                    break;
                case Keys.Left: MoveCaret(0, -1, shift, ctrl); e.Handled = true; break;
                case Keys.Right: MoveCaret(0, 1, shift, ctrl); e.Handled = true; break;
                case Keys.Up: MoveCaret(-1, 0, shift, ctrl); e.Handled = true; break;
                case Keys.Down: MoveCaret(1, 0, shift, ctrl); e.Handled = true; break;
                case Keys.Home: MoveHome(shift, ctrl); e.Handled = true; break;
                case Keys.End: MoveEnd(shift, ctrl); e.Handled = true; break;
                case Keys.PageUp: MovePage(-1, shift); e.Handled = true; break;
                case Keys.PageDown: MovePage(1, shift); e.Handled = true; break;
                case Keys.Back:
                    HandleBackspace(ctrl);
                    if (HasMultipleCursors) DeleteAtAllCursors();
                    e.Handled = true;
                    break;
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
                case Keys.D:
                    if (ctrl)
                    {
                        SelectNextOccurrence();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.L: if (ctrl && shift) { DeleteLine(); e.Handled = true; } break;
                case Keys.U:
                    if (ctrl && shift) { TransformCase(true); e.Handled = true; }
                    else if (ctrl) { TransformCase(false); e.Handled = true; }
                    break;
                case Keys.F:
                    if (ctrl) { ShowFind(); e.Handled = true; e.SuppressKeyPress = true; }
                    break;
                case Keys.H:
                    if (ctrl) { ShowReplace(); e.Handled = true; e.SuppressKeyPress = true; }
                    break;
                case Keys.F3:
                    if (shift) { FindPrevious(); e.Handled = true; }
                    else { FindNext(); e.Handled = true; }
                    break;
                case Keys.Space:
                    if (ctrl) { ShowCompletion(); e.Handled = true; e.SuppressKeyPress = true; }
                    break;
                case Keys.OemOpenBrackets:
                    if (ctrl) { IndentSelection(false); e.Handled = true; }
                    break;
                case Keys.OemCloseBrackets:
                    if (ctrl) { IndentSelection(true); e.Handled = true; }
                    break;
                case Keys.Oem2:
                    if (ctrl) { ToggleLineComment(); e.Handled = true; e.SuppressKeyPress = true; }
                    break;
            }

            UpdateBracketMatching();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (e.KeyChar < 32 && e.KeyChar != '\t') return;
            if (char.IsControl(e.KeyChar)) return;

            e.Handled = true;

            InsertCharacter(e.KeyChar);
            if (HasMultipleCursors)
                InsertAtAllCursors(e.KeyChar);

            UpdateBracketMatching();

            if (_completionVisible)
                UpdateCompletionFilter();
            else if (_completionProvider != null && (char.IsLetterOrDigit(e.KeyChar) || e.KeyChar == '_' || e.KeyChar == '.'))
                ShowCompletion();
        }

        #endregion

        #region Text Editing Operations

        private void InsertCharacter(char c)
        {
            if (_hasSelection) DeleteSelection();
            string s = c.ToString();
            string closing = null;

            string line = _doc.GetLine(_caret.Line);
            bool nextCharMatches = _caret.Column < line.Length && line[_caret.Column] == c;

            if ((c == ')' || c == ']' || c == '}') && nextCharMatches)
            {
                _caret = new TextPosition(_caret.Line, _caret.Column + 1);
                ClearSelection();
                _desiredColumn = -1;
                EnsureCaretVisible();
                ResetCaretBlink();
                return;
            }

            if ((c == '"' || c == '\'') && nextCharMatches)
            {
                bool prevCharMatches = _caret.Column > 0 && line[_caret.Column - 1] == c;
                bool isTripleClose = _caret.Column >= 2 && line[_caret.Column - 1] == c && line[_caret.Column - 2] == c
                    && _caret.Column + 2 < line.Length && line[_caret.Column + 1] == c && line[_caret.Column + 2] == c;
                if (isTripleClose)
                {
                    _caret = new TextPosition(_caret.Line, _caret.Column + 3);
                    ClearSelection();
                    _desiredColumn = -1;
                    EnsureCaretVisible();
                    ResetCaretBlink();
                    return;
                }
                if (prevCharMatches)
                {
                    _caret = new TextPosition(_caret.Line, _caret.Column + 1);
                    ClearSelection();
                    _desiredColumn = -1;
                    EnsureCaretVisible();
                    ResetCaretBlink();
                    return;
                }
            }

            if (c == '(' ) closing = ")";
            else if (c == '[') closing = "]";
            else if (c == '{') closing = "}";
            else if (c == '"' || c == '\'')
            {
                string q = c.ToString();
                string before = line.Substring(0, _caret.Column);
                if (before.EndsWith(q + q))
                {
                    closing = new string(c, 3);
                }
                else
                {
                    closing = q;
                }
            }

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

            _selectionAnchor = new TextPosition(startLine, Math.Max(0, range.Start.Column + adjustStart));
            _caret = new TextPosition(endLine, Math.Max(0, _doc.GetLineLength(endLine)));
            _hasSelection = true;
            EnsureCaretVisible();
            ResetCaretBlink();
            Invalidate();
        }

        private void ToggleLineComment()
        {
            string token = _ruleset?.LineCommentToken;
            if (string.IsNullOrEmpty(token)) return;

            int startLine, endLine;
            if (_hasSelection)
            {
                var range = GetSelectionRange();
                startLine = range.Start.Line;
                endLine = range.End.Line;
                if (range.End.Column == 0 && endLine > startLine) endLine--;
            }
            else
            {
                startLine = _caret.Line;
                endLine = _caret.Line;
            }

            bool allCommented = true;
            for (int i = startLine; i <= endLine; i++)
            {
                string trimmed = _doc.GetLine(i).TrimStart();
                if (trimmed.Length > 0 && !trimmed.StartsWith(token))
                {
                    allCommented = false;
                    break;
                }
            }

            _doc.BeginComposite(_caret);

            int caretColDelta = 0;
            for (int i = startLine; i <= endLine; i++)
            {
                string line = _doc.GetLine(i);
                if (allCommented)
                {
                    int idx = line.IndexOf(token);
                    if (idx >= 0)
                    {
                        int removeLen = token.Length;
                        if (idx + removeLen < line.Length && line[idx + removeLen] == ' ')
                            removeLen++;
                        var from = new TextPosition(i, idx);
                        var to = new TextPosition(i, idx + removeLen);
                        _doc.Delete(from, to);
                        if (i == _caret.Line) caretColDelta -= removeLen;
                    }
                }
                else
                {
                    if (line.TrimStart().Length == 0 && line.Length == 0) continue;
                    int indent = 0;
                    while (indent < line.Length && (line[indent] == ' ' || line[indent] == '\t')) indent++;
                    string insert = token + " ";
                    _doc.Insert(new TextPosition(i, indent), insert);
                    if (i == _caret.Line) caretColDelta += insert.Length;
                }
            }

            _doc.EndComposite(new TextPosition(_caret.Line, Math.Max(0, _caret.Column + caretColDelta)));
            _caret = new TextPosition(_caret.Line, Math.Max(0, _caret.Column + caretColDelta));

            if (_hasSelection)
            {
                _selectionAnchor = new TextPosition(startLine, 0);
                _caret = new TextPosition(endLine, _doc.GetLineLength(endLine));
                _hasSelection = true;
            }

            EnsureCaretVisible();
            ResetCaretBlink();
            Invalidate();
        }

        #endregion

        #region Caret Navigation

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
                    _scrollY = Math.Min(Math.Max(0, GetVisibleLineCount() - 1), _scrollY + 1);
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
            _scrollY = Math.Max(0, Math.Min(_scrollY, Math.Max(0, GetVisibleLineCount() - 1)));
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

        #endregion

        #region Public Commands

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

        #endregion

        #region Focus and Resize

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
            if (_completionVisible) HideCompletion();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionFindPanel();
            UpdateScrollBars();
            Invalidate();
        }

        #endregion

        #region Diagnostics

        private void ScheduleDiagnosticUpdate()
        {
            if (_diagnosticProvider == null) return;
            _diagnosticTimer.Stop();
            _diagnosticTimer.Start();
        }

        private void DiagnosticTimer_Tick(object sender, EventArgs e)
        {
            _diagnosticTimer.Stop();
            RunDiagnostics();
        }

        private void RunDiagnostics()
        {
            if (_diagnosticProvider == null)
            {
                ClearDiagnostics();
                return;
            }

            try
            {
                List<Diagnostic> results;
                if (_analysisContext != null)
                    results = _diagnosticProvider.Analyze(_doc.Text, _analysisContext);
                else
                    results = _diagnosticProvider.Analyze(_doc.Text);
                _diagnostics = results ?? new List<Diagnostic>();
            }
            catch
            {
                _diagnostics = new List<Diagnostic>();
            }

            RebuildDiagnosticIndex();
            OnDiagnosticsChanged();
            Invalidate();
        }

        private void RebuildDiagnosticIndex()
        {
            _diagnosticsByLine = new Dictionary<int, List<Diagnostic>>();
            foreach (var d in _diagnostics)
            {
                if (!_diagnosticsByLine.ContainsKey(d.Line))
                    _diagnosticsByLine[d.Line] = new List<Diagnostic>();
                _diagnosticsByLine[d.Line].Add(d);
            }
        }

        private void OnDiagnosticsChanged()
        {
            DiagnosticsChanged?.Invoke(this, new DiagnosticsChangedEventArgs(_diagnostics.AsReadOnly()));
        }

        private void PaintDiagnostics(Graphics g, int firstVisIdx, int lastVisIdx)
        {
            if (_diagnosticsByLine.Count == 0) return;

            for (int vi = firstVisIdx; vi <= lastVisIdx; vi++)
            {
                int line = VisibleToActualLine(vi);
                if (!_diagnosticsByLine.ContainsKey(line)) continue;

                float y = (vi - _scrollY) * _lineHeight;
                float baseline = y + _lineHeight - 2;

                foreach (var diag in _diagnosticsByLine[line])
                {
                    int len = Math.Max(1, diag.Length);
                    float x1 = _gutterWidth + TextLeftPadding + diag.Column * _charWidth - _scrollX;
                    float x2 = x1 + len * _charWidth;

                    Color squiggleColor;
                    switch (diag.Severity)
                    {
                        case DiagnosticSeverity.Error:
                            squiggleColor = Color.FromArgb(255, 0, 0);
                            break;
                        case DiagnosticSeverity.Warning:
                            squiggleColor = Color.FromArgb(206, 145, 0);
                            break;
                        case DiagnosticSeverity.Hint:
                            squiggleColor = Color.FromArgb(160, 160, 160);
                            break;
                        default:
                            squiggleColor = Color.FromArgb(0, 128, 0);
                            break;
                    }

                    if (diag.Severity == DiagnosticSeverity.Hint)
                    {
                        using (var brush = new SolidBrush(squiggleColor))
                        {
                            float dotSize = 2f;
                            float dotSpacing = 4f;
                            float cx = x1;
                            while (cx < x2)
                            {
                                g.FillEllipse(brush, cx, baseline - 1f, dotSize, dotSize);
                                cx += dotSpacing;
                            }
                        }
                    }
                    else
                    {
                        using (var pen = new Pen(squiggleColor, 1f))
                        {
                            float waveHeight = 2f;
                            float waveWidth = 4f;
                            var points = new List<PointF>();
                            float cx = x1;
                            bool up = true;
                            while (cx < x2)
                            {
                                points.Add(new PointF(cx, up ? baseline - waveHeight : baseline));
                                cx += waveWidth / 2;
                                up = !up;
                            }
                            points.Add(new PointF(x2, up ? baseline - waveHeight : baseline));

                            if (points.Count >= 2)
                                g.DrawLines(pen, points.ToArray());
                        }
                    }
                }
            }
        }

        private void UpdateDiagnosticTooltip(int mouseX, int mouseY)
        {
            var pos = PositionFromPoint(mouseX, mouseY);

            if (pos.Line == _lastTooltipLine && pos.Column == _lastTooltipCol) return;
            _lastTooltipLine = pos.Line;
            _lastTooltipCol = pos.Column;

            if (_diagnosticsByLine.ContainsKey(pos.Line))
            {
                foreach (var diag in _diagnosticsByLine[pos.Line])
                {
                    if (pos.Column >= diag.Column && pos.Column < diag.Column + Math.Max(1, diag.Length))
                    {
                        string prefix;
                        switch (diag.Severity)
                        {
                            case DiagnosticSeverity.Error: prefix = "Error"; break;
                            case DiagnosticSeverity.Warning: prefix = "Warning"; break;
                            case DiagnosticSeverity.Hint: prefix = "Hint"; break;
                            default: prefix = "Info"; break;
                        }
                        _diagnosticTooltip.Show($"{prefix}: {diag.Message}", this, mouseX + 10, mouseY + 15);
                        return;
                    }
                }
            }

            _diagnosticTooltip.Hide(this);
        }

        #endregion

        #region Bracket Matching

        private void UpdateBracketMatching()
        {
            _matchBracketA = null;
            _matchBracketB = null;

            if (_caret.Line >= _doc.LineCount) return;
            string line = _doc.GetLine(_caret.Line);

            char ch = '\0';
            int col = _caret.Column;

            if (col < line.Length && IsBracket(line[col]))
            {
                ch = line[col];
            }
            else if (col > 0 && col - 1 < line.Length && IsBracket(line[col - 1]))
            {
                ch = line[col - 1];
                col = col - 1;
            }

            if (ch == '\0') return;

            _matchBracketA = new TextPosition(_caret.Line, col);
            var match = FindMatchingBracket(_caret.Line, col, ch);
            if (match.HasValue)
                _matchBracketB = match.Value;
        }

        private bool IsBracket(char c)
        {
            return c == '(' || c == ')' || c == '[' || c == ']' || c == '{' || c == '}';
        }

        private bool IsOpenBracket(char c)
        {
            return c == '(' || c == '[' || c == '{';
        }

        private char GetMatchingBracket(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '{': return '}';
                case '}': return '{';
                default: return '\0';
            }
        }

        private TextPosition? FindMatchingBracket(int line, int col, char bracket)
        {
            char target = GetMatchingBracket(bracket);
            bool forward = IsOpenBracket(bracket);
            int depth = 0;

            if (forward)
            {
                for (int i = line; i < _doc.LineCount; i++)
                {
                    string l = _doc.GetLine(i);
                    int startCol = (i == line) ? col : 0;
                    for (int j = startCol; j < l.Length; j++)
                    {
                        if (l[j] == bracket) depth++;
                        else if (l[j] == target) { depth--; if (depth == 0) return new TextPosition(i, j); }
                    }
                }
            }
            else
            {
                for (int i = line; i >= 0; i--)
                {
                    string l = _doc.GetLine(i);
                    int startCol = (i == line) ? col : l.Length - 1;
                    for (int j = startCol; j >= 0; j--)
                    {
                        if (l[j] == bracket) depth++;
                        else if (l[j] == target) { depth--; if (depth == 0) return new TextPosition(i, j); }
                    }
                }
            }

            return null;
        }

        private void PaintBracketMatching(Graphics g)
        {
            if (!_matchBracketA.HasValue || !_matchBracketB.HasValue) return;

            using (var brush = new SolidBrush(Color.FromArgb(60, 0, 150, 255)))
            {
                foreach (var pos in new[] { _matchBracketA.Value, _matchBracketB.Value })
                {
                    if (!IsLineVisible(pos.Line)) continue;
                    float x = _gutterWidth + TextLeftPadding + pos.Column * _charWidth - _scrollX;
                    float y = ScreenYForLine(pos.Line);
                    g.FillRectangle(brush, x, y, _charWidth, _lineHeight);
                }
            }

            using (var pen = new Pen(Color.FromArgb(120, 0, 100, 200), 1f))
            {
                foreach (var pos in new[] { _matchBracketA.Value, _matchBracketB.Value })
                {
                    if (!IsLineVisible(pos.Line)) continue;
                    float x = _gutterWidth + TextLeftPadding + pos.Column * _charWidth - _scrollX;
                    float y = ScreenYForLine(pos.Line);
                    g.DrawRectangle(pen, x, y, _charWidth, _lineHeight);
                }
            }
        }

        #endregion

        #region Find and Replace

        private void InitFindPanel()
        {
            _findPanel = new Panel();
            _findPanel.Visible = false;
            _findPanel.BackColor = Color.FromArgb(240, 240, 240);
            _findPanel.Height = 34;
            _findPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _findPanel.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(200, 200, 200)))
                    e.Graphics.DrawRectangle(pen, 0, 0, _findPanel.Width - 1, _findPanel.Height - 1);
            };

            _findInput = new TextBox();
            _findInput.Font = new Font("Segoe UI", 9f);
            _findInput.Location = new Point(6, 6);
            _findInput.Width = 180;
            _findInput.Height = 22;
            _findInput.TextChanged += (s, e) => UpdateFindHighlights();
            _findInput.KeyDown += FindInput_KeyDown;

            _replaceInput = new TextBox();
            _replaceInput.Font = new Font("Segoe UI", 9f);
            _replaceInput.Location = new Point(6, 32);
            _replaceInput.Width = 180;
            _replaceInput.Height = 22;
            _replaceInput.Visible = false;
            _replaceInput.KeyDown += FindInput_KeyDown;

            _caseSensitiveChk = new CheckBox();
            _caseSensitiveChk.Text = "Aa";
            _caseSensitiveChk.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            _caseSensitiveChk.Location = new Point(190, 7);
            _caseSensitiveChk.Size = new Size(38, 20);
            _caseSensitiveChk.Appearance = Appearance.Button;
            _caseSensitiveChk.FlatStyle = FlatStyle.Flat;
            _caseSensitiveChk.CheckedChanged += (s, e) => UpdateFindHighlights();

            _findCountLabel = new Label();
            _findCountLabel.Font = new Font("Segoe UI", 8f);
            _findCountLabel.Location = new Point(230, 9);
            _findCountLabel.Size = new Size(60, 16);
            _findCountLabel.ForeColor = Color.FromArgb(100, 100, 100);

            _findPrevBtn = CreateFindButton("\u25B2", new Point(292, 5), (s, e) => FindPrevious());
            _findNextBtn = CreateFindButton("\u25BC", new Point(316, 5), (s, e) => FindNext());
            _replaceBtn = CreateFindButton("R", new Point(292, 31), (s, e) => ReplaceOne());
            _replaceAllBtn = CreateFindButton("All", new Point(316, 31), (s, e) => ReplaceAll());
            _replaceBtn.Visible = false;
            _replaceAllBtn.Visible = false;

            _findCloseBtn = CreateFindButton("\u2715", new Point(344, 5), (s, e) => HideFind());

            _findPanel.Controls.AddRange(new Control[] {
                _findInput, _replaceInput, _caseSensitiveChk, _findCountLabel,
                _findPrevBtn, _findNextBtn, _replaceBtn, _replaceAllBtn, _findCloseBtn
            });

            Controls.Add(_findPanel);
            PositionFindPanel();
        }

        private Button CreateFindButton(string text, Point location, EventHandler click)
        {
            var btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 8f);
            btn.Location = location;
            btn.Size = new Size(22, 22);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += click;
            return btn;
        }

        private void PositionFindPanel()
        {
            _findPanel.Width = 374;
            _findPanel.Height = _replaceVisible ? 60 : 34;
            _findPanel.Location = new Point(ClientSize.Width - _findPanel.Width - _vScrollBar.Width - 4, 2);
        }

        private void FindInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && e.Shift)
            { FindPrevious(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Enter)
            { FindNext(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Escape)
            { HideFind(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.F3 && e.Shift)
            { FindPrevious(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.F3)
            { FindNext(); e.Handled = true; e.SuppressKeyPress = true; }
        }

        public void ShowFind()
        {
            _findVisible = true;
            _replaceVisible = false;
            _replaceInput.Visible = false;
            _replaceBtn.Visible = false;
            _replaceAllBtn.Visible = false;
            PositionFindPanel();
            _findPanel.Visible = true;
            _findPanel.BringToFront();

            if (_hasSelection)
            {
                string sel = GetSelectedText();
                if (!sel.Contains("\n")) _findInput.Text = sel;
            }

            _findInput.Focus();
            _findInput.SelectAll();
            UpdateFindHighlights();
        }

        public void ShowReplace()
        {
            _findVisible = true;
            _replaceVisible = true;
            _replaceInput.Visible = true;
            _replaceBtn.Visible = true;
            _replaceAllBtn.Visible = true;
            PositionFindPanel();
            _findPanel.Visible = true;
            _findPanel.BringToFront();

            if (_hasSelection)
            {
                string sel = GetSelectedText();
                if (!sel.Contains("\n")) _findInput.Text = sel;
            }

            _findInput.Focus();
            _findInput.SelectAll();
            UpdateFindHighlights();
        }

        public void HideFind()
        {
            _findVisible = false;
            _replaceVisible = false;
            _findPanel.Visible = false;
            _findMatches.Clear();
            _currentMatchIndex = -1;
            Focus();
            Invalidate();
        }

        private void UpdateFindHighlights()
        {
            _findMatches.Clear();
            _currentMatchIndex = -1;

            string search = _findInput != null ? _findInput.Text : "";
            if (string.IsNullOrEmpty(search))
            {
                if (_findCountLabel != null) _findCountLabel.Text = "";
                Invalidate();
                return;
            }

            string fullText = _doc.Text;
            StringComparison comp = _caseSensitiveChk != null && _caseSensitiveChk.Checked
                ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            int idx = 0;
            while ((idx = fullText.IndexOf(search, idx, comp)) >= 0)
            {
                int sLine, sCol, eLine, eCol;
                OffsetToPosition(idx, out sLine, out sCol);
                OffsetToPosition(idx + search.Length, out eLine, out eCol);
                _findMatches.Add(new TextRange
                {
                    Start = new TextPosition(sLine, sCol),
                    End = new TextPosition(eLine, eCol)
                });
                idx += Math.Max(1, search.Length);
            }

            if (_findMatches.Count > 0)
            {
                _currentMatchIndex = 0;
                for (int i = 0; i < _findMatches.Count; i++)
                {
                    if (_findMatches[i].Start >= _caret) { _currentMatchIndex = i; break; }
                }
            }

            if (_findCountLabel != null)
                _findCountLabel.Text = _findMatches.Count > 0
                    ? $"{_currentMatchIndex + 1}/{_findMatches.Count}" : "0";

            Invalidate();
        }

        private void OffsetToPosition(int offset, out int line, out int col)
        {
            line = 0;
            col = 0;
            int pos = 0;
            for (int i = 0; i < _doc.LineCount; i++)
            {
                int lineLen = _doc.GetLineLength(i);
                if (pos + lineLen >= offset)
                {
                    line = i;
                    col = offset - pos;
                    return;
                }
                pos += lineLen + 1;
            }
            line = _doc.LineCount - 1;
            col = _doc.GetLineLength(line);
        }

        public void FindNext()
        {
            if (_findMatches.Count == 0) return;
            _currentMatchIndex = (_currentMatchIndex + 1) % _findMatches.Count;
            GoToMatch();
        }

        public void FindPrevious()
        {
            if (_findMatches.Count == 0) return;
            _currentMatchIndex = (_currentMatchIndex - 1 + _findMatches.Count) % _findMatches.Count;
            GoToMatch();
        }

        private void GoToMatch()
        {
            if (_currentMatchIndex < 0 || _currentMatchIndex >= _findMatches.Count) return;
            var match = _findMatches[_currentMatchIndex];
            SetSelection(match.Start, match.End);
            _caret = match.End;
            EnsureCaretVisible();
            ResetCaretBlink();
            if (_findCountLabel != null)
                _findCountLabel.Text = $"{_currentMatchIndex + 1}/{_findMatches.Count}";
            Invalidate();
        }

        public void ReplaceOne()
        {
            if (_findMatches.Count == 0) return;
            if (_currentMatchIndex < 0 || _currentMatchIndex >= _findMatches.Count) return;

            var match = _findMatches[_currentMatchIndex];
            _doc.Delete(match.Start, match.End);
            var endPos = _doc.Insert(match.Start, _replaceInput.Text);
            _caret = endPos;
            ClearSelection();
            UpdateFindHighlights();
            FindNext();
        }

        public void ReplaceAll()
        {
            if (_findMatches.Count == 0) return;

            _doc.BeginComposite(_caret);
            for (int i = _findMatches.Count - 1; i >= 0; i--)
            {
                var match = _findMatches[i];
                _doc.Delete(match.Start, match.End);
                _doc.Insert(match.Start, _replaceInput.Text);
            }
            _doc.EndComposite(_caret);
            UpdateFindHighlights();
        }

        private void PaintFindHighlights(Graphics g, int firstVisIdx, int lastVisIdx)
        {
            if (_findMatches.Count == 0) return;

            for (int mi = 0; mi < _findMatches.Count; mi++)
            {
                var match = _findMatches[mi];
                bool isCurrent = mi == _currentMatchIndex;
                Color color = isCurrent ? Color.FromArgb(120, 255, 150, 50) : Color.FromArgb(80, 255, 235, 59);

                using (var brush = new SolidBrush(color))
                {
                    for (int ln = match.Start.Line; ln <= match.End.Line; ln++)
                    {
                        if (!IsLineVisible(ln)) continue;
                        float y = ScreenYForLine(ln);
                        if (y + _lineHeight < 0 || y > ClientSize.Height) continue;
                        int startCol = (ln == match.Start.Line) ? match.Start.Column : 0;
                        int endCol = (ln == match.End.Line) ? match.End.Column : _doc.GetLineLength(ln);
                        float x1 = _gutterWidth + TextLeftPadding + startCol * _charWidth - _scrollX;
                        float x2 = _gutterWidth + TextLeftPadding + endCol * _charWidth - _scrollX;
                        g.FillRectangle(brush, x1, y, x2 - x1, _lineHeight);
                    }
                }
            }
        }

        #endregion

        #region Code Folding

        private void RebuildFoldRegions()
        {
            if (_foldingProvider == null) { _foldRegions.Clear(); return; }
            if (!_foldingDirty) return;
            _foldingDirty = false;

            var newRegions = _foldingProvider.GetFoldRegions(_doc.Text);
            var oldCollapsed = new HashSet<int>();
            foreach (var r in _foldRegions)
            {
                if (r.IsCollapsed) oldCollapsed.Add(r.StartLine);
            }

            _foldRegions = newRegions ?? new List<FoldRegion>();
            foreach (var r in _foldRegions)
            {
                if (oldCollapsed.Contains(r.StartLine))
                    r.IsCollapsed = true;
            }

            RebuildCollapsedLineMap();
        }

        private void RebuildCollapsedLineMap()
        {
            _collapsedLines.Clear();
            foreach (var r in _foldRegions)
            {
                if (r.IsCollapsed)
                {
                    for (int i = r.StartLine + 1; i <= r.EndLine; i++)
                        _collapsedLines[i] = true;
                }
            }
            RebuildVisibleLinesList();
        }

        private void RebuildVisibleLinesList()
        {
            _visibleLinesList.Clear();
            _lineToVisibleIndex.Clear();
            for (int i = 0; i < _doc.LineCount; i++)
            {
                if (!_collapsedLines.ContainsKey(i))
                {
                    _lineToVisibleIndex[i] = _visibleLinesList.Count;
                    _visibleLinesList.Add(i);
                }
            }
        }

        private bool IsLineVisible(int line)
        {
            return !_collapsedLines.ContainsKey(line);
        }

        private int GetVisibleLineCount()
        {
            return _visibleLinesList.Count > 0 ? _visibleLinesList.Count : _doc.LineCount;
        }

        private int VisibleToActualLine(int visibleIndex)
        {
            if (visibleIndex < 0) return 0;
            if (_visibleLinesList.Count == 0)
                return Math.Max(0, Math.Min(visibleIndex, _doc.LineCount - 1));
            if (visibleIndex >= _visibleLinesList.Count)
                return _visibleLinesList[_visibleLinesList.Count - 1];
            return _visibleLinesList[visibleIndex];
        }

        private int ActualToVisibleLine(int actualLine)
        {
            if (_lineToVisibleIndex.Count == 0)
                return actualLine;
            int vi;
            if (_lineToVisibleIndex.TryGetValue(actualLine, out vi))
                return vi;
            for (int i = actualLine; i >= 0; i--)
            {
                if (_lineToVisibleIndex.TryGetValue(i, out vi))
                    return vi;
            }
            return 0;
        }

        private float ScreenYForLine(int actualLine)
        {
            int vi = ActualToVisibleLine(actualLine);
            return (vi - _scrollY) * _lineHeight;
        }

        private void ToggleFold(int line)
        {
            foreach (var r in _foldRegions)
            {
                if (r.StartLine == line)
                {
                    r.IsCollapsed = !r.IsCollapsed;
                    RebuildCollapsedLineMap();
                    UpdateScrollBars();
                    Invalidate();
                    return;
                }
            }
        }

        private FoldRegion GetFoldRegionForLine(int line)
        {
            foreach (var r in _foldRegions)
                if (r.StartLine == line) return r;
            return null;
        }

        private void PaintFoldMargin(Graphics g, int screenY, int actualLine)
        {
            if (_foldingProvider == null) return;

            int foldX = _gutterWidth - FoldMarginWidth;
            float y = screenY;

            var region = GetFoldRegionForLine(actualLine);
            if (region != null)
            {
                int cx = foldX + FoldMarginWidth / 2;
                int cy = (int)(y + _lineHeight / 2);
                int sz = 8;

                using (var pen = new Pen(Color.FromArgb(120, 120, 120), 1f))
                {
                    g.DrawRectangle(pen, cx - sz / 2, cy - sz / 2, sz, sz);

                    g.DrawLine(pen, cx - 2, cy, cx + 2, cy);
                    if (region.IsCollapsed)
                        g.DrawLine(pen, cx, cy - 2, cx, cy + 2);
                }

                if (region.IsCollapsed)
                {
                    PaintCollapsedIndicator(g, actualLine, y);
                }
            }
        }

        private void PaintCollapsedIndicator(Graphics g, int line, float y)
        {
            string lineText = _doc.GetLine(line);
            float textEndX = _gutterWidth + TextLeftPadding + lineText.Length * _charWidth - _scrollX;

            float boxX = textEndX + 4;
            float boxY = y + 2;
            float boxH = _lineHeight - 4;

            string indicator = "...";
            var indicatorSize = g.MeasureString(indicator, _editorFont, 0, StringFormat.GenericTypographic);
            float boxW = indicatorSize.Width + 6;

            using (var bgBrush = new SolidBrush(Color.FromArgb(230, 230, 230)))
                g.FillRectangle(bgBrush, boxX, boxY, boxW, boxH);
            using (var borderPen = new Pen(Color.FromArgb(180, 180, 180), 1f))
                g.DrawRectangle(borderPen, boxX, boxY, boxW, boxH);
            using (var textBrush = new SolidBrush(Color.FromArgb(100, 100, 100)))
                g.DrawString(indicator, _editorFont, textBrush, boxX + 3, y + 1, StringFormat.GenericTypographic);
        }

        #endregion

        #region Autocomplete

        private void InitCompletionList()
        {
            _completionList = new ListBox();
            _completionList.Visible = false;
            _completionList.Font = new Font("Courier New", 10f);
            _completionList.BorderStyle = BorderStyle.FixedSingle;
            _completionList.IntegralHeight = false;
            _completionList.Size = new Size(260, 140);
            _completionList.DrawMode = DrawMode.OwnerDrawFixed;
            _completionList.ItemHeight = 20;
            _completionList.DrawItem += CompletionList_DrawItem;
            _completionList.DoubleClick += (s, e) => AcceptCompletion();
            _completionList.MouseDown += (s, e) => AcceptCompletion();
            Controls.Add(_completionList);
        }

        private void CompletionList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _completionItems.Count) return;

            e.DrawBackground();
            var item = _completionItems[e.Index];

            string kindText;
            Color kindColor;
            switch (item.Kind)
            {
                case CompletionItemKind.Keyword: kindText = "kw"; kindColor = Color.Blue; break;
                case CompletionItemKind.Type: kindText = "T"; kindColor = Color.FromArgb(43, 145, 175); break;
                case CompletionItemKind.Function: kindText = "fn"; kindColor = Color.FromArgb(116, 83, 31); break;
                case CompletionItemKind.Variable: kindText = "v"; kindColor = Color.FromArgb(30, 30, 30); break;
                case CompletionItemKind.Property: kindText = "p"; kindColor = Color.Purple; break;
                default: kindText = "·"; kindColor = Color.Gray; break;
            }

            using (var kindBrush = new SolidBrush(kindColor))
                e.Graphics.DrawString(kindText, e.Font, kindBrush, e.Bounds.X + 4, e.Bounds.Y + 2);

            using (var textBrush = new SolidBrush(e.ForeColor))
                e.Graphics.DrawString(item.Text, e.Font, textBrush, e.Bounds.X + 28, e.Bounds.Y + 2);

            e.DrawFocusRectangle();
        }

        private void ShowCompletion()
        {
            if (_completionProvider == null) return;

            string partial = GetPartialWord();
            if (string.IsNullOrEmpty(partial) || partial.Length < 1)
            {
                HideCompletion();
                return;
            }

            _completionPartial = partial;
            _completionItems = _completionProvider.GetCompletions(_doc.Text, _caret, partial);

            if (_completionItems.Count == 0)
            {
                HideCompletion();
                return;
            }

            if (_completionItems.Count == 1 && _completionItems[0].Text == partial)
            {
                HideCompletion();
                return;
            }

            _completionList.Items.Clear();
            foreach (var item in _completionItems)
                _completionList.Items.Add(item.Text);

            float cx = _gutterWidth + TextLeftPadding + (_caret.Column - partial.Length) * _charWidth - _scrollX;
            float cy = ScreenYForLine(_caret.Line) + _lineHeight;

            if (cy + _completionList.Height > ClientSize.Height - _hScrollBar.Height)
                cy = ScreenYForLine(_caret.Line) - _completionList.Height;

            _completionList.Location = new Point((int)cx, (int)cy);
            _completionList.SelectedIndex = 0;
            _completionList.Visible = true;
            _completionVisible = true;
            _completionList.BringToFront();
        }

        private void HideCompletion()
        {
            _completionVisible = false;
            _completionList.Visible = false;
            _completionItems.Clear();
        }

        private void AcceptCompletion()
        {
            if (!_completionVisible || _completionList.SelectedIndex < 0) return;
            if (_completionList.SelectedIndex >= _completionItems.Count) return;

            string selected = _completionItems[_completionList.SelectedIndex].Text;
            string partial = _completionPartial;

            var deleteStart = new TextPosition(_caret.Line, _caret.Column - partial.Length);
            _doc.Delete(deleteStart, _caret);
            var endPos = _doc.Insert(deleteStart, selected);
            _caret = endPos;
            ClearSelection();
            HideCompletion();
            EnsureCaretVisible();
            Invalidate();
        }

        private string GetPartialWord()
        {
            if (_caret.Line >= _doc.LineCount) return "";
            string line = _doc.GetLine(_caret.Line);
            int col = _caret.Column;
            int start = col;
            while (start > 0 && IsWordChar(line[start - 1]))
                start--;
            if (start == col) return "";
            return line.Substring(start, col - start);
        }

        private void UpdateCompletionFilter()
        {
            if (!_completionVisible) return;

            string partial = GetPartialWord();
            if (string.IsNullOrEmpty(partial))
            {
                HideCompletion();
                return;
            }

            _completionPartial = partial;
            _completionItems = _completionProvider.GetCompletions(_doc.Text, _caret, partial);

            if (_completionItems.Count == 0)
            {
                HideCompletion();
                return;
            }

            _completionList.Items.Clear();
            foreach (var item in _completionItems)
                _completionList.Items.Add(item.Text);
            _completionList.SelectedIndex = 0;
        }

        #endregion

        #region Multi-Cursor

        private void AddCursorAtPosition(TextPosition pos)
        {
            foreach (var c in _carets)
                if (c == pos) return;

            _carets.Add(pos);
            _selectionAnchors.Add(pos);
            _hasSelections.Add(false);
            Invalidate();
        }

        private void ClearExtraCursors()
        {
            _carets.Clear();
            _selectionAnchors.Clear();
            _hasSelections.Clear();
            Invalidate();
        }

        private bool HasMultipleCursors => _carets.Count > 0;

        private void SelectNextOccurrence()
        {
            string word;
            if (_hasSelection)
            {
                word = GetSelectedText();
            }
            else
            {
                word = GetWordAtCaret();
                if (string.IsNullOrEmpty(word)) return;
                int wordStart = _caret.Column;
                string line = _doc.GetLine(_caret.Line);
                while (wordStart > 0 && IsWordChar(line[wordStart - 1])) wordStart--;
                int wordEnd = wordStart + word.Length;
                SetSelection(new TextPosition(_caret.Line, wordStart), new TextPosition(_caret.Line, wordEnd));
                _caret = new TextPosition(_caret.Line, wordEnd);
                Invalidate();
                return;
            }

            if (string.IsNullOrEmpty(word)) return;

            TextPosition searchFrom = _caret;
            if (_carets.Count > 0)
            {
                searchFrom = _carets[_carets.Count - 1];
            }

            string fullText = _doc.Text;
            int offset = 0;
            for (int i = 0; i < searchFrom.Line; i++)
                offset += _doc.GetLineLength(i) + 1;
            offset += searchFrom.Column;

            int idx = fullText.IndexOf(word, offset, StringComparison.Ordinal);
            if (idx < 0)
                idx = fullText.IndexOf(word, 0, StringComparison.Ordinal);

            if (idx >= 0)
            {
                int sLine, sCol;
                OffsetToPosition(idx, out sLine, out sCol);
                int eLine, eCol;
                OffsetToPosition(idx + word.Length, out eLine, out eCol);

                var newPos = new TextPosition(eLine, eCol);
                bool alreadyExists = (newPos == _caret);
                foreach (var c in _carets)
                    if (c == newPos) { alreadyExists = true; break; }

                if (!alreadyExists)
                {
                    _carets.Add(newPos);
                    _selectionAnchors.Add(new TextPosition(sLine, sCol));
                    _hasSelections.Add(true);
                    EnsurePositionVisible(newPos);
                    Invalidate();
                }
            }
        }

        private string GetWordAtCaret()
        {
            if (_caret.Line >= _doc.LineCount) return "";
            string line = _doc.GetLine(_caret.Line);
            int col = _caret.Column;

            int start = col;
            while (start > 0 && IsWordChar(line[start - 1])) start--;
            int end = col;
            while (end < line.Length && IsWordChar(line[end])) end++;

            if (start == end) return "";
            return line.Substring(start, end - start);
        }

        private void EnsurePositionVisible(TextPosition pos)
        {
            int visibleLines = Math.Max(1, (int)(TextAreaHeight / _lineHeight));
            int posVi = ActualToVisibleLine(pos.Line);
            if (posVi < _scrollY)
                _scrollY = posVi;
            else if (posVi >= _scrollY + visibleLines)
                _scrollY = posVi - visibleLines + 1;
            UpdateScrollBars();
        }

        private void InsertAtAllCursors(char c)
        {
            if (_carets.Count == 0) return;

            _doc.BeginComposite(_caret);

            var sorted = new List<int>();
            for (int i = 0; i < _carets.Count; i++) sorted.Add(i);
            sorted.Sort((a, b) => {
                int cmp = _carets[b].Line.CompareTo(_carets[a].Line);
                return cmp != 0 ? cmp : _carets[b].Column.CompareTo(_carets[a].Column);
            });

            foreach (int idx in sorted)
            {
                if (_hasSelections[idx])
                {
                    var range = _selectionAnchors[idx] < _carets[idx]
                        ? new TextRange { Start = _selectionAnchors[idx], End = _carets[idx] }
                        : new TextRange { Start = _carets[idx], End = _selectionAnchors[idx] };
                    _doc.Delete(range.Start, range.End);
                    _carets[idx] = range.Start;
                    _hasSelections[idx] = false;
                }

                string s = c.ToString();
                var endPos = _doc.Insert(_carets[idx], s);
                _carets[idx] = endPos;
                _selectionAnchors[idx] = endPos;
            }

            _doc.EndComposite(_caret);
            Invalidate();
        }

        private void DeleteAtAllCursors()
        {
            if (_carets.Count == 0) return;

            _doc.BeginComposite(_caret);

            var sorted = new List<int>();
            for (int i = 0; i < _carets.Count; i++) sorted.Add(i);
            sorted.Sort((a, b) => {
                int cmp = _carets[b].Line.CompareTo(_carets[a].Line);
                return cmp != 0 ? cmp : _carets[b].Column.CompareTo(_carets[a].Column);
            });

            foreach (int idx in sorted)
            {
                var pos = _carets[idx];
                if (pos.Column > 0)
                {
                    var delStart = new TextPosition(pos.Line, pos.Column - 1);
                    _doc.Delete(delStart, pos);
                    _carets[idx] = delStart;
                    _selectionAnchors[idx] = delStart;
                }
                else if (pos.Line > 0)
                {
                    int prevLen = _doc.GetLineLength(pos.Line - 1);
                    var delStart = new TextPosition(pos.Line - 1, prevLen);
                    _doc.Delete(delStart, pos);
                    _carets[idx] = delStart;
                    _selectionAnchors[idx] = delStart;
                }
            }

            _doc.EndComposite(_caret);
            Invalidate();
        }

        private void PaintExtraCursors(Graphics g)
        {
            if (_carets.Count == 0) return;

            for (int i = 0; i < _carets.Count; i++)
            {
                if (_hasSelections[i])
                {
                    var range = _selectionAnchors[i] < _carets[i]
                        ? new TextRange { Start = _selectionAnchors[i], End = _carets[i] }
                        : new TextRange { Start = _carets[i], End = _selectionAnchors[i] };

                    using (var brush = new SolidBrush(_ruleset.SelectionColor))
                    {
                        for (int ln = range.Start.Line; ln <= range.End.Line; ln++)
                        {
                            if (!IsLineVisible(ln)) continue;
                            float y = ScreenYForLine(ln);
                            int startCol = (ln == range.Start.Line) ? range.Start.Column : 0;
                            int endCol = (ln == range.End.Line) ? range.End.Column : _doc.GetLineLength(ln);
                            float x1 = _gutterWidth + TextLeftPadding + startCol * _charWidth - _scrollX;
                            float x2 = _gutterWidth + TextLeftPadding + endCol * _charWidth - _scrollX;
                            g.FillRectangle(brush, x1, y, x2 - x1, _lineHeight);
                        }
                    }
                }

                if (_caretVisible && IsLineVisible(_carets[i].Line))
                {
                    float cx = _gutterWidth + TextLeftPadding + _carets[i].Column * _charWidth - _scrollX;
                    float cy = ScreenYForLine(_carets[i].Line);
                    using (var pen = new Pen(_ruleset.CaretColor, 2f))
                        g.DrawLine(pen, cx, cy, cx, cy + _lineHeight);
                }
            }
        }

        #endregion
    }
}
