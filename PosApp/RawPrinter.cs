using System.Runtime.InteropServices;
namespace PosApp;
// Sends raw ESC/POS bytes to a Windows printer. The cash drawer plugs into the receipt printer's RJ11 port.
public static class RawPrinter
{
    public static readonly byte[] KickDrawer = { 0x1B, 0x70, 0x00, 0x19, 0xFA };
    public static readonly byte[] Cut = { 0x1D, 0x56, 0x42, 0x00 };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    class DOCINFO { public string pDocName = "POS Receipt"; public string? pOutputFile = null; public string pDataType = "RAW"; }
    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", CharSet = CharSet.Ansi, SetLastError = true)] static extern bool OpenPrinter(string n, out IntPtr h, IntPtr d);
    [DllImport("winspool.Drv", SetLastError = true)] static extern bool ClosePrinter(IntPtr h);
    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", CharSet = CharSet.Ansi, SetLastError = true)] static extern bool StartDocPrinter(IntPtr h, int l, [In] DOCINFO di);
    [DllImport("winspool.Drv", SetLastError = true)] static extern bool EndDocPrinter(IntPtr h);
    [DllImport("winspool.Drv", SetLastError = true)] static extern bool StartPagePrinter(IntPtr h);
    [DllImport("winspool.Drv", SetLastError = true)] static extern bool EndPagePrinter(IntPtr h);
    [DllImport("winspool.Drv", SetLastError = true)] static extern bool WritePrinter(IntPtr h, IntPtr b, int c, out int w);

    public static bool Send(string printer, byte[] data)
    {
        if (!OpenPrinter(printer, out var h, IntPtr.Zero)) return false;
        var p = Marshal.AllocCoTaskMem(data.Length);
        try
        {
            Marshal.Copy(data, 0, p, data.Length);
            if (!StartDocPrinter(h, 1, new DOCINFO())) return false;
            StartPagePrinter(h);
            var ok = WritePrinter(h, p, data.Length, out _);
            EndPagePrinter(h); EndDocPrinter(h);
            return ok;
        }
        finally { Marshal.FreeCoTaskMem(p); ClosePrinter(h); }
    }
}
