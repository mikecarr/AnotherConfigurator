using Avalonia;
using Avalonia.Controls;
using OpenIPC.ViewModels;



namespace OpenIPC.Views;

public partial class TelemetryTabView : UserControl
{
    public TelemetryTabView()
    {
        InitializeComponent();
        
        if (!Design.IsDesignMode)
        {
            DataContext = new TelemetryTabViewModel();
        }
    }
}