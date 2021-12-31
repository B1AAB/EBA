using System.Security.Cryptography;
using System.Text;

namespace BC2G
{
    internal class Utilities
    {
        // To be consistent with Bitcoin client. 
        public const int FractionalDigitsCount = 8;

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
    }
}
