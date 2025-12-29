namespace Domain.Models;

public class PasswordOptions
{
    public int Iterations { get; set; } = 100_000;
    public int SaltSize { get; set; } = 16; // bytes
    public int KeySize { get; set; } = 32;  // bytes
    public string HashAlgorithm { get; set; } = "SHA256"; // SHA256|SHA512
}
