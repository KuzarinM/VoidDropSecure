using Microsoft.Extensions.Caching.Memory;
using SecretDrop.Models;
using SecretDrop.Services.Interfaces;

namespace SecretDrop.Services.Implementations
{
    public class DropSessionStore : IDropSessionStore
    {
        private readonly IMemoryCache _cache;

        // Сессия живет 10 минут. Если User A ушел - сессия умирает.
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

        public DropSessionStore(IMemoryCache cache) => _cache = cache;

        public string CreateSession(string publicKeyJson)
        {
            var id = Guid.NewGuid().ToString("N")[..12]; // Короткий ID
            var session = new DropSessionData
            {
                PublicKeyJson = publicKeyJson,
                IsCompleted = false
            };
            _cache.Set(id, session, _ttl);
            return id;
        }

        public DropSessionData? GetSession(string id)
        {
            _cache.TryGetValue(id, out DropSessionData? session);
            return session;
        }

        public void CompleteSession(string id, string fileId, string encryptedKey, string metaJson)
        {
            if (_cache.TryGetValue(id, out DropSessionData? session) && session != null)
            {
                session.IsCompleted = true;
                session.FileId = fileId;
                session.EncryptedFileKey = encryptedKey;
                session.MetaJson = metaJson;
                // Продлеваем жизнь сессии, чтобы User A успел забрать данные
                _cache.Set(id, session, TimeSpan.FromMinutes(5));
            }
        }
    }
}
