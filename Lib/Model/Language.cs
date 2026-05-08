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
                { "delete_invalid", "Suppression impossible : le numéro ne correspond à aucun travail existant." },

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
                { "invalid_option", "Option invalide : choisissez un numéro proposé dans le menu." },
                { "prompt_retry_input", "Corrigez votre saisie selon le détail de l'erreur affichée ci-dessus." },

                { "error_empty_execute_input", "Saisie vide : entrez au moins un numéro (ex. 1 ou 1 2)." },
                { "error_no_works_to_execute", "Aucun travail à exécuter : créez d'abord un travail." },
                { "error_invalid_execute_format", "Format invalide : entrez uniquement des numéros séparés par des espaces (ex. 1 ou 1 2 3)." },
                { "error_invalid_work_selection", "Aucun travail sélectionné : les numéros saisis ne correspondent à aucun travail existant." },
                { "error_empty_work_name", "Le nom du travail ne peut pas être vide." },
                { "error_empty_source", "Le répertoire source ne peut pas être vide." },
                { "error_empty_destination", "Le répertoire de destination ne peut pas être vide." },
                { "error_source_not_found", "Le répertoire source n'existe pas." },
                { "error_destination_not_found", "Le répertoire de destination n'existe pas." },
                { "error_same_source_destination", "La source et la destination doivent être différentes." },
                { "error_invalid_backup_type", "Type invalide : entrez 1 (complet) ou 2 (différentielle)." },
                { "error_missing_create_parameters", "Saisie incomplète : nom, source, destination et type sont requis." },

                { "wpf_status_idle", "En attente" },
                { "wpf_status_inactive", "Inactif" },
                { "wpf_status_active", "En cours" },
                { "wpf_section_menu", "MENU" },
                { "wpf_section_language", "Langue" },
                { "wpf_backup_full_title", "Sauvegarde complète" },
                { "wpf_backup_full_desc", "Copie tous les fichiers à chaque exécution." },
                { "wpf_backup_diff_title", "Sauvegarde différentielle" },
                { "wpf_backup_diff_desc", "Copie uniquement les fichiers modifiés depuis la dernière complète." },

                { "wpf_pause", "Pause" },
                { "wpf_resume", "Reprendre" },
                { "wpf_stop", "Arrêter" },
                { "wpf_status_paused", "En pause" },
                { "wpf_status_cancelled", "Annulé" },
                { "wpf_backup_stopped", "Sauvegarde interrompue." },

                { "wpf_settings_title", "Paramètres" },
                { "wpf_settings_subtitle", "Affichage, emplacement des données et informations." },
                { "wpf_settings_section_display", "Affichage de la liste" },
                { "wpf_settings_page_size", "Travaux par page" },
                { "wpf_settings_page_size_hint", "Aide à parcourir un grand nombre de travaux sans ralentir l’interface." },
                { "wpf_settings_section_data", "Données et journaux" },
                { "wpf_settings_data_folder", "Dossier des journaux et du fichier d’état" },
                { "wpf_settings_works_file", "Liste des travaux (works.json)" },
                { "wpf_settings_section_about", "À propos" },
                { "wpf_settings_about_body", "EasySave v2 — interface graphique. Les travaux sont enregistrés dans works.json à côté de l’exécutable (répertoire de travail de l’application)." },
                { "wpf_page_prev", "Page précédente" },
                { "wpf_page_next", "Page suivante" }
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
                { "delete_invalid", "Delete failed: this number does not match any existing job." },

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
                { "invalid_option", "Invalid option: choose a number from the menu." },
                { "prompt_retry_input", "Correct your input using the detailed error shown above." },

                { "error_empty_execute_input", "Empty input: enter at least one number (e.g. 1 or 1 2)." },
                { "error_no_works_to_execute", "No work to execute: create a work first." },
                { "error_invalid_execute_format", "Invalid format: enter numbers separated by spaces only (e.g. 1 or 1 2 3)." },
                { "error_invalid_work_selection", "No job selected: the entered numbers do not match any existing job." },
                { "error_empty_work_name", "Work name cannot be empty." },
                { "error_empty_source", "Source directory cannot be empty." },
                { "error_empty_destination", "Destination directory cannot be empty." },
                { "error_source_not_found", "Source directory does not exist." },
                { "error_destination_not_found", "Destination directory does not exist." },
                { "error_same_source_destination", "Source and destination must be different." },
                { "error_invalid_backup_type", "Invalid type: enter 1 (full) or 2 (differential)." },
                { "error_missing_create_parameters", "Incomplete input: name, source, destination and type are required." },

                { "wpf_status_idle", "Idle" },
                { "wpf_status_inactive", "Inactive" },
                { "wpf_status_active", "Running" },
                { "wpf_section_menu", "MENU" },
                { "wpf_section_language", "Language" },
                { "wpf_backup_full_title", "Full backup" },
                { "wpf_backup_full_desc", "Copies all files on every run." },
                { "wpf_backup_diff_title", "Differential backup" },
                { "wpf_backup_diff_desc", "Copies only files changed since the last full backup." },

                { "wpf_pause", "Pause" },
                { "wpf_resume", "Resume" },
                { "wpf_stop", "Stop" },
                { "wpf_status_paused", "Paused" },
                { "wpf_status_cancelled", "Cancelled" },
                { "wpf_backup_stopped", "Backup stopped." },

                { "wpf_settings_title", "Settings" },
                { "wpf_settings_subtitle", "Display, data location and information." },
                { "wpf_settings_section_display", "List display" },
                { "wpf_settings_page_size", "Jobs per page" },
                { "wpf_settings_page_size_hint", "Helps browse many jobs without slowing down the UI." },
                { "wpf_settings_section_data", "Data and logs" },
                { "wpf_settings_data_folder", "Logs and state file folder" },
                { "wpf_settings_works_file", "Job list (works.json)" },
                { "wpf_settings_section_about", "About" },
                { "wpf_settings_about_body", "EasySave v2 — graphical UI. Jobs are stored in works.json next to the app (current working directory)." },
                { "wpf_page_prev", "Previous page" },
                { "wpf_page_next", "Next page" }
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
            "error_source_not_found",
            "error_destination_not_found",
            "error_same_source_destination",
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
        if (message == dict["error_source_not_found"]) return 1;
        if (message == dict["error_destination_not_found"]) return 2;
        if (message == dict["error_same_source_destination"]) return 1;
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
