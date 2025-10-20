using BuildingBlock.Api.Localization.Providers;
using BuildingBlock.Api.Middleware;
using BuildingBlock.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BuildingBlock.Api.Bootstrap
{
    public static class LocalizationExtensions
    {
        public static IServiceCollection AddSharedLocalization(
            this IServiceCollection services,
            Action<SharedLocalizationOptions>? configure = null)
        {
            var opts = new SharedLocalizationOptions();
            configure?.Invoke(opts);

            // AddLocalization مطلوب حتى لو بنستخدم Strongly Typed Resources
            services.AddLocalization();

            services.AddSingleton(opts);

            services.AddOptions<RequestLocalizationOptions>().Configure<IServiceProvider>((options, sp) =>
            {
                var o = sp.GetRequiredService<SharedLocalizationOptions>();

                var cultures = o.SupportedCultures.Select(c => new CultureInfo(c)).ToList();
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
                options.DefaultRequestCulture = new RequestCulture(o.DefaultCulture);

                // ترتيب الـ Providers: QueryString ثم Accept-Language ثم Cookie (لو حبيت تضيفه لاحقًا)
                var providers = new List<IRequestCultureProvider>();

                if (o.AllowQueryStringLang)
                {
                    providers.Add(new QueryStringRequestCultureProvider
                    {
                        // يستخدم ?culture= أو ?ui-culture= افتراضيًا
                        // ونضيف دعم ?lang= أيضًا (مخصص) عبر wrapper بسيط:
                        // سنستخدم QueryStringRequestCultureProvider كما هو، ومع ذلك يمكنك قبلها معالجة "lang" إن أردت.
                    });
                }

                providers.Add(new SimpleAcceptLanguageProvider(o));
                providers.Add(new AcceptLanguageHeaderRequestCultureProvider()); // fallback

                options.RequestCultureProviders = providers;
            });

            return services;
        }

        public static IApplicationBuilder UseSharedLocalization(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(options);

            app.UseMiddleware<ResponseContentLanguageMiddleware>();
            return app;
        }
    }
}

/*
 * builder.Services.AddSharedLocalization(opts =>
{
    opts.SupportedCultures = new[] { "ar", "en" };
    opts.DefaultCulture = "ar";               // لو حاب تخليها "en" غيّرها
    opts.AllowQueryStringLang = true;         // يدعم ?culture=ar أو ?lang=ar
});
*/

//After  var app = builder.Build();

/*
 * app.UseSharedLocalization();
*/