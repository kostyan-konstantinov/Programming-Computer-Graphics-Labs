using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using lab2.Models;

namespace lab2.Parsers
{
    public static class JpegParser
    {
        // Порядок коэффициентов в JPEG-файле.
        private static readonly int[] ZigZag =
        {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63
        };

        // Стандартная таблица яркости JPEG примерно для качества 50.
        // Используется только для оценки качества.
        private static readonly int[] StandardLuminanceTable =
        {
            16, 11, 10, 16, 24, 40, 51, 61,
            12, 12, 14, 19, 26, 58, 60, 55,
            14, 13, 16, 24, 40, 57, 69, 56,
            14, 17, 22, 29, 51, 87, 80, 62,
            18, 22, 37, 56, 68,109,103, 77,
            24, 35, 55, 64, 81,104,113, 92,
            49, 64, 78, 87,103,121,120,101,
            72, 92, 95, 98,112,100,103, 99
        };

        public static ImageInfo Read(string path)
        {
            ImageInfo info = new ImageInfo
            {
                FileName = Path.GetFileName(path),
                Format = "JPEG",

                // Если настоящий DPI не найден,
                // оставляем значение по умолчанию.
                Dpi = "96 x 96 (по умолчанию)",

                ColorDepth = "-",
                Compression = "JPEG",
                Status = "OK",
                ExtraInfo = ""
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

                // JPEG должен начинаться с FF D8.
                if (fs.ReadByte() != 0xFF ||
                    fs.ReadByte() != 0xD8)
                {
                    info.Status = "Неверная сигнатура";
                    return info;
                }

                bool foundSize = false;
                bool foundDqt = false;

                StringBuilder dqtInfo =
                    new StringBuilder();

                double? estimatedQuality = null;

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

                    // SOS - дальше идут уже сжатые данные пикселей.
                    if (marker == 0xDA)
                        break;

                    // Restart-маркеры.
                    if (marker >= 0xD0 &&
                        marker <= 0xD7)
                    {
                        continue;
                    }

                    if (marker == 0x01)
                        continue;

                    int length =
                        ReadUInt16BE(fs);

                    if (length < 2)
                    {
                        info.Status =
                            "Файл поврежден";

                        return info;
                    }

                    int dataLength =
                        length - 2;

                    long segmentStart =
                        fs.Position;

                    if (segmentStart +
                        dataLength >
                        fs.Length)
                    {
                        info.Status =
                            "Файл поврежден";

                        return info;
                    }

                    // --------------------------------
                    // APP0 / JFIF - DPI
                    // --------------------------------
                    if (marker == 0xE0 &&
                        dataLength >= 14)
                    {
                        byte[] app0 =
                            new byte[dataLength];

                        fs.ReadExactly(
                            app0,
                            0,
                            dataLength
                        );

                        if (
                            app0[0] == (byte)'J' &&
                            app0[1] == (byte)'F' &&
                            app0[2] == (byte)'I' &&
                            app0[3] == (byte)'F' &&
                            app0[4] == 0
                        )
                        {
                            int units =
                                app0[7];

                            int densityX =
                                (app0[8] << 8) |
                                app0[9];

                            int densityY =
                                (app0[10] << 8) |
                                app0[11];

                            // 1 = DPI уже задан напрямую.
                            if (
                                units == 1 &&
                                densityX > 0 &&
                                densityY > 0
                            )
                            {
                                info.Dpi =
                                    densityX +
                                    " x " +
                                    densityY;
                            }

                            // 2 = dots/cm.
                            else if (
                                units == 2 &&
                                densityX > 0 &&
                                densityY > 0
                            )
                            {
                                double dpiX =
                                    densityX * 2.54;

                                double dpiY =
                                    densityY * 2.54;

                                info.Dpi =
                                    Math.Round(dpiX) +
                                    " x " +
                                    Math.Round(dpiY);
                            }
                        }
                    }

                    // --------------------------------
                    // DQT = FF DB
                    // --------------------------------
                    else if (marker == 0xDB)
                    {
                        byte[] dqt =
                            new byte[dataLength];

                        fs.ReadExactly(
                            dqt,
                            0,
                            dataLength
                        );

                        ParseDqt(
                            dqt,
                            dqtInfo,
                            ref estimatedQuality
                        );

                        foundDqt = true;
                    }

                    // --------------------------------
                    // SOF0
                    // --------------------------------
                    else if (
                        marker == 0xC0 &&
                        dataLength >= 6
                    )
                    {
                        ReadSof(fs, info);

                        info.Compression =
                            "JPEG Baseline";

                        foundSize = true;
                    }

                    // SOF1
                    else if (
                        marker == 0xC1 &&
                        dataLength >= 6
                    )
                    {
                        ReadSof(fs, info);

                        info.Compression =
                            "JPEG Extended";

                        foundSize = true;
                    }

                    // SOF2
                    else if (
                        marker == 0xC2 &&
                        dataLength >= 6
                    )
                    {
                        ReadSof(fs, info);

                        info.Compression =
                            "JPEG Progressive";

                        foundSize = true;
                    }

                    // Переходим в конец сегмента.
                    fs.Position =
                        segmentStart +
                        dataLength;
                }

                if (!foundSize)
                {
                    info.Status =
                        "Не найден заголовок изображения";

                    return info;
                }

                // --------------------------------
                // Формируем ExtraInfo
                // --------------------------------

                StringBuilder extra =
                    new StringBuilder();

                if (
                    foundDqt &&
                    estimatedQuality.HasValue
                )
                {
                    double quality =
                        estimatedQuality.Value;

                    double loss =
                        100.0 - quality;

                    extra.AppendLine(
                        "Оценка качества JPEG: " +
                        Math.Round(quality) +
                        "%"
                    );

                    extra.AppendLine(
                        "Условные потери: " +
                        Math.Round(loss) +
                        "%"
                    );

                    extra.AppendLine();
                }

                if (foundDqt)
                {
                    extra.Append(dqtInfo);
                }
                else
                {
                    extra.AppendLine(
                        "DQT не найдена"
                    );
                }

                info.ExtraInfo =
                    extra.ToString().Trim();

                // Проверяем конец JPEG.
                fs.Seek(
                    -2,
                    SeekOrigin.End
                );

                int end1 =
                    fs.ReadByte();

                int end2 =
                    fs.ReadByte();

                if (
                    end1 != 0xFF ||
                    end2 != 0xD9
                )
                {
                    info.Status =
                        "Файл поврежден: нет EOI";

                    return info;
                }

                info.Status = "OK";
            }
            catch (EndOfStreamException)
            {
                info.Status =
                    "Файл поврежден";
            }
            catch (UnauthorizedAccessException)
            {
                info.Status =
                    "Нет доступа";
            }
            catch (IOException)
            {
                info.Status =
                    "Ошибка чтения";
            }
            catch
            {
                info.Status =
                    "Неизвестная ошибка";
            }

            return info;
        }

        // ----------------------------------------
        // Чтение DQT
        // ----------------------------------------
        private static void ParseDqt(
            byte[] data,
            StringBuilder output,
            ref double? estimatedQuality)
        {
            int pos = 0;

            while (pos < data.Length)
            {
                int tableInfo =
                    data[pos++];

                // Старшие 4 бита:
                // 0 = коэффициенты 8 bit,
                // 1 = коэффициенты 16 bit.
                int precision =
                    tableInfo >> 4;

                // Младшие 4 бита:
                // номер таблицы.
                int tableId =
                    tableInfo & 0x0F;

                if (
                    precision != 0 &&
                    precision != 1
                )
                {
                    return;
                }

                int bytesPerValue =
                    precision == 0
                    ? 1
                    : 2;

                int needed =
                    64 * bytesPerValue;

                if (
                    pos + needed >
                    data.Length
                )
                {
                    return;
                }

                int[] matrix =
                    new int[64];

                for (int i = 0; i < 64; i++)
                {
                    int value;

                    if (precision == 0)
                    {
                        value =
                            data[pos++];
                    }
                    else
                    {
                        value =
                            (data[pos] << 8) |
                            data[pos + 1];

                        pos += 2;
                    }

                    // JPEG хранит значения
                    // в zig-zag порядке.
                    // Возвращаем обычную матрицу.
                    matrix[ZigZag[i]] =
                        value;
                }

                if (tableId == 0)
                {
                    output.AppendLine(
                        "DQT 0 - яркость:"
                    );
                }
                else if (tableId == 1)
                {
                    output.AppendLine(
                        "DQT 1 - цветность:"
                    );
                }
                else
                {
                    output.AppendLine(
                        "DQT " +
                        tableId +
                        ":"
                    );
                }

                // Вывод 8x8.
                for (
                    int row = 0;
                    row < 8;
                    row++
                )
                {
                    for (
                        int col = 0;
                        col < 8;
                        col++
                    )
                    {
                        output.Append(
                            matrix[
                                row * 8 + col
                            ]
                            .ToString()
                            .PadLeft(4)
                        );
                    }

                    output.AppendLine();
                }

                output.AppendLine();

                // Основная таблица яркости
                // используется для оценки качества.
                if (
                    tableId == 0 &&
                    precision == 0 &&
                    !estimatedQuality.HasValue
                )
                {
                    estimatedQuality =
                        EstimateQuality(
                            matrix
                        );
                }
            }
        }

        // ----------------------------------------
        // Приблизительная оценка quality
        // ----------------------------------------
        private static double EstimateQuality(
            int[] matrix)
        {
            List<double> ratios =
                new List<double>();

            for (int i = 0; i < 64; i++)
            {
                double ratio =
                    matrix[i] *
                    100.0 /
                    StandardLuminanceTable[i];

                ratios.Add(ratio);
            }

            ratios.Sort();

            // Берём медиану,
            // чтобы отдельные коэффициенты
            // меньше влияли на результат.
            double scale =
                ratios[
                    ratios.Count / 2
                ];

            double quality;

            if (scale <= 100)
            {
                quality =
                    (200 - scale) / 2;
            }
            else
            {
                quality =
                    5000 / scale;
            }

            return Math.Clamp(
                quality,
                1,
                100
            );
        }

        // ----------------------------------------
        // SOF
        // ----------------------------------------
        private static void ReadSof(
            FileStream fs,
            ImageInfo info)
        {
            int precision =
                fs.ReadByte();

            int height =
                ReadUInt16BE(fs);

            int width =
                ReadUInt16BE(fs);

            int components =
                fs.ReadByte();

            info.Width = width;
            info.Height = height;

            info.ColorDepth =
                (precision * components) +
                " bit";
        }

        private static int ReadUInt16BE(
            FileStream fs)
        {
            int b1 =
                fs.ReadByte();

            int b2 =
                fs.ReadByte();

            if (
                b1 == -1 ||
                b2 == -1
            )
            {
                throw new
                    EndOfStreamException();
            }

            return
                (b1 << 8) |
                b2;
        }
    }
}