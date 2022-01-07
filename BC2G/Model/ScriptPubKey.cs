using System.Text.Json.Serialization;

namespace BC2G.Model
{
    public class ScriptPubKey : BasePaymentType, IBase64Serializable
    {
        [JsonPropertyName("asm")]
        public string Asm { get; set; } = string.Empty;

        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        public override ScriptType ScriptType
        {
            get
            {
                return
                    Enum.TryParse(
                        Type, ignoreCase:true, 
                        out ScriptType scriptType)
                    ? scriptType : ScriptType.Unknown;
            }
        }

        public override string GetAddress()
        {
            return Address;
        }

        public string ToBase64String()
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Asm);
                writer.Write(Hex);
                writer.Write(Address);
                writer.Write(Type);
            }
            return Convert.ToBase64String(stream.ToArray());
        }
        public void FromBase64String(string base64String)
        {
            throw new NotImplementedException();
        }
    }
}
