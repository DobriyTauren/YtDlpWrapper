using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using YtDlpWrapper.Services;
using YtDlpWrapper.ViewModels;
using YtDlpWrapper.Utils;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;


namespace YtDlpWrapper
{
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }

        public static IServiceProvider Services { get; private set; }

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var services = new ServiceCollection();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<DownloadViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // Services
            services.AddSingleton<YtDlpService>();
            services.AddSingleton<SettingsService>();

            // Window
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            MainWindow = Services.GetRequiredService<MainWindow>();

            MainWindow.Activate();
        }
    }
}
