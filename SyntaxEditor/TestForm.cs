using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodeEditor
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();

            _codeTextBox.DiagnosticsChanged += CodeTextBox_DiagnosticsChanged;
            _codeTextBox.Text = GetSampleCSharpCode();
            _languageSelector.SelectedIndex = 0;
        }

        private void FileNewMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.Text = "";
        }

        private void FileExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void EditUndoMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.PerformUndo();
        }

        private void EditRedoMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.PerformRedo();
        }

        private void EditCutMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.Cut();
        }

        private void EditCopyMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.Copy();
        }

        private void EditPasteMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.Paste();
        }

        private void EditSelectAllMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.SelectAll();
        }

        private void EditDuplicateLineMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.DuplicateLine();
        }

        private void EditDeleteLineMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.DeleteLine();
        }

        private void EditFindMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.ShowFind();
        }

        private void EditReplaceMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.ShowReplace();
        }

        private void ViewLineNumbersMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.ShowLineNumbers = _viewLineNumbersMenuItem.Checked;
        }

        private void ViewHighlightCurrentLineMenuItem_Click(object sender, EventArgs e)
        {
            _codeTextBox.HighlightCurrentLine = _viewHighlightCurrentLineMenuItem.Checked;
        }

        private void ViewGoToLineMenuItem_Click(object sender, EventArgs e)
        {
            ShowGoToLineDialog();
        }

        private void CodeTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void LanguageSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (_languageSelector.SelectedIndex)
            {
                case 0:
                    _codeTextBox.Ruleset = SyntaxRuleset.CreateCSharpRuleset();
                    _codeTextBox.DiagnosticProvider = new CSharpDiagnosticProvider();
                    _codeTextBox.CompletionProvider = new CSharpCompletionProvider();
                    _codeTextBox.FoldingProvider = new BraceFoldingProvider();
                    _codeTextBox.Text = GetSampleCSharpCode();
                    break;
                case 1:
                    _codeTextBox.Ruleset = SyntaxRuleset.CreatePythonRuleset();
                    _codeTextBox.DiagnosticProvider = new PythonDiagnosticProvider();
                    _codeTextBox.CompletionProvider = new PythonCompletionProvider();
                    _codeTextBox.FoldingProvider = new IndentFoldingProvider();
                    _codeTextBox.Text = GetSamplePythonCode();
                    break;
                case 2:
                    _codeTextBox.Ruleset = SyntaxRuleset.CreateJavaScriptRuleset();
                    _codeTextBox.DiagnosticProvider = new JavaScriptDiagnosticProvider();
                    _codeTextBox.CompletionProvider = new JavaScriptCompletionProvider();
                    _codeTextBox.FoldingProvider = new BraceFoldingProvider();
                    _codeTextBox.Text = GetSampleJavaScriptCode();
                    break;
                case 3:
                    _codeTextBox.Ruleset = SyntaxRuleset.CreatePlainTextRuleset();
                    _codeTextBox.DiagnosticProvider = null;
                    _codeTextBox.CompletionProvider = null;
                    _codeTextBox.FoldingProvider = null;
                    _codeTextBox.Text = "Hello, World!\n\nThis is plain text mode with no syntax highlighting.\nYou can still use all the editing features:\n- Undo/Redo\n- Line duplication\n- Selection and drag\n- Auto-indent\n- And more!";
                    break;
            }
            UpdateStatus();
        }

        private void CodeTextBox_DiagnosticsChanged(object sender, DiagnosticsChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var pos = _codeTextBox.CaretPosition;
            var diags = _codeTextBox.Diagnostics;
            int errors = 0, warnings = 0, infos = 0;
            foreach (var d in diags)
            {
                switch (d.Severity)
                {
                    case DiagnosticSeverity.Error: errors++; break;
                    case DiagnosticSeverity.Warning: warnings++; break;
                    case DiagnosticSeverity.Info: infos++; break;
                }
            }
            string diagText = "";
            if (errors > 0 || warnings > 0 || infos > 0)
                diagText = $"  |  Errors: {errors}  Warnings: {warnings}  Info: {infos}";
            _statusLabel.Text = $"Ln {pos.Line + 1}, Col {pos.Column + 1}  |  Lines: {_codeTextBox.LineCount}  |  {_codeTextBox.Ruleset.LanguageName}{diagText}";
        }

        private void ShowGoToLineDialog()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Go To Line";
                dialog.Size = new Size(300, 140);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(240, 240, 240);
                dialog.ForeColor = Color.FromArgb(30, 30, 30);

                var label = new Label { Text = $"Line number (1 - {_codeTextBox.LineCount}):", Location = new Point(12, 15), AutoSize = true };
                var textBox = new TextBox { Location = new Point(12, 40), Width = 260, BackColor = Color.White, ForeColor = Color.FromArgb(30, 30, 30) };
                var okButton = new Button { Text = "Go", DialogResult = DialogResult.OK, Location = new Point(116, 70), Width = 75, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
                var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(197, 70), Width = 75, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(218, 218, 218), ForeColor = Color.FromArgb(30, 30, 30) };

                dialog.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    int line;
                    if (int.TryParse(textBox.Text, out line))
                        _codeTextBox.GoToLine(line);
                }
            }
        }

        private string GetSampleCSharpCode()
        {
            return @"using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleApp
{
    /// <summary>
    /// Demonstrates syntax highlighting for C#
    /// </summary>
    public class Program
    {
        private static readonly string AppName = ""Demo Application"";
        private const int MaxRetries = 3;

        public static void Main(string[] args)
        {
            Console.WriteLine($""Welcome to {AppName}"");

            var numbers = new List<int> { 1, 2, 3, 4, 5 };
            var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();

            foreach (var num in evenNumbers)
            {
                Console.WriteLine($""Even: {num}"");
            }

            // Process data with error handling
            try
            {
                ProcessData(42, ""hello world"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($""Error: {ex.Message}"");
            }
        }

        public static async Task<bool> ProcessData(int id, string value)
        {
            if (id <= 0)
                throw new ArgumentException(""ID must be positive"");

            double result = Math.Sqrt(id) * 3.14159;
            bool isValid = result > 0 && value != null;

            /* Multi-line comment:
               This demonstrates various C# features
               including async/await and LINQ */
            await Task.Delay(100);

            return isValid;
        }
    }
}";
        }

        private string GetSamplePythonCode()
        {
            return @"#!/usr/bin/env python3
""""""
Sample Python module demonstrating syntax highlighting.
This module shows various Python language features.
""""""

import os
import sys
from typing import List, Dict, Optional
from dataclasses import dataclass

# Constants
MAX_RETRIES = 3
DEFAULT_TIMEOUT = 30.0
API_URL = ""https://api.example.com/v1""

@dataclass
class User:
    name: str
    age: int
    email: Optional[str] = None

    def greet(self) -> str:
        return f""Hello, my name is {self.name}""

    @property
    def is_adult(self) -> bool:
        return self.age >= 18

class DataProcessor:
    """"""Processes and transforms data.""""""

    def __init__(self, source: str):
        self.source = source
        self._cache: Dict[str, any] = {}

    async def fetch_data(self, query: str) -> List[dict]:
        results = []
        for i in range(MAX_RETRIES):
            try:
                data = await self._make_request(query)
                results.extend(data)
                break
            except Exception as e:
                print(f""Retry {i + 1}/{MAX_RETRIES}: {e}"")

        return results

    def process(self, items: List[dict]) -> List[dict]:
        # Filter and transform
        return [
            {**item, 'processed': True}
            for item in items
            if item.get('valid', False)
        ]

def main():
    users = [
        User(""Alice"", 30, ""alice@example.com""),
        User(""Bob"", 17),
    ]

    for user in users:
        print(user.greet())
        status = ""adult"" if user.is_adult else ""minor""
        print(f""  Status: {status}"")

    # Lambda and comprehension
    numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
    squares = list(map(lambda x: x ** 2, filter(lambda x: x % 2 == 0, numbers)))
    print(f""Even squares: {squares}"")

if __name__ == ""__main__"":
    main()";
        }

        private string GetSampleJavaScriptCode()
        {
            return @"// Modern JavaScript / ES2023 Example
'use strict';

import { readFile, writeFile } from 'fs/promises';
import path from 'path';

const API_BASE = 'https://api.example.com';
const MAX_RETRIES = 3;
const TIMEOUT_MS = 5000;

class EventEmitter {
    #listeners = new Map();

    on(event, callback) {
        if (!this.#listeners.has(event)) {
            this.#listeners.set(event, []);
        }
        this.#listeners.get(event).push(callback);
        return this;
    }

    emit(event, ...args) {
        const handlers = this.#listeners.get(event) ?? [];
        handlers.forEach(handler => handler(...args));
    }
}

class DataService extends EventEmitter {
    constructor(baseUrl) {
        super();
        this.baseUrl = baseUrl;
        this.cache = new Map();
    }

    async fetchData(endpoint, options = {}) {
        const url = `${this.baseUrl}/${endpoint}`;
        const cacheKey = JSON.stringify({ url, options });

        if (this.cache.has(cacheKey)) {
            return this.cache.get(cacheKey);
        }

        try {
            const response = await fetch(url, {
                ...options,
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers,
                },
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const data = await response.json();
            this.cache.set(cacheKey, data);
            this.emit('dataFetched', { endpoint, data });
            return data;
        } catch (error) {
            this.emit('error', { endpoint, error });
            throw error;
        }
    }
}

// Arrow functions and destructuring
const processUsers = (users) => {
    return users
        .filter(({ active }) => active)
        .map(({ name, email, role }) => ({
            displayName: name.toUpperCase(),
            contact: email,
            isAdmin: role === 'admin',
        }))
        .sort((a, b) => a.displayName.localeCompare(b.displayName));
};

// Async main with top-level await pattern
const main = async () => {
    const service = new DataService(API_BASE);

    service.on('error', ({ endpoint, error }) => {
        console.error(`Failed to fetch ${endpoint}:`, error.message);
    });

    const users = await service.fetchData('users');
    const processed = processUsers(users);
    console.log(`Processed ${processed.length} active users`);

    // Template literals and nullish coalescing
    for (const user of processed) {
        const status = user.isAdmin ? 'Admin' : 'User';
        console.log(`${user.displayName} (${status}) - ${user.contact ?? 'N/A'}`);
    }
};

main().catch(console.error);";
        }
    }
}
