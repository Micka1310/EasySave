using LanguageFile;

namespace EasySave.WPF.ViewModels;

public partial class MainViewModel
{
    private bool _isFrench;
    public bool IsFrench
    {
        get => _isFrench;
        set
        {
            if (SetField(ref _isFrench, value))
            {
                _lang.SetLanguage(value ? Lang.FR : Lang.EN);
                NotifyAllLabels();
            }
        }
    }

    public string LblPrimaryHeader => ShowWorksPanel ? LblHeader : LblSettingsTitle;
    public string LblPrimarySubtitle => ShowWorksPanel ? LblCount : LblSettingsSubtitle;
    public string LblAppTitle => "EasySave";
    public string LblAppSubtitle => IsFrench ? "Outil de sauvegarde — v2" : "Backup tool — v2";
    public string LblSectionMenu => _lang.GetString("wpf_section_menu");
    public string LblSectionLanguage => _lang.GetString("wpf_section_language");
    public string LblNavWorks => IsFrench ? "Travaux" : "Jobs";
    public string LblNavSettings => IsFrench ? "Paramètres" : "Settings";
    public string LblHeader => IsFrench ? "Mes travaux de sauvegarde" : "My backup jobs";
    public string LblCount => IsFrench ? $"{Works.Count} travaux" : $"{Works.Count} jobs";
    public string LblNew => IsFrench ? "Nouveau" : "New";
    public string LblRunSelected => IsFrench ? "Exécuter la sélection" : "Run selected";
    public string LblRunAll => IsFrench ? "Tout exécuter" : "Run all";
    public string LblEmptyTitle => IsFrench ? "Aucun travail pour le moment" : "No jobs yet";
    public string LblEmptySubtitle => IsFrench
        ? "Créez votre premier travail de sauvegarde pour commencer."
        : "Create your first backup job to get started.";
    public string LblCreateFirst => IsFrench ? "Créer un travail" : "Create a job";
    public string LblFull => _lang.GetString("backup_type_short_full");
    public string LblDiff => _lang.GetString("backup_type_short_diff");
    public string LblBackupFullTitle => _lang.GetString("wpf_backup_full_title");
    public string LblBackupFullDesc => _lang.GetString("wpf_backup_full_desc");
    public string LblBackupDiffTitle => _lang.GetString("wpf_backup_diff_title");
    public string LblBackupDiffDesc => _lang.GetString("wpf_backup_diff_desc");
    public string LblSource => "Source";
    public string LblDestination => "Destination";
    public string LblDelete => IsFrench ? "Supprimer" : "Delete";
    public string LblDialogTitle => IsFrench ? "Nouveau travail" : "New job";
    public string LblFieldName => IsFrench ? "Nom du travail" : "Job name";
    public string LblFieldSource => IsFrench ? "Dossier source" : "Source folder";
    public string LblFieldDestination => IsFrench ? "Dossier destination" : "Destination folder";
    public string LblFieldType => IsFrench ? "Type de sauvegarde" : "Backup type";
    public string LblBrowse => IsFrench ? "Parcourir..." : "Browse...";
    public string LblSave => IsFrench ? "Enregistrer" : "Save";
    public string LblCancel => IsFrench ? "Annuler" : "Cancel";
    public string LblSettingsTitle => _lang.GetString("wpf_settings_title");
    public string LblSettingsSubtitle => _lang.GetString("wpf_settings_subtitle");
    public string LblSettingsSectionDisplay => _lang.GetString("wpf_settings_section_display");
    public string LblSectionLogFormat => _lang.GetString("log_format_header_label");
    public string LblLogJson => _lang.GetString("log_format_json");
    public string LblLogXml => _lang.GetString("log_format_xml");
    public string LblSettingsPageSize => _lang.GetString("wpf_settings_page_size");
    public string LblSettingsPageSizeHint => _lang.GetString("wpf_settings_page_size_hint");
    public string LblSettingsSectionData => _lang.GetString("wpf_settings_section_data");
    public string LblSettingsDataFolder => _lang.GetString("wpf_settings_data_folder");
    public string LblSettingsWorksFile => _lang.GetString("wpf_settings_works_file");
    public string LblSettingsLogsFile => _lang.GetString("wpf_settings_logs_file");
    public string LblSettingsLogsPreview => _lang.GetString("wpf_settings_logs_preview");
    public string LblSettingsLogsRefresh => _lang.GetString("wpf_settings_logs_refresh");
    public string LblSettingsOpenLogsFolder => _lang.GetString("wpf_settings_open_logs_folder");
    public string LblSettingsSectionLogRouting => _lang.GetString("wpf_settings_section_log_routing");
    public string LblSettingsLogDestination => _lang.GetString("wpf_settings_log_destination");
    public string LblSettingsCentralLogUrl => _lang.GetString("wpf_settings_central_log_url");
    public string LblSettingsCentralLogHint => _lang.GetString("wpf_settings_central_log_hint");
    public string LblSettingsSectionBusinessSoftware => _lang.GetString("wpf_settings_section_business_software");
    public string LblSettingsBusinessSoftwareLabel => _lang.GetString("wpf_settings_business_software_label");
    public string LblSettingsBusinessSoftwareHint => _lang.GetString("wpf_settings_business_software_hint");
    public string LblSettingsBusinessSoftwareAddButton => _lang.GetString("wpf_settings_business_software_add_button");
    public string LblSettingsBusinessSoftwareList => _lang.GetString("wpf_settings_business_software_list");
    public string LblSettingsBusinessSoftwareResetDefaults => _lang.GetString("wpf_settings_business_software_reset_defaults");
    public string LblSettingsBusinessSoftwareClearAll => _lang.GetString("wpf_settings_business_software_clear_all");
    public string LblSettingsBusinessSoftwareEmpty => _lang.GetString("wpf_settings_business_software_empty");
    public string LblSettingsSectionPriority => _lang.GetString("wpf_settings_section_priority");
    public string LblSettingsPriorityExtensions => _lang.GetString("wpf_settings_priority_extensions");
    public string LblSettingsPrioritySelected => _lang.GetString("wpf_settings_priority_selected");
    public string LblSettingsPriorityAdd => _lang.GetString("wpf_settings_priority_add");
    public string LblSettingsPriorityAddButton => _lang.GetString("wpf_settings_priority_add_button");
    public string LblSettingsPriorityHint => _lang.GetString("wpf_settings_priority_hint");
    public string LblSettingsSectionBandwidth => _lang.GetString("wpf_settings_section_bandwidth");
    public string LblSettingsBandwidthThreshold => _lang.GetString("wpf_settings_bandwidth_threshold");
    public string LblSettingsBandwidthThresholdUnit => _lang.GetString("wpf_settings_bandwidth_threshold_unit");
    public string LblSettingsBandwidthHint => _lang.GetString("wpf_settings_bandwidth_hint");
    public string LblSettingsBandwidthDisabled => _lang.GetString("wpf_settings_bandwidth_disabled");
    public string LblSettingsSectionEncryption => _lang.GetString("wpf_settings_section_encryption");
    public string LblSettingsEncryptionExtensions => _lang.GetString("wpf_settings_encryption_extensions");
    public string LblSettingsEncryptionSelected => _lang.GetString("wpf_settings_encryption_selected");
    public string LblSettingsEncryptionAdd => _lang.GetString("wpf_settings_encryption_add");
    public string LblSettingsEncryptionAddButton => _lang.GetString("wpf_settings_encryption_add_button");
    public string LblSettingsEncryptionHint => _lang.GetString("wpf_settings_encryption_hint");
    public string LblSettingsCryptoSoftFolder => _lang.GetString("wpf_settings_cryptosoft_folder");
    public string LblSettingsSectionAbout => _lang.GetString("wpf_settings_section_about");
    public string LblSettingsAboutBody => _lang.GetString("wpf_settings_about_body");
    public string LblPagePrev => _lang.GetString("wpf_page_prev");
    public string LblPageNext => _lang.GetString("wpf_page_next");
    public string LblPause => _lang.GetString("wpf_pause");
    public string LblResume => _lang.GetString("wpf_resume");
    public string LblStop => _lang.GetString("wpf_stop");
    public string LblPauseAll => IsFrench ? "Tout mettre en pause" : "Pause all";
    public string LblResumeAll => IsFrench ? "Tout reprendre" : "Resume all";
    public string LblStopAll => IsFrench ? "Tout arrêter" : "Stop all";

    private void NotifyAllLabels()
    {
        string[] labels =
        [
            nameof(LblAppSubtitle), nameof(LblSectionMenu), nameof(LblSectionLanguage), nameof(LblSectionLogFormat),
            nameof(LblLogJson), nameof(LblLogXml), nameof(LblNavWorks), nameof(LblNavSettings), nameof(LblHeader),
            nameof(LblCount), nameof(LblPrimaryHeader), nameof(LblPrimarySubtitle), nameof(LblNew),
            nameof(LblRunSelected), nameof(LblRunAll), nameof(LblEmptyTitle), nameof(LblEmptySubtitle),
            nameof(LblCreateFirst), nameof(LblFull), nameof(LblDiff), nameof(LblBackupFullTitle), nameof(LblBackupFullDesc),
            nameof(LblBackupDiffTitle), nameof(LblBackupDiffDesc), nameof(LblSource), nameof(LblDestination),
            nameof(LblDelete), nameof(LblDialogTitle), nameof(LblFieldName), nameof(LblFieldSource),
            nameof(LblFieldDestination), nameof(LblFieldType), nameof(LblBrowse), nameof(LblSave), nameof(LblCancel),
            nameof(LblSettingsTitle), nameof(LblSettingsSubtitle), nameof(LblSettingsSectionDisplay), nameof(LblSettingsPageSize),
            nameof(LblSettingsPageSizeHint), nameof(LblSettingsSectionData), nameof(LblSettingsDataFolder),
            nameof(LblSettingsWorksFile), nameof(LblSettingsLogsFile), nameof(LblSettingsLogsPreview), nameof(LblSettingsLogsRefresh),
            nameof(LblSettingsOpenLogsFolder), nameof(LblSettingsSectionLogRouting), nameof(LblSettingsLogDestination),
            nameof(LblSettingsCentralLogUrl), nameof(LblSettingsCentralLogHint),
            nameof(LblSettingsSectionBusinessSoftware), nameof(LblSettingsBusinessSoftwareLabel),
            nameof(LblSettingsBusinessSoftwareHint), nameof(LblSettingsBusinessSoftwareAddButton), nameof(LblSettingsBusinessSoftwareList),
            nameof(LblSettingsBusinessSoftwareResetDefaults), nameof(LblSettingsBusinessSoftwareClearAll), nameof(LblSettingsBusinessSoftwareEmpty),
            nameof(LblSettingsSectionPriority), nameof(LblSettingsPriorityExtensions), nameof(LblSettingsPrioritySelected),
            nameof(LblSettingsPriorityAdd), nameof(LblSettingsPriorityAddButton), nameof(LblSettingsPriorityHint),
            nameof(LblSettingsSectionBandwidth), nameof(LblSettingsBandwidthThreshold), nameof(LblSettingsBandwidthThresholdUnit),
            nameof(LblSettingsBandwidthHint), nameof(LblSettingsBandwidthDisabled), nameof(LargeFileThresholdSummary),
            nameof(LblSettingsSectionEncryption), nameof(LblSettingsEncryptionExtensions), nameof(LblSettingsEncryptionSelected),
            nameof(LblSettingsEncryptionAdd), nameof(LblSettingsEncryptionAddButton), nameof(LblSettingsEncryptionHint),
            nameof(LblSettingsCryptoSoftFolder), nameof(LblSettingsSectionAbout), nameof(LblSettingsAboutBody),
            nameof(LblPagePrev), nameof(LblPageNext), nameof(LblPaginationDetail), nameof(LblPaginationPages),
            nameof(LblPause), nameof(LblResume), nameof(LblStop),
            nameof(LblPauseAll), nameof(LblResumeAll), nameof(LblStopAll)
        ];

        foreach (string label in labels)
            OnPropertyChanged(label);

        foreach (WorkItemViewModel w in Works)
            w.RefreshLocalization();

        RefreshLogPreview();
    }
}
