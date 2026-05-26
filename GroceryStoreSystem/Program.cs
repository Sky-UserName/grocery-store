using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
});
builder.Services.AddControllers();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "grocery_admin";
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<MobileAccessTokenService>();
builder.Services.AddSingleton<QrCodeService>();
builder.Services.AddScoped<SqlDataStore>();
builder.Services.AddScoped<DatabaseInitializer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
