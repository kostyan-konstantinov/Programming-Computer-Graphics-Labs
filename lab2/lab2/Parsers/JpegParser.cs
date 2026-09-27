
using System;
using System.IO;
using lab2.Models;

namespace lab2.Parsers
{
    public static class JpegParser
    {
        public static ImageInfo Read(string path)
        {
            ImageInfo info = new ImageInfo
            {
                FileName = Path.GetFileName(path),
                Format = "JPEG",
                Dpi = "Не задано",
                ColorDepth = "-",
                Compression = "JPEG",
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

                if (fs.Length < 4)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                // SOI
                if (fs.ReadByte() != 0xFF ||
                    fs.ReadByte() != 0xD8)
                {
                    info.Status = "Неверная сигнатура";
                    return info;
                }

                bool foundSize = false;

                while (fs.Position < fs.Length)
                {
                    int prefix = fs.ReadByte();

                    if (prefix == -1)
                        break;

                    if (prefix != 0xFF)
                        continue;

                    int marker;

                    do
                    {
                        marker = fs.ReadByte();
                    }
                    while (marker == 0xFF);

                    if (marker == -1)
                        break;

                    // EOI
                    if (marker == 0xD9)
                        break;

                    // SOS
                    if (marker == 0xDA)
                        break;

                    // Маркеры без длины
                    if (marker >= 0xD0 && marker <= 0xD7)
                        continue;

                    if (marker == 0x01)
                        continue;

                    int length = ReadUInt16BE(fs);

                    if (length < 2)
                    {
                        info.Status = "Файл поврежден";
                        return info;
                    }

                    int dataLength = length - 2;

                    long segmentStart = fs.Position;

                    // APP0 JFIF
                    if (marker == 0xE0 && dataLength >= 14)
                    {
                        byte[] app0 = new byte[dataLength];
                        fs.ReadExactly(app0, 0, dataLength);

                        if (app0.Length >= 14 &&
                            app0[0] == (byte)'J' &&
                            app0[1] == (byte)'F' &&
                            app0[2] == (byte)'I' &&
                            app0[3] == (byte)'F' &&
                            app0[4] == 0)
                        {
                            int units = app0[7];

                            int densityX =
                                (app0[8] << 8) |
                                app0[9];

                            int densityY =
                                (app0[10] << 8) |
                                app0[11];

                            if (units == 1)
                            {
                                info.Dpi =
                                    densityX + " x " + densityY;
                            }
                            else if (units == 2)
                            {
                                double dpiX = densityX * 2.54;
                                double dpiY = densityY * 2.54;

                                info.Dpi =
                                    Math.Round(dpiX) + " x " +
                                    Math.Round(dpiY);
                            }
                        }
                    }

                    // SOF0 - Baseline JPEG
                    else if (marker == 0xC0 && dataLength >= 6)
                    {
                        ReadSof(fs, info);

                        info.Compression = "JPEG Baseline";
                        foundSize = true;
                    }

                    // SOF1 - Extended Sequential
                    else if (marker == 0xC1 && dataLength >= 6)
                    {
                        ReadSof(fs, info);

                        info.Compression = "JPEG Extended";
                        foundSize = true;
                    }

                    // SOF2 - Progressive JPEG
                    else if (marker == 0xC2 && dataLength >= 6)
                    {
                        ReadSof(fs, info);

                        info.Compression = "JPEG Progressive";
                        foundSize = true;
                    }

                    fs.Position = segmentStart + dataLength;

                    if (fs.Position > fs.Length)
                    {
                        info.Status = "Файл поврежден";
                        return info;
                    }
                }

                if (!foundSize)
                {
                    info.Status = "Не найден заголовок изображения";
                    return info;
                }

                // Проверяем EOI FF D9 в конце
                fs.Seek(-2, SeekOrigin.End);

                int end1 = fs.ReadByte();
                int end2 = fs.ReadByte();

                if (end1 != 0xFF || end2 != 0xD9)
                {
                    info.Status = "Файл поврежден: нет EOI";
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

        private static void ReadSof(
            FileStream fs,
            ImageInfo info)
        {
            int precision = fs.ReadByte();

            int height = ReadUInt16BE(fs);
            int width = ReadUInt16BE(fs);

            int components = fs.ReadByte();

            info.Width = width;
            info.Height = height;

            info.ColorDepth =
                (precision * components) + " bit";
        }

        private static int ReadUInt16BE(FileStream fs)
        {
            int b1 = fs.ReadByte();
            int b2 = fs.ReadByte();

            if (b1 == -1 || b2 == -1)
                throw new EndOfStreamException();

            return (b1 << 8) | b2;
        }
    }
}
