

namespace BajadorProbando.Helpers;
public class Utils
{

    public static void ExtractZip(string zipName, string filepath)
    {
        System.IO.Compression.ZipFile.ExtractToDirectory(zipName, filepath, true);
    }

    public static string GenerateFileName((DateTime startDate, DateTime endDate) dateRange, string suffix = "")
    {
        var startDate = dateRange.startDate.ToString("yyMMdd");
        var endDate = dateRange.endDate.ToString("yyMMdd");
        return $"{startDate}-{endDate}{(!string.IsNullOrEmpty(suffix) ? $".{suffix}" : "")}";
    }

    public static (DateTime start, DateTime end) CalculateDate(List<(DateTime startDate, DateTime endDate)> dateRanges)
    {
        (DateTime start, DateTime end) validDate = new();
        var today = DateTime.Now;

        foreach (var date in dateRanges.Select((range, index) => (range, index)))
        {

            if (today >= date.range.startDate.Date && today <= date.range.endDate.Date)
            {
                (DateTime start, DateTime end) previousRange;
                if (date.index == 0)
                    previousRange = dateRanges.Last();
                else previousRange = dateRanges[date.index];

                validDate.start = previousRange.start;
                validDate.end = previousRange.end;
                break;
            }
        }

        return validDate;
    }

    public static void RenameFile(string file, string filepath, string folderName, List<string> parametersToRename )
    {
        var oldFileName = Path.GetFileName(file);
        
        var oldFilePath = $"{filepath}/{oldFileName}";
        var prependText = string.Join("-", parametersToRename);
        string newFilePath;
        string directoryPath = Path.GetDirectoryName(filepath);
        if (oldFileName.StartsWith("F8010"))
        {
            newFilePath = Path.Combine(directoryPath, "8010", folderName, oldFileName);
            Directory.CreateDirectory(Path.Combine(directoryPath, "8010", folderName));
        }
        else
        {
            string newFileName = prependText + oldFileName;
            newFilePath = Path.Combine(directoryPath, folderName, newFileName);
        }
        File.Move(oldFilePath, newFilePath);
    }
}
