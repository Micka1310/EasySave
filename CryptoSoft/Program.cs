using System.Globalization;
using System.Threading;

namespace CryptoSoft;

public static class Program
{
    /// <summary>Nom du mutex système pour l’instance unique (tests : voir UnitTest).</summary>
    internal const string SingleInstanceMutexName = "Global\\CryptoSoft_SingleInstance_Mutex";

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

        using var mutex = new Mutex(false, SingleInstanceMutexName);

        bool acquired;
        try
        {
            acquired = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            acquired = true;
        }

        if (!acquired)
        {
            Console.Error.WriteLine("Another instance of CryptoSoft is already running.");
            return 4;
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
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
