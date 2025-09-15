using Web.POC.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

builder.Services.AddAppServices(builder.Configuration);

var mvc = builder.Services.AddControllersWithViews();

var app = builder.Build();

// typical middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();          // wwwroot

app.UseRouting();
app.UseAuthentication();       // if you added auth in AddAppServices
app.UseAuthorization();

// default conventional route: /{controller=Home}/{action=Index}/{id?}
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();