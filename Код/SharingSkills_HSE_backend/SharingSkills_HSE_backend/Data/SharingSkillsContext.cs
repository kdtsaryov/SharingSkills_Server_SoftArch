using Microsoft.EntityFrameworkCore;

namespace SharingSkills_HSE_backend.Models
{
    /// <summary>
    /// Контекст базы данных
    /// </summary>
    public class SharingSkillsContext : DbContext
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="options">Параметры базы данных</param>
        public SharingSkillsContext(DbContextOptions<SharingSkillsContext> options) : base(options) 
        {
            Database.EnsureCreated();
        }

        /// <summary>
        /// Создание моделей
        /// </summary>
        /// <param name="builder">"Строитель", связывающий модеди с контекстом БД</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        /// <summary>
        /// Навыки
        /// </summary>
        public DbSet<Skill> Skills { get; set; }
        /// <summary>
        /// Обмены
        /// </summary>
        public DbSet<Transaction> Transactions { get; set; }
        /// <summary>
        /// Пользователи
        /// </summary>
        public DbSet<User> Users { get; set; } 
    }
}
