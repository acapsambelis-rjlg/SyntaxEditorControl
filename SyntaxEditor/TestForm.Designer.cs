namespace CodeEditor
{
    partial class TestForm
    {
        private System.ComponentModel.IContainer components = null;

        private CodeTextBox _codeTextBox;
        private System.Windows.Forms.ComboBox _languageSelector;
        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.MenuStrip _menuStrip;
        private System.Windows.Forms.Panel _toolbar;
        private System.Windows.Forms.Label _toolbarLabel;
        private System.Windows.Forms.Label _helpLabel;

        private System.Windows.Forms.ToolStripMenuItem _fileMenu;
        private System.Windows.Forms.ToolStripMenuItem _fileNewMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _fileExitMenuItem;

        private System.Windows.Forms.ToolStripMenuItem _editMenu;
        private System.Windows.Forms.ToolStripMenuItem _editUndoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editRedoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editCutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editCopyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editPasteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editSelectAllMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editDuplicateLineMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editDeleteLineMenuItem;

        private System.Windows.Forms.ToolStripMenuItem _viewMenu;
        private System.Windows.Forms.ToolStripMenuItem _viewLineNumbersMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _viewHighlightCurrentLineMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _viewGoToLineMenuItem;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this._menuStrip = new System.Windows.Forms.MenuStrip();
            this._fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._fileNewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._editUndoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editRedoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editCutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editCopyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editPasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editSelectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editDuplicateLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editDeleteLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._viewMenu = new System.Windows.Forms.ToolStripMenuItem();
            this._viewLineNumbersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._viewHighlightCurrentLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._viewGoToLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolbar = new System.Windows.Forms.Panel();
            this._toolbarLabel = new System.Windows.Forms.Label();
            this._languageSelector = new System.Windows.Forms.ComboBox();
            this._helpLabel = new System.Windows.Forms.Label();
            this._codeTextBox = new CodeEditor.CodeTextBox();
            this._statusLabel = new System.Windows.Forms.Label();

            this._menuStrip.SuspendLayout();
            this._toolbar.SuspendLayout();
            this.SuspendLayout();

            //
            // _menuStrip
            //
            this._menuStrip.BackColor = System.Drawing.Color.FromArgb(45, 45, 45);
            this._menuStrip.ForeColor = System.Drawing.Color.White;
            this._menuStrip.Renderer = new DarkMenuRenderer();
            this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this._fileMenu,
                this._editMenu,
                this._viewMenu
            });

            //
            // _fileMenu
            //
            this._fileMenu.Text = "File";
            this._fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this._fileNewMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                this._fileExitMenuItem
            });

            //
            // _fileNewMenuItem
            //
            this._fileNewMenuItem.Text = "New";
            this._fileNewMenuItem.Click += new System.EventHandler(this.FileNewMenuItem_Click);

            //
            // _fileExitMenuItem
            //
            this._fileExitMenuItem.Text = "Exit";
            this._fileExitMenuItem.Click += new System.EventHandler(this.FileExitMenuItem_Click);

            //
            // _editMenu
            //
            this._editMenu.Text = "Edit";
            this._editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this._editUndoMenuItem,
                this._editRedoMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                this._editCutMenuItem,
                this._editCopyMenuItem,
                this._editPasteMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                this._editSelectAllMenuItem,
                this._editDuplicateLineMenuItem,
                this._editDeleteLineMenuItem
            });

            //
            // _editUndoMenuItem
            //
            this._editUndoMenuItem.Text = "Undo (Ctrl+Z)";
            this._editUndoMenuItem.Click += new System.EventHandler(this.EditUndoMenuItem_Click);

            //
            // _editRedoMenuItem
            //
            this._editRedoMenuItem.Text = "Redo (Ctrl+Y)";
            this._editRedoMenuItem.Click += new System.EventHandler(this.EditRedoMenuItem_Click);

            //
            // _editCutMenuItem
            //
            this._editCutMenuItem.Text = "Cut (Ctrl+X)";
            this._editCutMenuItem.Click += new System.EventHandler(this.EditCutMenuItem_Click);

            //
            // _editCopyMenuItem
            //
            this._editCopyMenuItem.Text = "Copy (Ctrl+C)";
            this._editCopyMenuItem.Click += new System.EventHandler(this.EditCopyMenuItem_Click);

            //
            // _editPasteMenuItem
            //
            this._editPasteMenuItem.Text = "Paste (Ctrl+V)";
            this._editPasteMenuItem.Click += new System.EventHandler(this.EditPasteMenuItem_Click);

            //
            // _editSelectAllMenuItem
            //
            this._editSelectAllMenuItem.Text = "Select All (Ctrl+A)";
            this._editSelectAllMenuItem.Click += new System.EventHandler(this.EditSelectAllMenuItem_Click);

            //
            // _editDuplicateLineMenuItem
            //
            this._editDuplicateLineMenuItem.Text = "Duplicate Line (Ctrl+D)";
            this._editDuplicateLineMenuItem.Click += new System.EventHandler(this.EditDuplicateLineMenuItem_Click);

            //
            // _editDeleteLineMenuItem
            //
            this._editDeleteLineMenuItem.Text = "Delete Line (Ctrl+Shift+L)";
            this._editDeleteLineMenuItem.Click += new System.EventHandler(this.EditDeleteLineMenuItem_Click);

            //
            // _viewMenu
            //
            this._viewMenu.Text = "View";
            this._viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this._viewLineNumbersMenuItem,
                this._viewHighlightCurrentLineMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                this._viewGoToLineMenuItem
            });

            //
            // _viewLineNumbersMenuItem
            //
            this._viewLineNumbersMenuItem.Text = "Line Numbers";
            this._viewLineNumbersMenuItem.Checked = true;
            this._viewLineNumbersMenuItem.CheckOnClick = true;
            this._viewLineNumbersMenuItem.Click += new System.EventHandler(this.ViewLineNumbersMenuItem_Click);

            //
            // _viewHighlightCurrentLineMenuItem
            //
            this._viewHighlightCurrentLineMenuItem.Text = "Highlight Current Line";
            this._viewHighlightCurrentLineMenuItem.Checked = true;
            this._viewHighlightCurrentLineMenuItem.CheckOnClick = true;
            this._viewHighlightCurrentLineMenuItem.Click += new System.EventHandler(this.ViewHighlightCurrentLineMenuItem_Click);

            //
            // _viewGoToLineMenuItem
            //
            this._viewGoToLineMenuItem.Text = "Go To Line...";
            this._viewGoToLineMenuItem.Click += new System.EventHandler(this.ViewGoToLineMenuItem_Click);

            //
            // _toolbar
            //
            this._toolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this._toolbar.Height = 36;
            this._toolbar.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this._toolbar.Padding = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this._toolbar.Controls.Add(this._helpLabel);
            this._toolbar.Controls.Add(this._languageSelector);
            this._toolbar.Controls.Add(this._toolbarLabel);

            //
            // _toolbarLabel
            //
            this._toolbarLabel.Text = "Language:";
            this._toolbarLabel.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this._toolbarLabel.AutoSize = true;
            this._toolbarLabel.Location = new System.Drawing.Point(8, 9);

            //
            // _languageSelector
            //
            this._languageSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._languageSelector.Location = new System.Drawing.Point(80, 5);
            this._languageSelector.Width = 160;
            this._languageSelector.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this._languageSelector.ForeColor = System.Drawing.Color.White;
            this._languageSelector.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._languageSelector.Items.AddRange(new object[] { "C#", "Python", "JavaScript", "Plain Text" });
            this._languageSelector.SelectedIndexChanged += new System.EventHandler(this.LanguageSelector_SelectedIndexChanged);

            //
            // _helpLabel
            //
            this._helpLabel.Text = "Shortcuts: Ctrl+D Duplicate | Ctrl+Shift+L Delete Line | Ctrl+Z/Y Undo/Redo | Tab/Shift+Tab Indent | Ctrl+U Case";
            this._helpLabel.ForeColor = System.Drawing.Color.FromArgb(140, 140, 140);
            this._helpLabel.AutoSize = true;
            this._helpLabel.Location = new System.Drawing.Point(260, 9);

            //
            // _codeTextBox
            //
            this._codeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._codeTextBox.Ruleset = SyntaxRuleset.CreateCSharpRuleset();
            this._codeTextBox.TabSize = 4;
            this._codeTextBox.TextChanged += new System.EventHandler(this.CodeTextBox_TextChanged);

            //
            // _statusLabel
            //
            this._statusLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._statusLabel.Height = 24;
            this._statusLabel.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this._statusLabel.ForeColor = System.Drawing.Color.White;
            this._statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._statusLabel.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._statusLabel.Text = "Ready";

            //
            // TestForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 750);
            this.Text = "Code Editor - Test Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(45, 45, 45);
            this.ForeColor = System.Drawing.Color.White;
            this.MainMenuStrip = this._menuStrip;

            this.Controls.Add(this._codeTextBox);
            this.Controls.Add(this._toolbar);
            this.Controls.Add(this._statusLabel);
            this.Controls.Add(this._menuStrip);

            this._codeTextBox.BringToFront();

            this._menuStrip.ResumeLayout(false);
            this._menuStrip.PerformLayout();
            this._toolbar.ResumeLayout(false);
            this._toolbar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
