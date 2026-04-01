using System.Globalization;
using System.Text.RegularExpressions;
using YtDlpWrapper.Services;

namespace YtDlpWrapper.Utils
{
    public static class DownloadProgressStatusBuilder
    {
        private static readonly Regex TotalSizeRegex = new(@"of\s+(?:~\s*)?(?<size>\d+(?:\.\d+)?)\s*(?<unit>B|KiB|MiB|GiB|TiB)");

        public static string Build(DownloadProgressInfo progress)
        {
            var currentItemText = progress.IsPostProcessing
                ? LocalizationService.GetString("Progress_PostProcessing")
                : BuildItemStatus(progress.ItemPercent, progress.Text);

            if (!progress.IsPlaylist || !progress.PlaylistItemIndex.HasValue || !progress.PlaylistItemCount.HasValue)
            {
                return currentItemText;
            }

            var playlistText = $"{progress.PlaylistItemIndex.Value}/{progress.PlaylistItemCount.Value}";
            return string.IsNullOrWhiteSpace(currentItemText)
                ? playlistText
                : LocalizationService.Format("Progress_PlaylistWithCurrent", playlistText, currentItemText);
        }

        private static string BuildItemStatus(double percent, string? ytDlpLine)
        {
            var percentText = $"{percent:0.#}%";
            var totalSize = TryGetTotalSize(ytDlpLine);

            return string.IsNullOrWhiteSpace(totalSize)
                ? percentText
                : LocalizationService.Format("Progress_WithTotalSize", percentText, totalSize);
        }

        private static string? TryGetTotalSize(string? ytDlpLine)
        {
            if (string.IsNullOrWhiteSpace(ytDlpLine))
            {
                return null;
            }

            var sizeMatch = TotalSizeRegex.Match(ytDlpLine);
            if (!sizeMatch.Success)
            {
                return null;
            }

            var sizeValue = double.Parse(sizeMatch.Groups["size"].Value, CultureInfo.InvariantCulture);
            var sizeUnitToken = sizeMatch.Groups["unit"].Value;
            var sizeUnit = sizeUnitToken switch
            {
                "TiB" => LocalizationService.GetString("Unit_TiB"),
                "GiB" => LocalizationService.GetString("Unit_GiB"),
                "MiB" => LocalizationService.GetString("Unit_MiB"),
                "KiB" => LocalizationService.GetString("Unit_KiB"),
                "B" => LocalizationService.GetString("Unit_B"),
                _ => sizeUnitToken
            };

            var numberFormat = sizeValue >= 100 || sizeUnitToken == "B" ? "0" : sizeValue >= 10 ? "0.#" : "0.##";
            var formattedValue = sizeValue.ToString(numberFormat, CultureInfo.CurrentCulture);

            return $"{formattedValue} {sizeUnit}";
        }
    }
}
