namespace CodeEditor
{
    partial class CodeTextBox
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.VScrollBar _vScrollBar;
        private System.Windows.Forms.HScrollBar _hScrollBar;
        private System.Windows.Forms.Timer _caretTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this._vScrollBar = new System.Windows.Forms.VScrollBar();
            this._hScrollBar = new System.Windows.Forms.HScrollBar();
            this._caretTimer = new System.Windows.Forms.Timer(this.components);

            this.SuspendLayout();

            //
            // _vScrollBar
            //
            this._vScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
            this._vScrollBar.ValueChanged += new System.EventHandler(this.VScrollBar_ValueChanged);

            //
            // _hScrollBar
            //
            this._hScrollBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._hScrollBar.ValueChanged += new System.EventHandler(this.HScrollBar_ValueChanged);

            //
            // _caretTimer
            //
            this._caretTimer.Interval = 530;
            this._caretTimer.Tick += new System.EventHandler(this.CaretTimer_Tick);

            //
            // CodeTextBox
            //
            this.Controls.Add(this._vScrollBar);
            this.Controls.Add(this._hScrollBar);

            this.ResumeLayout(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
                if (_editorFont != null)
                    _editorFont.Dispose();
                if (_diagnosticTimer != null)
                    _diagnosticTimer.Dispose();
                if (_diagnosticTooltip != null)
                    _diagnosticTooltip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
