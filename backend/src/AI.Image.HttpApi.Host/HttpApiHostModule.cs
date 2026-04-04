using AI.Image.EntityFrameworkCore;
using AI.Image.Filters;
using AI.Image.Middlewares;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Timing;

namespace AI.Image;

[DependsOn(
	typeof(HttpApiModule),
	typeof(ApplicationModule),
	typeof(EntityFrameworkCoreModule),
	typeof(AbpAspNetCoreSerilogModule),
	typeof(AbpAutofacModule),	//替换 ASP.NET Core 默认的依赖注入容器
	typeof(AbpSwashbuckleModule)//集成 Swagger/OpenAPI
)]
public class HttpApiHostModule : AbpModule
{
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		var configuration = context.Services.GetConfiguration();
		var hostingEnvironment = context.Services.GetHostingEnvironment();
		ConfigureAutoApiControllers();
		ConfigureCors(context.Services, configuration);
		ConfigureSwaggerServices(context.Services);
		context.Services.AddHttpClient();

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
		app.UseStaticFiles(); // 提供 wwwroot 下的静态文件（上传的图片）
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