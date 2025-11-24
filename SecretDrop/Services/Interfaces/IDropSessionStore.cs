using SecretDrop.Models;

namespace SecretDrop.Services.Interfaces
{
    public interface IDropSessionStore
    {
        string CreateSession(string publicKeyJson);
        DropSessionData? GetSession(string id);
        void CompleteSession(string id, string fileId, string encryptedKey, string metaJson);
    }
}
