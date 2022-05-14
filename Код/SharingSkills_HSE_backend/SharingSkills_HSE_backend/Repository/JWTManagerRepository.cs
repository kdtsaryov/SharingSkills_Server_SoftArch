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
		public JWTManagerRepository()
		{
		}

		/// <summary>
		///	Метод полной аутентификации пользователя.
		///	Создается новый токен доступа и новый токен обновления, 
		///	Токен обновления записывается в структуру пользователя, которая позже сохранятеся в БД
		/// </summary>
		/// <param name="user">Структура с данными пользователя</param>
		/// <returns>Структура-токен: токен доступа, токен обновления и почта пользователя</returns>
		public Tokens Authenticate(ref User user)
		{
			var tokens = GenerateJWTToken(user);
			user.RefreshToken = GenerateRefreshToken();
			user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(3);

			tokens.RefreshToken = user.RefreshToken;

			return tokens;
		}

		/// <summary>
		///	Метод частичной аутентификации пользователя.
		///	Создается только новый токен доступа
		/// </summary>
		/// <param name="user">Структура с данными пользователя</param>
		/// <returns>Структура-токен: токен доступа и почта пользователя. Токен обновления пустой</returns>
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

		/// <summary>
		///	Метод генерации токена обновления
		/// </summary>
		/// <returns>Строковое представление токена</returns>
		private static string GenerateRefreshToken()
		{
			using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();

			var randomBytes = new byte[64];
			rngCryptoServiceProvider.GetBytes(randomBytes);
			return Convert.ToBase64String(randomBytes);
		}

		/// <summary>
		///	Метод создания сущности ClaimsIdentity из сущности пользователя
		/// </summary>
		/// <returns>Сущность ClaimsIdentity</returns>
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
