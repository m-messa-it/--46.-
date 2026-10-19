using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PostServiceFormsApp.Models;

namespace PostServiceFormsApp.Services
{
    public class PostServiceClient : IPostServiceClient
    {
        private readonly HttpClient _httpClient;

        public string BaseUrl
        {
            get => _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5000";
            set
            {
                if (Uri.TryCreate(value.TrimEnd('/'), UriKind.Absolute, out var uri))
                {
                    _httpClient.BaseAddress = uri;
                }
            }
        }

        public PostServiceClient(string baseUrl = "http://localhost:5000")
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        // GET /postings
        public async Task<Posting[]> GetPostingsAsync()
        {
            using var response = await _httpClient.GetAsync("/postings");
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<Posting[]>() ?? Array.Empty<Posting>();
        }

        // GET /postings/{id}
        public async Task<Posting> GetPostingByIdAsync(int id)
        {
            using var response = await _httpClient.GetAsync($"/postings/{id}");
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<Posting>()
                ?? throw new InvalidOperationException("Сервер повернув порожні дані.");
        }

        // POST /postings
        public async Task<Posting> CreatePostingAsync(Posting posting)
        {
            using var response = await _httpClient.PostAsJsonAsync("/postings", posting);
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<Posting>()
                ?? throw new InvalidOperationException("Сервер повернув порожні дані після створення.");
        }

        // PUT /postings/{id}
        public async Task<Posting> UpdatePostingAsync(int id, Posting posting)
        {
            using var response = await _httpClient.PutAsJsonAsync($"/postings/{id}", posting);
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<Posting>()
                ?? throw new InvalidOperationException("Сервер повернув порожні дані після оновлення.");
        }

        // DELETE /postings/{id}
        public async Task DeletePostingAsync(int id)
        {
            using var response = await _httpClient.DeleteAsync($"/postings/{id}");
            await EnsureSuccessAsync(response);
        }

        // Тестовий метод для виклику штучної помилки 500 на сервері
        public async Task Trigger500ErrorAsync()
        {
            using var response = await _httpClient.GetAsync("/postings/error-500");
            await EnsureSuccessAsync(response);
        }

        // Центральний метод обробки статус-кодів
        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            // Помилка валідації введених користувачем даних (400)
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                ValidationErrorResponse? problem = null;
                try
                {
                    problem = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
                }
                catch
                {
                    // Якщо відповідь не відповідає JSON-структурі
                }

                var errors = problem?.Errors ?? new Dictionary<string, string[]>
                {
                    { "General", new[] { "Некоректні вхідні дані." } }
                };

                throw new PostingValidationException(errors);
            }

            // Для решти помилок (404, 500, 503 тощо) генеруємо HttpRequestException зі збереженням StatusCode
            throw new HttpRequestException(
                $"Сервер повернув помилку {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }
    }
}
