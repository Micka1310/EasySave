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
        CreateWorkDialog dlg = new CreateWorkDialog
        {
            DataContext = vm,
            Owner = this
        };

        vm.CloseRequested += (_, _) => dlg.Close();
        dlg.ShowDialog();
    }
}
