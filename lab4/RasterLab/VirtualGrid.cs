namespace RasterLab;

public sealed class VirtualGrid : IRasterTarget
{
    private readonly double[,] pixels;

    public int Limit { get; }

    public VirtualGrid(int limit)
    {
        Limit = limit;
        pixels = new double[2 * limit + 1, 2 * limit + 1];
    }

    public double GetPixel(int x, int y)
    {
        if (x < -Limit || x > Limit || y < -Limit || y > Limit)
            return 0;
        return pixels[x + Limit, y + Limit];
    }

    public void SetPixel(int x, int y, double intensity)
    {
        if (x < -Limit || x > Limit || y < -Limit || y > Limit)
            return;

        // при наложении сохраняем наибольшую интенсивность
        int column = x + Limit;
        int row = y + Limit;
        pixels[column, row] = Math.Max(pixels[column, row], Math.Clamp(intensity, 0, 1));
    }

    public void Clear()
    {
        Array.Clear(pixels);
    }
}
