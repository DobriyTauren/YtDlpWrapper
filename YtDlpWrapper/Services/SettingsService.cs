using Windows.Storage;
using YtDlpWrapper.Models;

namespace YtDlpWrapper.Services
{
    public class SettingsService
    {
        private const string FolderKey = "DownloadFolder";
        private const string DownloadTypeKey = "DownloadType";
        private const string FormatKey = "Format";
        private const string VideoQualityKey = "VideoQuality";

        private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

        public DownloadType DownloadType
        {
            get => (DownloadType)(_settings.Values[DownloadTypeKey] ?? DownloadType.Video);
            set => _settings.Values[DownloadTypeKey] = (int)value;
        }

        public string Format
        {
            get => _settings.Values[FormatKey] as string ?? "mp4";
            set => _settings.Values[FormatKey] = value;
        }

        public VideoQuality VideoQuality
        {
            get => (VideoQuality)(_settings.Values[VideoQualityKey] ?? VideoQuality.Best);
            set => _settings.Values[VideoQualityKey] = (int)value;
        }

        public string DownloadFolder => _settings.Values[FolderKey] as string
            ?? UserDataPaths.GetDefault().Downloads;

        public void SetDownloadFolder(string path)
        {
            _settings.Values[FolderKey] = path;
        }
    }
}
