using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SBFirstLast4;
using Blazored.LocalStorage;
using BlazorDownloadFile;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazorDownloadFile(ServiceLifetime.Scoped);

var host = builder.Build();

await host.RunAsync();
