using System.Text.Json.Serialization;

namespace BC2G.Model
{
    public class ScriptPubKey : BasePaymentType
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
    }
}
