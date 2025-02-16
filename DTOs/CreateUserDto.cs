namespace Identity.Server.Keycloak.DTOs;

public class CreateUserDto
{
    public string Username { get; set; }
    public string Password { get; set; }
    public List<string> Roles { get; set; } = new();
}