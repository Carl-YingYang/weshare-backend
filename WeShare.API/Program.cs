using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WeShare.API.Data;
using WeShare.API.Repositories;
using WeShare.API.Services;

namespace WeShare.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ==========================================
            // 1. DATABASE CONFIGURATION
            // ==========================================
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ==========================================
            // 2. DEPENDENCY INJECTION 
            // ==========================================
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPostRepository, PostRepository>();
            builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPostService, PostService>();
            builder.Services.AddScoped<IFriendshipService, FriendshipService>();
            builder.Services.AddScoped<IUserService, UserService>();

            // ==========================================
            // 3. JWT AUTHENTICATION CONFIGURATION (ETO YUNG NAWALA KANINA!)
            // ==========================================
            var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!)),
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    };

                    // ── BAGONG CODE PARA SA SIGNALR (WEBSOCKETS) ──
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            // Kung nagko-connect siya sa ChatHub, basahin ang token sa URL
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // ==========================================
            // 4. CORS CONFIGURATION (Para sa React UI)
            // ==========================================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "https://weshare-frontend-prototype.vercel.app")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSignalR();

            // ==========================================
            // 5. SWAGGER SECURITY CONFIGURATION
            // ==========================================
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Mag-type ng: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey, // Binago ko ito para hindi na mag-doble ang 'Bearer'
                    Scheme = "Bearer"
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

            var app = builder.Build();

            // ==========================================
            // 6. HTTP REQUEST PIPELINE
            // ==========================================
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles(); // 🚨 YOU MUST ADD THIS HERE

            app.UseHttpsRedirection();
            app.UseCors("AllowReactApp");


            app.UseAuthentication(); // ?? MUST BE BEFORE AUTHORIZATION (Binabasa yung Token)
            app.UseAuthorization();  // ?? (Tinitignan kung may access ka ba talaga)

            app.MapControllers();
            app.MapHub<WeShare.API.Hubs.ChatHub>("/chathub");

            app.Run();
        }
    }
}