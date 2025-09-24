namespace AspNet.Backend.Feature;

public static class Error
{
    // Auth
    public const string UserCreationFailed = "User creation failed, check details.";
    public const string UserNotFound = "User not found";
    public const string UserAlreadyExists = "User already exists";
    
    public const string InvalidEmail = "Invalid email";
    public const string InvalidPassword = "Invalid password";
    public const string InvalidEmailOrPassword = "Invalid email or password";
    public const string InvalidLink = "Invalid link";
    
    // Email
    public const string EmailConfirmationFailed = "Email confirmation failed, check details.";
    public const string EmailNotConfirmed = "Email not confirmed, please confirm your email first.";
    
    // Password
    public const string PasswordResetFailed = "Password reset failed, check details.";
    
    // Entity
    public const string EntityNotFound = "Entity not found";
}