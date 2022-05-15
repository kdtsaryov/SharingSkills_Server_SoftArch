using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharingSkills_HSE_backend.Models;
using SharingSkills_HSE_backend.Other;
using Microsoft.AspNetCore.Authorization;
using SharingSkills_HSE_backend.Repository;

namespace SharingSkills_HSE_backend.Controllers
{
    /// <summary>
    /// Контроллер пользователей
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
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
        public async Task<ActionResult<User>> ForgotPassword(string mail)
        {
            // Находим пользователя
            var user = await _context.Users.Include(u => u.Transactions).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Mail == mail);
            // Если такой пользователь есть
            if (user != null)
            {
                var callbackUrl = Url.Action("ResetPassword", "Users", new { userMail = user.Mail}, protocol: HttpContext.Request.Scheme);
                // Отправляем письмо с паролем
                await Mail.SendEmailAsync(user.Mail, "Сброс пароля", 
                    $"Для сброса пароля пройдите по ссылке: <a href='{callbackUrl}'>link</a>");
                return Ok($"Для сброса пароля пройдите по ссылке: <a href='{callbackUrl}'>link</a>");
            }
            return BadRequest();
        }

        /// <summary>
        /// Сброс пароля
        /// </summary>
        [HttpGet("ResetPassword")]
        // GET: api/Users/ResetPassword?userMail=kdtsaryov@edu.hse.ru
        public IActionResult ResetPassword(string userMail)
        {
            return View();
        }

        /// <summary>
        /// Сброс пароля
        /// </summary>
        /// <param name="model">Визуальная модель для сброса пароля</param>
        [HttpPost("ResetPassword")]
        // POST: api/Users/ResetPassword?userMail=kdtsaryov@edu.hse.ru
        public async Task<IActionResult> ResetPassword(string userMail, [FromForm] ResetPasswordViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Mail == model.Mail);
            if (user == null)
            {
                return View("ResetPasswordConfirmation");
            }
            user.Password = model.Password;
            // Генерация добавки для хеширования пароля
            GenerateSalt(ref user);
            // Хеширования указанного пароля
            user.Password = HashPassword(user.SaltForPassword, user.Password);

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return View("ResetPasswordConfirmation");
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

            User userData = _context.Users.FirstOrDefault(x => x.Mail == mail);
            if (userData == null)
            {
                return NotFound();
            }

            // Если пользователь меняет пароль - его надо захешировать
            if (userData.Password != user.Password)
            { // При каждой смене пароля добавка также перегенерируется, чтобы сложнее было найти закономерность хеша
                // Генерация добавки для хеширования пароля
                GenerateSalt(ref user);
                // Хеширования указанного при изменении пользователя пароля
                user.Password = HashPassword(user.SaltForPassword, user.Password);
            }

            _context.Entry(user).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
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
            // Генерация добавки для хеширования пароля
            GenerateSalt(ref user);
            // Хеширования указанного при создании пользователя пароля
            user.Password = HashPassword(user.SaltForPassword, user.Password);

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
        /// Получение токена авторизации
        /// </summary>
        /// <param name="mail">Почта</param>
        /// <param name="password">Пароль</param>
        [HttpPost("authenticate/{mail}/{password}")]
        // POST: api/Users/authenticate/kdtsaryov@edu.hse.ru/123456789
        public async Task<IActionResult> Authenticate(string mail, string password)
        {
            User userData = _context.Users.FirstOrDefault(x => x.Mail == mail);

            if (userData == null || IsPasswordIncorrect(userData, password))
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
        /// Генерация добавки ("соли") к паролю пользователя для надежности хеша
        /// </summary>
        /// <param name="user">Сущность пользователя, в которую добавляется соль</param>
        private static void GenerateSalt(ref User user)
        {
            byte[] salt = new byte[128 / 8];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetNonZeroBytes(salt);
            }

            user.SaltForPassword = salt;
        }

        /// <summary>
        /// Хеширование пароля
        /// </summary>
        /// <param name="salt">Добавка для хеша</param>
        /// <param name="password">Хешируемый пароль</param>
        /// <returns>Хеш пароля</returns>
        private static string HashPassword(byte[] salt, string password)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password, // хешируемый пароль
                salt: salt, // добавка для секьюрности хеша
                prf: KeyDerivationPrf.HMACSHA256, // Хеш (SHA256 вполне хватает для нужд хранения паролей)
                iterationCount: 100000, // количество хеш-итераций
                numBytesRequested: 256 / 8)); // желаемая длина выходного ключа
        }

        /// <summary>
        /// Проверка корректности переданного пароля
        /// </summary>
        /// <param name="user">Сущность пользователя, для которой проверяется пароль</param>
        /// <param name="password">Проверяемый пароль</param>
        /// <returns>Булка. 1 - если пароль не соответствует захешированному в сущности, 0 - иначе</returns>
        private static bool IsPasswordIncorrect(User user, string password)
        {
            var hashedPassword = HashPassword(user.SaltForPassword, password);

            return user.Password != hashedPassword;
        }
    }
}
