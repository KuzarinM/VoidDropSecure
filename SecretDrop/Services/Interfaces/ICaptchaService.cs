namespace SecretDrop.Services.Interfaces
{
    public interface ICaptchaService
    {
        public Task<bool> VerifyToken(string token);
    }
}
