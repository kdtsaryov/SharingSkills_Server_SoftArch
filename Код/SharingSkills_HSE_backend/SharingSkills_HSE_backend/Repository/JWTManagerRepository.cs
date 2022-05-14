using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SharingSkills_HSE_backend.Models;
using SharingSkills_HSE_backend.Other;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SharingSkills_HSE_backend.Repository
{
    public class JWTManagerRepository : IJWTManagerRepository
    {
		private readonly IConfiguration iconfiguration;
		public JWTManagerRepository(IConfiguration iconfiguration)
		{
			this.iconfiguration = iconfiguration;
		}
		public Tokens Authenticate(ref User user)
		{
			var tokens = GenerateJWTToken(user);
			user.RefreshToken = GenerateRefreshToken();
			user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(3);

			tokens.RefreshToken = user.RefreshToken;

			return tokens;
		}

		public Tokens GenerateJWTToken(User user)
		{
			var identity = GetIdentity(user);
			// создаем JWT-токен
			var jwt = new JwtSecurityToken(
					issuer: AuthOptions.ISSUER,
					audience: AuthOptions.AUDIENCE,
					notBefore: DateTime.UtcNow,
					claims: identity.Claims,
					expires: DateTime.UtcNow.AddMinutes(AuthOptions.LIFETIME),
					signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
			var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
			return new Tokens { Token = encodedJwt, Mail = user.Mail };
		}

		private static string GenerateRefreshToken()
		{
			using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();

			var randomBytes = new byte[64];
			rngCryptoServiceProvider.GetBytes(randomBytes);
			return Convert.ToBase64String(randomBytes);
		}

		private static ClaimsIdentity GetIdentity(User user)
		{
			var claims = new List<Claim>
				{
					new Claim(ClaimsIdentity.DefaultNameClaimType, user.Mail)
				};
			ClaimsIdentity claimsIdentity = new(claims, "Token", ClaimsIdentity.DefaultNameClaimType, user.Mail);
			return claimsIdentity;
		}
	}
}
