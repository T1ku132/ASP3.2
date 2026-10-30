using System.ComponentModel.DataAnnotations;

namespace Pr1_ASP.Dtos;

public class LoginDto
{
    [Required]
    public string Username { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}