namespace SecretDrop.Models
{
    public class CreateSessionRequest
    {
        public string PublicKey { get; set; }
        public string CaptchaToken { get; set; }
    }
}
