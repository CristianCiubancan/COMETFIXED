namespace Comet.Tools.GM.UI.Pages
{
    public static class PagesExtensions
    {
        public static MauiAppBuilder ConfigurePages(this MauiAppBuilder builder)
        {
            // Singleton
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<AboutPage>();

            // Transient
            builder.Services.AddTransient<DashboardPage>();
            return builder;
        }
    }
}
