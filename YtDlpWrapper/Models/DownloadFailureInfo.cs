namespace YtDlpWrapper.Models
{
    public sealed class DownloadFailureInfo
    {
        public DownloadFailureInfo(string message, bool canUpdateYtDlp = false)
        {
            Message = message;
            CanUpdateYtDlp = canUpdateYtDlp;
        }

        public string Message { get; }

        public bool CanUpdateYtDlp { get; }
    }
}
