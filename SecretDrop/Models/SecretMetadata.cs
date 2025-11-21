namespace SecretDrop.Models
{
    public class SecretMetadata
    {
        public string MetaJson { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int MaxDownloads { get; set; }
        public int CurrentDownloads { get; set; }
        public bool HasPassword { get; set; }      // Есть ли пароль?
        public string? PasswordSalt { get; set; }  // Соль для PBKDF2
        public string? WrappedKey { get; set; }    // Файловый ключ, зашифрованный паролем
    }
}
