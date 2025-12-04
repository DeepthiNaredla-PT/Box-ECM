using ECM_API.Models;
using ECM_API.Services;

namespace ECM_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.Configure<BoxOptions>(builder.Configuration.GetSection("Box"));
            builder.Services.AddControllersWithViews();

            builder.Services.AddSingleton<Store>();
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<BoxAuthService>();
            builder.Services.AddSingleton<BoxApiService>();
            builder.Services.AddSingleton<TokenService>();
            builder.Services.AddSingleton<ScormProcessingService>();
            builder.Services.AddSingleton<TriggerService>();
            builder.Services.AddCors(o => o.AddPolicy("react", p =>
                p.AllowAnyOrigin()
                 .AllowAnyHeader()
                 .AllowAnyMethod()
            ));

            var app = builder.Build();
            app.UseCors("react");
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
