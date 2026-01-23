using Windows.Storage;
using YtDlpWrapper.Models;

namespace YtDlpWrapper.Services
{
    public class SettingsService
    {
        private const string FolderKey = "DownloadFolder";

        private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

        public DownloadType DownloadType
        {
            get => (DownloadType)(_settings.Values["DownloadType"] ?? DownloadType.Video);
            set => _settings.Values["DownloadType"] = (int)value;
        }

        public string Format
        {
            get => _settings.Values["Format"] as string ?? "mp4";
            set => _settings.Values["Format"] = value;
        }

        public VideoQuality VideoQuality
        {
            get => (VideoQuality)(_settings.Values["VideoQuality"] ?? VideoQuality.Best);
            set => _settings.Values["VideoQuality"] = (int)value;
        }

        public string DownloadFolder =>
            ApplicationData.Current.LocalSettings.Values[FolderKey] as string
            ?? UserDataPaths.GetDefault().Downloads;

        public void SetDownloadFolder(string path)
        {
            ApplicationData.Current.LocalSettings.Values[FolderKey] = path;
        }
    }
}
