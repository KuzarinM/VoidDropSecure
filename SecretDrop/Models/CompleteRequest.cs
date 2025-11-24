namespace SecretDrop.Models
{
    public class CompleteRequest
    {
        public string SessionId { get; set; }
        public string FileId { get; set; }
        public string EncryptedKey { get; set; }
        public string MetaJson { get; set; }
    }
}
