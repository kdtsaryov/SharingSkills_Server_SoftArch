using SharingSkills_HSE_backend.Models;

namespace SharingSkills_HSE_backend.Repository
{
    public interface IJWTManagerRepository
    {
        Tokens Authenticate(ref User user);
        Tokens GenerateJWTToken(User user);
    }
}
