namespace Comet.Launcher.Server.States.Patches
{
    public struct UpdateStruct
    {
        public int From { get; set; }
        public int To { get; set; }

        public string FileName => IsBundle ? $"{From}-{To}" : $"{From}";
        public string Extension { get; set; }
        public string FullFileName => $"{FileName}.{Extension}";

        public string Hash { get; set; }

        public bool IsBundle => To == 0 || From != To;
        /// <summary>
        /// If the current patch is a updater client patch.
        /// </summary>
        public bool IsUpdate => From >= 10000;
    }
}
