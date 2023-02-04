using Havit.Blazor.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SDMetaTool;
using SDMetaTool.Cache;
using SDMetaTool.Processors;
using System.IO.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
AddCustomServices(builder);
builder.Services.AddControllers();
builder.Services.AddHxServices();        // <------ ADD THIS LINE
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static void AddCustomServices(WebApplicationBuilder builder)
{
	builder.Services.AddSingleton<IFileSystem, FileSystem>();
	builder.Services.AddTransient<IPngFileDataSource, SqliteDataSource>();
	builder.Services.AddSingleton<IFileLister, FileLister>();
	builder.Services.AddTransient<IPngFileLoader>(x => 
		new CachedPngFileLoader(x.GetRequiredService<IFileSystem>(),
		new PngFileLoader(x.GetRequiredService<IFileSystem>()),
		x.GetRequiredService<IPngFileDataSource>()
		));
	builder.Services.AddTransient<Rescan>();
}