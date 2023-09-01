using BlazorApp1.Models;
using System.Net.Http.Json;

namespace BlazorApp1.Clients
{
    public class AuthorsClient
    {
        private readonly HttpClient _httpClient;

        public AuthorsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Author>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("/authors");

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return await response.Content.ReadFromJsonAsync<List<Author>>();

            return new List<Author>();
        }

        public async Task<Author> GetDetailsAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<Author>($"/authors/{id}");
        }

        public async Task<Author?> CreateAsync(Author author)
        {
            var response = await _httpClient.PostAsJsonAsync("/authors", author);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Author>();

            return null;
        }

        public async Task<Author?> UpdateAsync(Author author)
        {
            var response = await _httpClient.PutAsJsonAsync($"/authors/{author.Id}", author);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Author>();

            return null;
        }

        public async Task<bool> DeleteAsync(Author author)
        {
            var resp = await _httpClient.DeleteAsync($"/authors/{author.Id}");

            return resp.IsSuccessStatusCode;
        }
    }
}
