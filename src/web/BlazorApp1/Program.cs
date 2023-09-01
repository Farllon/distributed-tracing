using BlazorApp1;
using BlazorApp1.Clients;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient<AuthorsClient>(c =>
{
    c.BaseAddress = new Uri("http://localhost:5000");
});

builder.Services.AddHttpClient<PostsClient>(c =>
{
    c.BaseAddress = new Uri("http://localhost:5002");
});

await builder.Build().RunAsync();
