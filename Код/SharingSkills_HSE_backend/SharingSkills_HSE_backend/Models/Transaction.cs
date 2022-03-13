using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SharingSkills_HSE_backend.Models
{
    /// <summary>
    /// Обмен
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// ID обмена
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Первый навык
        /// </summary>
        [StringLength(51, ErrorMessage = "Слишком много символов")]
        public string Skill1 { get; set; }

        /// <summary>
        /// Второй навык
        /// </summary>
        [StringLength(51, ErrorMessage = "Слишком много символов")]
        public string Skill2 { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        [StringLength(301, ErrorMessage = "Слишком много символов")]
        public string Description { get; set; }

        /// <summary>
        /// Почта отправителя
        /// </summary>
        [RegularExpression(@"[_A-Za-z0-9]+@edu.hse.ru", ErrorMessage = "Некорректный почтовый адрес отправителя")]
        public string SenderMail { get; set; }

        /// <summary>
        /// Почта получателя
        /// </summary>
        [RegularExpression(@"[_A-Za-z0-9]+@edu.hse.ru", ErrorMessage = "Некорректный почтовый адрес получателя")]
        public string ReceiverMail { get; set; }

        /// <summary>
        /// Почта того, кто "хочет"
        /// </summary>
        [RegularExpression(@"[_A-Za-z0-9]+@edu.hse.ru", ErrorMessage = "Некорректный почтовый адрес получателя")]
        public string WhoWantMail { get; set; }

        /// <summary>
        /// Статус: 0 - отправлен, 1 - подтвержден (активен), 2 - завершен
        /// </summary>
        [Range(0, 2, ErrorMessage = "Некорректный статус обмена")]
        public int Status { get; set; }

        /// <summary>
        /// Отправитель и получатель
        /// </summary>
        public List<User> Users { get; set; } = new List<User>();
    }
}
