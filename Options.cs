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
        }

        // private UIelement[] UIArrPlayerOptions;
        public readonly Configurable<bool> Spiders;
        public readonly Configurable<bool> RotCysts;
        public readonly Configurable<bool> Noots;

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
                new OpCheckBox(RotCysts, new(10f, 500f)),
                new OpLabel(40f, 500f, "Rot cysts"),
                new OpCheckBox(Noots, new(10f, 470f)),
                new OpLabel(40f, 470f, "Noodleflies")
            );
        }
    }
}