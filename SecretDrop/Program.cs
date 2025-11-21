using Microsoft.AspNetCore.Http.Features;
using SecretDrop.Configurations;
using SecretDrop.Services.Implementations;
using SecretDrop.Services.Interfaces;
using SecretDrop.Workers;

var builder = WebApplication.CreateBuilder(args);

// 1. Загружаем конфиг
// (Автоматически подтянет appsettings.json и ENV переменные)
builder.Services.Configure<AppOptions>(
    builder.Configuration.GetSection(AppOptions.Section));

// Получаем конфиг прямо сейчас, чтобы настроить Kestrel
var appOptions = builder.Configuration
    .GetSection(AppOptions.Section)
    .Get<AppOptions>() ?? new AppOptions();

// 2. Настраиваем лимиты Kestrel из конфига
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = appOptions.MaxUploadSizeBytes;
});

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = appOptions.MaxUploadSizeBytes;
});

// 3. Регистрация сервисов
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ISecretFileStore, FileStoreService>();
builder.Services.AddHostedService<FileCleanupWorker>();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true; // Превращает /View/AbC в /view/abc
    options.LowercaseQueryStrings = false; // Query string (?a=B) лучше не трогать, там может быть Base64
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();