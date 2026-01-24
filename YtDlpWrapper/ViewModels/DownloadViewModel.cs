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

            _cts = new CancellationTokenSource();

            CurrentDownload.Progress = 0;
            CurrentDownload.Status = "Подготовка…";
            CurrentDownload.IsDownloading = true;

            try
            {
                await _ytDlp.DownloadAsync(cleanUrl,
                    DownloadType,
                    Format,
                    VideoQuality,
                    _settingsService.DownloadFolder,
                    progress =>
                    {
                        // ⚠️ UI thread
                        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            CurrentDownload.Progress = progress.Percent;
                            CurrentDownload.Status = $"Загрузка… {progress.Percent:0.0}%";
                        });
                    },
                    _cts.Token);

                if (_cts.IsCancellationRequested)
                    return;

                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    CurrentDownload.Progress = 100;
                    CurrentDownload.Status = "Готово";
                    CurrentDownload.IsDownloading = false;
                });
            }
            catch (OperationCanceledException)
            {
                // отмена
            }
            catch (System.Exception ex)
            {
                CurrentDownload.Status = "Ошибка";
                DownloadFailed?.Invoke(ex.Message);
            }
            finally
            {
                CurrentDownload.IsDownloading = false;
            }
        }

        public void Shutdown()
        {
            _cts?.Cancel();
            _ytDlp.KillCurrent();
        }
    }
}
