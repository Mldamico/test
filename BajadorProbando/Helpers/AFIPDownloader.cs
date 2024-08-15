using System.Runtime.InteropServices;


namespace BajadorProbando.Helpers;
public static  class AFIPDownloader
{
    [DllImport("downloader.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int AFIPDownload(string ip, string DateFrom, string DateTo, string Filename);

    [DllImport("downloader.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr LastErrorMessage();
}
