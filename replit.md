# Code Editor Control

## Overview
A custom code text editor control built with Mono/.NET Framework (C# + WinForms) that provides IDE-like functionality without using RichTextBox. The control is generic and reusable, supporting configurable syntax highlighting rulesets for any programming language.

## Project Architecture
- **Runtime**: Mono 6.14.1, .NET Framework 4.7.2 compatible
- **UI Framework**: System.Windows.Forms (WinForms)
- **Theme**: Light theme (white background, dark text, VS-style syntax colors)
- **Build (Replit)**: `build.sh` compiles with `mcs` (Mono C# Compiler)
- **Build (Visual Studio)**: Open `CodeEditor.sln`, build via MSBuild
- **Output**: `CodeEditor.exe` (run via `mono CodeEditor.exe` on Linux)

## File Structure
```
CodeEditor.sln                        - Visual Studio solution file
SyntaxEditor/
  SyntaxEditor.csproj                  - Visual Studio project file (.NET Framework 4.7.2)
  SyntaxRule.cs                        - SyntaxRule, SyntaxRuleset classes for configurable highlighting
  TextDocument.cs                      - Document model with undo/redo stack (TextPosition, TextRange, UndoAction)
  Diagnostic.cs                        - Diagnostic, DiagnosticSeverity, IDiagnosticProvider, DiagnosticsChangedEventArgs
  DiagnosticProviders.cs               - Example providers: CSharpDiagnosticProvider, PythonDiagnosticProvider, JavaScriptDiagnosticProvider
  CompletionProvider.cs                - ICompletionProvider, CompletionItem, CompletionItemKind + C#/Python/JS providers
  FoldingProvider.cs                   - IFoldingProvider, FoldRegion, BraceFoldingProvider, IndentFoldingProvider
  CodeTextBox.cs                       - Main custom Control (CodeTextBox) - rendering and input handling
  CodeTextBox.Designer.cs              - Designer-generated component initialization (scrollbars, timer)
  CodeTextBox.resx                     - CodeTextBox embedded resources
  TestForm.cs                          - Demo form with menu, language selector, and sample code
  TestForm.Designer.cs                 - Designer-generated form layout and controls
  TestForm.resx                        - TestForm embedded resources
  Program.cs                           - Entry point
  App.config                           - Runtime configuration
  Properties/
    AssemblyInfo.cs                    - Assembly metadata
    Resources.Designer.cs              - Auto-generated resources accessor
    Resources.resx                     - Project resources
    Settings.Designer.cs               - Auto-generated settings accessor
build.sh                               - Mono build script (for Replit environment)
```

## Key Features
- Custom owner-drawn text rendering (no RichTextBox)
- Configurable syntax highlighting via SyntaxRuleset (regex-based)
- All theme colors driven by SyntaxRuleset properties (fully themeable)
- Diagnostics system with IDiagnosticProvider interface, squiggly underlines, and hover tooltips
- Undo/Redo with composite actions
- Line duplication (Ctrl+D), line deletion (Ctrl+Shift+L)
- Selection via mouse and keyboard, click-and-drag text with drop cursor preview
- Auto-indent, smart Home key, bracket auto-close with skip-over closing characters
- Block indent/outdent (Tab/Shift+Tab on selection)
- Line/block commenting toggle (Ctrl+/) using LineCommentToken from SyntaxRuleset
- Case transform (Ctrl+U / Ctrl+Shift+U)
- Ctrl+scroll wheel zoom (6pt–48pt), Shift+scroll horizontal scrolling
- Go To Line dialog
- Line numbers, current line highlight, scrollbars (gutter clips text area)
- Indent guides (faint dotted vertical lines at each indentation level)
- Built-in rulesets: C#, Python, JavaScript, Plain Text
- **Find & Replace** panel (Ctrl+F / Ctrl+H) with match highlighting
- **Autocomplete** popup with ICompletionProvider (Ctrl+Space, auto-trigger on typing)
- **Bracket matching** highlight for `()`, `[]`, `{}`
- **Code folding** with IFoldingProvider (brace-based and indent-based)
- **Multi-cursor editing** (Ctrl+Click, Ctrl+D select next occurrence)

## Diagnostics System
- **IDiagnosticProvider** interface: implement `List<Diagnostic> Analyze(string text)` to provide diagnostics
  - Also supports `Analyze(string text, AnalysisContext context)` for cross-file symbol resolution
- **DiagnosticSeverity**: Error (red squiggle), Warning (yellow squiggle), Info (green squiggle)
- **CodeTextBox.DiagnosticProvider** property: set to auto-analyze on text change (500ms debounce)
- **CodeTextBox.AnalysisContext** property: set to provide cross-file symbol info to the provider
- **CodeTextBox.SetDiagnostics()** / **ClearDiagnostics()**: manually set/clear diagnostics
- **CodeTextBox.DiagnosticsChanged** event: fires when diagnostics update
- **Built-in providers**: CSharpDiagnosticProvider, PythonDiagnosticProvider, JavaScriptDiagnosticProvider
  - Bracket/brace matching, missing semicolons/colons, empty catch blocks, unused variables, style warnings

## Find & Replace
- **Ctrl+F**: Opens find panel (overlay at top-right of editor)
- **Ctrl+H**: Opens find & replace panel
- All matches highlighted in yellow, current match in orange
- Case-sensitive toggle button
- F3 / Shift+F3: Navigate between matches
- Enter: Find next, Shift+Enter: Find previous
- Replace one / Replace all buttons
- Escape closes panel
- Match count displayed in panel

## Autocomplete / Intellisense
- **ICompletionProvider** interface: `List<CompletionItem> GetCompletions(string text, TextPosition caret, string partialWord)`
- **CompletionItem**: Text, Kind (Keyword/Type/Function/Variable/Property/Snippet/Constant/Module), Description
- **CompletionItemKind** enum drives icon/color in popup
- **CodeTextBox.CompletionProvider** property: set to enable autocomplete
- Ctrl+Space: Force open completion popup
- Auto-triggers on typing identifiers or `.`
- Arrow keys navigate, Enter/Tab accept, Escape dismiss
- Filters as you type
- **Built-in providers**: CSharpCompletionProvider, PythonCompletionProvider, JavaScriptCompletionProvider

## Code Folding
- **IFoldingProvider** interface: `List<FoldRegion> GetFoldRegions(string text)`
- **FoldRegion**: StartLine, EndLine, IsCollapsed
- **CodeTextBox.FoldingProvider** property: set to enable folding
- Fold margin with +/- toggle boxes in gutter
- Click fold margin to collapse/expand regions
- Collapsed lines are hidden from rendering; `[...]` indicator drawn at end of fold start line
- **BraceFoldingProvider** attaches fold to the declaration line (line before lone `{` brace)
- Collapse state preserved across text edits
- **Built-in providers**: BraceFoldingProvider (C#/JS), IndentFoldingProvider (Python)

## Multi-Cursor Editing
- **Ctrl+Click**: Add cursor at clicked position
- **Ctrl+D**: Select next occurrence of current word/selection
- All cursors type, delete, and operate simultaneously
- Extra cursors rendered with carets and selections
- **Escape**: Clear all extra cursors, return to single cursor
- Primary caret (`_caret`) remains separate; extra cursors in `_carets` list

## Bracket Matching
- Highlights matching bracket pair when caret is adjacent to `()`, `[]`, `{}`
- Light blue background highlight on both brackets
- Scans forward for open brackets, backward for close brackets
- Updates on every caret move (keyboard, mouse)

## Cross-File Analysis Infrastructure (for multi-file editors)
- **SymbolInfo**: represents an exported symbol (name, kind, return type, parameters, visibility, metadata)
- **SymbolKind** enum: Class, Struct, Interface, Enum, Function, Method, Property, Field, Variable, Constant, Namespace, Module, Event, Delegate, TypeAlias
- **FileSymbols**: groups exported symbols for a single file (HasSymbol, GetSymbol, GetSymbolsByKind)
- **AnalysisContext**: holds a map of FileSymbols keyed by file path, with cross-file resolution:
  - `ResolveSymbol(name)` — finds a symbol across all other files
  - `GetAllExportedSymbolNames()` / `GetAllExportedSymbols()` — enumerates available cross-file symbols
  - `CurrentFilePath` — identifies which file is being analyzed (excluded from resolution)
- Usage: host app populates an AnalysisContext with FileSymbols from each open file, sets `CodeTextBox.AnalysisContext`, and the provider's `Analyze(text, context)` uses it to avoid false positives on cross-file imports

## Keyboard Shortcuts
- Ctrl+Z: Undo | Ctrl+Y / Ctrl+Shift+Z: Redo
- Ctrl+D: Select next occurrence (multi-cursor)
- Ctrl+Shift+L: Delete line
- Ctrl+C/X/V: Copy/Cut/Paste (copies whole line if no selection)
- Ctrl+A: Select All
- Ctrl+U: Lowercase | Ctrl+Shift+U: Uppercase
- Ctrl+F: Find | Ctrl+H: Find & Replace
- F3 / Shift+F3: Find next / previous
- Ctrl+Space: Open autocomplete
- Ctrl+Click: Add cursor
- Tab/Shift+Tab: Indent/Outdent (or accept completion)
- Ctrl+[/]: Indent/Outdent selection
- Ctrl+/: Toggle line/block comment
- Home: Smart home (first non-whitespace / column 0)
- Ctrl+Home/End: Document start/end
- Ctrl+Scroll: Zoom in/out
- Shift+Scroll: Horizontal scroll
- Escape: Close find panel / clear extra cursors / dismiss completion
