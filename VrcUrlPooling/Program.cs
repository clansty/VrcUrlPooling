using Microsoft.EntityFrameworkCore;
using VrcUrlPooling;
using VrcUrlPooling.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<UrlRegisterService>();

var connectionString = builder.Configuration.GetSection("MySqlConnectionString").Value;
var serverVersion = ServerVersion.AutoDetect(connectionString);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString!, serverVersion));

var app = builder.Build();
app.MapControllers();
app.Run();