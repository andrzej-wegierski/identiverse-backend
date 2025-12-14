namespace Domain.Abstractions;

public interface ICurrentUserContext
{ 
    int UserId { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
    
}