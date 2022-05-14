using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SharingSkills_HSE_backend.Models;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SharingSkills_HSE_backend.Other;

namespace SharingSkills_HSE_backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Подключение сервисов
        /// </summary>
        /// <param name="services">Сервисы</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Подключение JSON сериализатора
            services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
            // Подключение контроллеров
            services.AddControllers();
            // Подключение контекста базы данных
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
                services.AddDbContext<SharingSkillsContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("SharingSkillsContextProd")));
            else
                services.AddDbContext<SharingSkillsContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("SharingSkillsContext")));
            services.BuildServiceProvider().GetService<SharingSkillsContext>().Database.Migrate();
            // Подключение аутентификации
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            // Укзывает, будет ли валидироваться издатель при валидации токена
                            ValidateIssuer = true,
                            // Строка, представляющая издателя
                            ValidIssuer = AuthOptions.ISSUER,

                            // Будет ли валидироваться потребитель токена
                            ValidateAudience = true,
                            // Установка потребителя токена
                            ValidAudience = AuthOptions.AUDIENCE,
                            // Будет ли валидироваться время существования
                            ValidateLifetime = true,

                            // Установка ключа безопасности
                            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                            // Валидация ключа безопасности
                            ValidateIssuerSigningKey = true,
                        };
                    });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
