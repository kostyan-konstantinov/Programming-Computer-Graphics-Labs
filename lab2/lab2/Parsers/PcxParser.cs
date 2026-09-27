using System;
using System.IO;
using lab2.Models;

namespace lab2.Parsers
{
    public static class PcxParser
    {
        public static ImageInfo Read(string path)
        {
            ImageInfo info = new ImageInfo
            {
                FileName = Path.GetFileName(path),
                Format = "PCX",
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

                if (fs.Length < 128)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                byte[] header = new byte[128];
                fs.ReadExactly(header, 0, 128);

                // PCX должен начинаться с 0x0A
                if (header[0] != 0x0A)
                {
                    info.Status = "Неверная сигнатура";
                    return info;
                }

                int encoding = header[2];
                int bitsPerPixel = header[3];

                int xMin = ReadUInt16LE(header, 4);
                int yMin = ReadUInt16LE(header, 6);
                int xMax = ReadUInt16LE(header, 8);
                int yMax = ReadUInt16LE(header, 10);

                info.Width = xMax - xMin + 1;
                info.Height = yMax - yMin + 1;

                int dpiX = ReadUInt16LE(header, 12);
                int dpiY = ReadUInt16LE(header, 14);

                if (dpiX > 0 && dpiY > 0)
                {
                    info.Dpi = dpiX + " x " + dpiY;
                }

                int colorPlanes = header[65];

                int totalDepth = bitsPerPixel * colorPlanes;

                info.ColorDepth = totalDepth + " bit";

                if (encoding == 1)
                {
                    info.Compression = "RLE";
                }
                else if (encoding == 0)
                {
                    info.Compression = "Без сжатия";
                }
                else
                {
                    info.Compression = "Неизвестно";
                }

                if (info.Width <= 0 || info.Height <= 0)
                {
                    info.Status = "Некорректный размер";
                    return info;
                }

                info.Status = "OK";
            }
            catch (EndOfStreamException)
            {
                info.Status = "Файл поврежден";
            }
            catch (UnauthorizedAccessException)
            {
                info.Status = "Нет доступа";
            }
            catch (IOException)
            {
                info.Status = "Ошибка чтения";
            }
            catch
            {
                info.Status = "Неизвестная ошибка";
            }

            return info;
        }

        private static int ReadUInt16LE(byte[] data, int offset)
        {
            return data[offset] |
                   (data[offset + 1] << 8);
        }
    }
}