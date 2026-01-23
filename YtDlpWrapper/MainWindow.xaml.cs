using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;
using WinRT.Interop;
using YtDlpWrapper.Models;
using YtDlpWrapper.ViewModels;
using YtDlpWrapper.Views;
using AppWindow = Microsoft.UI.Windowing.AppWindow;


namespace YtDlpWrapper
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow _appWindow;

        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            RootGrid.DataContext = vm;

            DownloadFrame.Navigate(typeof(DownloadPage));
            SettingsFrame.Navigate(typeof(SettingsPage));

            InitializeWindow(vm.Window);

            this.Closed += MainWindow_Closed;
        }

        private void InitializeWindow(WindowOptions options)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            _appWindow.SetIcon("Assets/icon.ico");

            _appWindow.Resize(new Windows.Graphics.SizeInt32(
                options.Width,
                options.Height));

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                //presenter.PreferredMinimumWidth = options.MinWidth;
                //presenter.PreferredMinimumHeight = options.MinHeight;

                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }

            _appWindow.SetPresenter(AppWindowPresenterKind.Default);

            if (_appWindow.TitleBar != null)
            {
                var tb = _appWindow.TitleBar;

                tb.BackgroundColor = Color.FromArgb(255, 32, 32, 32);
                tb.InactiveBackgroundColor = Color.FromArgb(255, 28, 28, 28);

                tb.ForegroundColor = Colors.White;
                tb.InactiveForegroundColor = Colors.Gray;

                tb.ButtonBackgroundColor = Color.FromArgb(255, 32, 32, 32);
                tb.ButtonForegroundColor = Colors.White;

                tb.ButtonInactiveBackgroundColor = Color.FromArgb(255, 24, 24, 24);
                tb.ButtonInactiveForegroundColor = Color.FromArgb(255, 96, 96, 96);

                tb.ButtonHoverBackgroundColor = Color.FromArgb(255, 60, 60, 60);
                tb.ButtonPressedBackgroundColor = Color.FromArgb(255, 80, 80, 80);
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (Content is FrameworkElement root &&
                root.DataContext is DownloadViewModel vm)
            {
                vm.Shutdown();
            }
        }
    }
}
