using Microsoft.Extensions.Caching.Memory;
using SecretDrop.Services.Interfaces;

namespace SecretDrop.Services.Implementations
{
    public class SecretStoreService : ISecretStore
    {
        private readonly IMemoryCache _cache;

        public SecretStoreService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<string> SaveSecretAsync(string encryptedData, TimeSpan ttl)
        {
            var id = Guid.NewGuid().ToString("N"); // Генерируем ID
            _cache.Set(id, encryptedData, ttl);    // Сохраняем с таймером жизни
            return Task.FromResult(id);
        }

        public Task<string?> GetAndBurnAsync(string id)
        {
            if (_cache.TryGetValue(id, out string? data))
            {
                _cache.Remove(id);
                return Task.FromResult(data);
            }
            return Task.FromResult<string?>(null);
        }
    }
}
