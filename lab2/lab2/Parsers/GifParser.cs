using System;
using System.IO;
using System.Text;
using lab2.Models;

namespace lab2.Parsers
{
    public static class GifParser
    {
        public static ImageInfo Read(string path)
        {
            ImageInfo info = new ImageInfo
            {
                FileName = Path.GetFileName(path),
                Format = "GIF",
                Dpi = "Не задано",
                ColorDepth = "-",
                Compression = "LZW",
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

                if (fs.Length < 13)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                byte[] header = new byte[6];
                fs.ReadExactly(header, 0, 6);

                string signature = Encoding.ASCII.GetString(header);

                if (signature != "GIF87a" && signature != "GIF89a")
                {
                    info.Status = "Неверная сигнатура";
                    return info;
                }

                byte[] descriptor = new byte[7];
                fs.ReadExactly(descriptor, 0, 7);

                info.Width =
                    descriptor[0] |
                    (descriptor[1] << 8);

                info.Height =
                    descriptor[2] |
                    (descriptor[3] << 8);

                byte packed = descriptor[4];

                bool globalColorTable =
                    (packed & 0b10000000) != 0;

                int paletteBits =
                    (packed & 0b00000111) + 1;

                if (globalColorTable)
                {
                    int colors = 1 << paletteBits;

                    info.ColorDepth =
                        paletteBits + " bit (" +
                        colors + " цветов)";
                }
                else
                {
                    info.ColorDepth =
                        paletteBits + " bit";
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
            catch (Exception)
            {
                info.Status = "Неизвестная ошибка";
            }

            return info;
        }
    }
}