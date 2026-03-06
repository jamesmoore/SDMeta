using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libjpeg;
using PhotoSauce.NativeCodecs.Libpng;
using SDMeta;
using SDMeta.Api.Endpoints;
using SDMeta.Api.Services;
using SDMeta.Auto1111;
using SDMeta.Cache;
using SDMeta.Comfy;
using SDMeta.Parameterless;
using SDMeta.Processors;
using System.IO.Abstractions;

CodecManager.Configure(codecs =>
{
    codecs.UseLibpng();
    codecs.UseLibjpeg();
});

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<DataPath>();
builder.Services.AddSingleton<DbPath>();
builder.Services.AddSingleton<IFileLister, FileLister>();
builder.Services.AddSingleton<IParameterDecoder, ParameterDecoder>();
builder.Services.AddSingleton<Auto1111ParameterDecoder>();
builder.Services.AddSingleton<ComfyUIParameterDecoder>();
builder.Services.AddSingleton<ParameterlessDecoder>();
builder.Services.AddSingleton<IImageDir, ImageDirSettings>();
builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
builder.Services.AddSingleton<IImageIdCodec, ImageIdCodec>();
builder.Services.AddSingleton<IImagePathAuthorizer, ImagePathAuthorizer>();
builder.Services.AddSingleton<PendingFileChangeQueue>();
builder.Services.AddSingleton<ScanJobManager>();

builder.Services.AddScoped<IImageFileDataSource, SqliteDataSource>();
builder.Services.AddScoped<ImageFileLoader>();
builder.Services.AddScoped<CachedImageFileLoader>();
builder.Services.AddScoped<IImageFileLoader>(x =>
    new RetryingFileLoader(
        new CachedImageFileLoader(
            x.GetRequiredService<ImageFileLoader>(),
            x.GetRequiredService<IImageFileDataSource>()
        ),
        x.GetRequiredService<ILogger<RetryingFileLoader>>()
    ));
builder.Services.AddScoped<Rescan>();

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapApiV1();

var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");

app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexPath = Path.Combine(webRoot, "index.html");
    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Frontend assets not found. Run 'npm run build:api' in SDMeta.Web.");
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indexPath);
});

using (var scope = app.Services.CreateScope())
{
    using var db = scope.ServiceProvider.GetRequiredService<IImageFileDataSource>();
    db.Initialize();
}

app.Run();
