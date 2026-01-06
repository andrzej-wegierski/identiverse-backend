namespace Domain.Models;

public class ApplicationUserDto
{
    public int? PersonId { get; set; }
    public PersonDto? Person { get; set; }
}