using SecretDrop.Models;

namespace SecretDrop.Services.Interfaces
{
    public interface ISecretFileStore
    {
        // Инициализация загрузки
        string InitUpload(string metadataJson, int ttlMinutes, int downloadLimit, string? pwdSalt, string? wrappedKey);

        // Добавление куска данных
        Task AppendDataAsync(string id, Stream dataStream);

        // Получение метаданных (только чтение, счетчик НЕ меняется)
        // Используется, чтобы JS мог получить IV и имя файла перед скачиванием
        SecretMetadata? GetMeta(string id);

        // Получение файла (Здесь происходит увеличение счетчика и удаление при лимите)
        FileStream? GetFileStream(string id);

        // Очистка устаревших файлов (для фонового процесса)
        int DeleteExpired();
    }
}
