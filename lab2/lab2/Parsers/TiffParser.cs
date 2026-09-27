using System;
using System.IO;
using lab2.Models;

namespace lab2.Parsers
{
    public static class TiffParser
    {
        public static ImageInfo Read(string path)
        {
            ImageInfo info = new ImageInfo
            {
                FileName = Path.GetFileName(path),
                Format = "TIFF",
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

                if (fs.Length < 8)
                {
                    info.Status = "Файл поврежден";
                    return info;
                }

                int b1 = fs.ReadByte();
                int b2 = fs.ReadByte();

                bool littleEndian;

                if (b1 == 0x49 && b2 == 0x49)
                {
                    littleEndian = true;
                }
                else if (b1 == 0x4D && b2 == 0x4D)
                {
                    littleEndian = false;
                }
                else
                {
                    info.Status = "Неверная сигнатура";
                    return info;
                }

                ushort magic =
                    ReadUInt16(fs, littleEndian);

                if (magic != 42)
                {
                    info.Status = "Некорректный TIFF";
                    return info;
                }

                uint ifdOffset =
                    ReadUInt32(fs, littleEndian);

                if (ifdOffset >= fs.Length)
                {
                    info.Status = "Поврежденный IFD";
                    return info;
                }

                fs.Position = ifdOffset;

                ushort entryCount =
                    ReadUInt16(fs, littleEndian);

                double? xResolution = null;
                double? yResolution = null;

                ushort resolutionUnit = 2;

                int bitsPerSample = 0;
                int samplesPerPixel = 1;

                for (int i = 0; i < entryCount; i++)
                {
                    long entryPosition = fs.Position;

                    ushort tag =
                        ReadUInt16(fs, littleEndian);

                    ushort type =
                        ReadUInt16(fs, littleEndian);

                    uint count =
                        ReadUInt32(fs, littleEndian);

                    uint valueOffset =
                        ReadUInt32(fs, littleEndian);

                    switch (tag)
                    {
                        // ImageWidth
                        case 256:
                            info.Width =
                                (int)GetSimpleValue(
                                    type,
                                    valueOffset,
                                    littleEndian
                                );
                            break;

                        // ImageLength
                        case 257:
                            info.Height =
                                (int)GetSimpleValue(
                                    type,
                                    valueOffset,
                                    littleEndian
                                );
                            break;

                        // BitsPerSample
                        case 258:
                            if (count == 1)
                            {
                                bitsPerSample =
                                    (int)GetSimpleValue(
                                        type,
                                        valueOffset,
                                        littleEndian
                                    );
                            }
                            else
                            {
                                long oldPos = fs.Position;

                                fs.Position = valueOffset;

                                bitsPerSample =
                                    ReadUInt16(
                                        fs,
                                        littleEndian
                                    );

                                fs.Position = oldPos;
                            }
                            break;

                        // Compression
                        case 259:
                            int compression =
                                (int)GetSimpleValue(
                                    type,
                                    valueOffset,
                                    littleEndian
                                );

                            info.Compression =
                                GetCompressionName(compression);
                            break;

                        // SamplesPerPixel
                        case 277:
                            samplesPerPixel =
                                (int)GetSimpleValue(
                                    type,
                                    valueOffset,
                                    littleEndian
                                );
                            break;

                        // XResolution
                        case 282:
                            xResolution =
                                ReadRational(
                                    fs,
                                    valueOffset,
                                    littleEndian
                                );
                            break;

                        // YResolution
                        case 283:
                            yResolution =
                                ReadRational(
                                    fs,
                                    valueOffset,
                                    littleEndian
                                );
                            break;

                        // ResolutionUnit
                        case 296:
                            resolutionUnit =
                                (ushort)GetSimpleValue(
                                    type,
                                    valueOffset,
                                    littleEndian
                                );
                            break;
                    }

                    fs.Position =
                        entryPosition + 12;
                }

                if (bitsPerSample > 0)
                {
                    info.ColorDepth =
                        (bitsPerSample * samplesPerPixel)
                        + " bit";
                }

                if (xResolution.HasValue &&
                    yResolution.HasValue)
                {
                    double dpiX = xResolution.Value;
                    double dpiY = yResolution.Value;

                    // TIFF unit 3 = сантиметры
                    if (resolutionUnit == 3)
                    {
                        dpiX *= 2.54;
                        dpiY *= 2.54;
                    }

                    info.Dpi =
                        Math.Round(dpiX) + " x " +
                        Math.Round(dpiY);
                }

                if (info.Width <= 0 ||
                    info.Height <= 0)
                {
                    info.Status =
                        "Не удалось определить размер";
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
                info.Status = "Неизвестная ошибка";
            }

            return info;
        }

        private static ushort ReadUInt16(
            FileStream fs,
            bool littleEndian)
        {
            int b1 = fs.ReadByte();
            int b2 = fs.ReadByte();

            if (b1 < 0 || b2 < 0)
                throw new EndOfStreamException();

            if (littleEndian)
                return (ushort)(b1 | (b2 << 8));

            return (ushort)((b1 << 8) | b2);
        }

        private static uint ReadUInt32(
            FileStream fs,
            bool littleEndian)
        {
            byte[] data = new byte[4];

            fs.ReadExactly(data, 0, 4);

            if (littleEndian)
            {
                return
                    (uint)data[0] |
                    ((uint)data[1] << 8) |
                    ((uint)data[2] << 16) |
                    ((uint)data[3] << 24);
            }

            return
                ((uint)data[0] << 24) |
                ((uint)data[1] << 16) |
                ((uint)data[2] << 8) |
                data[3];
        }

        private static uint GetSimpleValue(
            ushort type,
            uint value,
            bool littleEndian)
        {
            if (type == 3) // SHORT
            {
                if (littleEndian)
                    return value & 0xFFFF;

                return value >> 16;
            }

            return value;
        }

        private static double ReadRational(
            FileStream fs,
            uint offset,
            bool littleEndian)
        {
            long oldPosition = fs.Position;

            fs.Position = offset;

            uint numerator =
                ReadUInt32(fs, littleEndian);

            uint denominator =
                ReadUInt32(fs, littleEndian);

            fs.Position = oldPosition;

            if (denominator == 0)
                return 0;

            return (double)numerator / denominator;
        }

        private static string GetCompressionName(
            int compression)
        {
            switch (compression)
            {
                case 1:
                    return "Без сжатия";

                case 5:
                    return "LZW";

                case 6:
                    return "JPEG";

                case 7:
                    return "JPEG";

                case 8:
                    return "Deflate";

                case 32773:
                    return "PackBits";

                default:
                    return "Неизвестно (" +
                           compression + ")";
            }
        }
    }
}