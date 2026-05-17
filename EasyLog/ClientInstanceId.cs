namespace EasyLog;

internal static class ClientInstanceId
{
    private static string? _cached;

    public static string Get()
    {
        if (!string.IsNullOrEmpty(_cached))
            return _cached;

        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "client-instance.id");

        try
        {
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path).Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    _cached = text;
                    return _cached;
                }
            }
        }
        catch { }

        _cached = Guid.NewGuid().ToString("N");
        try { File.WriteAllText(path, _cached); } catch { }
        return _cached;
    }
}
