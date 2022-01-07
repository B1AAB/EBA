namespace BC2G.Model
{
    internal interface IBase64Serializable
    {
        string ToBase64String();
        void FromBase64String(string base64String);
    }
}
