
using System.Runtime.InteropServices;
using System.Xml;
using BajadorProbando.Helpers;
using BajadorProbando.Models;
using System.Security.Cryptography.Pkcs;

namespace BajadorProbando.Services;


public class AfipService
{
    private readonly List<DayToInform> _schedule;
    private readonly string _ipAddress;
    private readonly string _pemPath;

    public AfipService(List<DayToInform> schedule, string ipAddress, string pemPath)
    {
        _schedule = schedule;
        _ipAddress = ipAddress;
        _pemPath = pemPath;
    }
    public void Execute()
    {
        if (!IsDayValidToProcess())
            return;
        (DateTime start, DateTime end) validDate = Utils.CalculateDate(AFIPDates());

        var startDate = validDate.start.ToString("yyMMdd");
        var endDate = validDate.end.ToString("yyMMdd");
        var zipFilename = Utils.GenerateFileName(validDate,"zip");
        var filename = Utils.GenerateFileName(validDate);
        var filepath = $"{_pemPath}\\{filename}";
        var result = Download(startDate, endDate, zipFilename);

        if (result != 0)
        {
            Utils.ExtractZip(zipFilename, filepath);
            ReadPem(filepath, filename);
        }
    }

    public int Download(string startDate, string endDate, string zipFilename)
    {
        var resp = AFIPDownloader.AFIPDownload(_ipAddress, startDate, endDate, zipFilename);

        try
        {
            if (resp == 1)
            {
                Console.WriteLine($"A zip has been generated from {startDate} to {endDate}");
            }
            else
            {
                Console.WriteLine($"Error generating file from {startDate} to {endDate}.\n" + AFIPDownloader.LastErrorMessage());
            }
            return resp;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    bool IsDayValidToProcess()
    {
        var today = DateTime.Now.Day;
        return _schedule.Any(x => x.DayOfTheMonth == today);
    }

    void ReadPem(string filepath, string filename)
    {
        string[] file8011 = Directory.GetFiles(filepath, "F8011*");
        string pemContent = File.ReadAllText(file8011[0]);
        string base64Content = ExtractBase64Content(pemContent);
        byte[] cmsData = Convert.FromBase64String(base64Content);
        var cms = new SignedCms();
        cms.Decode(cmsData);
        var str = System.Text.Encoding.Default.GetString(cms.ContentInfo.Content);
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(str);
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("tns", "http://ar.gob.afip.controladorfiscal/comprobantes_auditoria");

        var puntoDeVentaNode = xmlDoc.SelectNodes("//tns:auditoria/emisor/numeroPuntoVenta", nsmgr);
        var cuitNode = xmlDoc.SelectNodes("//tns:auditoria/emisor/cuitEmisor", nsmgr);

        if (puntoDeVentaNode[0].InnerText != null && cuitNode[0].InnerText != null)
        {
            string[] filesToRename = Directory.GetFiles(filepath);
            foreach (var file in filesToRename)
            {
                Utils.RenameFile(file, filepath, filename, [puntoDeVentaNode[0].InnerText, cuitNode[0].InnerText]);
            }
        }
        else
        {
            string[] filesToRename = Directory.GetFiles(filepath);
            foreach (var file in filesToRename)
            {
                Utils.RenameFile(file, filepath, filename, [""]);
            }
        }
    }

    static string ExtractBase64Content(string pemContent)
    {
        const string startMarker = "-----BEGIN CMS-----";
        const string endMarker = "-----END CMS-----";

        var start = pemContent.IndexOf(startMarker, StringComparison.Ordinal);
        var end = pemContent.IndexOf(endMarker, StringComparison.Ordinal);

        if (start < 0 || end < 0)
            throw new InvalidOperationException("PEM file does not contain valid CMS content.");
        

        start += startMarker.Length;
        end -= start;

        var base64Content = pemContent.Substring(start, end).Replace("\r", "").Replace("\n", "");

        return base64Content;
    }


    List<(DateTime start, DateTime end)> AFIPDates()
    {
        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;
        var possiblePreviousYear = DateTime.Now.Month == 0;
        List<(DateTime start, DateTime end)> dateRanges = new();

        foreach (var date in _schedule.Select((day, index) => (day, index)))
        {
            var dateTuple = new ValueTuple<DateTime, DateTime>();
            if (date.index == 0)
            {
                dateTuple = new ValueTuple<DateTime, DateTime>(
                    new DateTime(possiblePreviousYear ? currentYear - 1 : currentYear,
                        possiblePreviousYear ? 12 : currentMonth - 1, date.day.AfipDates.StartDate),
                    new DateTime(currentYear, currentMonth, date.day.AfipDates.EndDate));
                dateRanges.Add(dateTuple);
                continue;
            }
            dateTuple = new ValueTuple<DateTime, DateTime>(
                new DateTime(currentYear, DateTime.Now.Month, date.day.AfipDates.StartDate),
                new DateTime(currentYear, currentMonth, date.day.AfipDates.EndDate));
            dateRanges.Add(dateTuple);
        }

        return dateRanges;
    }
}
