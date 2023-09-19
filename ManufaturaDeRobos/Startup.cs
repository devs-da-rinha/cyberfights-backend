using ManufaturaDeRobos.API;
using ManufaturaDeRobos.Data;
using ManufaturaDeRobos.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using MySql.EntityFrameworkCore;
using System.IO;
using System.Reflection;
using System.Text;

namespace ManufaturaDeRobos
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers();

            #region swagger settings
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", 
                    new OpenApiInfo { 
                        Title = "ManufaturaDeRobos", 
                        Version = "v1",
                        Description = "Manufatura de rob�s -- Melhor loja para se comprar um rob� para destruir a humanidade!",
                        Contact = new OpenApiContact
                        {
                            Name = "Caio Silva Ferrari",
                            Email = "caioferrari0484@gmail.com",
                            Url = new Uri("https://github.com/Caioferrari04")
                        }
                    });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = $"{Path.Combine(AppContext.BaseDirectory, xmlFile)}";
                c.IncludeXmlComments(xmlPath);
            });
            #endregion

            services.AddDbContext<ManufactoryContext>(options => options.UseMySQL("server=localhost;database=estudante;user=estudante;password=estudante"));

            services.AddDefaultIdentity<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ManufactoryContext>();

            #region transient configuration
            services.AddTransient<IRobotService, RobotSqlService>();
            services.AddTransient<RobotStaticService>();
            services.AddTransient<IAuthService, AuthService>();
            #endregion

            #region bearer auth
            var key = Encoding.ASCII.GetBytes(Configuration["Jwt:Key"]);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
            #endregion

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ManufaturaDeRobos v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            CreateRoles(serviceProvider);
        }

        private void CreateRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = Enum.GetNames(typeof(RoleType)); ;
            foreach (var role in roleNames)
            {
                var roleExist = roleManager.RoleExistsAsync(role);
                if (!roleExist.Result)
                {
                    var roleResult = roleManager.CreateAsync(new IdentityRole(role));
                    roleResult.Wait();
                }
            }
        }
    }
}
