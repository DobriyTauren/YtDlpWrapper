using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using YtDlpWrapper.Models;
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
                Title = "Ошибка загрузки",
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = failure.Message,
                        TextWrapping = TextWrapping.Wrap
                    }
                },
                CloseButtonText = failure.CanUpdateYtDlp ? "Отмена" : "ОК",
                XamlRoot = this.XamlRoot
            };

            if (failure.CanUpdateYtDlp)
            {
                dialog.PrimaryButtonText = "Обновить yt-dlp";
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
                await ShowInfoDialogAsync("yt-dlp обновлён", "yt-dlp успешно обновлён. Попробуйте запустить загрузку ещё раз.");
            }
            catch (Exception ex)
            {
                await ShowInfoDialogAsync("Не удалось обновить yt-dlp", ex.Message);
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
                CloseButtonText = "ОК",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
