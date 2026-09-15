namespace Identity.Application.Users.SignUp;

public sealed record SignUpRequest(string Email, string Password, string DisplayName);
