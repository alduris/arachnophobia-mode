using BepInEx.Logging;
using Menu.Remix.MixedUI;

namespace SpiderMod
{
    internal class Options : OptionInterface
    {
        private readonly ManualLogSource logger;

        public Options(ManualLogSource loggerSource)
        {
            logger = loggerSource;

            Spiders = config.Bind<bool>("Arachno_Spiders", true, new ConfigurableInfo("Whether or not spiders are transformed"));
            RotCysts = config.Bind<bool>("Arachno_Rot", false, new ConfigurableInfo("Whether or not DLLs are transformed"));
            Noots = config.Bind<bool>("Arachno_Noots", false, new ConfigurableInfo("Whether or not noots are transformed"));
            Eggbugs = config.Bind<bool>("Arachno_Eggbugs", true, new ConfigurableInfo("Whether or not eggbugs and firebugs are transformed"));
            Dropwigs = config.Bind<bool>("Arachno_Dropwigs", true, new ConfigurableInfo("Whether or not dropwigs are transformed"));

            SpidersFull = config.Bind<bool>("Arachno_SpiderFull", false, new ConfigurableInfo("Whether or not spiders are transformed"));
        }

        // private UIelement[] UIArrPlayerOptions;
        public static Configurable<bool> Spiders;
        public static Configurable<bool> SpidersFull;
        public static Configurable<bool> RotCysts;
        public static Configurable<bool> Noots;
        public static Configurable<bool> Eggbugs;
        public static Configurable<bool> Dropwigs;

        public override void Initialize()
        {
            base.Initialize();

            // Initialize tab
            var opTab = new OpTab(this, "Options");
            Tabs = [opTab];

            // Add stuff to tab
            opTab.AddItems(
                new OpLabel(10f, 560f, "OPTIONS", true),
                new OpCheckBox(Spiders, new(10f, 530f)),
                new OpLabel(40f, 530f, "Spiders"),
                new OpCheckBox(SpidersFull, new(10f, 500f)),
                new OpLabel(40f, 500f, "Coalescipede full \"Spider\" text"),
                new OpCheckBox(RotCysts, new(10f, 470f)),
                new OpLabel(40f, 470f, "Rot cysts"),
                new OpCheckBox(Noots, new(10f, 440f)),
                new OpLabel(40f, 440f, "Noodleflies"),
                new OpCheckBox(Eggbugs, new(10f, 410f)),
                new OpLabel(40f, 410f, ModManager.MSC ? "Eggbugs/Firebugs" : "Eggbugs"),
                new OpCheckBox(Dropwigs, new(10f, 380f)),
                new OpLabel(40f, 380f, "Dropwigs")
            );
        }
    }
}