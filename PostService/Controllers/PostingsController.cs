using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PostService.Models;

namespace PostService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostingsController : ControllerBase
    {
        // Статичний список посилок для збереження даних під час роботи сервера
        private static readonly List<Posting> _postings = new()
        {
            new Posting
            {
                Id = 1,
                From = "Київ",
                To = "Львів",
                Content = "Документи та книги",
                DeliveryType = DeliveryType.Courier,
                Weight = 1.5f,
                Width = 25,
                Height = 15,
                Depth = 20,
                Value = 350.0f,
                Price = 85.0f,
                CreatedAt = DateTime.UtcNow
            },
            new Posting
            {
                Id = 2,
                From = "Рівне",
                To = "Луцьк",
                Content = "Одяг та аксесуари",
                DeliveryType = DeliveryType.Standard,
                Weight = 2.0f,
                Width = 30,
                Height = 20,
                Depth = 10,
                Value = 500.0f,
                Price = 90.0f,
                CreatedAt = DateTime.UtcNow
            }
        };

        // GET: /postings
        [HttpGet]
        public ActionResult<IEnumerable<Posting>> GetAll()
        {
            return Ok(_postings);
        }

        // GET: /postings/error-500 (Ендпойнт для перевірки реакції клієнта на помилку 500)
        [HttpGet("error-500")]
        public IActionResult ThrowServerError()
        {
            throw new InvalidOperationException("Тестова критична помилка сервера (Код 500) для перевірки глобального обробника!");
        }

        // GET: /postings/5
        [HttpGet("{id}")]
        public ActionResult<Posting> GetById(int id)
        {
            // Якщо id == 999, симулюємо збій сервера
            if (id == 999)
            {
                throw new Exception("Штучний збій бази даних сервера при отриманні ID 999.");
            }

            var item = _postings.FirstOrDefault(p => p.Id == id);
            if (item == null)
            {
                return NotFound(new { message = $"Відправлення з ID {id} не знайдено." });
            }
            return Ok(item);
        }

        // POST: /postings
        [HttpPost]
        public IActionResult Create([FromBody] Posting posting)
        {
            var errors = ValidatePosting(posting);
            if (errors.Count > 0)
            {
                return BadRequest(new { errors });
            }

            posting.Id = _postings.Count > 0 ? _postings.Max(p => p.Id) + 1 : 1;
            posting.CreatedAt = DateTime.UtcNow;
            _postings.Add(posting);
            return CreatedAtAction(nameof(GetById), new { id = posting.Id }, posting);
        }

        // PUT: /postings/5
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Posting updated)
        {
            var existing = _postings.FirstOrDefault(p => p.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"Відправлення з ID {id} для оновлення не знайдено." });
            }

            var errors = ValidatePosting(updated);
            if (errors.Count > 0)
            {
                return BadRequest(new { errors });
            }

            existing.From = updated.From;
            existing.To = updated.To;
            existing.Content = updated.Content;
            existing.DeliveryType = updated.DeliveryType;
            existing.Weight = updated.Weight;
            existing.Width = updated.Width;
            existing.Height = updated.Height;
            existing.Depth = updated.Depth;
            existing.Value = updated.Value;
            existing.Price = updated.Price;

            return Ok(existing);
        }

        // DELETE: /postings/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var item = _postings.FirstOrDefault(p => p.Id == id);
            if (item == null)
            {
                return NotFound(new { message = $"Відправлення з ID {id} не знайдено, або воно вже було видалене." });
            }

            _postings.Remove(item);
            return NoContent();
        }

        // Метод валідації поштових відправлень
        private static Dictionary<string, string[]> ValidatePosting(Posting p)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(p.From))
            {
                errors["From"] = new[] { "Пункт відправлення обов'язковий." };
            }

            if (string.IsNullOrWhiteSpace(p.To))
            {
                errors["To"] = new[] { "Пункт призначення обов'язковий." };
            }

            if (string.IsNullOrWhiteSpace(p.Content))
            {
                errors["Content"] = new[] { "Опис вмісту відправлення є обов'язковим." };
            }

            if (p.Weight <= 0)
            {
                errors["Weight"] = new[] { "Вага посилки має бути більшою за 0 кг." };
            }

            return errors;
        }
    }
}
