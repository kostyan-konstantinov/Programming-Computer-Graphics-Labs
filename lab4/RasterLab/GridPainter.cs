using System.ComponentModel;

namespace RasterLab;

public sealed class GridPainter : Control
{
    private readonly VirtualGrid grid;
    private Point lastMouse;
    private bool dragging;
    private float panX;
    private float panY;
    private int cellSize = 20;

    public event EventHandler? ZoomChanged;

    [DefaultValue(20)]
    public int CellSize
    {
        get => cellSize;
        set
        {
            cellSize = Math.Clamp(value, 2, 40);
            Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public GridPainter(VirtualGrid grid)
    {
        this.grid = grid;
        DoubleBuffered = true;
        BackColor = Color.White;
        Dock = DockStyle.Fill;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public void Fit(int minX, int minY, int maxX, int maxY)
    {
        int width = maxX - minX + 5;
        int height = maxY - minY + 5;
        CellSize = Math.Min(Math.Max(1, Width - 60) / width,
            Math.Max(1, Height - 60) / height);
        panX = -(minX + maxX) * CellSize / 2f;
        panY = (minY + maxY) * CellSize / 2f;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        float originX = Width / 2f + panX;
        float originY = Height / 2f + panY;
        int minX = Math.Max(-grid.Limit, (int)Math.Floor(-originX / CellSize) - 1);
        int maxX = Math.Min(grid.Limit, (int)Math.Ceiling((Width - originX) / CellSize) + 1);
        int minY = Math.Max(-grid.Limit, (int)Math.Floor((originY - Height) / CellSize) - 1);
        int maxY = Math.Min(grid.Limit, (int)Math.Ceiling(originY / CellSize) + 1);

        // целочисленная координата задает центр квадратного пикселя
        using var brush = new SolidBrush(Color.Black);
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                double intensity = grid.GetPixel(x, y);
                if (intensity <= 0) continue;
                brush.Color = Color.FromArgb((int)Math.Round(255 * intensity), Color.Black);
                g.FillRectangle(brush, originX + (x - 0.5f) * CellSize,
                    originY - (y + 0.5f) * CellSize, CellSize, CellSize);
            }
        }

        // сетка и оси относятся только к интерфейсу
        using var gridPen = new Pen(Color.LightGray);
        if (CellSize >= 5)
        {
            for (int x = minX; x <= maxX + 1; x++)
            {
                float screenX = originX + (x - 0.5f) * CellSize;
                g.DrawLine(gridPen, screenX, 0, screenX, Height);
            }
            for (int y = minY; y <= maxY + 1; y++)
            {
                float screenY = originY - (y - 0.5f) * CellSize;
                g.DrawLine(gridPen, 0, screenY, Width, screenY);
            }
        }

        using var axisPen = new Pen(Color.SteelBlue, 1.5f);
        g.DrawLine(axisPen, 0, originY, Width, originY);
        g.DrawLine(axisPen, originX, 0, originX, Height);
        int labelStep = CellSize >= 18 ? 1 : (CellSize >= 8 ? 5 : 10);
        float labelY = Math.Clamp(originY + 3, 0, Math.Max(0, Height - 20));
        float labelX = Math.Clamp(originX + 3, 0, Math.Max(0, Width - 35));
        for (int x = minX; x <= maxX; x++)
        {
            if (x % labelStep != 0) continue;
            float screenX = originX + x * CellSize;
            g.DrawString(x.ToString(), Font, Brushes.SteelBlue, screenX + 2, labelY);
        }
        for (int y = minY; y <= maxY; y++)
        {
            if (y == 0 || y % labelStep != 0) continue;
            float screenY = originY - y * CellSize;
            g.DrawString(y.ToString(), Font, Brushes.SteelBlue, labelX, screenY + 1);
        }
        g.DrawString("X", Font, Brushes.SteelBlue, Width - 20, labelY);
        g.DrawString("Y", Font, Brushes.SteelBlue, labelX, 3);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        dragging = true;
        lastMouse = e.Location;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!dragging) return;
        panX += e.X - lastMouse.X;
        panY += e.Y - lastMouse.Y;
        lastMouse = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        dragging = false;
        Capture = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        float oldOriginX = Width / 2f + panX;
        float oldOriginY = Height / 2f + panY;
        int oldSize = CellSize;
        CellSize += Math.Sign(e.Delta) * 2;
        float ratio = (float)CellSize / oldSize;
        panX = e.X + (oldOriginX - e.X) * ratio - Width / 2f;
        panY = e.Y + (oldOriginY - e.Y) * ratio - Height / 2f;
        Invalidate();
    }
}
