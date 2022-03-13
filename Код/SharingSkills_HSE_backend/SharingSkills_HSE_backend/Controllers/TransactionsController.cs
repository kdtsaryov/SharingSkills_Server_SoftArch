using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharingSkills_HSE_backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharingSkills_HSE_backend.Other;

namespace SharingSkills_HSE_backend.Controllers
{
    /// <summary>
    /// Контроллер обменов
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly SharingSkillsContext _context;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="context">Контекст базы данных</param>
        public TransactionsController(SharingSkillsContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает конкретный обмен
        /// </summary>
        /// <param name="id">ID обмена</param>
        // GET: api/Transactions/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var t = await _context.Transactions.Include(t => t.Users).FirstOrDefaultAsync(t => t.Id == id);
            if (t == null)
                return NotFound();
            return t;
        }

        /// <summary>
        /// Возвращает все обмены
        /// </summary>
        // GET: api/Transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            return await _context.Transactions.Include(t => t.Users).ToListAsync();
        }

        /// <summary>
        /// Изменение обмена
        /// </summary>
        /// <param name="id">ID обмена</param>
        /// <param name="transaction">Обмен</param>
        /// <param name="mail">Почта того, кто завершил обмен</param>
        // PUT: api/Transactions/1?mail=kdtsaryov@edu.hse.ru
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransaction(int id, Transaction transaction, string mail = "")
        {
            if (id != transaction.Id)
                return BadRequest();
            _context.Entry(transaction).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransactionExists(id))
                    return NotFound();
                else
                    throw;
            }
            // Если обмен приняли, то оповещаем отправителя
            if (transaction.Status == 1)
            {
                // Находим отправителя и получателя
                var sender = await _context.Users.FindAsync(transaction.SenderMail);
                var receiver = await _context.Users.FindAsync(transaction.ReceiverMail);
                await Mail.SendEmailAsync(sender.Mail, "Принятый обмен",
                    $"{receiver.Name} {receiver.Surname} принял(а) Ваш обмен.\n" +
                    $"Зайдите в приложение \"Обмен навыками\", чтобы узнать детали.");
            }
            // Если обмен завершил один пользователь, то оповещаем другого пользователя
            if (transaction.Status == 2)
            {
                // Находим отправителя и получателя
                var sender = await _context.Users.FindAsync(transaction.SenderMail);
                var receiver = await _context.Users.FindAsync(transaction.ReceiverMail);
                // Если завершил отправитель
                if (sender.Mail == mail)
                {
                    await Mail.SendEmailAsync(receiver.Mail, "Обмен завершен",
                        $"{sender.Name} {sender.Surname} завершил(а) обмен.\n" +
                        $"Зайдите в приложение \"Обмен навыками\", чтобы узнать детали.");
                }
                // Если завершил получатель
                if (receiver.Mail == mail)
                {
                    await Mail.SendEmailAsync(sender.Mail, "Обмен завершен",
                        $"{receiver.Name} {receiver.Surname} завершил(а) обмен.\n" +
                        $"Зайдите в приложение \"Обмен навыками\", чтобы узнать детали.");
                }
            }
            return NoContent();
        }

        /// <summary>
        /// Добавление нового обмена
        /// </summary>
        /// <param name="transaction">Обмен</param>
        // POST: api/Transactions
        [HttpPost]
        public async Task<ActionResult<Transaction>> PostTransaction(Transaction transaction)
        {
            // Если такой обмен уже есть
            var t = await _context.Transactions.FindAsync(transaction.Id);
            if (t != null)
                return BadRequest();
            // Находим отправителя и получателя
            var sender = await _context.Users.FindAsync(transaction.SenderMail);
            var receiver = await _context.Users.FindAsync(transaction.ReceiverMail);
            if (sender == null || receiver == null || sender == receiver) 
                return BadRequest();
            transaction.Status = 0;
            // Добавляем обмен
            sender.Transactions.Add(transaction);
            receiver.Transactions.Add(transaction);
            _context.Transactions.Add(transaction);
            // Оповещаем получателя нового обмена
            await Mail.SendEmailAsync(receiver.Mail, "Новый обмен",
                $"{sender.Name} {sender.Surname} предложил(а) Вам новый обмен.\n" +
                $"Зайдите в приложение \"Обмен навыками\", чтобы узнать детали.");
            // Сохраняем изменения
            _context.Entry(sender).State = EntityState.Modified;
            _context.Entry(receiver).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetTransaction", new { id = transaction.Id }, transaction);
        }

        /// <summary>
        /// Удаление обмена
        /// </summary>
        /// <param name="id">ID обмена</param>
        /// <param name="sendNotification">Отправлять ли уведомление</param>
        // DELETE: api/Transactions/1?sendNotification=true
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(long id, bool sendNotification = false)
        {
            var t = await _context.Transactions.FindAsync(id);
            if (t == null)
                return NotFound();
            // Находим отправителя и получателя
            var sender = await _context.Users.FindAsync(t.SenderMail);
            var receiver = await _context.Users.FindAsync(t.ReceiverMail);
            // Если надо, то отправляем уведомление
            if (sendNotification)
            {
                await Mail.SendEmailAsync(sender.Mail, "Отказ в обмене",
                    $"{receiver.Name} {receiver.Surname} отказал(а) Вам в обмене.\n" +
                    $"Зайдите в приложение \"Обмен навыками\", чтобы узнать детали.");
            }
            _context.Transactions.Remove(t);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Проверка наличия обмена
        /// </summary>
        /// <param name="id">ID обмена</param>
        private bool TransactionExists(long id)
        {
            return _context.Transactions.Any(e => e.Id == id);
        }
    }
}
