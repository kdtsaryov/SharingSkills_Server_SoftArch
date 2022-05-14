using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SharingSkills_HSE_backend.Models
{
    /// <summary>
    /// Визуальная модель для сброса пароля
    /// </summary>
    public class ResetPasswordViewModel
    {
        /// <summary>
        /// Почта
        /// </summary>
        [RegularExpression(@"[_A-Za-z0-9]+@edu.hse.ru", ErrorMessage = "Некорректный почтовый адрес")]
        [EmailAddress]
        public string Mail { get; set; }

        /// <summary>
        /// Пароль
        /// </summary>
        [StringLength(41, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 40 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Подтверждение пароля
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }
    }
}
