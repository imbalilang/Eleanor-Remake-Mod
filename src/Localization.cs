namespace EleanorRomance
{
    public static class L
    {
        public static string Get(string key, params object[] args)
        {
            return ModHelper.GetLocalized(ModConstants.ModId, key, args);
        }

        public static string Eleanor
        {
            get
            {
                string name = LocHelper.GetLocalizedClientName("name_eleanor");
                return string.IsNullOrEmpty(name) ? "Eleanor" : name;
            }
        }

        public const string Player = "PLAYER";
    }
}
