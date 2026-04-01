using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using YtDlpWrapper.Models;
using YtDlpWrapper.Services;

namespace YtDlpWrapper.Utils
{
    public class YtDlpService
    {
        private const int SoundCloudSocketTimeoutSeconds = 45;
        private const int SoundCloudRetryCount = 5;

        private Process? _currentProcess;

        public async Task DownloadAsync(string url, DownloadType downloadType, string format, VideoQuality quality,
            bool downloadPlaylist, string outputFolder, Action<DownloadProgressInfo> onProgress,
            CancellationToken cancellationToken = default)
        {
            var toolsFolder = await EnsureToolsFolderAsync(cancellationToken).ConfigureAwait(false);
            var ytDlpPath = Path.Combine(toolsFolder, "yt-dlp.exe");
            var progressTracker = new YtDlpProgressTracker();
            var existingPartFiles = CapturePartFiles(outputFolder);

            if (!File.Exists(ytDlpPath))
                throw new FileNotFoundException(LocalizationService.GetString("Error_YtDlpExecutableMissing"));

            var arguments = BuildArguments(
                url,
                downloadType,
                format,
                quality,
                downloadPlaylist,
                toolsFolder,
                outputFolder
            );

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = toolsFolder,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _currentProcess = new Process { StartInfo = psi };
                _currentProcess.Start();

                using var cancellationRegistration = cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!_currentProcess.HasExited)
                            _currentProcess.Kill(true);
                    }
                    catch { }
                });

                var errorLines = new List<string>();
                var stdoutTask = Task.Run(() => ReadStandardOutputAsync(_currentProcess, progressTracker, onProgress), cancellationToken);
                var stderrTask = Task.Run(() => ReadStandardErrorAsync(_currentProcess, progressTracker, onProgress, errorLines), cancellationToken);

                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                await _currentProcess.WaitForExitAsync().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);

                if (_currentProcess.ExitCode != 0)
                    throw new Exception(MapErrorMessage(errorLines));
            }
            finally
            {
                try
                {
                    _currentProcess?.Dispose();
                }
                catch { }

                if (cancellationToken.IsCancellationRequested)
                {
                    TryDeleteCreatedPartFiles(outputFolder, existingPartFiles);
                }

                _currentProcess = null;
            }

        }

        private HashSet<string> CapturePartFiles(string path)
        {
            var existingFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    return existingFiles;
                }

                foreach (var file in Directory.GetFiles(path, "*.part"))
                {
                    existingFiles.Add(file);
                }
            }
            catch
            {
                // Не мешаем основному сценарию загрузки, если не удалось снять снимок директории.
            }

            return existingFiles;
        }

        private void TryDeleteCreatedPartFiles(string path, HashSet<string> existingPartFiles)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    return;
                }

                foreach (var file in Directory.GetFiles(path, "*.part"))
                {
                    if (!existingPartFiles.Contains(file))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Отмена не должна ломаться из-за неудачной очистки временных файлов.
            }
        }

        private string BuildArguments(string url, DownloadType type, string format, VideoQuality quality, bool downloadPlaylist, string toolsFolder, string outputFolder)
        {
            var args = new List<string>
            {
                $"\"{url}\"",
                "--newline",
                $"--ffmpeg-location \"{toolsFolder}\"",
                "--postprocessor-args \"FFmpeg:-loglevel info -progress pipe:2 -nostats\"",
                downloadPlaylist ? "--yes-playlist" : "--no-playlist"
            };

            if (IsYoutubeUrl(url))
            {
                args.Add("--extractor-args \"youtube:player-client=tv_embedded\"");
            }

            if (IsSoundCloudUrl(url))
            {
                args.Add($"--socket-timeout {SoundCloudSocketTimeoutSeconds}");
                args.Add($"--extractor-retries {SoundCloudRetryCount}");
                args.Add($"--retries {SoundCloudRetryCount}");
                args.Add("--retry-sleep extractor:2");
                args.Add("--retry-sleep http:2");
            }

            if (type == DownloadType.Audio)
            {
                args.Add("-x");
                args.Add($"--audio-format {format}");
                args.Add("--audio-quality 0");
            }
            else
            {
                args.Add(GetVideoFormatArg(format, quality));
                args.Add($"--merge-output-format {format}");
            }

            if (!string.IsNullOrWhiteSpace(outputFolder))
            {
                args.Add($"-o \"{outputFolder}\\%(title)s.%(ext)s\"");
            }

            return string.Join(" ", args);
        }

        public async Task UpdateYtDlpAsync(CancellationToken cancellationToken = default)
        {
            var toolsFolder = await EnsureToolsFolderAsync(cancellationToken).ConfigureAwait(false);
            var ytDlpPath = Path.Combine(toolsFolder, "yt-dlp.exe");
            var outputLines = new List<string>();

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = "-U",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = toolsFolder,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdoutTask = ReadProcessLinesAsync(process.StandardOutput, outputLines, cancellationToken);
            var stderrTask = ReadProcessLinesAsync(process.StandardError, outputLines, cancellationToken);

            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var updateError = string.Join("\n", outputLines.Where(line => !string.IsNullOrWhiteSpace(line)));
                throw new Exception(string.IsNullOrWhiteSpace(updateError)
                    ? LocalizationService.GetString("Error_UpdateYtDlpFailed")
                    : updateError);
            }
        }

        private async Task<string> EnsureToolsFolderAsync(CancellationToken cancellationToken)
        {
            var bundledToolsFolder = GetBundledToolsFolder();
            var writableToolsFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "yt-dlp");

            Directory.CreateDirectory(writableToolsFolder);

            await EnsureToolCopiedAsync(
                Path.Combine(bundledToolsFolder, "yt-dlp.exe"),
                Path.Combine(writableToolsFolder, "yt-dlp.exe"),
                cancellationToken).ConfigureAwait(false);

            await EnsureToolCopiedAsync(
                Path.Combine(bundledToolsFolder, "ffmpeg.exe"),
                Path.Combine(writableToolsFolder, "ffmpeg.exe"),
                cancellationToken).ConfigureAwait(false);

            return writableToolsFolder;
        }

        private string GetBundledToolsFolder()
        {
            var appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new DirectoryNotFoundException(LocalizationService.GetString("Error_AppFolderNotFound"));

            return Path.Combine(appFolder, "yt-dlp");
        }

        private static async Task EnsureToolCopiedAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            if (File.Exists(destinationPath))
            {
                return;
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(LocalizationService.Format("Error_RequiredFileMissing", Path.GetFileName(sourcePath)));
            }

            await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ReadProcessLinesAsync(StreamReader reader, List<string> outputLines, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    outputLines.Add(line);
                }
            }
        }

        private async Task ReadStandardOutputAsync(Process process, YtDlpProgressTracker progressTracker, Action<DownloadProgressInfo> onProgress)
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    break;
                }

                var progress = progressTracker.Parse(line);

                if (progress != null)
                {
                    onProgress(progress);
                }
            }
        }

        private async Task ReadStandardErrorAsync(Process process, YtDlpProgressTracker progressTracker, Action<DownloadProgressInfo> onProgress, List<string> errorLines)
        {
            while (true)
            {
                var line = await process.StandardError.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    break;
                }

                var progress = progressTracker.Parse(line);

                if (progress != null)
                {
                    onProgress(progress);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    errorLines.Add(line);
                }
            }
        }

        private string MapErrorMessage(List<string> errorLines)
        {
            var errorText = string.Join("\n", errorLines.Where(line => !string.IsNullOrWhiteSpace(line)));

            if (IsOutdatedYtDlpError(errorText))
            {
                throw new YtDlpUpdateRequiredException(
                    LocalizationService.GetString("Error_BundledYtDlpOutdated"));
            }

            if (IsVpnBlockedYoutubeError(errorText))
            {
                return LocalizationService.GetString("Error_VpnBlockedYoutube");
            }

            if (IsSoundCloudFormatUnavailableError(errorText))
            {
                return LocalizationService.GetString("Error_SoundCloudFormatUnavailable");
            }

            if (IsRequestedFormatUnavailableError(errorText))
            {
                return LocalizationService.GetString("Error_FormatUnavailable");
            }

            if (IsNetworkTimeoutError(errorText))
            {
                return IsSoundCloudError(errorText)
                    ? LocalizationService.GetString("Error_SoundCloudTimeout")
                    : LocalizationService.GetString("Error_NetworkTimeout");
            }

            return string.IsNullOrWhiteSpace(errorText)
                ? LocalizationService.GetString("Error_DownloadFailed")
                : NormalizeRawErrorMessage(errorText);
        }

        private bool IsVpnBlockedYoutubeError(string errorText)
        {
            if (string.IsNullOrWhiteSpace(errorText))
            {
                return false;
            }

            return errorText.Contains("Sign in to confirm you’re not a bot", StringComparison.OrdinalIgnoreCase)
                || errorText.Contains("Sign in to confirm you're not a bot", StringComparison.OrdinalIgnoreCase)
                || errorText.Contains("Use --cookies-from-browser", StringComparison.OrdinalIgnoreCase)
                || errorText.Contains("[youtube]", StringComparison.OrdinalIgnoreCase) && errorText.Contains("not a bot", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsOutdatedYtDlpError(string errorText)
        {
            if (string.IsNullOrWhiteSpace(errorText))
            {
                return false;
            }

            return errorText.Contains("The following content is not available on this app", StringComparison.OrdinalIgnoreCase)
                || errorText.Contains("Watch on the latest version of YouTube", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRequestedFormatUnavailableError(string errorText)
        {
            if (string.IsNullOrWhiteSpace(errorText))
            {
                return false;
            }

            return errorText.Contains("Requested format is not available", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSoundCloudFormatUnavailableError(string errorText)
        {
            return IsSoundCloudError(errorText) && IsRequestedFormatUnavailableError(errorText);
        }

        private bool IsNetworkTimeoutError(string errorText)
        {
            if (string.IsNullOrWhiteSpace(errorText))
            {
                return false;
            }

            return errorText.Contains("Read timed out", StringComparison.OrdinalIgnoreCase)
                || errorText.Contains("timed out", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSoundCloudError(string errorText)
        {
            if (string.IsNullOrWhiteSpace(errorText))
            {
                return false;
            }

            return errorText.Contains("[soundcloud]", StringComparison.OrdinalIgnoreCase)
                || errorText.Contains("soundcloud.com", StringComparison.OrdinalIgnoreCase)
                || errorText.Contains("api.soundcloud.com", StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeRawErrorMessage(string errorText)
        {
            var lines = errorText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count == 0)
            {
                return LocalizationService.GetString("Error_DownloadFailed");
            }

            if (lines[0].StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                lines[0] = lines[0][6..].TrimStart();
            }

            return string.Join("\n", lines);
        }

        private bool IsYoutubeUrl(string url)
        {
            return url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                || url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSoundCloudUrl(string url)
        {
            return url.Contains("soundcloud.com", StringComparison.OrdinalIgnoreCase)
                || url.Contains("snd.sc", StringComparison.OrdinalIgnoreCase)
                || url.Contains("api.soundcloud.com", StringComparison.OrdinalIgnoreCase);
        }

        private string GetVideoFormatArg(string format, VideoQuality quality)
        {
            string height = quality switch
            {
                VideoQuality.Q2160p => "2160",
                VideoQuality.Q1440p => "1440",
                VideoQuality.Q1080p => "1080",
                VideoQuality.Q720p => "720",
                VideoQuality.Q480p => "480",
                _ => null
            };

            // ===== MP4 =====
            if (format.Equals("mp4", StringComparison.OrdinalIgnoreCase))
            {
                if (height == null)
                    return "-f \"bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]\"";

                return $"-f \"bv*[ext=mp4][height<={height}]+ba[ext=m4a]/b[ext=mp4]\"";
            }

            // ===== WEBM =====
            if (format.Equals("webm", StringComparison.OrdinalIgnoreCase))
            {
                if (height == null)
                    return "-f \"bv*[ext=webm]+ba[acodec=opus]/b\"";

                return $"-f \"bv*[ext=webm][height<={height}]+ba[acodec=opus]/b\"";
            }

            // ===== MKV =====
            if (format.Equals("mkv", StringComparison.OrdinalIgnoreCase))
            {
                return "-f \"bv*+ba/b\"";
            }

            return "-f best";
        }

        public void KillCurrent()
        {
            try
            {
                if (_currentProcess != null && !_currentProcess.HasExited)
                {
                    _currentProcess.Kill(true);
                }
            }
            catch { }
        }

    }
}
