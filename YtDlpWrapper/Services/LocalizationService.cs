using Microsoft.Windows.ApplicationModel.Resources;
using System.Globalization;

namespace YtDlpWrapper.Services
{
    public static class LocalizationService
    {
        private static readonly ResourceLoader Loader = new();

        public static string GetString(string resourceKey)
        {
            var value = Loader.GetString(resourceKey);
            return string.IsNullOrWhiteSpace(value) ? resourceKey : value;
        }

        public static string GetOptionalString(string resourceKey)
        {
            return Loader.GetString(resourceKey);
        }

        public static string Format(string resourceKey, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, GetString(resourceKey), args);
        }
    }
}
