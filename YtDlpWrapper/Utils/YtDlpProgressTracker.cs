using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YtDlpWrapper.Utils
{
    public class YtDlpProgressTracker
    {
        private static readonly Regex PercentRegex = new(@"\[download\]\s+(?<percent>\d+(?:\.\d+)?)%");
        private static readonly Regex PlaylistItemRegex = new(@"\[download\]\s+Downloading item (?<index>\d+) of (?<count>\d+)");
        private static readonly Regex FfmpegStageRegex = new(@"\[(ExtractAudio|VideoRemuxer|VideoConvertor|Merger)\]", RegexOptions.IgnoreCase);

        private int? _playlistItemIndex;
        private int? _playlistItemCount;
        private double _lastOverallPercent;
        private double _lastItemPercent;

        public DownloadProgressInfo? Parse(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            var playlistMatch = PlaylistItemRegex.Match(line);
            if (playlistMatch.Success)
            {
                _playlistItemIndex = int.Parse(playlistMatch.Groups["index"].Value, CultureInfo.InvariantCulture);
                _playlistItemCount = int.Parse(playlistMatch.Groups["count"].Value, CultureInfo.InvariantCulture);

                return CreateProgressInfo(line, 0);
            }

            var percentMatch = PercentRegex.Match(line);
            if (percentMatch.Success)
            {
                var itemPercent = double.Parse(percentMatch.Groups["percent"].Value, CultureInfo.InvariantCulture);
                return CreateProgressInfo(line, itemPercent);
            }

            if (!FfmpegStageRegex.IsMatch(line))
            {
                return null;
            }

            return new DownloadProgressInfo
            {
                Percent = IsPlaylistActive() ? _lastOverallPercent : 100,
                ItemPercent = IsPlaylistActive() ? _lastItemPercent : 100,
                Text = line,
                IsPlaylist = IsPlaylistActive(),
                PlaylistItemIndex = _playlistItemIndex,
                PlaylistItemCount = _playlistItemCount,
                IsPostProcessing = true
            };
        }

        private DownloadProgressInfo CreateProgressInfo(string line, double itemPercent)
        {
            var overallPercent = itemPercent;
            var isPlaylist = _playlistItemIndex.HasValue && _playlistItemCount.GetValueOrDefault() > 1;

            if (isPlaylist)
            {
                overallPercent = ((_playlistItemIndex!.Value - 1) + (itemPercent / 100d)) / _playlistItemCount!.Value * 100d;
            }

            _lastOverallPercent = Math.Clamp(overallPercent, 0, 100);
            _lastItemPercent = Math.Clamp(itemPercent, 0, 100);

            return new DownloadProgressInfo
            {
                Percent = _lastOverallPercent,
                ItemPercent = _lastItemPercent,
                Text = line,
                IsPlaylist = isPlaylist,
                PlaylistItemIndex = _playlistItemIndex,
                PlaylistItemCount = _playlistItemCount
            };
        }

        private bool IsPlaylistActive()
        {
            return _playlistItemIndex.HasValue && _playlistItemCount.GetValueOrDefault() > 1;
        }
    }
}
