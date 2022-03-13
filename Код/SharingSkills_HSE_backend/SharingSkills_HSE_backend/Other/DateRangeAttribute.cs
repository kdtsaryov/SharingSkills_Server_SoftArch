using System;
using System.ComponentModel.DataAnnotations;

namespace SharingSkills_HSE_backend
{
    /// <summary>
    /// Атрибут валидации даты рождения
    /// </summary>
    public class DateRangeAttribute : ValidationAttribute
    {
        /// <summary>
        /// Макимальный год рождения
        /// </summary>
        int maxYear;
        /// <summary>
        /// Минимальный год рождения
        /// </summary>
        int minYear;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="min">Минимальный возраст</param>
        /// <param name="max">Максимальный возраст</param>
        public DateRangeAttribute(int min, int max)
        {
            maxYear = DateTime.Now.Year - min;
            minYear = DateTime.Now.Year - max;
        }

        /// <summary>
        /// Непосредственно валидация
        /// </summary>
        /// <param name="value">Переданная дата рождения</param>
        /// <returns></returns>
        public override bool IsValid(object value)
        {
            DateTime basic = new DateTime();
            if (((DateTime)value) == basic)
                return true;
            if (value != null && ((DateTime)value).Year >= minYear && ((DateTime)value).Year <= maxYear)
                return true;
            return false;
        }
    }
}
