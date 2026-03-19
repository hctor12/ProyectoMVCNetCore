using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProyectoMVCNetCore.Data;
using ProyectoMVCNetCore.Repositories;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configurar Licencia de QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
string connectionString = builder.Configuration.GetConnectionString("GestionIncidencias")!;

builder.Services.AddDbContext<IncidenciasContext>(options =>
    options.UseSqlServer(connectionString));

// Inyectar Repositorios
builder.Services.AddTransient<IRepositoryIncidencias, RepositoryIncidencias>();

// Configurar SignalR
builder.Services.AddSignalR();

// Políticas de Autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",
        policy => policy.RequireRole("Admin"));
    options.AddPolicy("TecnicoOrAdmin",
        policy => policy.RequireRole("Admin", "Tecnico"));
});

// Configurar Cookie Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(
    CookieAuthenticationDefaults.AuthenticationScheme,
    config =>
    {
        config.AccessDeniedPath = "/Auth/AccesoDenegado";
    });

// Session (solo para TempData de redirección post-login)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews(options =>
    options.EnableEndpointRouting = false)
    .AddSessionStateTempDataProvider();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseMvc(routes =>
{
    routes.MapRoute(
        name: "default",
        template: "{controller=Home}/{action=Index}/{id?}");
});

app.MapHub<ProyectoMVCNetCore.Hubs.IncidenciasHub>("/incidenciasHub");
app.MapHub<ProyectoMVCNetCore.Hubs.PresenceHub>("/presenceHub");

app.Run();
