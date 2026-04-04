using BRMSBS_capstoneproject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddDbContext<MyAppContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

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

// Enable session before routing so controllers can access HttpContext.Session
app.UseSession();

// Middleware to add no-cache headers for HTML responses to prevent browser from caching protected pages
app.Use(async (context, next) =>
{
    context.Response.OnStarting(state =>
    {
        var httpContext = (HttpContext)state;
        var ct = httpContext.Response.ContentType ?? string.Empty;
        if (ct.Contains("text/html") || ct == string.Empty)
        {
            httpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            httpContext.Response.Headers["Pragma"] = "no-cache";
            httpContext.Response.Headers["Expires"] = "0";
        }
        return Task.CompletedTask;
    }, context);

    await next();
});

// Authentication guard: redirect to login when session is not authenticated
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    var lower = path.ToLowerInvariant();

    // Allow static files and login endpoints through
    if (lower.StartsWith("/lib") || lower.StartsWith("/css") || lower.StartsWith("/js") || lower.StartsWith("/images") || lower.StartsWith("/favicon.ico")
        || lower.StartsWith("/system/login") || lower.StartsWith("/system/logout") || lower.StartsWith("/home/error"))
    {
        await next();
        return;
    }

    try
    {
        var isAuth = context.Session.GetString("IsAuthenticated");
        if (string.IsNullOrEmpty(isAuth))
        {
            context.Response.Redirect("/System/Login");
            return;
        }
    }
    catch
    {
        // if session not available, allow next so login can proceed
    }

    await next();
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=System}/{action=HomeDashboardAdmin}/{id?}");

app.Run();


