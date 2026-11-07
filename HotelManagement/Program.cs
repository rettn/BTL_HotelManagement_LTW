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
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".HotelManagement.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<PaymentSettingService>();
builder.Services.AddScoped<SePayService>();
builder.Services.AddHostedService<DatPhongHoldExpirationService>();

builder.Services.AddControllersWithViews();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Lưu cookie trong 30 ngày
    options.SlidingExpiration = true; // Tự động gia hạn thời gian nếu người dùng có tương tác
});

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

// Quan ly nhan vien
builder.Services.AddScoped<NhanVienRepository>();
builder.Services.AddScoped<NhanVienService>();

//Quan ly chuc vu
builder.Services.AddScoped<ChucVuRepository>();
builder.Services.AddScoped<ChucVuService>();

// Repository quan ly luong
builder.Services.AddScoped<BangLuongRepository>();
builder.Services.AddScoped<ThuongRepository>();
builder.Services.AddScoped<PhuCapRepository>();
builder.Services.AddScoped<KhauTruRepository>();

// Service quan ly luong
builder.Services.AddScoped<BangLuongService>();
builder.Services.AddScoped<ThuongService>();
builder.Services.AddScoped<PhuCapService>();
builder.Services.AddScoped<KhauTruService>();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

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

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<HotelManagement.Security.ModulePermissionMiddleware>();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
