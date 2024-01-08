using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;
using SpriteLeaser = RoomCamera.SpriteLeaser;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SpiderMod
{
    internal static class CWTs
    {
        private static readonly ConditionalWeakTable<SpriteLeaser, FLabel> sleaserCWT = new();
        public static FLabel GetLabel(this SpriteLeaser spider) => sleaserCWT.GetValue(spider, self => {
            if (self.drawableObject is SpiderGraphics)
            {
                return new(Custom.GetFont(), "Spider");
            }
            else if (self.drawableObject is BigSpiderGraphics bug)
            {
                string s = "Big Spider";
                if (bug.Spitter) s = $"Spitter{Environment.NewLine}Spider";
                else if (bug.Mother) s = $"Mother{Environment.NewLine}Spider";
                return new(Custom.GetFont(), s);
            }
            return null;
        });
    }

    [BepInPlugin("alduris.arachnophobia", "Arachnophobia Mode", "1.0.2")]
    internal class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;

        public Plugin()
        {
            try
            {
                Logger = base.Logger;
            }
            catch (Exception ex)
            {
                base.Logger.LogError(ex);
                throw;
            }
        }

        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private static bool Applied;
        private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (!Applied)
            {
                Applied = true;
                if (ModManager.ActiveMods.Exists(m => m.id == "alduris.wordworld"))
                {
                    Logger.LogMessage("Hooks not applied because Word World detected");
                }
                else
                {
                    Logger.LogInfo("Hooking");
                    try
                    {
                        On.RoomCamera.SpriteLeaser.ctor += SpriteLeaser_ctor;
                        On.RoomCamera.SpriteLeaser.Update += SpriteLeaser_Update;
                        On.RoomCamera.SpriteLeaser.CleanSpritesAndRemove += SpriteLeaser_CleanSpritesAndRemove;

                        Logger.LogInfo("Success");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Ran into error");
                        Logger.LogError(e);
                    }
                }
            }
        }

        private static void SpriteLeaser_ctor(On.RoomCamera.SpriteLeaser.orig_ctor orig, SpriteLeaser self, IDrawable obj, RoomCamera rCam)
        {
            orig(self, obj, rCam);
            var label = CWTs.GetLabel(self);

            if (self.drawableObject is SpiderGraphics spiderGraf)
            {
                // Coalescipede
                label.alignment = FLabelAlignment.Center;
                label.scale = spiderGraf.spider.firstChunk.rad * 4f / LabelTest.GetWidth("Spider", false);
                label.color = rCam.currentPalette.blackColor;
                
                self.sprites[0].container.AddChild(label);
            }
            else if (self.drawableObject is BigSpiderGraphics bigSpidGraf)
            {
                // Big spider
                label.alignment = FLabelAlignment.Center;
                label.scale = (bigSpidGraf.bug.bodyChunks[0].rad + bigSpidGraf.bug.bodyChunks[1].rad + bigSpidGraf.bug.bodyChunkConnections[0].distance) * 1.5f / LabelTest.GetWidth(label.text, false);
                if (!bigSpidGraf.bug.mother && !bigSpidGraf.bug.spitter) label.scale *= 4f/3f;
                label.color = bigSpidGraf.yellowCol;

                self.sprites[0].container.AddChild(label);
            }
        }
        private static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(self, timeStacker, rCam, camPos);
            var label = CWTs.GetLabel(self);

            if (label != null)
            {
                foreach (var sprite in self.sprites)
                {
                    sprite.isVisible = false;
                }
            }

            if (self.drawableObject is SpiderGraphics spiderGraf)
            {
                // Coalescipede
                var pos = self.sprites[spiderGraf.BodySprite].GetPosition();
                var rot = self.sprites[spiderGraf.BodySprite].rotation;

                label.SetPosition(pos);
                label.rotation = rot;
            }
            else if (self.drawableObject is BigSpiderGraphics bigSpidGraf)
            {
                // Big spider
                var pos = bigSpidGraf.bug.bodyChunks[1].pos - camPos;
                var rot = self.sprites[bigSpidGraf.HeadSprite].rotation;

                // Force rotation to be between 0 and 180 degrees
                if (rot < 0) rot += 180f * ((int)rot / 180 + 1);
                rot %= 180f;
                rot -= 90f;

                label.SetPosition(pos);
                label.rotation = rot;
            }
        }
        private static void SpriteLeaser_CleanSpritesAndRemove(On.RoomCamera.SpriteLeaser.orig_CleanSpritesAndRemove orig, SpriteLeaser self)
        {
            orig(self);

            // Remove label
            CWTs.GetLabel(self)?.RemoveFromContainer();
        }

    }
}
