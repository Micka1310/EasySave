namespace LanguageFile;

public enum Lang
{
    FR,
    EN
}

public class Language
{
    private static Language? instance;
    private Lang currentLanguage;

    private static readonly Dictionary<Lang, Dictionary<string, string>> translations = new()
    {
        {
            Lang.FR, new Dictionary<string, string>
            {
                { "option_display", "Afficher les travaux" },
                { "option_create", "Créer un nouveau travaux" },
                { "option_execute", "Exécuter un travaux" },
                { "option_delete", "Supprimer un travail" },
                { "option_language", "Changer la langue" },

                { "create_name", "Saisissez un nom de fichier :" },
                { "create_source", "Saisissez le répertoire source :" },
                { "create_destination", "Saisissez le répertoire de destination :" },
                { "create_type", "Choisissez le type du fichier : \n1. Complet\n2. Différentielle" },

                { "execute_input", "Numéros des travaux à lancer (ex. 1 ou 1 2 3) :" },
                { "execute_jobs_header", "Travaux enregistrés :" },
                { "execute_no_jobs_yet", "(Aucun travail — créez-en un avec l'option « Créer ».)" },

                { "backup_type_short_full", "Complet" },
                { "backup_type_short_diff", "Diff." },

                { "language_choice", "Choisissez la langue : \n1. FR\n2. EN" },

                { "delete_input", "Numéro du travail à supprimer :" },
                { "delete_no_jobs", "Aucun travail à supprimer." },
                { "delete_success", "Travail supprimé." },
                { "delete_invalid", "Numéro invalide — aucun travail supprimé." },

                { "progress_job", "Travail" },
                { "progress_status", "En cours..." },
                { "progress_done", "Terminé" },
                { "progress_error", "Erreur" },
                { "progress_files", "Fichiers" },
                { "progress_size", "Taille" },
                { "progress_remaining", "Restant" },
                { "progress_bar", "Progression" },
                { "progress_current_file", "Fichier en cours" },

                { "display_work_title", "Travaux n°" },
                { "display_file_name", "- Nom du fichier : " },
                { "display_source", "- Répertoire source : " },
                { "display_destination", "- Répertoire destination : " },
                { "display_type", "- Type de sauvegarde : " },

                { "work_saved", "Travaux sauvegardé" },
                { "work_max_reached", "Maximum de 5 travaux atteint" },
                { "language_changed_to_fr", "Langue changée en Français" },
                { "language_changed_to_en", "Langue changée en anglais" },

                { "menu_title", "Choisissez une option :" },
                { "invalid_option", "Option invalide" },
                { "prompt_retry_input", "La saisie est invalide — vous pouvez corriger ci-dessous." },

                { "error_empty_execute_input", "Saisie vide : entrez au moins un numéro (ex. 1 ou 1 2)." },
                { "error_no_works_to_execute", "Aucun travail à exécuter : créez d'abord un travail." },
                { "error_invalid_execute_format", "Format invalide : entrez uniquement des numéros séparés par des espaces (ex. 1 ou 1 2 3)." },
                { "error_invalid_work_selection", "Sélection invalide : aucun numéro ne correspond à un travail existant." },
                { "error_empty_work_name", "Le nom du travail ne peut pas être vide." },
                { "error_empty_source", "Le répertoire source ne peut pas être vide." },
                { "error_empty_destination", "Le répertoire de destination ne peut pas être vide." },
                { "error_invalid_backup_type", "Type invalide : entrez 1 (complet) ou 2 (différentielle)." },
                { "error_missing_create_parameters", "Saisie incomplète : nom, source, destination et type sont requis." }
            }
        },
        {
            Lang.EN, new Dictionary<string, string>
            {
                { "option_display", "Display works" },
                { "option_create", "Create a new work" },
                { "option_execute", "Execute a work" },
                { "option_delete", "Delete a work" },
                { "option_language", "Change language" },

                { "create_name", "Enter a file name:" },
                { "create_source", "Enter the source directory:" },
                { "create_destination", "Enter the destination directory:" },
                { "create_type", "Choose the file type: \n1. Full\n2. Differential" },

                { "execute_input", "Job numbers to run (e.g. 1 or 1 2 3):" },
                { "execute_jobs_header", "Saved jobs:" },
                { "execute_no_jobs_yet", "(No jobs yet — create one with « Create ».)" },

                { "backup_type_short_full", "Full" },
                { "backup_type_short_diff", "Diff" },

                { "language_choice", "Choose the language: \n1. FR\n2. EN" },

                { "delete_input", "Job number to delete:" },
                { "delete_no_jobs", "No jobs to delete." },
                { "delete_success", "Work deleted." },
                { "delete_invalid", "Invalid number — no work deleted." },

                { "progress_job", "Job" },
                { "progress_status", "Running..." },
                { "progress_done", "Done" },
                { "progress_error", "Error" },
                { "progress_files", "Files" },
                { "progress_size", "Size" },
                { "progress_remaining", "Remaining" },
                { "progress_bar", "Progress" },
                { "progress_current_file", "Current file" },

                { "display_work_title", "Work n°" },
                { "display_file_name", "- File name: " },
                { "display_source", "- Source directory: " },
                { "display_destination", "- Destination directory: " },
                { "display_type", "- Backup type: " },

                { "work_saved", "Work saved" },
                { "work_max_reached", "Maximum of 5 works reached" },
                { "language_changed_to_fr", "Language switched to French" },
                { "language_changed_to_en", "Language changed to English" },

                { "menu_title", "Choose an option:" },
                { "invalid_option", "Invalid option" },
                { "prompt_retry_input", "Invalid input — you can correct it below." },

                { "error_empty_execute_input", "Empty input: enter at least one number (e.g. 1 or 1 2)." },
                { "error_no_works_to_execute", "No work to execute: create a work first." },
                { "error_invalid_execute_format", "Invalid format: enter numbers separated by spaces only (e.g. 1 or 1 2 3)." },
                { "error_invalid_work_selection", "Invalid selection: no number matches an existing work." },
                { "error_empty_work_name", "Work name cannot be empty." },
                { "error_empty_source", "Source directory cannot be empty." },
                { "error_empty_destination", "Destination directory cannot be empty." },
                { "error_invalid_backup_type", "Invalid type: enter 1 (full) or 2 (differential)." },
                { "error_missing_create_parameters", "Incomplete input: name, source, destination and type are required." }
            }
        }
    };

    private Language()
    {
        currentLanguage = Lang.FR;
    }

    public static Language GetInstance()
    {
        if (instance == null)
        {
            instance = new Language();
        }
        return instance;
    }

    public static void Reset()
    {
        instance = null;
    }

    public string GetString(string key)
    {
        return translations[currentLanguage][key];
    }

    public bool ShouldPromptAgainForMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        ReadOnlySpan<string> keys =
        [
            "invalid_option",
            "error_empty_execute_input",
            "error_invalid_execute_format",
            "error_invalid_work_selection",
            "error_empty_work_name",
            "error_empty_source",
            "error_empty_destination",
            "error_invalid_backup_type",
            "error_missing_create_parameters",
            "delete_invalid"
        ];

        Dictionary<string, string> dict = translations[currentLanguage];
        foreach (string key in keys)
        {
            if (message == dict[key])
            {
                return true;
            }
        }

        return false;
    }

    public int? GetRetryFieldIndexForMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return null;
        }

        Dictionary<string, string> dict = translations[currentLanguage];

        if (message == dict["error_empty_work_name"]) return 0;
        if (message == dict["error_empty_source"]) return 1;
        if (message == dict["error_empty_destination"]) return 2;
        if (message == dict["error_invalid_backup_type"]) return 3;
        if (message == dict["invalid_option"]) return 0;
        if (message == dict["delete_invalid"]) return 0;

        return null;
    }

    public void SetLanguage(Lang language)
    {
        currentLanguage = language;
    }

    public Lang GetCurrentLanguage()
    {
        return currentLanguage;
    }
}
