namespace SecretDrop.Models
{
    public class InitRequest
    {
        public string Meta { get; set; }
        public int Ttl { get; set; }
        public int Limit { get; set; }
        public string? PasswordSalt { get; set; }
        public string? WrappedKey { get; set; }
    }
}
