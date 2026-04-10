using CafePos.Data;
using CafePos.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Register Photo Cloudinary
builder.Services.AddScoped<PhotoService>();

builder.Services.AddDbContext<CafePosDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// ---- THÊM ĐOẠN NÀY ĐỂ KÍCH HOẠT COOKIE AUTHENTICATION ----
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn bị đá về nếu chưa đăng nhập
        options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn khi không có quyền
    });
// ĐĂNG KÝ CÁC SERVICE TẠI ĐÂY
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
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
app.UseAuthentication();
app.UseAuthorization();

// 1. Route dành cho Areas - Phải đặt TRÊN cùng
// Đặt đoạn này nằm TRÊN MapControllerRoute mặc định
app.MapControllerRoute(
    name: "MyAreaAdmin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 2. Route mặc định cho khách
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Thêm dòng này vào để API hoạt động
app.MapControllers();


// ---- THÊM ĐOẠN NÀY ĐỂ AUTO TẠO DB VÀ DỮ LIỆU ----
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CafePosDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        // Ghi log lỗi nếu quá trình khởi tạo thất bại
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi tạo dữ liệu mẫu (Seeding Database).");
    }
}
// ------------------------------------------------

app.Run();
