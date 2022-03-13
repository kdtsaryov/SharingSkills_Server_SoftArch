using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharingSkills_HSE_backend.Models;

namespace SharingSkills_HSE_backend.Controllers
{
    /// <summary>
    /// Контроллер навыков
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SkillsController : ControllerBase
    {
        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly SharingSkillsContext _context;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="context">Контекст базы данных</param>
        public SkillsController(SharingSkillsContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает все навыки
        /// </summary>
        // GET: api/Skills
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
        {
            return await _context.Skills.Include(s => s.User).ToListAsync();
        }

        /// <summary>
        /// Возвращает конкретный навык
        /// </summary>
        /// <param name="id">ID навыка</param>
        // GET: api/Skills/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Skill>> GetSkill(long id)
        {
            var skill = await _context.Skills.Include(s => s.User).FirstOrDefaultAsync(t => t.Id == id);
            if (skill == null)
                return NotFound();
            return skill;
        }

        /// <summary>
        /// Возвращает все навыки конкретного пользователя
        /// </summary>
        /// <param name="mail">Почта</param>
        // GET: api/Skills/kdtsaryov@edu.hse.ru/skills
        [HttpGet("{mail}/skills")]
        public async Task<ActionResult<IEnumerable<Skill>>> GetUserSkills(string mail)
        {
            return (await _context.Skills.Include(s => s.User).ToListAsync()).FindAll(s => s.UserMail == mail);
        }

        /// <summary>
        /// Изменение навыка
        /// </summary>
        /// <param name="id">ID навыка</param>
        /// <param name="skill">Навык</param>
        // PUT: api/Skills/1
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSkill(long id, Skill skill)
        {
            if (id != skill.Id)
                return BadRequest();
            _context.Entry(skill).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SkillExists(id))
                    return NotFound();
                else
                    throw;
            }
            return NoContent();
        }

        /// <summary>
        /// Добавление навыка
        /// </summary>
        /// <param name="skill">Навык</param>
        // POST: api/Skills
        [HttpPost]
        public async Task<ActionResult<Skill>> PostSkill(Skill skill)
        {
            // Если такой навык уже есть
            var s = await _context.Skills.FindAsync(skill.Id);
            if (s != null)
                return BadRequest();
            // Находим пользователя
            var user = await _context.Users.FindAsync(skill.UserMail);
            if (user == null)
                return BadRequest();
            // Добавляем навык
            user.Skills.Add(skill);
            _context.Skills.Add(skill);
            // Сохраняем изменения
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetSkill", new { id = skill.Id }, skill);
        }

        /// <summary>
        /// Удаление навыка
        /// </summary>
        /// <param name="id">ID навыка</param>
        // DELETE: api/Skills/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSkill(long id)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill == null)
                return NotFound();
            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Проверка наличия навыка
        /// </summary>
        /// <param name="id">ID навыка</param>
        private bool SkillExists(long id)
        {
            return _context.Skills.Any(e => e.Id == id);
        }
    }
}
