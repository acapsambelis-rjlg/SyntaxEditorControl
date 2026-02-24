#!/bin/bash
mcs -target:winexe -out:CodeEditor.exe \
    -r:System.dll \
    -r:System.Drawing.dll \
    -r:System.Windows.Forms.dll \
    SyntaxEditor/SyntaxRule.cs \
    SyntaxEditor/TextDocument.cs \
    SyntaxEditor/CodeTextBox.Designer.cs \
    SyntaxEditor/CodeTextBox.cs \
    SyntaxEditor/DarkMenuRenderer.cs \
    SyntaxEditor/TestForm.Designer.cs \
    SyntaxEditor/TestForm.cs \
    SyntaxEditor/Program.cs \
    SyntaxEditor/Properties/AssemblyInfo.cs
