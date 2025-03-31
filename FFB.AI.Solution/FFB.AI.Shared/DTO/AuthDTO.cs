namespace FFB.AI.Shared.DTO
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public bool Successful { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string DisplayName { get; set; }
    }

    public class RegisterResponse
    {
        public bool Successful { get; set; }
        public string[] Errors { get; set; }
    }
}