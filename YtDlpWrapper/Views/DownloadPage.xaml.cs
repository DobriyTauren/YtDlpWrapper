using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using YtDlpWrapper.Models;
using YtDlpWrapper.Services;
using YtDlpWrapper.ViewModels;

namespace YtDlpWrapper.Views
{
    public sealed partial class DownloadPage : Page
    {
        private readonly DownloadViewModel _vm;

        public DownloadPage()
        {
            InitializeComponent();

            _vm = App.Services.GetRequiredService<DownloadViewModel>();
            DataContext = _vm;

            _vm.DownloadFailed += ShowErrorDialog;
        }

        private async void ShowErrorDialog(DownloadFailureInfo failure)
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.GetString("DownloadErrorDialog_Title"),
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = failure.Message,
                        TextWrapping = TextWrapping.Wrap
                    }
                },
                CloseButtonText = failure.CanUpdateYtDlp
                    ? LocalizationService.GetString("Common_Cancel")
                    : LocalizationService.GetString("Common_Ok"),
                XamlRoot = this.XamlRoot
            };

            if (failure.CanUpdateYtDlp)
            {
                dialog.PrimaryButtonText = LocalizationService.GetString("DownloadErrorDialog_UpdateButton");
            }

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && failure.CanUpdateYtDlp)
            {
                await TryUpdateYtDlpAsync();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _vm.DownloadFailed -= ShowErrorDialog;
            base.OnNavigatedFrom(e);
        }

        private async Task TryUpdateYtDlpAsync()
        {
            try
            {
                await _vm.UpdateYtDlpAsync();
                await ShowInfoDialogAsync(
                    LocalizationService.GetString("YtDlpUpdated_Title"),
                    LocalizationService.GetString("YtDlpUpdated_Message"));
            }
            catch (Exception ex)
            {
                await ShowInfoDialogAsync(LocalizationService.GetString("YtDlpUpdateFailed_Title"), ex.Message);
            }
        }

        private async Task ShowInfoDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap
                    }
                },
                CloseButtonText = LocalizationService.GetString("Common_Ok"),
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
