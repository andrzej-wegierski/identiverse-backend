namespace Domain.Abstractions;

public interface IPasswordPolicy
{
    void Validate(string password);
}
