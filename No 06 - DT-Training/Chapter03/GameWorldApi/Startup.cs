using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; //Eklendi
using NorthwindLib; //Eklendi
using System.IO; //Eklendi
using GameWorldApi.Repository; //Eklendi
// Swagger desteği için eklenenler
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks; // HealtChecksOptions sınıfı kullanımı için eklendi
using Microsoft.AspNetCore.Http; // HttpResonse.WriteAsync kullanımı için eklendi

namespace GameWorldApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // SQLite veritabanımızı kullanacak şekilde EF Servisimizi ekliyoruz.
            // Böylece Controller veya ilgili sınıflare constructor üzerinden Context nesnesini enjekte edebiliriz.
            string dbPath = Path.Combine("..", "NorthwindGameCatalog.db");
            services.AddDbContext<Northwind>(opts => opts.UseSqlite($"Data Source={dbPath}"));

            /* 
                Repository kaydını yapıyoruz.
                Yani çalışma zamanında ICompanyRepository ile çalışanlar için CompanyRepository nesnesi kullanılacak.
                CompanyController sınıfının Constructor metoduna dikkat.
            */
            services.AddScoped<ICompanyRepository, CompanyRepository>();

            /*
                Swagger'dan yararlanarak servisin 1.0 versiyonu için dokümantasyon desteği ekliyoruz.
                Ek olarak Configure metodunda yaptığımız ilaveler de var.
            */
            services.AddSwaggerGen(opt =>
            {
                opt.SwaggerDoc(
                    name: "v1",
                    info: new OpenApiInfo
                    {
                        Title = "Northwind Game Catalog Service API",
                        Version = "v1.0"
                    });
            });

            services.AddControllers();

            /* 
                Health Check senaryosu için aşağıdaki kod parçası eklendi.
                Bu basit senaryoda EF'in bir başka deyişle SQlite veritabanı bağlantısının durumunu kontrol ediyoruz.
                Bakalım sağlıklı mı?
                Çalışma zamanında https://localhost:5551/api/isefonline adresine gidip bunu kontrol edebiliriz.
            */
            services.AddHealthChecks().AddDbContextCheck<Northwind>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            /*
                Servisin sunduğu HTTP metodları için Swagger UI desteğini ekliyoruz.
                Servis API dokümantasyonuna erişmek için /swagger adresine gitmek yeterli olacaktır.
                Ayrıca SupportedSubmitMethos metodu ile test tarafında desteklenen HTTP metodlarını da belirtiyoruz.
                Örneğe göre HTTP Get, Post, Put, Delete operasyonları destekleniyor.    
            */
            app.UseSwagger();
            app.UseSwaggerUI(opt =>
            {
                opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Northwind Game Catalog Service Version 1.0");
                opt.SupportedSubmitMethods(new[] { SubmitMethod.Get, SubmitMethod.Post, SubmitMethod.Put, SubmitMethod.Delete });
            });

            /*
                Health Check işleminin sonucunu kontrol etmek için bir adrese gelmemiz gerekiyor.
                Yukarıda Northwind EF Context'i için bir health check opsiyonu eklemiştik.
                Bunun sonucunu çalışma zamanında görmek için aşağıdaki adrese gelinmesi yeterli.

                HealthCheckOptions nesnesini normalde Healthy ve Unhealthy şeklinde düz metin formatında olan
                çıktıları farklı şekilde sunmak için kullanabiliriz. JSON olur, HTML olur ;)
                
            */

            var hcOptions = new HealthCheckOptions();
            hcOptions.ResponseWriter = async (context, r) =>
            {
                context.Response.ContentType = "text/html";
                var status = $"<p>Northwind connection health check status; <b>{r.Status.ToString()}</b>. Response Time <b>{r.TotalDuration.TotalMilliseconds.ToString()}</b> milliseconds. ;)</p>";
                await context.Response.WriteAsync(status);
            };
            app.UseHealthChecks(path: "/api/isefonline", options: hcOptions);
        }
    }
}
