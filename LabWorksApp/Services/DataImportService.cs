using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LabWorksApp.Services;

public class DataImportService
{
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Парсит массив целых чисел из файла (.xlsx, .csv, .txt).
    /// </summary>
    public int[] ImportFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл '{filePath}' не найден.");

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".xlsx" => ImportFromExcel(filePath),
            ".csv" or ".txt" => ImportFromDelimitedText(File.ReadAllText(filePath)),
            _ => throw new NotSupportedException($"Формат файла '{ext}' не поддерживается. Используйте .xlsx, .csv или .txt.")
        };
    }

    /// <summary>
    /// Парсит данные напрямую из файла Excel (.xlsx) без внешних зависимостей через OpenXML.
    /// </summary>
    public int[] ImportFromExcel(string xlsxPath)
    {
        var numbers = new List<int>();

        using (var zip = ZipFile.OpenRead(xlsxPath))
        {
            // Проверим наличие sharedStrings.xml
            var sharedStrings = new List<string>();
            var sstEntry = zip.GetEntry("xl/sharedStrings.xml");
            if (sstEntry != null)
            {
                using var stream = sstEntry.Open();
                var sstDoc = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                foreach (var si in sstDoc.Descendants(ns + "si"))
                {
                    sharedStrings.Add(si.Value);
                }
            }

            // Читаем первый лист sheet1.xml
            var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml")
                ?? zip.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet") && e.FullName.EndsWith(".xml"));

            if (sheetEntry == null)
                throw new InvalidOperationException("В файле Excel не найден рабочий лист.");

            using (var stream = sheetEntry.Open())
            {
                var doc = XDocument.Load(stream);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                foreach (var c in doc.Descendants(ns + "c"))
                {
                    string? type = c.Attribute("t")?.Value;
                    string? val = c.Element(ns + "v")?.Value;

                    if (string.IsNullOrWhiteSpace(val)) continue;

                    string cellText = val;
                    if (type == "s" && int.TryParse(val, out int sstIdx) && sstIdx >= 0 && sstIdx < sharedStrings.Count)
                    {
                        cellText = sharedStrings[sstIdx];
                    }

                    if (TryParseInt(cellText, out int parsedNum))
                    {
                        numbers.Add(parsedNum);
                        if (numbers.Count >= ArrayGeneratorService.MaxArraySize) break;
                    }
                }
            }
        }

        if (numbers.Count == 0)
            throw new InvalidOperationException("В файле Excel не найдено числовых значений для формирования массива.");

        return numbers.ToArray();
    }

    /// <summary>
    /// Загружает данные из Google Таблицы по URL публичного доступа.
    /// </summary>
    public async Task<int[]> ImportFromGoogleSheetsAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Ссылка на Google Таблицу не может быть пустой.");

        string exportUrl = url.Trim();

        // Если передана стандартная ссылка вида https://docs.google.com/spreadsheets/d/{KEY}/edit...
        var match = Regex.Match(exportUrl, @"/spreadsheets/d/([a-zA-Z0-9-_]+)");
        if (match.Success)
        {
            string sheetId = match.Groups[1].Value;
            exportUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv";
        }
        else if (!exportUrl.Contains("format=csv", StringComparison.OrdinalIgnoreCase))
        {
            exportUrl = exportUrl.Contains('?') ? $"{exportUrl}&format=csv" : $"{exportUrl}?format=csv";
        }

        try
        {
            using var response = await HttpClient.GetAsync(exportUrl);
            response.EnsureSuccessStatusCode();
            string csvContent = await response.Content.ReadAsStringAsync();

            int[] numbers = ImportFromDelimitedText(csvContent);
            if (numbers.Length == 0)
                throw new InvalidOperationException("В загруженной Google Таблице не найдено числовых данных.");

            return numbers;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Ошибка загрузки Google Таблицы: {ex.Message}. Убедитесь, что таблица доступна по ссылке (доступ открыт для всех, у кого есть ссылка).", ex);
        }
    }

    /// <summary>
    /// Парсит числа из разделительного текста (CSV, TSV, пробелы, запятые, точки с запятой, переносы строк).
    /// </summary>
    public int[] ImportFromDelimitedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<int>();

        var tokens = text.Split(new[] { ',', ';', '\t', '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<int>();

        foreach (var token in tokens)
        {
            if (TryParseInt(token, out int val))
            {
                result.Add(val);
                if (result.Count >= ArrayGeneratorService.MaxArraySize)
                    break;
            }
        }

        return result.ToArray();
    }

    private static bool TryParseInt(string? s, out int val)
    {
        val = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;

        s = s.Trim().Trim('"', '\'');
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
            return true;

        if (double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double dVal))
        {
            val = (int)Math.Round(dVal);
            return true;
        }

        return false;
    }
}
