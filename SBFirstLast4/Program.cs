using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorDownloadFile;
using Blazored.LocalStorage;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;
using SBFirstLast4;
using SBFirstLast4.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazorJSRuntime();
builder.Services.AddWebWorkerService(s => s.TaskPool.MaxPoolSize = 24);

builder.Services.AddSingleton<IWordLoaderService, WordLoaderService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazorDownloadFile();

await builder.Build().BlazorJSRunAsync();