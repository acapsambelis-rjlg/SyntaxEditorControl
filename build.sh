#!/bin/bash
mcs -target:winexe -out:CodeEditor.exe \
    -r:System.dll \
    -r:System.Drawing.dll \
    -r:System.Windows.Forms.dll \
    src/SyntaxRule.cs \
    src/TextDocument.cs \
    src/CodeTextBox.cs \
    src/TestForm.cs \
    src/Program.cs
