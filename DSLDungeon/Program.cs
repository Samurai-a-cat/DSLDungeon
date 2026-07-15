using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DSLDungeon;
using DSLDungeon.Services;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<GameUiAgent>(sp => sp.GetRequiredService<GameService>().UiAgent);

builder.Services.AddSingleton<DslCompilerService>();
builder.Services.AddSingleton<CodeEditorService>(sp =>
{
    var compiler = sp.GetRequiredService<DslCompilerService>();
    var js = sp.GetRequiredService<IJSRuntime>();
    return new CodeEditorService(compiler, js);
});

await builder.Build().RunAsync();
