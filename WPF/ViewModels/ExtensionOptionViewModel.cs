namespace EasySave.WPF.ViewModels;

public sealed class ExtensionOptionViewModel : ViewModelBase
{
    private bool _isSelected;

    public ExtensionOptionViewModel(string extension, bool isSelected = false)
    {
        Extension = extension;
        _isSelected = isSelected;
    }

    public string Extension { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }
}
