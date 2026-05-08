using System.Globalization;

namespace CryptoSoft;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 2 || !string.Equals(args[0], "--encrypt", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Usage: CryptoSoft --encrypt <filePath>");
            return 2;
        }

        string targetFile = args[1];
        if (!File.Exists(targetFile))
        {
            Console.Error.WriteLine($"File not found: {targetFile}");
            return 3;
        }

        try
        {
            long elapsedMs = CryptoEngine.EncryptFileInPlace(targetFile);
            Console.WriteLine(elapsedMs.ToString(CultureInfo.InvariantCulture));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
