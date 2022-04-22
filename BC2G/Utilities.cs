using System.Security.Cryptography;
using System.Text;

namespace BC2G
{
    internal class Utilities
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

        public static double Round(double input)
        {
            // Regarding the motivation behind this, read the following:
            // https://stackoverflow.com/q/588004/947889
            return Math.Round(input, digits: FractionalDigitsCount);
        }

        public static double ThreadsafeAdd(ref double location, double value)
        {
            // This is implemented based on the following Stackoverflow answer.
            // https://stackoverflow.com/a/16893641/947889

            double newCurrentValue = location; // non-volatile read, so may be stale
            while (true)
            {
                double currentValue = newCurrentValue;
                double newValue = Round(currentValue + value);
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
    }
}
