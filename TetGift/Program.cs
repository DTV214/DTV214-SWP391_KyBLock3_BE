using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TetGift.BackgroundJobs;
using TetGift.BLL.Interfaces;
using TetGift.BLL.Services;
using TetGift.DAL.Context;
using TetGift.DAL.Interfaces;
using TetGift.DAL.Repositories;
using TetGift.DAL.UnitOfWork;
using TetGift.Middlewares;

namespace TetGift
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ConnectionString
            builder.Services.AddDbContext<DatabaseContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Cấu hình Jwt
            var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Missing config: Jwt:Key");
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                        ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
                        ValidIssuer = jwtIssuer,

                        ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
                        ValidAudience = jwtAudience,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            builder.Services.AddAuthorization();

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Đăng ký Background Services
            builder.Services.AddHostedService<PendingAccountCleanupService>();

            // Đăng ký các Repository
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Đăng ký các external Service
            builder.Services.AddScoped<IEmailTemplateRenderer, FileEmailTemplateRenderer>();
            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, http) =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var apiKey = cfg["Resend:ApiKey"];

                http.BaseAddress = new Uri("https://api.resend.com/");
                if (!string.IsNullOrWhiteSpace(apiKey))
                    http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);
            });

            // Đăng ký các Service
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IProductConfigService, ProductConfigService>();
            builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
            builder.Services.AddScoped<IConfigDetailService, ConfigDetailService>();
            builder.Services.AddScoped<IProductService, ProductService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            //Middleware
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseAuthorization();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
