namespace BC2G.Serializers
{
    public class JsonSerializer<T>
        where T : new()
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = false,
            IncludeFields = true
        };

        public static async Task<T> DeserializeAsync(string path, CancellationToken cT)
        {
            T obj = new();
            if (!File.Exists(path))
                await SerializeAsync(obj, path, cT);

            using var stream = File.Open(path, FileMode.Open);
            obj = await JsonSerializer.DeserializeAsync<T>(stream, _options, cT) ?? new T();

            return obj;
        }

        public static async Task SerializeAsync(T obj, string path, CancellationToken cT)
        {
            using var stream = File.Open(path, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, obj, _options, cT);
        }
    }
}
