using LedApp.Hubs;
using LedApp.MiddlewareExtensions;
using LedApp.Models;
using LedApp.Repositories;
using LedApp.SubscribeTableDependencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//add dbcontext
//dbcontext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    {
        options.UseSqlServer(connectionString);
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
    ,
    ServiceLifetime.Singleton
);
builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true).AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDBContext>();


//add SignalR
builder.Services.AddSignalR();
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});
//Add sql denpendecy
// DI
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<CuaXuatResposity>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<SignalServer>();
builder.Services.AddSingleton<SubscribeChitietXuatTableDependency>();
builder.Services.AddSingleton<SubscribeNhapTableDependency>();        // ? thêm m?i
builder.Services.AddSingleton<SubscribeChitietNhapTableDependency>();
builder.Services.AddSingleton<SubscribeDulieuxuatTableDependency>();
// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

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
app.UseAuthentication();;

app.UseAuthorization();
app.UseSession();
app.MapRazorPages();
app.MapHub<SignalServer>("/signalserver");

app.MapAreaControllerRoute(
    name: "MyAreaAdmin",
    areaName: "admin",
    pattern: "admin/{controller=Trangchu}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Administrator}/{action=Index}/{id?}");

//Ki?ch hoa?t SubscribeTableDependency
app.UseSqlTableDependency<SubscribeChitietXuatTableDependency>(connectionString);
app.UseSqlTableDependency<SubscribeNhapTableDependency>(connectionString);        // ? thêm m?i
app.UseSqlTableDependency<SubscribeChitietNhapTableDependency>(connectionString); // ? thêm m?i
app.UseSqlTableDependency<SubscribeDulieuxuatTableDependency>(connectionString);
app.Run();
