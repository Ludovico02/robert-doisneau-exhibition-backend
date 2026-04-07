namespace RobertDoisneau.Login.WebApi.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreationDate { get; set; } = DateTime.Now;
}
