namespace BC2G.Utilities;

internal class Helpers
{
    // Bitcoin clinet has 8 fractional points, add a few
    // bases to account for numbers smaller than that
    // as a result of computations.
    public const int FractionalDigitsCount = 12;

    public static readonly char[] chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

    /// <summary>
    /// Generates a cryptographically-safe random string. 
    /// 
    /// Implemented based-on: 
    /// <see cref="https://stackoverflow.com/a/1344255/947889"/>
    /// </summary>
    /// <param name="length">Number of characters in the generated string.</param>
    /// <returns></returns>
    public static string GetRandomString(int length)
    {
        byte[] data = new byte[4 * length];
        using (var crypto = RandomNumberGenerator.Create())
            crypto.GetBytes(data);

        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % chars.Length;

            result.Append(chars[idx]);
        }

        return result.ToString();
    }

    public static long BTC2Satoshi(double btc)
    {
        return Round(BitcoinAgent.Coin * btc);
    }

    public static double Satoshi2BTC(long satoshi)
    {
        return satoshi / (double)BitcoinAgent.Coin;
    }

    public static long Round(double input)
    {
        // Read the following post on the motivation behind this rounding.
        // https://stackoverflow.com/q/588004/947889
        //
        // The "round half way from zero" method is chose to match 
        // the behavior of the Bitcoin client.

        return (long)Math.Round(input, MidpointRounding.AwayFromZero);
    }

    public static double ThreadsafeAdd(ref double location, double value)
    {
        // This is implemented based on the following Stackoverflow answer.
        // https://stackoverflow.com/a/16893641/947889

        if (double.IsNaN(location))
        {
            // TODO: better handle this case.
            throw new NotImplementedException("Cannot thread-safe add to NaN.");
        }

        double newCurrentValue = location; // non-volatile read, so may be stale

        // TODO: the loop should not be infinite, put a max iteration counter.
        while (true)
        {
            double currentValue = newCurrentValue;
            double newValue = currentValue + value;
            newCurrentValue = Interlocked.CompareExchange(ref location, newValue, currentValue);
            if (newCurrentValue == currentValue)
                return newValue;
        }
    }

    public static uint ThreadsafeAdd(ref uint location, uint value)
    {
        // This is implemented based on the following Stackoverflow answer.
        // https://stackoverflow.com/a/16893641/947889

        uint newCurrentValue = location; // non-volatile read, so may be stale
        while (true)
        {
            uint currentValue = newCurrentValue;
            uint newValue = currentValue + value;
            newCurrentValue = Interlocked.CompareExchange(ref location, newValue, currentValue);
            if (newCurrentValue == currentValue)
                return newValue;
        }
    }

    public static long ThreadsafeAdd(ref long location, long value)
    {
        // This is implemented based on the following Stackoverflow answer.
        // https://stackoverflow.com/a/16893641/947889

        long newCurrentValue = location; // non-volatile read, so may be stale
        while (true)
        {
            long currentValue = newCurrentValue;
            long newValue = currentValue + value;
            newCurrentValue = Interlocked.CompareExchange(ref location, newValue, currentValue);
            if (newCurrentValue == currentValue)
                return newValue;
        }
    }

    public static string GetSHA256(string input)
    {
        var builder = new StringBuilder();
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            // x2: format string as hexadecimal
        }
        return builder.ToString();
    }

    public static void CsvSerialize<T>(
        IEnumerable<T[]> data,
        string filename,
        IEnumerable<string>? header = null,
        char delimiter = '\t',
        bool append = false)
    {
        var addHeader = header != null ? true : false;
        if (File.Exists(filename) && append)
            addHeader = false;

        using var writter = new StreamWriter(filename, append: append);
        if (addHeader)
            writter.WriteLine(string.Join(delimiter, header));
        foreach (var item in data)
            writter.WriteLine(string.Join(delimiter, item));
    }

    public static bool AssertPathEqual(string? pathX, string? pathY)
    {
        if (pathX == null || pathY == null)
            return false;

        return ComparePath(pathX, pathY) == 0;
    }
    public static int ComparePath(string? pathX, string? pathY)
    {
        return string.Compare(
            pathX?.TrimEnd(Path.DirectorySeparatorChar),
            pathY?.TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.InvariantCultureIgnoreCase);
    }

    public static string ToAbsPath(string filename, string workingDir)
    {
        if (filename == Path.GetFileName(filename))
            return Path.Join(workingDir, filename);
        else
            return filename;
    }

    public static string GetTimestamp()
    {
        return $"{DateTime.Now:yyyyMMddHHmmssffff}";
    }

    public static string GetUnixTimeSeconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    public static string GetEtInSeconds(TimeSpan elapsed)
    {
        return elapsed.TotalSeconds.ToString("F3");
    }

    public static double GetMedian(IEnumerable<int> data)
    {
        if (!data.Any())
            return double.NaN;

        var count = data.Count();
        var sortedData = data.OrderBy(x => x);

        if (count % 2 == 0)
        {
            var middle = count / 2;
            return (sortedData.ElementAt(middle - 1) + sortedData.ElementAt(middle)) / 2.0;
        }
        else
        {
            return sortedData.ElementAt(count / 2);
        }
    }

    public static double GetMedian(IEnumerable<long> data)
    {
        if (!data.Any())
            return double.NaN;

        var count = data.Count();
        var sortedData = data.OrderBy(x => x);

        if (count % 2 == 0)
        {
            var middle = count / 2;
            return (sortedData.ElementAt(middle - 1) + sortedData.ElementAt(middle)) / 2.0;
        }
        else
        {
            return sortedData.ElementAt(count / 2);
        }
    }

    // TODO: merge this and the above overload to a single method.
    public static double GetMedian(IEnumerable<double> data)
    {
        if (!data.Any())
            return double.NaN;

        var count = data.Count();
        var sortedData = data.OrderBy(x => x);

        if (count % 2 == 0)
        {
            var middle = count / 2;
            return (sortedData.ElementAt(middle - 1) + sortedData.ElementAt(middle)) / 2.0;
        }
        else
        {
            return sortedData.ElementAt(count / 2);
        }
    }

    public static double GetVariance(IEnumerable<long> data)
    {
        if (data.Count() < 2)
            return double.NaN;

        var mean = data.Average();
        var sumOfSquares = data.Sum(x => Math.Pow(x - mean, 2));
        return sumOfSquares / (data.Count() - 1);
    }

    public static double GetVariance(IEnumerable<int> data)
    {
        if (data.Count() < 2)
            return double.NaN;

        var mean = data.Average();
        var sumOfSquares = data.Sum(x => Math.Pow(x - mean, 2));
        return sumOfSquares / (data.Count() - 1);
    }

    // TODO: merge this and the above overload to a single method.
    public static double GetVariance(IEnumerable<double> data)
    {
        if (data.Count() < 2)
            return double.NaN;

        var mean = data.Average();
        var sumOfSquares = data.Sum(x => Math.Pow(x - mean, 2));
        return sumOfSquares / (data.Count() - 1);
    }
}
