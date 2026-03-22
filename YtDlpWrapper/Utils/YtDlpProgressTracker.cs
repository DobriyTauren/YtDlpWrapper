using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YtDlpWrapper.Utils
{
    public class YtDlpProgressTracker
    {
        private static readonly Regex PercentRegex = new(@"\[download\]\s+(?<percent>\d+(?:\.\d+)?)%");
        private static readonly Regex PlaylistItemRegex = new(@"\[download\]\s+Downloading item (?<index>\d+) of (?<count>\d+)");

        private int? _playlistItemIndex;
        private int? _playlistItemCount;

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
            if (!percentMatch.Success)
            {
                return null;
            }

            var itemPercent = double.Parse(percentMatch.Groups["percent"].Value, CultureInfo.InvariantCulture);
            return CreateProgressInfo(line, itemPercent);
        }

        private DownloadProgressInfo CreateProgressInfo(string line, double itemPercent)
        {
            var overallPercent = itemPercent;
            var isPlaylist = _playlistItemIndex.HasValue && _playlistItemCount.GetValueOrDefault() > 1;

            if (isPlaylist)
            {
                overallPercent = ((_playlistItemIndex!.Value - 1) + (itemPercent / 100d)) / _playlistItemCount!.Value * 100d;
            }

            return new DownloadProgressInfo
            {
                Percent = Math.Clamp(overallPercent, 0, 100),
                ItemPercent = Math.Clamp(itemPercent, 0, 100),
                Text = line,
                IsPlaylist = isPlaylist,
                PlaylistItemIndex = _playlistItemIndex,
                PlaylistItemCount = _playlistItemCount
            };
        }
    }
}
