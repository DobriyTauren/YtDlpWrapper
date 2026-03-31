using YtDlpWrapper.Utils;

namespace YtDlpWrapper.Models
{
    public class DownloadItem : ObservableObject
    {
        public DownloadItem()
        {
            _status = string.Empty;
        }

        public string Url { get; set; } = string.Empty;

        private double _progress;
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }
    }
}
