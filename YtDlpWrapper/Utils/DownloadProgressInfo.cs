namespace YtDlpWrapper.Utils
{
    public class DownloadProgressInfo
    {
        public double Percent { get; set; }
        public double ItemPercent { get; set; }
        public string? Text { get; set; }
        public bool IsPlaylist { get; set; }
        public int? PlaylistItemIndex { get; set; }
        public int? PlaylistItemCount { get; set; }
        public bool IsPostProcessing { get; set; }
    }
}
