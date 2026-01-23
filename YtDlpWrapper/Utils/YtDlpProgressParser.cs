using System.Globalization;
using System.Text.RegularExpressions;

public class YtDlpProgress
{
    public double Percent { get; set; }
    public string Text { get; set; }
}

public static class YtDlpProgressParser
{
    private static readonly Regex _regex = new(@"\[download\]\s+(\d+(\.\d+)?)%");

    public static YtDlpProgress Parse(string line)
    {
        var m = _regex.Match(line);
        if (!m.Success) return null;

        return new YtDlpProgress
        {
            Percent = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
            Text = line
        };
    }
}
