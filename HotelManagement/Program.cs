using HotelManagement.Data;
using HotelManagement.Repositories;
using HotelManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection")));
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Chua cau hinh ConnectionStrings:DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

//Quan ly dat phong
builder.Services.AddScoped<DatPhongRepository>();
builder.Services.AddScoped<DatPhongService>();

// Quan ly loai phong
builder.Services.AddScoped<LoaiPhongRepository>();
builder.Services.AddScoped<LoaiPhongService>();

// Quan ly phong
builder.Services.AddScoped<PhongRepository>();
builder.Services.AddScoped<PhongService>();

// Quan li khach hang
builder.Services.AddScoped<KhachHangRepository>();
builder.Services.AddScoped<KhachHangService>();

//Quan li dat phong
builder.Services.AddScoped<DatPhongRepository>();
builder.Services.AddScoped<DatPhongService>();

var app = builder.Build();
await IdentitySeeder.SeedAsync(
    app.Services,
    app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
