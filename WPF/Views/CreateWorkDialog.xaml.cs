using System.Windows;
using System.Windows.Input;

namespace EasySave.WPF.Views;

public partial class CreateWorkDialog : Window
{
    public CreateWorkDialog()
    {
        InitializeComponent();
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        };
    }
}
