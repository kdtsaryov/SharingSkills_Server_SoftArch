using System;
using System.ComponentModel.DataAnnotations;

namespace SharingSkills_HSE_backend.Models
{
    /// <summary>
    /// Навык
    /// </summary>
    public class Skill
    {
        /// <summary>
        /// ID навыка
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Статус: 1 - могу, 2 - хочу
        /// </summary>
        [Range(0, 2, ErrorMessage = "Некорректный статус навыка")]
        public int Status { get; set; }

        /// <summary>
        /// Название
        /// </summary>
        [StringLength(51, ErrorMessage = "Слишком много символов")]
        public string Name { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        [StringLength(301, ErrorMessage = "Слишком много символов")]
        public string Description { get; set; }

        /// <summary>
        /// Категория
        /// </summary>
        [Range(0, 1, ErrorMessage = "Некорректная категория")]
        public int Category { get; set; }

        /// <summary>
        /// Подкатегория
        /// </summary>
        [Range(0, 21, ErrorMessage = "Некорректная подкатегория")]
        public int Subcategory { get; set; }

        /// <summary>
        /// Почта пользователя
        /// </summary>
        [RegularExpression(@"[_A-Za-z0-9]+@edu.hse.ru", ErrorMessage = "Некорректный почтовый адрес")]
        public string UserMail { get; set; }

        /// <summary>
        /// Пользователь
        /// </summary>
        public User User { get; set; }
    }
}
