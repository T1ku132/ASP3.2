namespace Pr1_ASP.Dtos;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = "";
    public int ExpiresInMinutes { get; set; }
}