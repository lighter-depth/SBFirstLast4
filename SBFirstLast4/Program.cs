using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SBFirstLast4;
using Blazored.LocalStorage;
using Magic.IndexedDb.Extensions;
using Magic.IndexedDb.Helpers;
using BlazorDownloadFile;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazorDownloadFile(ServiceLifetime.Scoped);
string EncryptionKey = "zQfTuWnZi8u7x!A%C*F-JaBdRlUkXp2l";

builder.Services.AddBlazorDB(options =>
{
	options.Name = SBUtils.DB_NAME;
	options.Version = "1";
	options.EncryptionKey = EncryptionKey;
	options.StoreSchemas = SchemaHelper.GetAllSchemas(SBUtils.DB_NAME);
});
var host = builder.Build();

await host.RunAsync();
