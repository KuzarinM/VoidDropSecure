namespace SecretDrop.Configurations
{
    public class AppOptions
    {
        // Название секции в appsettings.json / ENV
        public const string Section = "AppConfig";

        // Путь к папке с файлами (В Docker это будет Volume)
        public string StoragePath { get; set; } = "SecretData";

        // Как часто запускать чистильщик (в минутах)
        public int CleanupIntervalMinutes { get; set; } = 10;

        // Максимальный размер загрузки в байтах (по умолчанию 5 ГБ)
        public long MaxUploadSizeBytes { get; set; } = 5L * 1024 * 1024 * 1024;
    }
}
