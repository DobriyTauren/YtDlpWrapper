using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using YtDlpWrapper.Services;
using YtDlpWrapper.Utils;
using YtDlpWrapper.ViewModels;
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
            Services = ConfigureServices();
            MainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow.Activate();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<DownloadViewModel>();
            services.AddSingleton<SettingsViewModel>();

            services.AddSingleton<YtDlpService>();
            services.AddSingleton<SettingsService>();

            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }
    }
}
