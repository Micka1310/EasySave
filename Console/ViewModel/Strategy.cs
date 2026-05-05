namespace ConsoleStrategyFile;

using LanguageFile;
using LogFileLib;
using StateFileLib;
using System.Text.RegularExpressions;
using WorkFile;
using WorkListFile;

// Interface for creating strategies
public interface IStrategy
{
    // Attributes
    string option { get; }
    List<string> parameterMessage { get; }

    // Methods
    public string Execution(List<string> parameters, WorkList workList);
}

// Option 1 : display all the works
public class DisplayWork1 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_display");
    public List<string> parameterMessage => [];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();
        string displayString = "";
        int index = 0;

        List<Work> WorkList = workList.GetWork();

        foreach (Work elem in WorkList)
        {
            index = index + 1;
            displayString = displayString
                + lang.GetString("display_work_title") + $"{index} :\n"
                + lang.GetString("display_file_name") + elem.GetName() + "\n"
                + lang.GetString("display_source") + elem.GetSourceDirectory() + "\n"
                + lang.GetString("display_destination") + elem.GetDestinationDirectory() + "\n"
                + lang.GetString("display_type") + elem.GetWorkType() + "\n\n ";
        }

        return displayString;
    }
}

// Option 2 : créer un nouveau travail de sauvegarde
public class CreateWork2 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_create");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("create_name"),
        Language.GetInstance().GetString("create_source"),
        Language.GetInstance().GetString("create_destination"),
        Language.GetInstance().GetString("create_type")
    ];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        List<Work> workList1 = workList.GetWork();

        // If there are already 5 works
        if (workList1.Count() == 5)
        {
            return Language.GetInstance().GetString("5_works");
        }

        // To avoid duplicate work name
        foreach (Work work in workList1)
        {
            if (parameters[0].Contains(work.GetName()))
            {
                return Language.GetInstance().GetString("same_work_name");
            }
        }

        if (int.TryParse(parameters[3], out int result1))
        {
            int transferType = int.Parse(parameters[3]);

            // If the transfer type is wrong
            if (transferType < 0 || transferType > 2)
            {
                return Language.GetInstance().GetString("wrong_type_transfer");
            }
        }

        // Ajouter le travail à la liste
        workList.AddWork(parameters);

        // Enregistrer l'état du travail créé dans state.json
        StateFile stateFile = new StateFile();
        stateFile.WriteProcess(new WorkState
        {
            WorkName = parameters[0],
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = "Inactive",
            TotalFiles = 0,
            TotalSize = 0,
            RemainingFiles = 0,
            RemainingSize = 0,
            Progression = 0,
            CurrentSourceFile = parameters[1],
            CurrentDestinationFile = parameters[2]
        });

        // Enregistrer l'action dans le fichier log journalier
        LogFile logFile = new LogFile();
        logFile.WriteLogs(parameters[0], parameters[1], parameters[2], 0, 0);

        return Language.GetInstance().GetString("work_saved");
    }
}

// Option 3 : exécuter un ou plusieurs travaux de sauvegarde
public class ExecuteWork3 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_execute");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("execute_input")
    ];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        // If the input contain at least a number. The symbol "-" or ";" are optional
        if (!Regex.IsMatch(parameters[0], @"^(?=.*\d)[\d;-]+$"))
        {
            return Language.GetInstance().GetString("invalid_input");
        }

        // Récupérer les index des travaux à exécuter depuis le paramètre (ex: "1-3", "1;3", "2")
        List<int> indexes = ParseIndexes(parameters[0], workList.GetWork().Count);

        // If the input have a number not associated to any work
        foreach (int numWork in indexes)
        {
            if (numWork == -1)
            {
                return Language.GetInstance().GetString("wrong_num_work");
            }

            if (numWork == -2)
            {
                return Language.GetInstance().GetString("invalid_input");
            }
        }

        // Indicateur global : false si au moins une erreur s'est produite
        bool success = true;

        foreach (int index in indexes)
        {
            // Récupérer le travail correspondant
            Work work = workList.GetWork()[index];

            // Exécuter selon le type de sauvegarde
            if (work.GetWorkType() == "1")
            {
                // Sauvegarde complète : copier tous les fichiers
                bool result = ExecuteFullBackup(work);
                if (!result) success = false;
            }
            else
            {
                // Sauvegarde différentielle : copier uniquement les nouveaux/modifiés
                bool result = ExecuteDifferentialBackup(work);
                if (!result) success = false;
            }
        }

        if (success)
        {
            return Language.GetInstance().GetString("work_transfered");
        }

        return Language.GetInstance().GetString("work_not_transfered");
    }

    // Analyse la saisie utilisateur et retourne la liste des index (base 0)
    private List<int> ParseIndexes(string input, int workCount)
    {
        List<int> indexes = new List<int>();

        // Format "1-3" : plage de travaux
        if (Regex.IsMatch(input, @"^(?=.*\d-)[\d-]+$"))
        {
            string[] parts = input.Split('-');

            // If invalid input (like 1--5, 51----6, 1-3-5, ...)
            if (!int.TryParse(parts[0], out int result1) || !int.TryParse(parts[1], out int result2) || parts.Count() > 2)
            {
                indexes.Add(-2);

                return indexes;
            }
            
            int start = int.Parse(parts[0]) - 1;
            int end = int.Parse(parts[1]) - 1;

            for (int i = start; i <= end; i++)
            {
                if (i >= 0 && i < workCount) indexes.Add(i);

                else
                {
                    indexes.Add(-1);
                    continue;
                }
            }

            return indexes;
        }

        // Format "1;3" : travaux spécifiques
        if (Regex.IsMatch(input, @"^(?=.*\d;)[\d;]+$"))
        {
            foreach (string part in input.Split(';'))
            {
                int i = int.Parse(part) - 1;

                if (i >= 0 && i < workCount) indexes.Add(i);

                else
                {
                    indexes.Add(-1);
                    continue;
                }
            }

            return indexes;
        }

        // Format "2" : un seul travail
        if (Regex.IsMatch(input, @"^(?=.*\d)[\d]+$"))
        {
            int i = int.Parse(input) - 1;

            if (i >= 0 && i < workCount) indexes.Add(i);

            else
            {
                indexes.Add(-1);
            }

            return indexes;
        }

        indexes.Add(-2);

        return indexes;
    }

    // Sauvegarde complète : copie tous les fichiers du répertoire source vers la destination
    private bool ExecuteFullBackup(Work work)
    {
        // Vérifier que le répertoire source existe
        if (!Directory.Exists(work.GetSourceDirectory()))
        {
            return false;
        }

        // Récupérer tous les fichiers du répertoire source (y compris sous-dossiers)
        string[] files = Directory.GetFiles(work.GetSourceDirectory(), "*", SearchOption.AllDirectories);

        int totalFiles = files.Length;
        int remainingFiles = totalFiles;
        long totalSize = files.Sum(f => new FileInfo(f).Length);
        long remainingSize = totalSize;
        bool success = true;

        StateFile stateFile = new StateFile();
        LogFile logFile = new LogFile();

        foreach (string sourceFile in files)
        {
            // Construire le chemin de destination en conservant la structure des sous-dossiers
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            // Créer le répertoire de destination si nécessaire
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            long fileSize = new FileInfo(sourceFile).Length;
            long transferTime;

            try
            {
                // Mesurer le temps de transfert
                var watch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(sourceFile, destinationFile, true);
                watch.Stop();
                transferTime = watch.ElapsedMilliseconds;
            }
            catch
            {
                // Temps négatif en cas d'erreur
                transferTime = -1;
                success = false;
            }

            // Mettre à jour les compteurs
            remainingFiles--;
            remainingSize -= fileSize;
            int progression = totalFiles > 0 ? (int)((totalFiles - remainingFiles) * 100 / totalFiles) : 100;

            // Mettre à jour l'état en temps réel
            stateFile.WriteProcess(new WorkState
            {
                WorkName = work.GetName(),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = remainingFiles > 0 ? "Active" : "Inactive",
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                RemainingFiles = remainingFiles,
                RemainingSize = remainingSize,
                Progression = progression,
                CurrentSourceFile = sourceFile,
                CurrentDestinationFile = destinationFile
            });

            // Écrire dans le log journalier
            logFile.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize, transferTime);
        }

        return success;
    }

    // Sauvegarde différentielle : copie uniquement les fichiers nouveaux ou modifiés
    private bool ExecuteDifferentialBackup(Work work)
    {
        // Vérifier que le répertoire source existe
        if (!Directory.Exists(work.GetSourceDirectory()))
        {
            return false;
        }

        string[] files = Directory.GetFiles(work.GetSourceDirectory(), "*", SearchOption.AllDirectories);

        // Filtrer uniquement les fichiers nouveaux ou modifiés
        List<string> filesToCopy = new List<string>();

        foreach (string sourceFile in files)
        {
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            // Copier si le fichier n'existe pas en destination ou s'il a été modifié
            if (!File.Exists(destinationFile) ||
                File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destinationFile))
            {
                filesToCopy.Add(sourceFile);
            }
        }

        int totalFiles = filesToCopy.Count;
        int remainingFiles = totalFiles;
        long totalSize = filesToCopy.Sum(f => new FileInfo(f).Length);
        long remainingSize = totalSize;
        bool success = true;

        StateFile stateFile = new StateFile();
        LogFile logFile = new LogFile();

        foreach (string sourceFile in filesToCopy)
        {
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            long fileSize = new FileInfo(sourceFile).Length;
            long transferTime;

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(sourceFile, destinationFile, true);
                watch.Stop();
                transferTime = watch.ElapsedMilliseconds;
            }
            catch
            {
                transferTime = -1;
                success = false;
            }

            remainingFiles--;
            remainingSize -= fileSize;
            int progression = totalFiles > 0 ? (int)((totalFiles - remainingFiles) * 100 / totalFiles) : 100;

            stateFile.WriteProcess(new WorkState
            {
                WorkName = work.GetName(),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = remainingFiles > 0 ? "Active" : "Inactive",
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                RemainingFiles = remainingFiles,
                RemainingSize = remainingSize,
                Progression = progression,
                CurrentSourceFile = sourceFile,
                CurrentDestinationFile = destinationFile
            });

            logFile.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize, transferTime);
        }

        return success;
    }
}

// option 4 : change the language
public class ChangeLanguage4 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_language");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("language_choice")
    ];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();

        switch (parameters[0])
        {
            case "1":
                lang.SetLanguage(Lang.FR);
                return lang.GetString("language_changed");
            case "2":
                lang.SetLanguage(Lang.EN);
                return lang.GetString("language_changed");
            default:
                return lang.GetString("invalid_option");
        }
    }
}

// option 5 : delete the work
public class DeleteWork5 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_delete");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("work_to_delete")
    ];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        foreach (Work work in workList.GetWork())
        {
            if (work.GetName() == parameters[0])
            {
                workList.DeleteWork(parameters[0]);
                return Language.GetInstance().GetString("work_deleted");
            }
        }
        

        return Language.GetInstance().GetString("work_not_deleted");
    }
}