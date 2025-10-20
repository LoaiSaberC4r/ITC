namespace BuildingBlock.Api.Options
{
    public sealed class SharedLocalizationOptions
    {
        public string[] SupportedCultures { get; set; } = new[] { "ar", "en" };

        public string DefaultCulture { get; set; } = "en";

        public bool AllowQueryStringLang { get; set; } = true;
    }
}