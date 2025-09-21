using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BC2G.Serializers;

public static class ArraySerializer
{
    public static void Serialize<T>(T[] items, string filename) where T : struct
    {
        var bytes = MemoryMarshal.Cast<T, byte>(items);
        using var stream = File.Open(filename, FileMode.Create);
        stream.Write(bytes);
    }

    public static T[] Deserialize<T>(string filename) where T : struct
    {
        var items = Array.Empty<T>();

        if (!File.Exists(filename))
            return items;

        using (var stream = File.OpenRead(filename))
        {
            int len = checked((int)(stream.Length / Unsafe.SizeOf<T>())), read;
            items = new T[len];
            var bytes = MemoryMarshal.Cast<T, byte>(items);
            while (!bytes.IsEmpty && (read = stream.Read(bytes)) > 0)
                bytes = bytes[read..];
        }
        return items;
    }
}
