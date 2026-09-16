using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

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
            Centipedes = config.Bind<bool>("Arachno_Centis", true, new ConfigurableInfo("Whether or not centipedes are transformed"));
            Crabs = config.Bind<bool>("Arachno_Crabs", true, new ConfigurableInfo("Whether or not crabs are transformed"));
            Barnacles = config.Bind<bool>("Arachno_Barnacles", true, new ConfigurableInfo("Whether or not barnacles are transformed"));

            SpidersFull = config.Bind<bool>("Arachno_SpiderFull", false, new ConfigurableInfo("Whether or not Coalescipedes use the full \"Spider\" text"));
        }

        // private UIelement[] UIArrPlayerOptions;
        public static Configurable<bool> Spiders;
        public static Configurable<bool> SpidersFull;
        public static Configurable<bool> RotCysts;
        public static Configurable<bool> Noots;
        public static Configurable<bool> Eggbugs;
        public static Configurable<bool> Dropwigs;
        public static Configurable<bool> Centipedes;
        public static Configurable<bool> Crabs;
        public static Configurable<bool> Barnacles;

        public override void Initialize()
        {
            base.Initialize();

            // Initialize tab
            var opTab = new OpTab(this, "Options");
            Tabs = [opTab];

            // Add stuff to tab
            opTab.AddItems(new OpLabel(10f, 560f, "OPTIONS", true));

            float y = 530f;
            AddCheckbox(Spiders, "Spiders", ref y);
            AddCheckbox(SpidersFull, "Coalescipede full \"Spider\" text", ref y);
            AddCheckbox(RotCysts, "Rot long legs", ref y);
            AddCheckbox(Noots, "Noodleflies", ref y);
            AddCheckbox(Eggbugs, ModManager.MSC ? "Eggbugs/Firebugs" : "Eggbugs", ref y);
            AddCheckbox(Dropwigs, "Dropwigs", ref y);
            AddCheckbox(Centipedes, "Centipedes", ref y);
            if (ModManager.Watcher)
            {
                AddCheckbox(Crabs, "Drill Crabs", ref y);
                AddCheckbox(Barnacles, "Barnacles", ref y);
            }

            void AddCheckbox(Configurable<bool> config, string displayText, ref float y)
            {
                var cb = new OpCheckBox(config, new Vector2(10f, y));
                var label = new OpLabel(40f, y, displayText)
                {
                    bumpBehav = cb.bumpBehav
                };
                opTab.AddItems(cb, label);
                y -= 30f;
            }
        }
    }
}