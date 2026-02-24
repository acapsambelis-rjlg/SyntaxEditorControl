using System.Drawing;
using System.Windows.Forms;

namespace CodeEditor
{
    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rc = new Rectangle(Point.Empty, e.Item.Size);
            Color c = e.Item.Selected ? Color.FromArgb(62, 62, 64) : Color.FromArgb(45, 45, 45);
            using (var brush = new SolidBrush(c))
                e.Graphics.FillRectangle(brush, rc);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(45, 45, 45)))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.White;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var rc = new Rectangle(Point.Empty, e.Item.Size);
            using (var brush = new SolidBrush(Color.FromArgb(45, 45, 45)))
                e.Graphics.FillRectangle(brush, rc);
            int y = rc.Height / 2;
            using (var pen = new Pen(Color.FromArgb(70, 70, 70)))
                e.Graphics.DrawLine(pen, 30, y, rc.Width - 4, y);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(45, 45, 45)))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
        }
    }
}
