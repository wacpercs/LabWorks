using System.IO;
using LabWorksApp.Services;

namespace LabWorksApp.Tests;

public class ServiceTests
{
    [Fact]
    public void ArrayGenerator_GeneratesCorrectCountAndRange()
    {
        var generator = new ArrayGeneratorService();
        int a = -20;
        int b = 50;
        int count = 25;

        var arr = generator.Generate(a, b, count);

        Assert.Equal(count, arr.Length);
        Assert.All(arr, val => Assert.True(val >= a && val <= b));
    }

    [Fact]
    public void ArrayGenerator_ThrowsOnOutOfRangeCount()
    {
        var generator = new ArrayGeneratorService();
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(0, 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(0, 10, 51));
    }

    [Fact]
    public void ArrayGenerator_HandlesInvertedRange()
    {
        var generator = new ArrayGeneratorService();
        var arr = generator.Generate(100, -50, 10);
        Assert.Equal(10, arr.Length);
        Assert.All(arr, val => Assert.True(val >= -50 && val <= 100));
    }

    [Fact]
    public void DataImport_ParsesDelimitedText_Correctly()
    {
        var service = new DataImportService();
        string input = "10, -5; 33 0\n44\r\n-100\t12";
        var result = service.ImportFromDelimitedText(input);

        Assert.Equal(new[] { 10, -5, 33, 0, 44, -100, 12 }, result);
    }

    [Fact]
    public void DataImport_EnforcesMaxArraySize()
    {
        var service = new DataImportService();
        var largeText = string.Join(" ", Enumerable.Range(1, 100));
        var result = service.ImportFromDelimitedText(largeText);

        Assert.Equal(ArrayGeneratorService.MaxArraySize, result.Length);
    }

    [Fact]
    public void DataImport_ImportsFromExcel_Correctly()
    {
        var service = new DataImportService();
        string tempXlsx = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.xlsx");

        try
        {
            using (var zip = System.IO.Compression.ZipFile.Open(tempXlsx, System.IO.Compression.ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("xl/worksheets/sheet1.xml");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <sheetData>
    <row r=""1"">
      <c r=""A1""><v>45</v></c>
      <c r=""B1""><v>-12</v></c>
      <c r=""C1""><v>78</v></c>
    </row>
  </sheetData>
</worksheet>");
            }

            var result = service.ImportFromExcel(tempXlsx);
            Assert.Equal(new[] { 45, -12, 78 }, result);
        }
        finally
        {
            if (File.Exists(tempXlsx)) File.Delete(tempXlsx);
        }
    }
}
