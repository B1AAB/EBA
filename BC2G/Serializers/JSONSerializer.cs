using System.Text.Json;

namespace BC2G.Serializers
{
    public class JsonSerializer<T>
        where T : new()
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = false
        };

        public static async Task<T> DeserializeAsync(string path)
        {
            T obj = new();
            if (!File.Exists(path))
                await SerializeAsync(obj, path);

            using var stream = File.Open(path, FileMode.Open);
            obj = await JsonSerializer.DeserializeAsync<T>(stream, _options) ?? new T();

            return obj;
        }

        public static async Task SerializeAsync(T obj, string path)
        {
            using var stream = File.Open(path, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, obj, _options);
        }
    }
}
