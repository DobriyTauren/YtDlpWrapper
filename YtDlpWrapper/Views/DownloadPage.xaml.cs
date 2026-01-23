using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using YtDlpWrapper.ViewModels;


namespace YtDlpWrapper.Views
{
    public sealed partial class DownloadPage : Page
    {
        private DownloadViewModel _vm;

        public DownloadPage()
        {
            InitializeComponent();

            _vm = App.Services.GetRequiredService<DownloadViewModel>();
            DataContext = _vm;

            _vm.DownloadFailed += ShowErrorDialog;
        }

        private async void ShowErrorDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Ошибка загрузки",
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap
                    }
                },
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _vm.DownloadFailed -= ShowErrorDialog;
            base.OnNavigatedFrom(e);
        }
    }
}
