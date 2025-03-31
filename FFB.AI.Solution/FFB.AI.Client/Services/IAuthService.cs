// FFB.AI.Client/Services/IAuthService.cs
using FFB.AI.Shared.DTO;
using System.Threading.Tasks;

namespace FFB.AI.Client.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest loginRequest);
        Task Logout();
        Task<RegisterResponse> Register(RegisterRequest registerRequest);
    }
}