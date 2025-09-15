using ElfAndSafety.Persistence;
using ElfAndSafety.Services;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register event bus
builder.Services.AddSingleton<ElfAndSafety.Persistence.Cqrs.IEventBus, ElfAndSafety.Persistence.Cqrs.InMemoryEventBus>();

// Register the SQLite user repository and initialize DB
var connectionString = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=elfandsafety.db";
SqliteDbInitializer.Initialize(connectionString);
builder.Services.AddSingleton<IUserRepository>(sp => new SqliteUserRepository(connectionString, sp.GetRequiredService<ElfAndSafety.Persistence.Cqrs.IEventBus>()));

// Register the user service (read-model) which will subscribe to events and use repository for commands
builder.Services.AddSingleton<IUserService>(sp => new UserService(sp.GetRequiredService<IUserRepository>(), sp.GetRequiredService<ElfAndSafety.Persistence.Cqrs.IEventBus>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
