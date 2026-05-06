using System.Text.Json;

namespace StateFileLib;

// Représente l'état courant d'un travail de sauvegarde
public class WorkState
{
    public string WorkName { get; set; } = "";
    public string Timestamp { get; set; } = ""; // Date et heure de l'état
    public string Status { get; set; } = "Inactive"; // "Active" ou "Inactive"
    public int TotalFiles { get; set; } // Nombre total de fichiers à traiter
    public long TotalSize { get; set; } // Taille totale des fichiers à traiter en octets
    public int RemainingFiles { get; set; } // Nombre de fichiers restants à traiter
    public long RemainingSize { get; set; } // Taille restante en octets
    public int Progression { get; set; } // Pourcentage 0-100
    public string CurrentSourceFile { get; set; } = ""; // Chemin complet du fichier source en cours
    public string CurrentDestinationFile { get; set; } = ""; // Chemin complet du fichier de destination en cours
}

// Gère l'écriture de l'état en temps réel de tous les travaux dans state.json
public class StateFile
{
    // state.json est stocké à côté de l'exécutable
    private string filePath;

    // Verrou pour éviter les accès simultanés au fichier (tests parallèles ou multi-thread)
    private static readonly object fileLock = new object();

    public StateFile()
    {
        filePath = Path.Combine(AppContext.BaseDirectory, "state.json");
    }

    // Met à jour l'état d'un travail spécifique dans state.json
    public void WriteProcess(WorkState workState)
    {
        // Le lock garantit qu'un seul thread écrit à la fois
        lock (fileLock)
        {
            // Lire les états existants si le fichier existe déjà
            List<WorkState> states = new List<WorkState>();

            if (File.Exists(filePath))
            {
                string existingContent = File.ReadAllText(filePath);
                states = JsonSerializer.Deserialize<List<WorkState>>(existingContent) ?? new List<WorkState>();
            }

            // Mettre à jour le travail s'il existe déjà, sinon l'ajouter
            int index = states.FindIndex(s => s.WorkName == workState.WorkName);

            if (index >= 0)
            {
                states[index] = workState;
            }
            else
            {
                states.Add(workState);
            }

            // Réécrire tous les états avec indentation pour la lisibilité
            string json = JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }

    /// <summary>
    /// Lit tous les états enregistrés (pour affichage console).
    /// </summary>
    public List<WorkState> ReadAllStates()
    {
        lock (fileLock)
        {
            if (!File.Exists(filePath))
            {
                return [];
            }

            string existingContent = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<WorkState>>(existingContent) ?? [];
        }
    }
}
