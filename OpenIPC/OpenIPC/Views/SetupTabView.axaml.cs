using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenIPC.ViewModels;

namespace OpenIPC.Views;

public partial class SetupTabView : UserControl
{
    public SetupTabView()
    {
        InitializeComponent();
        if (!Design.IsDesignMode)
        {
            DataContext = new SetupTabViewModel();
        }
        

    }
    
    
}