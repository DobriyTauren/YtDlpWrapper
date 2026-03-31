using System.Globalization;
using System.Text.RegularExpressions;

namespace YtDlpWrapper.Utils
{
    public static class DownloadProgressStatusBuilder
    {
        private static readonly Regex TotalSizeRegex = new(@"of\s+(?:~\s*)?(?<size>\d+(?:\.\d+)?)\s*(?<unit>B|KiB|MiB|GiB|TiB)");

        public static string Build(DownloadProgressInfo progress)
        {
            var currentItemText = progress.IsPostProcessing
                ? "Работает FFmpeg"
                : BuildItemStatus(progress.ItemPercent, progress.Text);

            if (!progress.IsPlaylist || !progress.PlaylistItemIndex.HasValue || !progress.PlaylistItemCount.HasValue)
            {
                return currentItemText;
            }

            var playlistText = $"{progress.PlaylistItemIndex.Value}/{progress.PlaylistItemCount.Value}";
            return string.IsNullOrWhiteSpace(currentItemText)
                ? playlistText
                : $"{playlistText} · {currentItemText}";
        }

        private static string BuildItemStatus(double percent, string? ytDlpLine)
        {
            var percentText = $"{percent:0.#}%";
            var totalSize = TryGetTotalSize(ytDlpLine);

            return string.IsNullOrWhiteSpace(totalSize)
                ? percentText
                : $"{percentText} из {totalSize}";
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
            var sizeUnit = sizeMatch.Groups["unit"].Value switch
            {
                "TiB" => "ТБ",
                "GiB" => "ГБ",
                "MiB" => "МБ",
                "KiB" => "КБ",
                "B" => "Б",
                _ => sizeMatch.Groups["unit"].Value
            };

            var numberFormat = sizeValue >= 100 || sizeUnit == "Б" ? "0" : sizeValue >= 10 ? "0.#" : "0.##";
            var formattedValue = sizeValue.ToString(numberFormat, CultureInfo.CurrentCulture);

            return $"{formattedValue} {sizeUnit}";
        }
    }
}
