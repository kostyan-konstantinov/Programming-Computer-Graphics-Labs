namespace RasterLab;

public sealed class BenchmarkResult
{
    public string Experiment { get; }
    public string Algorithm { get; }
    public int Count { get; }
    public int Length { get; }
    public double Milliseconds { get; }

    public BenchmarkResult(string experiment, string algorithm, int count, int length, double milliseconds)
    {
        Experiment = experiment;
        Algorithm = algorithm;
        Count = count;
        Length = length;
        Milliseconds = milliseconds;
    }
}
