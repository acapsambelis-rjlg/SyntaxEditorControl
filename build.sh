#!/bin/bash
mcs -target:winexe -out:CodeEditor.exe \
    -r:System.dll \
    -r:System.Drawing.dll \
    -r:System.Windows.Forms.dll \
    src/SyntaxRule.cs \
    src/TextDocument.cs \
    src/CodeTextBox.Designer.cs \
    src/CodeTextBox.cs \
    src/DarkMenuRenderer.cs \
    src/TestForm.Designer.cs \
    src/TestForm.cs \
    src/Program.cs \
    src/Properties/AssemblyInfo.cs
