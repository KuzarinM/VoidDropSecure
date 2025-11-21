namespace SecretDrop.Services
{
    public interface ISecretStore
    {
        Task<string> SaveSecretAsync(string encryptedData, TimeSpan ttl);
        Task<string?> GetAndBurnAsync(string id);
    }
}
