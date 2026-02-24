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
  CodeTextBox.cs                       - Main custom Control (CodeTextBox) - rendering and input handling
  CodeTextBox.Designer.cs              - Designer-generated component initialization (scrollbars, timer)
  CodeTextBox.resx                     - CodeTextBox embedded resources
  DarkMenuRenderer.cs                  - Light theme menu renderer (LightMenuRenderer class)
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
