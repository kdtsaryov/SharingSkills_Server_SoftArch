using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SharingSkills_HSE_backend.Other
{
    public class AuthOptions
    {
        public const string ISSUER = "MyAuthServer"; // Издатель токена
        public const string AUDIENCE = "MyAuthClient"; // Потребитель токена
        const string KEY = "mysupersecret_secretkey!123";   // Ключ для шифрования
        public const int LIFETIME = 10; // Время жизни токена - 10 минут
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}
