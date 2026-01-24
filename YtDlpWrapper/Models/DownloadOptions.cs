using System.ComponentModel;

namespace YtDlpWrapper.Models
{
    public enum DownloadType
    {
        Video,
        Audio
    }

    public enum VideoQuality
    { 
        [Description("Лучшее")] Best, 
        [Description("2160p")] Q2160p, 
        [Description("1440p")] Q1440p, 
        [Description("1080p")] Q1080p, 
        [Description("720p")] Q720p, 
        [Description("480p")] Q480p 
    }
}
