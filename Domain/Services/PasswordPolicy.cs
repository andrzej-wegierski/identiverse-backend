using System.Text.RegularExpressions;
using Domain.Abstractions;
using Domain.Exceptions;

namespace Domain.Services;

public class PasswordPolicy : IPasswordPolicy
{
    public void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new ValidationException("Password must be at least 10 characters long.");
        if (!Regex.IsMatch(password, @"[A-Z]"))
            throw new ValidationException("Password must contain an uppercase letter.");
        if (!Regex.IsMatch(password, @"[a-z]"))
            throw new ValidationException("Password must contain a lowercase letter.");
        if (!Regex.IsMatch(password, @"\d"))
            throw new ValidationException("Password must contain a digit.");
        if (!Regex.IsMatch(password, @"[^A-Za-z0-9]"))
            throw new ValidationException("Password must contain a non-alphanumeric character.");
    }
}
