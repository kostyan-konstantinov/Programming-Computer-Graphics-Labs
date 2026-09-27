using System;
using System.IO;
using lab2.Models;
using lab2.Parsers;

namespace lab2.Services
{
    public static class ImageScanner
    {
        public static ImageInfo ReadFile(string path)
        {
            try
            {
                string realFormat = DetectFormat(path);

                ImageInfo info;

                switch (realFormat)
                {
                    case "BMP":
                        info = BmpParser.Read(path);
                        break;

                    case "PNG":
                        info = PngParser.Read(path);
                        break;

                    case "JPEG":
                        info = JpegParser.Read(path);
                        break;

                    case "GIF":
                        info = GifParser.Read(path);
                        break;

                    case "TIFF":
                        info = TiffParser.Read(path);
                        break;

                    case "PCX":
                        info = PcxParser.Read(path);
                        break;

                    default:
                        return new ImageInfo
                        {
                            FileName = Path.GetFileName(path),
                            Format = "Unknown",
                            Width = 0,
                            Height = 0,
                            Dpi = "-",
                            ColorDepth = "-",
                            Compression = "-",
                            Status = "Неизвестный или поврежденный формат"
                        };
                }

                string expectedFormat = GetFormatFromExtension(path);

                if (expectedFormat != "Unknown" &&
                    expectedFormat != realFormat &&
                    info.Status == "OK")
                {
                    info.Status = "Расширение не соответствует формату";
                }

                return info;
            }
            catch (Exception)
            {
                return new ImageInfo
                {
                    FileName = Path.GetFileName(path),
                    Format = "Unknown",
                    Width = 0,
                    Height = 0,
                    Dpi = "-",
                    ColorDepth = "-",
                    Compression = "-",
                    Status = "Ошибка чтения"
                };
            }
        }

        private static string DetectFormat(string path)
        {
            using FileStream fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

            if (fs.Length == 0)
            {
                return "Unknown";
            }

            byte[] header = new byte[8];

            int count = (int)Math.Min(8, fs.Length);

            fs.ReadExactly(header, 0, count);

            // BMP: 42 4D = BM
            if (count >= 2 &&
                header[0] == 0x42 &&
                header[1] == 0x4D)
            {
                return "BMP";
            }

            // JPEG: FF D8
            if (count >= 2 &&
                header[0] == 0xFF &&
                header[1] == 0xD8)
            {
                return "JPEG";
            }

            // PNG
            if (count >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A)
            {
                return "PNG";
            }

            // GIF87a / GIF89a
            if (count >= 6 &&
                header[0] == (byte)'G' &&
                header[1] == (byte)'I' &&
                header[2] == (byte)'F' &&
                header[3] == (byte)'8' &&
                (header[4] == (byte)'7' || header[4] == (byte)'9') &&
                header[5] == (byte)'a')
            {
                return "GIF";
            }

            // TIFF little-endian: II 2A 00
            if (count >= 4 &&
                header[0] == 0x49 &&
                header[1] == 0x49 &&
                header[2] == 0x2A &&
                header[3] == 0x00)
            {
                return "TIFF";
            }

            // TIFF big-endian: MM 00 2A
            if (count >= 4 &&
                header[0] == 0x4D &&
                header[1] == 0x4D &&
                header[2] == 0x00 &&
                header[3] == 0x2A)
            {
                return "TIFF";
            }

            // PCX
            if (count >= 1 &&
                header[0] == 0x0A)
            {
                return "PCX";
            }

            return "Unknown";
        }

        private static string GetFormatFromExtension(string path)
        {
            string extension = Path.GetExtension(path).ToLower();

            switch (extension)
            {
                case ".bmp":
                    return "BMP";

                case ".png":
                    return "PNG";

                case ".jpg":
                case ".jpeg":
                    return "JPEG";

                case ".gif":
                    return "GIF";

                case ".tif":
                case ".tiff":
                    return "TIFF";

                case ".pcx":
                    return "PCX";

                default:
                    return "Unknown";
            }
        }
    }
}