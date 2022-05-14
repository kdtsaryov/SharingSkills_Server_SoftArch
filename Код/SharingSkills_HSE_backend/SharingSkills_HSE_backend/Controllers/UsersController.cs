using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharingSkills_HSE_backend.Models;
using SharingSkills_HSE_backend.Other;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SharingSkills_HSE_backend.Repository;

namespace SharingSkills_HSE_backend.Controllers
{
    /// <summary>
    /// Контроллер пользователей
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        /// <summary>
        /// Сущность управления JWT-токенами
        /// </summary>
        private readonly IJWTManagerRepository _jWTManager;
        /// <summary>
        /// Генератор случайных чисел для генерации кода подтверждения
        /// </summary>
        private readonly Random rnd = new Random();
        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly SharingSkillsContext _context;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="context">Контекст базы данных</param>
        public UsersController(SharingSkillsContext context, IJWTManagerRepository jWTManager)
        {
            _context = context;
            _jWTManager = jWTManager;
        }

        /// <summary>
        /// Напоминание забытого пароля по почте
        /// </summary>
        /// <param name="mail">Почта</param>
        // GET: api/Users/kdtsaryov@edu.hse.ru/password
        [HttpGet("{mail}/password")]
        [Authorize]
        public async Task<ActionResult<User>> ForgetPassword(string mail)
        {
            // Находим пользователя
            var user = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Mail == mail);
            // Если такой пользователь есть
            if (user != null)
            {
                // Отправляем письмо с паролем
                await Mail.SendEmailAsync(user.Mail, "Напоминание пароля", "Здравствуйте!\n" +
                "В Обмене Навыками была нажата кнопка \"Забыли пароль?\"\n" +
                $"Ваш пароль - {user.Password}\n");
                return Ok();
            }
            return BadRequest();
        }

        /// <summary>
        /// Возвращает всех пользователей
        /// </summary>
        // GET: api/Users
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).ToListAsync();
        }

        /// <summary>
        /// Возвращает пользователя с конкретной почтой
        /// </summary>
        /// <param name="mail">Почта</param>
        // GET: api/Users/kdtsaryov@edu.hse.ru
        [HttpGet("{mail}")]
        [Authorize]
        public async Task<ActionResult<User>> GetUser(string mail)
        {
            // Находим пользователя и возвращаем его, если такой нашелся
            var user = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Mail == mail);
            if (user == null)
                return NotFound();
            return user;
        }

        /// <summary>
        /// Возвращает навыки, удовлетворяющие каким-то критериям без навыков конкретного пользователя
        /// </summary>
        /// <param name="mail">Почта</param>
        /// <param name="studyingYearID">Курс</param>
        /// <param name="majorID">Образовательная программа</param>
        /// <param name="campusLocationID">Расположение корпуса</param>
        /// <param name="dormitoryID">Общежитие</param>
        /// <param name="gender">Пол</param>
        /// <param name="category">Категория</param>
        /// <param name="subcategory">Подкатегория</param>
        /// <param name="skillstatus">Могу или Хочу</param>
        // GET: api/Users/kdtsaryov@edu.hse.ru/skills?studyingYearID=1&majorID=1&campusLocationID=1&dormitoryID=1&gender=1&skillstatus=1&category=1&subcategory=1
        [HttpGet("{mail}/skills")]
        public async Task<ActionResult<IEnumerable<Skill>>> GetUsersSkills(string mail, int studyingYearID = -1, int majorID = -1, 
            int campusLocationID = -1, int dormitoryID = -1, int gender = -1,int skillstatus = -1, int category = -1, int subcategory = -1)
        {
            // Находим пользователя и возвращаем ошибку, если такой не нашелся
            var user = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Mail == mail);
            if (user == null)
                return NotFound();
            // Если параметры не переданы, то возвращаются все навыки
            var users = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).ToListAsync();
            users = users.FindAll(u => u.Mail != user.Mail);
            // Сначала фильтруем пользователей
            if (studyingYearID != -1)
                users = users.FindAll(u => u.StudyingYearId == studyingYearID);
            if (majorID != -1)
                users = users.FindAll(u => u.MajorId == majorID);
            if (campusLocationID != -1)
                users = users.FindAll(u => u.CampusLocationId == campusLocationID);
            if (dormitoryID != -1)
                users = users.FindAll(u => u.DormitoryId == dormitoryID);
            if (gender != -1)
                users = users.FindAll(u => u.Gender == gender);
            // Записываем все навыки всех отфильтрованных пользователей
            List<Skill> skills = new List<Skill>();
            foreach (User u in users)
            {
                skills.AddRange(u.Skills);
            }
            // Фильтруем уже их навыки по заданным параметрам
            if (skillstatus != -1)
                skills = skills.FindAll(s => s.Status == skillstatus);
            if (category != -1)
                skills = skills.FindAll(s => s.Category == category);
            if (subcategory != -1)
                skills = skills.FindAll(s => s.Subcategory == subcategory);
            return skills;
        }

        /// <summary>
        /// Возвращает текущие обмены конкретного пользователя
        /// </summary>
        /// <param name="mail">Почта</param>
        // GET: api/Users/kdtsaryov@edu.hse.ru/transactions/active
        [HttpGet("{mail}/transactions/active")]
        public async Task<ActionResult<Transaction>> GetUserActiveTransactions(string mail)
        {
            // Находим пользователя
            var user = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Mail == mail);
            if (user == null)
                return NotFound();
            // Если есть обмены
            if (user.Transactions != null)
                // Возвращаем текущие
                return Ok(user.Transactions.Where(t => t.Status == 1).ToList());
            else
                return NotFound();
        }

        /// <summary>
        /// Возвращает завершенные обмены конкретного пользователя
        /// </summary>
        /// <param name="mail">Почта</param>
        // GET: api/Users/kdtsaryov@edu.hse.ru/transactions/completed
        [HttpGet("{mail}/transactions/completed")]
        public async Task<ActionResult<Transaction>> GetUserCompletedTransactions(string mail)
        {
            // Находим пользователя
            var user = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Mail == mail);
            if (user == null)
                return NotFound();
            // Если есть обмены
            if (user.Transactions != null)
                // Возвращаем завершенные
                return Ok(user.Transactions.Where(t => t.Status == 2).ToList());
            else
                return NotFound();
        }

        /// <summary>
        /// Возвращает входящие обмены конкретного пользователя
        /// </summary>
        /// <param name="mail">Почта</param>
        // GET: api/Users/kdtsaryov@edu.hse.ru/transactions/in
        [HttpGet("{mail}/transactions/in")]
        public async Task<ActionResult<Transaction>> GetUserInTransactions(string mail)
        {
            // Находим пользователя
            var user = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Mail == mail);
            if (user == null)
                return NotFound();
            // Если есть обмены
            if (user.Transactions != null)
                // Возвращаем входящие
                return Ok(user.Transactions.Where(t => t.Status == 0).Where(t => t.ReceiverMail == user.Mail).ToList());
            else
                return NotFound();
        }

        /// <summary>
        /// Возвращает исходящие обмены конкретного пользователя
        /// </summary>
        /// <param name="mail">Почта</param>
        // GET: api/Users/kdtsaryov@edu.hse.ru/transactions/out
        [HttpGet("{mail}/transactions/out")]
        public async Task<ActionResult<Transaction>> GetUserOutTransactions(string mail)
        {
            // Находим пользователя
            var user = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Mail == mail);
            if (user == null)
                return NotFound();
            // Если есть обмены
            if (user.Transactions != null)
                // Возвращаем исходящие
                return Ok(user.Transactions.Where(t => t.Status == 0).Where(t => t.SenderMail == user.Mail).ToList());
            else
                return NotFound();
        }

        /// <summary>
        /// Обновление данных конкретного пользователя
        /// </summary>
        /// <param name="mail">Почта</param>
        /// <param name="user">Пользователь</param>
        // PUT: api/Users/kdtsaryov@edu.hse.ru
        [HttpPut("{mail}")]
        [Authorize]
        public async Task<IActionResult> PutUser(string mail, User user)
        {
            if (mail != user.Mail)
                return BadRequest();
            _context.Entry(user).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(mail))
                    return NotFound();
                else
                    throw;
            }
            return NoContent();
        }

        /// <summary>
        /// Добавление нового пользователя
        /// </summary>
        /// <param name="user">Пользователь</param>
        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            var u = await _context.Users.FindAsync(user.Mail);
            if (u != null)
                return BadRequest();
            // Генерация кода подтверждения
            user.ConfirmationCodeServer = rnd.Next(1000, 10000);
            // Отправка этого кода на почту
            await Mail.SendEmailAsync(user.Mail, "Код подтверждения регистрации", "Здравствуйте!\n" +
                "Спасибо за регистрацию в Обмене Навыками\n" +
                $"Ваш код подтверждения - {user.ConfirmationCodeServer}\n");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetUser", new { mail = user.Mail }, user);
        }

        /// <summary>
        /// Удаление пользователя
        /// </summary>
        /// <param name="mail">Почта</param>
        // DELETE: api/Users/kdtsaryov@edu.hse.ru
        [HttpDelete("{mail}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string mail)
        {
            var user = await _context.Users.FindAsync(mail);
            if (user == null)
                return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Проверка наличия пользователя
        /// </summary>
        /// <param name="mail">Почтовый адрес</param>
        /// <returns>Существует ли такой пользователь</returns>
        private bool UserExists(string mail)
        {
            return _context.Users.Any(e => e.Mail == mail);
        }

        /// <summary>
        /// Получение токена авторизации
        /// </summary>
        /// <param name="mail">Почта</param>
        /// <param name="password">Пароль</param>
        [HttpPost("authenticate/{mail}/{password}")]
        // POST: api/Users/authenticate/kdtsaryov@edu.hse.ru/1234567
        public async Task<IActionResult> Authenticate(string mail, string password)
        {
            User userData = _context.Users.FirstOrDefault(x => x.Mail == mail && x.Password == password);

            if (userData == null)
            {
                return Unauthorized();
            }
            var token = _jWTManager.Authenticate(ref userData);

            _context.Entry(userData).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(mail))
                    return NotFound();
                else
                    throw;
            }

            return Ok(token);
        }

        /// <summary>
        /// Обновление токена авторизации
        /// </summary>
        /// <param name="data">Предыдущий полученный токен. Включает в себя сам токен, токен обновления и почту пользователя</param>
        [HttpPost("refresh-token")]
        // POST: api/Users/refresh-token
        public IActionResult RefreshAuthToken(Tokens data)
        {
            User userData = _context.Users.FirstOrDefault(x => x.Mail == data.Mail && x.RefreshToken == data.RefreshToken);
            if (userData == null || userData.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized();
            }
            var token = _jWTManager.GenerateJWTToken(userData);
            token.RefreshToken = data.RefreshToken;

            return Ok(token);
        }
    }
}
