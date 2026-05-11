using System.Diagnostics;

namespace CryptoSoft;

/// <summary>
/// Moteur de chiffrement simple pour la v2.
/// Chiffre un fichier en place via XOR (démonstration pédagogique).
/// </summary>
public static class CryptoEngine
{
    private const byte XorKey = 0x5A;

    public static long EncryptFileInPlace(string filePath)
    {
        Stopwatch watch = Stopwatch.StartNew();

        byte[] data = File.ReadAllBytes(filePath);
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= XorKey;
        }

        File.WriteAllBytes(filePath, data);
        watch.Stop();
        return watch.ElapsedMilliseconds;
    }
}
