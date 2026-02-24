# Code Editor Control

## Overview
A custom code text editor control built with Mono/.NET Framework (C# + WinForms) that provides IDE-like functionality without using RichTextBox. The control is generic and reusable, supporting configurable syntax highlighting rulesets for any programming language.

## Project Architecture
- **Runtime**: Mono 6.14.1, .NET Framework compatible
- **UI Framework**: System.Windows.Forms (WinForms)
- **Build**: `build.sh` compiles with `mcs` (Mono C# Compiler)
- **Output**: `CodeEditor.exe` (run via `mono CodeEditor.exe`)

## File Structure
```
src/
  SyntaxRule.cs      - SyntaxRule, SyntaxRuleset classes for configurable highlighting
  TextDocument.cs    - Document model with undo/redo stack (TextPosition, TextRange, UndoAction)
  CodeTextBox.cs     - The main custom Control (CodeTextBox) - all rendering and input handling
  TestForm.cs        - Demo form with menu, language selector, and sample code
  Program.cs         - Entry point
build.sh             - Build script
```

## Key Features
- Custom owner-drawn text rendering (no RichTextBox)
- Configurable syntax highlighting via SyntaxRuleset (regex-based)
- Undo/Redo with composite actions
- Line duplication (Ctrl+D), line deletion (Ctrl+Shift+L)
- Selection via mouse and keyboard, click-and-drag text
- Auto-indent, smart Home key, bracket auto-close
- Block indent/outdent (Tab/Shift+Tab on selection)
- Case transform (Ctrl+U / Ctrl+Shift+U)
- Go To Line dialog
- Line numbers, current line highlight, scrollbars
- Built-in rulesets: C#, Python, JavaScript, Plain Text

## Keyboard Shortcuts
- Ctrl+Z: Undo | Ctrl+Y / Ctrl+Shift+Z: Redo
- Ctrl+D: Duplicate line/selection
- Ctrl+Shift+L: Delete line
- Ctrl+C/X/V: Copy/Cut/Paste (copies whole line if no selection)
- Ctrl+A: Select All
- Ctrl+U: Lowercase | Ctrl+Shift+U: Uppercase
- Tab/Shift+Tab: Indent/Outdent
- Ctrl+[/]: Indent/Outdent selection
- Home: Smart home (first non-whitespace / column 0)
- Ctrl+Home/End: Document start/end
