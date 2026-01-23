using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using YtDlpWrapper.Models;

namespace YtDlpWrapper.Utils
{
    public class YtDlpService
    {
        private Process _currentProcess;
        private string _currentOutputFile;

        public async Task DownloadAsync(string url, DownloadType downloadType, string format, VideoQuality quality,
            string outputFolder, Action<YtDlpProgress> onProgress, CancellationToken cancellationToken = default)
        {
            var appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var ytDlpPath = Path.Combine(appFolder, "yt-dlp", "yt-dlp.exe");

            if (!File.Exists(ytDlpPath))
                throw new FileNotFoundException("yt-dlp.exe не найден");

            var arguments = BuildArguments(
                url,
                downloadType,
                format,
                quality,
                outputFolder
            );

            var psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _currentProcess = new Process { StartInfo = psi };
                _currentProcess.Start();

                cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!_currentProcess.HasExited)
                            _currentProcess.Kill(true);
                    }
                    catch { }
                });

                var errorLines = new List<string>();

                var stdoutTask = Task.Run(async () =>
                {
                    while (!_currentProcess.StandardOutput.EndOfStream)
                    {
                        var line = await _currentProcess.StandardOutput.ReadLineAsync();

                        TryExtractOutputPath(line);

                        var progress = YtDlpProgressParser.Parse(line);
                        if (progress != null)
                            onProgress(progress);
                    }
                });

                var stderrTask = Task.Run(async () =>
                {
                    while (!_currentProcess.StandardError.EndOfStream)
                    {
                        var line = await _currentProcess.StandardError.ReadLineAsync();
                        errorLines.Add(line);
                    }
                });

                await Task.WhenAll(stdoutTask, stderrTask);
                await _currentProcess.WaitForExitAsync();

                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);

                if (_currentProcess.ExitCode != 0)
                    throw new Exception(string.Join("\n", errorLines));
            }
            finally
            {
                try
                {
                    _currentProcess?.Dispose();
                }
                catch { }

                if (cancellationToken.IsCancellationRequested && !string.IsNullOrWhiteSpace(_currentOutputFile))
                {
                    TryDelete(_currentOutputFile);
                    TryDelete(_currentOutputFile + ".part");
                }

                _currentProcess = null;
                _currentOutputFile = null;
            }

        }

        private void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }

        private void TryExtractOutputPath(string line)
        {
            const string dest = "Destination:";
            const string merge = "Merging formats into";

            if (line.Contains(dest))
            {
                _currentOutputFile = line
                    .Substring(line.IndexOf(dest) + dest.Length)
                    .Trim();
            }
            else if (line.Contains(merge))
            {
                var start = line.IndexOf('"') + 1;
                var end = line.LastIndexOf('"');
                if (start > 0 && end > start)
                    _currentOutputFile = line[start..end];
            }
        }


        private string BuildArguments(string url, DownloadType type, string format, VideoQuality quality, string outputFolder)
        {
            var args = new List<string>
            {
                $"\"{url}\"",
                "--newline",
                "--no-playlist"
            };

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
