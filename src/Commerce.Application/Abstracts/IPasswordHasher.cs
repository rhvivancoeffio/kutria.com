namespace Commerce.Application.Abstracts;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
