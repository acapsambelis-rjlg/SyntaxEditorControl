using System.Drawing;
using System.Windows.Forms;

namespace CodeEditor
{
    internal class MenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rc = new Rectangle(Point.Empty, e.Item.Size);
            Color c = e.Item.Selected ? Color.FromArgb(210, 228, 248) : Color.FromArgb(240, 240, 240);
            using (var brush = new SolidBrush(c))
                e.Graphics.FillRectangle(brush, rc);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(30, 30, 30);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var rc = new Rectangle(Point.Empty, e.Item.Size);
            using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                e.Graphics.FillRectangle(brush, rc);
            int y = rc.Height / 2;
            using (var pen = new Pen(Color.FromArgb(208, 208, 208)))
                e.Graphics.DrawLine(pen, 30, y, rc.Width - 4, y);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(204, 204, 204)))
            {
                var r = e.AffectedBounds;
                e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
            }
        }
    }
}
