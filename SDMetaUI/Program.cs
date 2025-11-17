using BlazorPro.BlazorSize;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.Extensions.FileProviders;
using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libjpeg;
using PhotoSauce.NativeCodecs.Libpng;
using SDMeta;
using SDMeta.Auto1111;
using SDMeta.Cache;
using SDMeta.Comfy;
using SDMeta.Parameterless;
using SDMeta.Processors;
using SDMetaUI;
using SDMetaUI.Controllers;
using SDMetaUI.Models;
using SDMetaUI.Services;
using System;
using System.IO.Abstractions;

CodecManager.Configure(codecs => {
	codecs.UseLibpng();
	codecs.UseLibjpeg();
});

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
AddCustomServices(builder);
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.MapGet("/images/thumb/{path:required}", ImagesController.GetThumb);
app.MapGet("/images/full/{path:required}/{realfilename}", ImagesController.GetFull);

// https://github.com/gmanvel/AspNetCoreSingleFileApp
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new EmbeddedFileProvider(
		 assembly: typeof(Program).Assembly,
		 baseNamespace: "SDMetaUI.wwwroot"),
});

app.UseStaticFiles();

app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    using var db = scope.ServiceProvider.GetRequiredService<IPngFileDataSource>();
    db.Initialize();
}

app.Run();

static void AddCustomServices(WebApplicationBuilder builder)
{
	builder.Services.AddHxServices();
	builder.Services.AddHxMessenger();
	builder.Services.AddResizeListener();

	builder.Services.AddSingleton<IFileSystem, FileSystem>();
	builder.Services.AddSingleton<DataPath>();
	builder.Services.AddSingleton<DbPath>();
	builder.Services.AddSingleton<IFileLister, FileLister>();
    builder.Services.AddSingleton<IParameterDecoder, ParameterDecoderFactory>();
    builder.Services.AddSingleton<Auto1111ParameterDecoder>();
    builder.Services.AddSingleton<ComfyUIParameterDecoder>();
    builder.Services.AddSingleton<ParameterlessDecoder>();

    builder.Services.AddScoped<IPngFileDataSource, SqliteDataSource>();
    builder.Services.AddScoped<PngFileLoader>();
	builder.Services.AddScoped<IPngFileLoader>(x => 
		new RetryingFileLoader(
		new CachedPngFileLoader(x.GetRequiredService<IFileSystem>(),
		x.GetRequiredService<PngFileLoader>(),
		x.GetRequiredService<IPngFileDataSource>()
		)));
	builder.Services.AddScoped<Rescan>();
	builder.Services.AddScoped<GalleryViewModel>();
	builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
	builder.Services.AddSingleton<PngFileViewModelBuilder>();
	builder.Services.AddSingleton<FileSystemObserver>();
	builder.Services.AddSingleton<IImageDir, ImageDir>();
	HxMessengerServiceExtensions.Defaults.InformationAutohideDelay = 1000;
}