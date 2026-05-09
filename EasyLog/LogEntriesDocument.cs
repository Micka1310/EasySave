using System.Xml.Serialization;

namespace EasyLog;

/// <summary>Racine XML pour une liste d'entrées de log (un fichier par jour).</summary>
[XmlRoot("EasySaveLogs")]
public class LogEntriesDocument
{
    [XmlElement("Entry")]
    public List<LogEntry> Entries { get; set; } = [];
}
