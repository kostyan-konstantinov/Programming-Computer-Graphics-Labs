using System;
using System.IO;
using System.Text;
using lab2.Models;

namespace lab2.Parsers
{
    public static class PngParser
    {
        public static ImageInfo Read(string path)
        {
            ImageInfo info = new ImageInfo
            {
                FileName = Path.GetFileName(path),
                Format = "PNG",
                Dpi = "Не задано",
                ColorDepth = "-",
                Compression = "Deflate",
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

                if (fs.Length < 33)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                byte[] signature = new byte[8];

                fs.ReadExactly(signature, 0, 8);

                byte[] pngSignature =
                {
                    0x89, 0x50, 0x4E, 0x47,
                    0x0D, 0x0A, 0x1A, 0x0A
                };

                for (int i = 0; i < 8; i++)
                {
                    if (signature[i] != pngSignature[i])
                    {
                        info.Status = "Неверная сигнатура";
                        return info;
                    }
                }

                bool foundIHDR = false;
                bool foundIEND = false;

                while (fs.Position < fs.Length)
                {
                    uint length = ReadUInt32BE(fs);

                    byte[] typeBytes = new byte[4];

                    fs.ReadExactly(typeBytes, 0, 4);

                    string chunkType =
                        Encoding.ASCII.GetString(typeBytes);

                    if (length > int.MaxValue)
                    {
                        info.Status = "Файл поврежден";
                        return info;
                    }

                    if (fs.Position + length + 4 > fs.Length)
                    {
                        info.Status = "Файл поврежден";
                        return info;
                    }

                    if (chunkType == "IHDR")
                    {
                        if (length != 13)
                        {
                            info.Status = "Файл поврежден";
                            return info;
                        }

                        byte[] data = new byte[13];

                        fs.ReadExactly(data, 0, 13);

                        info.Width =
                            (data[0] << 24) |
                            (data[1] << 16) |
                            (data[2] << 8) |
                            data[3];

                        info.Height =
                            (data[4] << 24) |
                            (data[5] << 16) |
                            (data[6] << 8) |
                            data[7];

                        int bitDepth = data[8];
                        int colorType = data[9];

                        int compressionMethod = data[10];
                        int filterMethod = data[11];
                        int interlaceMethod = data[12];

                        info.ColorDepth =
                            GetColorDepth(
                                bitDepth,
                                colorType
                            );

                        info.ExtraInfo =
                            "PNG Filter method: " +
                            filterMethod;

                        if (compressionMethod != 0 ||
                            filterMethod != 0)
                        {
                            info.Status = "Файл поврежден";
                            return info;
                        }

                        if (interlaceMethod != 0 &&
                            interlaceMethod != 1)
                        {
                            info.Status = "Файл поврежден";
                            return info;
                        }

                        foundIHDR = true;
                    }
                    else if (
                        chunkType == "pHYs" &&
                        length == 9)
                    {
                        byte[] data = new byte[9];

                        fs.ReadExactly(data, 0, 9);

                        uint pixelsX =
                            ReadUInt32BE(data, 0);

                        uint pixelsY =
                            ReadUInt32BE(data, 4);

                        int unit = data[8];

                        if (unit == 1)
                        {
                            double dpiX =
                                pixelsX * 0.0254;

                            double dpiY =
                                pixelsY * 0.0254;

                            info.Dpi =
                                Math.Round(dpiX) +
                                " x " +
                                Math.Round(dpiY);
                        }
                    }
                    else
                    {
                        fs.Seek(
                            length,
                            SeekOrigin.Current
                        );
                    }

                    // Пропускаем CRC чанка
                    fs.Seek(4, SeekOrigin.Current);

                    if (chunkType == "IEND")
                    {
                        foundIEND = true;
                        break;
                    }
                }

                if (!foundIHDR || !foundIEND)
                {
                    info.Status = "Файл поврежден";
                    return info;
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

        private static string GetColorDepth(
            int bitDepth,
            int colorType)
        {
            int channels;

            switch (colorType)
            {
                case 0:
                    channels = 1;
                    break;

                case 2:
                    channels = 3;
                    break;

                case 3:
                    channels = 1;
                    break;

                case 4:
                    channels = 2;
                    break;

                case 6:
                    channels = 4;
                    break;

                default:
                    return "Неизвестно";
            }

            return
                (bitDepth * channels) +
                " bit";
        }

        private static uint ReadUInt32BE(
            FileStream fs)
        {
            byte[] data = new byte[4];

            fs.ReadExactly(data, 0, 4);

            return ReadUInt32BE(data, 0);
        }

        private static uint ReadUInt32BE(
            byte[] data,
            int offset)
        {
            return
                ((uint)data[offset] << 24) |
                ((uint)data[offset + 1] << 16) |
                ((uint)data[offset + 2] << 8) |
                data[offset + 3];
        }
    }
}