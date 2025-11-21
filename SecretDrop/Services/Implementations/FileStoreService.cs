using Microsoft.Extensions.Options;
using SecretDrop.Configurations;
using SecretDrop.Models;
using SecretDrop.Services.Interfaces;
using System.Text.Json;

namespace SecretDrop.Services.Implementations
{
    public class FileStoreService : ISecretFileStore
    {
        private readonly string _storagePath;
        private readonly object _ioLock = new(); // Простой лок для синхронизации записи JSON

        public FileStoreService(IOptions<AppOptions> options)
        {
            _storagePath = options.Value.StoragePath;
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
        }

        private string GetMetaPath(string id) => Path.Combine(_storagePath, id + ".json");
        private string GetDataPath(string id) => Path.Combine(_storagePath, id + ".dat");

        public string InitUpload(string metadataJson, int ttlMinutes, int downloadLimit, string? pwdSalt, string? wrappedKey)
        {
            var id = Guid.NewGuid().ToString("N");

            var meta = new SecretMetadata
            {
                MetaJson = metadataJson,
                ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes),
                MaxDownloads = downloadLimit,
                CurrentDownloads = 0,

                // Сохраняем параметры парольной защиты
                HasPassword = !string.IsNullOrEmpty(wrappedKey),
                PasswordSalt = pwdSalt,
                WrappedKey = wrappedKey
            };

            // 1. Сохраняем .json на диск
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(meta);
            File.WriteAllBytes(GetMetaPath(id), jsonBytes);
            File.Create(GetDataPath(id)).Close();

            return id;
        }

        public async Task AppendDataAsync(string id, Stream dataStream)
        {
            var path = GetDataPath(id);
            // Если метаданных нет - значит файла нет или он удален
            if (!File.Exists(GetMetaPath(id))) throw new FileNotFoundException("Secret not found or expired");

            // Дописываем в .dat
            using var fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            await dataStream.CopyToAsync(fs);
        }

        public SecretMetadata? GetMeta(string id)
        {
            var metaPath = GetMetaPath(id);
            if (!File.Exists(metaPath)) return null;

            try
            {
                var json = File.ReadAllText(metaPath);
                var meta = JsonSerializer.Deserialize<SecretMetadata>(json);

                // Проверка срока годности
                if (meta == null || DateTime.UtcNow > meta.ExpiresAt)
                {
                    // Можно лениво удалить прямо сейчас, но оставим воркеру
                    return null;
                }
                return meta;
            }
            catch
            {
                return null; // Ошибка чтения JSON (битый файл)
            }
        }

        public FileStream? GetFileStream(string id)
        {
            var metaPath = GetMetaPath(id);
            var dataPath = GetDataPath(id);

            // Блокируем, чтобы два юзера одновременно не скачали последний раз
            lock (_ioLock)
            {
                if (!File.Exists(metaPath)) return null;

                SecretMetadata? meta;
                try
                {
                    var json = File.ReadAllText(metaPath);
                    meta = JsonSerializer.Deserialize<SecretMetadata>(json);
                }
                catch { return null; }

                if (meta == null || DateTime.UtcNow > meta.ExpiresAt) return null;

                // Логика счетчика
                meta.CurrentDownloads++;

                bool shouldBurn = false;
                if (meta.CurrentDownloads >= meta.MaxDownloads)
                {
                    shouldBurn = true;
                    // Удаляем .json (метаданные) СРАЗУ.
                    // Больше никто не сможет получить информацию о файле (GetMeta вернет 404).
                    // Сам .dat файл еще нужен для текущего скачивания.
                    File.Delete(metaPath);
                }
                else
                {
                    // Обновляем счетчик в JSON файле
                    var updatedJson = JsonSerializer.SerializeToUtf8Bytes(meta);
                    File.WriteAllBytes(metaPath, updatedJson);
                }

                if (!File.Exists(dataPath)) return null;

                var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
                if (shouldBurn)
                {
                    // Механизм самоуничтожения .dat файла после закрытия потока
                    fileOptions |= FileOptions.DeleteOnClose;
                }

                return new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, fileOptions);
            }
        }

        public int DeleteExpired()
        {
            int count = 0;
            var now = DateTime.UtcNow;

            // Ищем все .json файлы в папке
            var metaFiles = Directory.GetFiles(_storagePath, "*.json");

            foreach (var metaFile in metaFiles)
            {
                try
                {
                    // Читаем только expiry дату, чтобы не грузить лишнее
                    // Но проще прочитать весь json, он маленький
                    var json = File.ReadAllText(metaFile);
                    var meta = JsonSerializer.Deserialize<SecretMetadata>(json);

                    if (meta != null && now > meta.ExpiresAt)
                    {
                        // Удаляем JSON
                        File.Delete(metaFile);

                        // Удаляем DAT (если есть)
                        // metaFile = ".../abc.json" -> id = "abc"
                        var id = Path.GetFileNameWithoutExtension(metaFile);
                        var dataPath = Path.Combine(_storagePath, id + ".dat");
                        if (File.Exists(dataPath))
                        {
                            File.Delete(dataPath);
                        }
                        count++;
                    }
                }
                catch
                {
                    // Если JSON битый - удаляем его как мусор
                    try { File.Delete(metaFile); } catch { }
                }
            }

            // Дополнительная зачистка: удаление "сиротских" .dat файлов, у которых нет .json
            // (Например, сервер упал ровно посередине удаления)
            var dataFiles = Directory.GetFiles(_storagePath, "*.dat");
            foreach (var dataFile in dataFiles)
            {
                var id = Path.GetFileNameWithoutExtension(dataFile);
                var metaPath = Path.Combine(_storagePath, id + ".json");
                if (!File.Exists(metaPath))
                {
                    // Файл данных есть, а меты нет -> Мусор.
                    // Но осторожно! Вдруг мы прямо сейчас пишем этот файл (Upload)?
                    // Проверим время создания/модификации. Если старый (> 1 часа) и без меты - удаляем.
                    var info = new FileInfo(dataFile);
                    if (info.LastWriteTimeUtc < DateTime.UtcNow.AddHours(-1))
                    {
                        try { File.Delete(dataFile); } catch { }
                    }
                }
            }

            return count;
        }
    }
}
