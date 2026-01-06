using Microsoft.AspNetCore.Identity;

namespace Database.Entities;


public class ApplicationUser : IdentityUser<int>
{
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
}
