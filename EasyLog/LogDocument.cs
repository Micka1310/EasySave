using System.Xml.Serialization;

namespace EasyLog;

/// <summary>Racine XML pour une liste d'entrées de log.</summary>
[XmlRoot("Logs")]
public class LogDocument
{
    [XmlElement("Entry")]
    public List<LogEntry> Entries { get; set; } = [];
}
