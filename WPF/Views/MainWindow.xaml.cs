using System.Windows;
using EasySave.WPF.ViewModels;

namespace EasySave.WPF.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        _vm.RequestShowCreateDialog = ShowCreateDialog;
        DataContext = _vm;
    }

    private void ShowCreateDialog(CreateWorkViewModel vm)
    {
        try
        {
            CreateWorkDialog dlg = new CreateWorkDialog
            {
                DataContext = vm,
                Owner = this,
                Tag = _vm,
            };

            vm.CloseRequested += (_, _) => dlg.Close();
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Impossible d'ouvrir la fenêtre de création.\n\n{ex.Message}",
                "EasySave",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
