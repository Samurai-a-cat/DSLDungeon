using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DSLDungeon;
using DSLDungeon.Services;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<GameUiAgent>(sp => sp.GetRequiredService<GameService>().UiAgent);

builder.Services.AddSingleton<DslCompilerService>(sp =>
{
    var http = sp.GetRequiredService<HttpClient>();
    return new DslCompilerService(http);
});

builder.Services.AddSingleton<CodeEditorService>(sp =>
{
    var compiler = sp.GetRequiredService<DslCompilerService>();
    var game = sp.GetRequiredService<GameService>();
    var js = sp.GetRequiredService<IJSRuntime>();
    return new CodeEditorService(compiler, game, js);
});

await builder.Build().RunAsync();
