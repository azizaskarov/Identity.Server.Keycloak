namespace Identity.Server.Keycloak.DTOs;

public class UpdateUserDto
{
    public required string NewUsername { get; set; }
    public required string NewPassword { get; set; }
}
