using Domain.Exceptions;

namespace identiverse_backend.Services;

public static class AuthorizationHelpers
{
    public static bool CanAccessPerson(ICurrentUserService currentUser, int personId)
    {
        if (currentUser.IsAdmin)
            return true;

        var user = currentUser.GetCurrentUser();
        return user?.PersonId == personId;
    }

    public static void EnsureCanAccessPersonOrThrow(ICurrentUserService currentUser, int personId)
    {
        if (!CanAccessPerson(currentUser, personId))
            throw new UnauthorizedIdentiverseException("You do not have access to this person");
    }
}