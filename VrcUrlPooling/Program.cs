using Microsoft.EntityFrameworkCore;
using VrcUrlPooling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetSection("MySqlConnectionString").Value;
var serverVersion = ServerVersion.AutoDetect(connectionString);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString!, serverVersion));

var app = builder.Build();
app.MapControllers();
app.Run();