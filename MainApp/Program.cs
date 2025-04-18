using Repository.Interfaces;
using Repository.Models;
using Repository.Repositories;
using Service.Helpers;
using Service.Interfaces;
using Service.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("netmusicshop-b5d9c-firebase-adminsdk-fbsvc-85c243175f.json")
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddSingleton<MailSender>();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddDbContext<MusicShopDBContext>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
