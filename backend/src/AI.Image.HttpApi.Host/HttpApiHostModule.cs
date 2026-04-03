using AI.Image.EntityFrameworkCore;
using AI.Image.Filters;
using AI.Image.Middlewares;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Timing;
using Volo.Abp.VirtualFileSystem;

namespace AI.Image;

[DependsOn(
    typeof(HttpApiModule),
	typeof(ApplicationModule),
	typeof(EntityFrameworkCoreModule),
	typeof(AbpAutofacModule),//替换 ASP.NET Core 默认的依赖注入容器，提供更强大的依赖注入功能
	typeof(AbpDistributedLockingModule),
	typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)//用于集成 Swagger/OpenAPI 的模块，它基于流行的 Swashbuckle.AspNetCore 库
)]
public class HttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();
		ConfigureAutoApiControllers();
        ConfigureDistributedLocking(context.Services);
        ConfigureCors(context.Services, configuration);
        ConfigureSwaggerServices(context.Services);
		
		Configure<MvcOptions>(options =>
		{
			options.Filters.Add<ApiExceptionFilter>();
			options.Filters.Add<ApiResultFilter>();
		});

		Configure<AbpAntiForgeryOptions>(options =>
		{
			options.AutoValidate = false;
			options.TokenCookie.Name = "Image.XSRF-TOKEN";
			options.TokenCookie.SameSite = SameSiteMode.Lax;
			options.TokenCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
		});

		Configure<AbpClockOptions>(options =>
		{
			options.Kind = DateTimeKind.Local;
		});

		context.Services.AddHttpClient();
	}

    private void ConfigureAutoApiControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(ApplicationModule).Assembly);
        });
    }

	private static void ConfigureSwaggerServices(IServiceCollection services)
    {
		services.AddAbpSwaggerGen(options =>
		{
			options.SwaggerDoc("v1", new OpenApiInfo { Title = "Image API", Version = "v1", Description = "" });
			options.DocInclusionPredicate((docName, description) => true);
			options.CustomSchemaIds(type => type.FullName);
			options.HideAbpEndpoints();
		});
	}

    private static void ConfigureDistributedLocking(IServiceCollection services)
    {
		services.AddSingleton<IDistributedLockProvider>(sp =>
		{
			var lockFileDirectory = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "distributed-locks"));
			if (!lockFileDirectory.Exists)
			{
				lockFileDirectory.Create();
			}
			return new FileDistributedSynchronizationProvider(lockFileDirectory);
		});
	}

    private static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
    {
		var allowedOrigins = configuration.GetSection("CorsOrigins").Get<string[]>() ?? [];
		services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
				builder.WithOrigins(allowedOrigins)
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
		var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
		app.UseAbpRequestLocalization();
		app.UseApiResponseHandler();
		app.UseCorrelationId();
        app.UseRouting();
        app.UseCors();
		app.UseUnitOfWork();
        app.UseDynamicClaims();
		app.UseSwagger();
		app.UseAbpSwaggerUI(options =>
		{
			options.SwaggerEndpoint("/swagger/v1/swagger.json", "Image API");
			options.DefaultModelsExpandDepth(-1);
			options.DisplayRequestDuration();
		});
		app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }
}