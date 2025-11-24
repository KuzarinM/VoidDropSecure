namespace SecretDrop.Models
{
    public class DropSessionData
    {
        public string PublicKeyJson { get; set; } // Публичный RSA ключ получателя
        public bool IsCompleted { get; set; }
        public string? EncryptedFileKey { get; set; } // AES ключ, зашифрованный RSA
        public string? FileId { get; set; }           // ID загруженного файла
        public string? MetaJson { get; set; }         // Метаданные (имя, тип)
    }
}
