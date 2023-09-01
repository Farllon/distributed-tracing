using BlazorApp1.Models;
using System.Net.Http.Json;

namespace BlazorApp1.Clients
{
    public class PostsClient
    {
        private readonly HttpClient _httpClient;

        public PostsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Post>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("/posts");

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return await response.Content.ReadFromJsonAsync<List<Post>>();

            return new List<Post>();
        }

        public async Task<Post> GetDetailsAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<Post>($"/posts/{id}");
        }

        public async Task<Post?> CreateAsync(Post post)
        {
            var response = await _httpClient.PostAsJsonAsync("/posts", post);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Post>();

            return null;
        }

        public async Task<Post?> UpdateAsync(Post post)
        {
            var response = await _httpClient.PutAsJsonAsync($"/posts/{post.Id}", post);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Post>();

            return null;
        }

        public async Task<bool> DeleteAsync(Post post)
        {
            var resp = await _httpClient.DeleteAsync($"/posts/{post.Id}");

            return resp.IsSuccessStatusCode;
        }
    }
}
