using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TetGift.BackgroundJobs;
using TetGift.BLL.Common;
using TetGift.BLL.Interfaces;
using TetGift.BLL.Services;
using TetGift.DAL.Context;
using TetGift.DAL.Interfaces;
using TetGift.DAL.Repositories;
using TetGift.DAL.UnitOfWork;
using TetGift.Filters;
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

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ApiResponseWrapperFilter>();
            });

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

                    options.Events = new JwtBearerEvents
                    {
                        // 401
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                context.Response.ContentType = "application/json";

                                var payload = new ApiResponse<object?>
                                {
                                    Status = 401,
                                    Msg = "Unauthorized.",
                                    Data = null
                                };

                                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });

                                await context.Response.WriteAsync(json);
                            }
                        },

                        // 403
                        OnForbidden = async context =>
                        {
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.ContentType = "application/json";

                                var payload = new ApiResponse<object?>
                                {
                                    Status = 403,
                                    Msg = "Forbidden.",
                                    Data = null
                                };

                                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });

                                await context.Response.WriteAsync(json);
                            }
                        },

                        OnAuthenticationFailed = context =>
                        {
                            return Task.CompletedTask;
                        }
                    };
                });


            builder.Services.AddAuthorization();

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TetGift API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập: Bearer {your_jwt_token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            //builder.Services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            //    {
            //        Title = "TetGift",
            //        Version = "v1"
            //    });

            //    // Thêm JWT Bearer authentication vào Swagger
            //    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            //    {
            //        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            //        Name = "Authorization",
            //        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            //        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            //        Scheme = "Bearer",
            //        BearerFormat = "JWT"
            //    });

            //    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            //    {
            //        {
            //            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            //            {
            //                Reference = new Microsoft.OpenApi.Models.OpenApiReference
            //                {
            //                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            //                    Id = "Bearer"
            //                }
            //            },
            //            Array.Empty<string>()
            //        }
            //    });
            //});

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
            builder.Services.AddScoped<IProductDetailService, ProductDetailService>();
            //Khuyen mai, ton kho
            builder.Services.AddScoped<IPromotionService, PromotionService>();
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IQuotationService, QuotationService>();
            builder.Services.AddScoped<IOrderFromQuotationService, OrderFromQuotationService>();
            //Gio hang
            builder.Services.AddScoped<ICartService, CartService>();
            //Don hang
            builder.Services.AddScoped<IOrderService, OrderService>();
            //Thanh toan
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            //Dashboard
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // Enable Swagger for all environments
            app.UseSwagger();
            app.UseSwaggerUI();

            // Only redirect to HTTPS in production with SSL configured
            if (!app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            //Middleware
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
