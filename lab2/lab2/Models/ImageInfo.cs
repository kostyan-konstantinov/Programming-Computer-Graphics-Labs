namespace lab2.Models
{
    public class ImageInfo
    {
        public string FileName { get; set; } = "";
        public string Format { get; set; } = "";

        public int Width { get; set; }
        public int Height { get; set; }

        public string Dpi { get; set; } = "";
        public string ColorDepth { get; set; } = "";
        public string Compression { get; set; } = "";

        public string Status { get; set; } = "";

        public string ExtraInfo { get; set; } = "";
    }
}