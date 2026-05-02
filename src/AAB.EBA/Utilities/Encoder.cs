using System.Numerics;

namespace AAB.EBA.Utilities;

public static class Encoder
{
    private const string _alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public static string Base58(byte[] data)
    {
        // if performance issues with this implementation, then just switch to BouncyCastle's

        int zeroCount = 0;
        while (zeroCount < data.Length && data[zeroCount] == 0)
            zeroCount++;
        
        byte[] hexWithZero = new byte[data.Length + 1];
        Array.Copy(data, 0, hexWithZero, 1, data.Length);
        BigInteger number = new([.. hexWithZero.Reverse()]);

        string result = "";
        while (number > 0)
        {
            number = BigInteger.DivRem(number, 58, out BigInteger remainder);
            result = _alphabet[(int)remainder] + result;
        }

        return new string('1', zeroCount) + result;
    }
}
