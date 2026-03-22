using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using YtDlpWrapper.Models;
using YtDlpWrapper.Services;
using YtDlpWrapper.Utils;

namespace YtDlpWrapper.ViewModels
{
    public class DownloadViewModel : ObservableObject
    {
        private readonly YtDlpService _ytDlp;
        private readonly SettingsService _settingsService;

        private CancellationTokenSource _cts;
        private int _downloadOperationId;

        private double _progress;
        private int _selectedTypeIndex;

        public event Action<string> DownloadFailed;

        public DownloadItem CurrentDownload { get; } = new();

        public ICommand DownloadCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand VideoButtonCommand => new RelayCommand(() =>
        {
            DownloadType = DownloadType.Video;
        });

        public ICommand AudioButtonCommand => new RelayCommand(() =>
        {
            DownloadType = DownloadType.Audio;
        });


        public bool IsVideo => DownloadType == DownloadType.Video;

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public int SelectedTypeIndex
        {
            get => _selectedTypeIndex;
            set
            {
                if (SetProperty(ref _selectedTypeIndex, value))
                {

                    DownloadType = value == 0 ? DownloadType.Video : DownloadType.Audio;
                }
            }
        }

        public DownloadViewModel(YtDlpService service, SettingsService settingsService)
        {
            _ytDlp = service;
            _settingsService = settingsService;
            
            _selectedTypeIndex = DownloadType == DownloadType.Video ? 0 : 1;

            DownloadCommand = new RelayCommand(StartDownload);
            CancelCommand = new RelayCommand(CancelDownload);
        }

        public ObservableCollection<string> VideoFormats { get; } = new() { "mp4", "webm", "mkv" };
        public ObservableCollection<string> AudioFormats { get; } = new() { "mp3", "m4a", "opus" };
        public ObservableCollection<string> Formats => IsVideo ? VideoFormats : AudioFormats;
        public ObservableCollection<string> TypeOptions { get; } = new() { "🎬 Видео", "🎵 Аудио" };

        public ObservableCollection<VideoQuality> VideoQualities { get; } =
            new()
            {
                VideoQuality.Best,
                VideoQuality.Q2160p,
                VideoQuality.Q1440p,
                VideoQuality.Q1080p,
                VideoQuality.Q720p,
                VideoQuality.Q480p
            };

        public DownloadType DownloadType
        {
            get => _settingsService.DownloadType;
            set
            {
                _settingsService.DownloadType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsVideo));
                OnPropertyChanged(nameof(Formats));

                Format = Formats.FirstOrDefault();
            }
        }

        public string Format
        {
            get => _settingsService.Format;
            set
            {
                _settingsService.Format = value;
                OnPropertyChanged();
            }
        }

        public VideoQuality VideoQuality
        {
            get => _settingsService.VideoQuality;
            set
            {
                _settingsService.VideoQuality = value;
                OnPropertyChanged();
            }
        }

        private void CancelDownload()
        {
            Interlocked.Increment(ref _downloadOperationId);
            _cts?.Cancel();

            CurrentDownload.Status = "Отменено";
            CurrentDownload.IsDownloading = false;
            CurrentDownload.Progress = 0;
        }

        private async void StartDownload()
        {
            var cleanUrl = CurrentDownload.Url?.Trim();

            if (string.IsNullOrWhiteSpace(cleanUrl))
            {
                DownloadFailed?.Invoke("Введите корректную ссылку.");
                return;
            }

            if (!System.Uri.TryCreate(cleanUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                DownloadFailed?.Invoke("Некорректный URL.");
                return;
            }

            var downloadPlaylist = IsPlaylistUrl(uri);

            _cts = new CancellationTokenSource();
            var operationId = Interlocked.Increment(ref _downloadOperationId);

            CurrentDownload.Progress = 0;
            CurrentDownload.Status = "Подготовка…";
            CurrentDownload.IsDownloading = true;

            try
            {
                await _ytDlp.DownloadAsync(cleanUrl,
                    DownloadType,
                    Format,
                    VideoQuality,
                    downloadPlaylist,
                    _settingsService.DownloadFolder,
                    progress =>
                    {
                        if (!IsCurrentOperation(operationId) || _cts.IsCancellationRequested)
                        {
                            return;
                        }

                        // ⚠️ UI thread
                        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (!IsCurrentOperation(operationId) || _cts.IsCancellationRequested)
                            {
                                return;
                            }

                            CurrentDownload.Progress = progress.Percent;
                            CurrentDownload.Status = DownloadProgressStatusBuilder.Build(progress);
                        });
                    },
                    _cts.Token);

                if (!IsCurrentOperation(operationId) || _cts.IsCancellationRequested)
                    return;

                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    if (!IsCurrentOperation(operationId) || _cts.IsCancellationRequested)
                    {
                        return;
                    }

                    CurrentDownload.Progress = 100;
                    CurrentDownload.Status = "Готово";
                    CurrentDownload.IsDownloading = false;
                });
            }
            catch (OperationCanceledException)
            {
                if (IsCurrentOperation(operationId))
                {
                    CurrentDownload.Status = "Отменено";
                    CurrentDownload.Progress = 0;
                }
            }
            catch (System.Exception ex)
            {
                if (!IsCurrentOperation(operationId))
                {
                    return;
                }

                CurrentDownload.Status = "Ошибка";
                DownloadFailed?.Invoke(ex.Message);
            }
            finally
            {
                if (IsCurrentOperation(operationId))
                {
                    CurrentDownload.IsDownloading = false;
                }
            }
        }

        private bool IsCurrentOperation(int operationId)
        {
            return operationId == _downloadOperationId;
        }

        private static bool IsPlaylistUrl(Uri uri)
        {
            var host = uri.Host.ToLowerInvariant();
            var absolutePath = uri.AbsolutePath.ToLowerInvariant();
            var query = uri.Query.ToLowerInvariant();

            if (host.Contains("youtube.com") || host.Contains("youtu.be"))
            {
                return query.Contains("list=");
            }

            return absolutePath.Contains("/playlist")
                || absolutePath.Contains("/sets/")
                || absolutePath.Contains("/album/")
                || absolutePath.Contains("/mix/")
                || absolutePath.Contains("/collection/");
        }

        public void Shutdown()
        {
            _cts?.Cancel();
            _ytDlp.KillCurrent();
        }
    }
}
