using System;

namespace YtDlpWrapper.Utils
{
    public sealed class YtDlpUpdateRequiredException : Exception
    {
        public YtDlpUpdateRequiredException(string message) : base(message)
        {
        }
    }
}
