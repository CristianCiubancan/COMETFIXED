namespace Comet.Tools.GM.UI.Models
{
    public class AppSection
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public string IconDark { get; set; }
        public Type TargetType { get; set; }
        public bool MustBeSigned { get; set; } = true;

    }
}
