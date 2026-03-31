using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using YtDlpWrapper.ViewModels;


namespace YtDlpWrapper.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = App.Services.GetRequiredService<SettingsViewModel>();
        }
    }
}
