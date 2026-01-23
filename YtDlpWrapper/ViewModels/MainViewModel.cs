using YtDlpWrapper.Models;

namespace YtDlpWrapper.ViewModels
{
    public class MainViewModel
    {
        public WindowOptions Window { get; } = new();
        public DownloadViewModel Download { get; }
        public SettingsViewModel Settings { get; }


        public MainViewModel(DownloadViewModel download, SettingsViewModel settings)
        {
            Download = download;
            Settings = settings;
        }
    }
}
