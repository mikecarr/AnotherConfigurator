using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OpenIPC.ViewModels;

namespace OpenIPC.Views
{
    public partial class CameraSettingsView : UserControl, INotifyPropertyChanged
    {

        public CameraSettingsView()
        {
            InitializeComponent();
            if (!Design.IsDesignMode)
            {
                DataContext = new CameraSettingsTabViewModel();    
            }
            
            
        }
    }
}