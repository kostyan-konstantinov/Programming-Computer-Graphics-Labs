using System;
using System.IO;
using lab2.Models;

namespace lab2.Parsers
{
    public static class BmpParser
    {
        public static ImageInfo Read(string path)
        {
            ImageInfo info = new ImageInfo
            {
                FileName = Path.GetFileName(path),
                Format = "BMP",
                Dpi = "Не задано",
                ColorDepth = "-",
                Compression = "-",
                Status = "OK"
            };

            try
            {
                using FileStream fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );

                if (fs.Length < 54)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                byte[] header = new byte[54];

                fs.ReadExactly(header, 0, 54);

                // Сигнатура BM
                if (header[0] != 0x42 ||
                    header[1] != 0x4D)
                {
                    info.Status = "Неверная сигнатура";
                    return info;
                }

                // Размер файла из BITMAPFILEHEADER
                uint fileSize =
                    BitConverter.ToUInt32(header, 2);

                if (fileSize > fs.Length)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                // Смещение начала пиксельных данных
                uint pixelOffset =
                    BitConverter.ToUInt32(header, 10);

                if (pixelOffset >= fs.Length)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                // Размер BITMAPINFOHEADER
                uint dibHeaderSize =
                    BitConverter.ToUInt32(header, 14);

                if (dibHeaderSize < 40)
                {
                    info.Status =
                        "Неподдерживаемый BMP-заголовок";

                    return info;
                }

                info.Width =
                    BitConverter.ToInt32(header, 18);

                info.Height =
                    Math.Abs(
                        BitConverter.ToInt32(header, 22)
                    );

                ushort bitsPerPixel =
                    BitConverter.ToUInt16(header, 28);

                info.ColorDepth =
                    bitsPerPixel + " bit";

                uint compression =
                    BitConverter.ToUInt32(header, 30);

                info.Compression =
                    GetCompressionName(compression);

                int xPixelsPerMeter =
                    BitConverter.ToInt32(header, 38);

                int yPixelsPerMeter =
                    BitConverter.ToInt32(header, 42);

                if (xPixelsPerMeter > 0 &&
                    yPixelsPerMeter > 0)
                {
                    double dpiX =
                        xPixelsPerMeter * 0.0254;

                    double dpiY =
                        yPixelsPerMeter * 0.0254;

                    info.Dpi =
                        Math.Round(dpiX) +
                        " x " +
                        Math.Round(dpiY);
                }

                if (info.Width <= 0 ||
                    info.Height <= 0)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                info.Status = "OK";
            }
            catch (EndOfStreamException)
            {
                info.Status = "Файл поврежден";
            }
            catch (IOException)
            {
                info.Status = "Ошибка чтения";
            }
            catch
            {
                info.Status = "Ошибка чтения";
            }

            return info;
        }

        private static string GetCompressionName(
            uint compression)
        {
            switch (compression)
            {
                case 0:
                    return "BI_RGB";

                case 1:
                    return "BI_RLE8";

                case 2:
                    return "BI_RLE4";

                case 3:
                    return "BI_BITFIELDS";

                case 4:
                    return "BI_JPEG";

                case 5:
                    return "BI_PNG";

                default:
                    return "Неизвестно";
            }
        }
    }
}