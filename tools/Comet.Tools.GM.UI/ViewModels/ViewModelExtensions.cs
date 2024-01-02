namespace Comet.Tools.GM.UI.ViewModels
{
    public static class ViewModelExtensions
    {
        public static MauiAppBuilder ConfigureViewModels(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<ShellViewModel>();
            return builder;
        }      
    }
}
