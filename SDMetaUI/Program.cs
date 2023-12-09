using Havit.Blazor.Components.Web;
using System.IO.Abstractions;
using PhotoSauce.NativeCodecs.Libpng;
using PhotoSauce.NativeCodecs.Libjpeg;
using PhotoSauce.MagicScaler;
using SDMetaUI.Services;
using NLog.Web;
using BlazorPro.BlazorSize;
using SDMetaUI.Models;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.Extensions.FileProviders;
using SDMetaUI;
using SDMeta.Cache;
using SDMeta;
using SDMeta.Processors;

CodecManager.Configure(codecs => {
	codecs.UseLibpng();
	codecs.UseLibjpeg();
});

var builder = WebApplication.CreateBuilder(args);

// NLog: Setup NLog for Dependency injection
builder.Logging.ClearProviders();
builder.Host.UseNLog();

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

// https://github.com/gmanvel/AspNetCoreSingleFileApp
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new EmbeddedFileProvider(
		 assembly: typeof(Program).Assembly,
		 baseNamespace: "SDMetaUI.wwwroot"),
});

app.UseStaticFiles();

app.UseRouting();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static void AddCustomServices(WebApplicationBuilder builder)
{
	builder.Services.AddHxServices();
	builder.Services.AddHxMessenger();
	builder.Services.AddResizeListener();

	builder.Services.AddSingleton<IFileSystem, FileSystem>();
	builder.Services.AddSingleton<DataPath>();
	builder.Services.AddSingleton<DbPath>();
	builder.Services.AddScoped<IPngFileDataSource, SqliteDataSource>();
	builder.Services.AddSingleton<IFileLister, FileLister>();
	builder.Services.AddScoped<IPngFileLoader>(x => 
		new CachedPngFileLoader(x.GetRequiredService<IFileSystem>(),
		new PngFileLoader(x.GetRequiredService<IFileSystem>()),
		x.GetRequiredService<IPngFileDataSource>()
		));
	builder.Services.AddScoped<Rescan>();
	builder.Services.AddScoped<GalleryViewModel>();
	builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
	builder.Services.AddSingleton<PngFileViewModelBuilder>();
	builder.Services.AddSingleton<FileSystemObserver>();
	builder.Services.AddSingleton<IImageDir, ImageDir>();
	HxMessengerServiceExtensions.Defaults.InformationAutohideDelay = 1000;
}