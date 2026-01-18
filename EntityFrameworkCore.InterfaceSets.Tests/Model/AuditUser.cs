namespace EntityFrameworkCore.InterfaceSets.Tests.Model;

public class AuditUser
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
}
