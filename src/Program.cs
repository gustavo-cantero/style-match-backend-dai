using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StyleMatch.Helpers;
using StyleMatch.Models;
using System.Text;

namespace StyleMatch;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        DataHelper.ConnectionString = builder.Configuration.GetConnectionString("StyleMatch")!;

        ConfigurationModel config = new();
        builder.Configuration.GetSection("App").Bind(config);
        // Sobrescribir datos SMTP con variables de entorno (si existen) - para envío del mail de contraseña
        config.SmtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? config.SmtpServer;
        config.SmtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out int port) ? port : config.SmtpPort;
        config.SmtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? config.SmtpUser;
        config.SmtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? config.SmtpPassword;
        config.SmtpFrom = Environment.GetEnvironmentVariable("SMTP_FROM") ?? config.SmtpFrom;

        builder.Services.AddSingleton(config);

        builder.Services.AddControllers();

        //JWT
        builder.Services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config.JWTValidIssuer,
                ValidAudience = config.JWTValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.JWTSecret))
            };
        });

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
