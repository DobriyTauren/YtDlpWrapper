using System;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using YtDlpWrapper.Services;
using YtDlpWrapper.Utils;


namespace YtDlpWrapper.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;

        private string _downloadFolder;

        public string DownloadFolder
        {
            get => _downloadFolder;
            set => SetProperty(ref _downloadFolder, value);
        }

        public ICommand ChooseFolderCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;

            _downloadFolder = _settingsService.DownloadFolder;

            ChooseFolderCommand = new RelayCommand(ChooseFolder);
            OpenFolderCommand = new RelayCommand(OpenDownloadFolder);
        }

        private async void ChooseFolder()
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };

            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                DownloadFolder = folder.Path;
                _settingsService.SetDownloadFolder(folder.Path);
            }
        }

        private async void OpenDownloadFolder()
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(DownloadFolder);
            _ = await Windows.System.Launcher.LaunchFolderAsync(folder);
        }
    }
}
