using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using Color = UnityEngine.Color;
using SpriteLeaser = RoomCamera.SpriteLeaser;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SpiderMod
{
    internal static class CWTs
    {
        // https://stackoverflow.com/questions/3216085/split-a-pascalcase-string-into-separate-words
        internal static readonly Regex pascalRegex = new(@"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])");

        private static readonly ConditionalWeakTable<SpriteLeaser, FLabel[]> sleaserCWT = new();
        public static FLabel[] GetLabel(this SpriteLeaser spider) => sleaserCWT.GetValue(spider, self => {
            var font = Custom.GetFont();

            if (Plugin.options.Spiders.Value && self.drawableObject is SpiderGraphics)
            {
                return [new(font, "Spider")];
            }
            else if (Plugin.options.Spiders.Value && self.drawableObject is BigSpiderGraphics bug)
            {
                string s = "Big Spider";
                if (bug.Spitter) s = $"Spitter{Environment.NewLine}Spider";
                else if (bug.Mother) s = $"Mother{Environment.NewLine}Spider";
                return [new(font, s)];
            }
            else if (Plugin.options.RotCysts.Value && self.drawableObject is DaddyGraphics daddyGraf)
            {
                var type = daddyGraf.daddy.abstractCreature.creatureTemplate.type;
                int cut = type.value.IndexOf("LongLegs");
                string shortname = cut <= 0 ? pascalRegex.Replace(type.value, Environment.NewLine) : type.value.Substring(0, cut);
                if (ModManager.MSC)
                {
                    if (type == MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy) shortname = "Hunter";
                    else if (type == MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs) shortname = $"Your{Environment.NewLine}Mother";
                }
                List<FLabel> list = [new(font, shortname)];

                for (int i = 0; i < daddyGraf.daddy.tentacles.Length; i++)
                {
                    var tentacle = daddyGraf.daddy.tentacles[i];
                    int length = (int)(tentacle.idealLength / 20f);
                    int numOfOs = length - 7; // len("LongLegs") = 7
                    for (int j = 0; j < length; j++)
                    {
                        int k = (j >= 1 && j < 1 + numOfOs) ? 1 : (j < 1 ? j : j - numOfOs);
                        list.Add(new(font, "LongLeg"[k].ToString()));
                    }
                }
                return [.. list];
            }
            else if (Plugin.options.Noots.Value && self.drawableObject is NeedleWormGraphics nootGraf)
            {
                var type = nootGraf.worm.abstractCreature.creatureTemplate.type;
                int cut = type.value.IndexOf("Needle");
                if (cut == -1) cut = type.value.IndexOf("Noodle");
                if (cut == -1) cut = type.value.IndexOf("Noot");
                if (cut == -1) cut = type.value.Length;

                return [.. (type.value.Substring(0, cut) + "Noot").ToCharArray().Select(c => new FLabel(font, c.ToString()))];
            }
            return null;
        });
    }

    [BepInPlugin("alduris.arachnophobia", "Arachnophobia Mode", "1.0.3")]
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
        public static Options options;
        private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (!Applied)
            {
                Applied = true;
                options = new Options(Logger);
                MachineConnector.SetRegisteredOI("alduris.arachnophobia", options);
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
            var labels = CWTs.GetLabel(self);

            if (options.Spiders.Value && self.drawableObject is SpiderGraphics spiderGraf)
            {
                // Coalescipede
                labels[0].scale = spiderGraf.spider.firstChunk.rad * 4f / LabelTest.GetWidth("Spider", false);
                labels[0].color = rCam.currentPalette.blackColor;

                self.sprites[0].container.AddChild(labels[0]);
            }
            else if (options.Spiders.Value && self.drawableObject is BigSpiderGraphics bigSpidGraf)
            {
                // Big spider
                labels[0].scale = (bigSpidGraf.bug.bodyChunks[0].rad + bigSpidGraf.bug.bodyChunks[1].rad + bigSpidGraf.bug.bodyChunkConnections[0].distance) * 1.5f / LabelTest.GetWidth(labels[0].text, false);
                if (!bigSpidGraf.bug.mother && !bigSpidGraf.bug.spitter) labels[0].scale *= 4f/3f;
                labels[0].color = bigSpidGraf.yellowCol;

                self.sprites[0].container.AddChild(labels[0]);
            }
            else if (options.RotCysts.Value && self.drawableObject is DaddyGraphics daddyGraf)
            {
                // Main body chunk
                labels[0].scale = Mathf.Sqrt(daddyGraf.daddy.bodyChunks.Length) * daddyGraf.daddy.bodyChunks.Average(c => c.rad) * 2f / 20f;
                labels[0].color = daddyGraf.daddy.eyeColor;

                // Tentacles
                var tentacles = daddyGraf.daddy.tentacles;
                int k = 1;

                for (int i = 0; i < tentacles.Length; i++)
                {
                    var tentacle = tentacles[i];
                    int length = (int)(tentacle.idealLength / 20f);
                    for (int j = 0; j < length; j++, k++)
                    {
                        labels[k].scale = 1.5f;
                        labels[k].color = Color.Lerp(daddyGraf.blackColor, daddyGraf.daddy.eyeColor, Custom.LerpMap(j, 0, length, 0f, 1f, 1.5f));
                    }
                }

                foreach (var label in labels)
                {
                    self.sprites[0].container.AddChild(label);
                }
            }
            else if (options.Noots.Value && self.drawableObject is NeedleWormGraphics nootGraf)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].scale = nootGraf.worm.OnBodyRad(0) * 8f / 20f;
                }

                foreach (var label in labels)
                {
                    self.sprites[0].container.AddChild(label);
                }
            }
        }
        private static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(self, timeStacker, rCam, camPos);
            var labels = CWTs.GetLabel(self);

            if (labels != null)
            {
                foreach (var sprite in self.sprites)
                {
                    sprite.isVisible = false;
                }
            }

            if (options.Spiders.Value && self.drawableObject is SpiderGraphics spiderGraf)
            {
                // Coalescipede
                var pos = self.sprites[spiderGraf.BodySprite].GetPosition();
                var rot = self.sprites[spiderGraf.BodySprite].rotation;

                labels[0].SetPosition(pos);
                labels[0].rotation = rot;
            }
            else if (options.Spiders.Value && self.drawableObject is BigSpiderGraphics bigSpidGraf)
            {
                // Big spider
                var pos = bigSpidGraf.bug.bodyChunks[1].pos - camPos;
                var rot = self.sprites[bigSpidGraf.HeadSprite].rotation;

                // Force rotation to be between 0 and 180 degrees
                if (rot < 0) rot += 180f * ((int)rot / 180 + 1);
                rot %= 180f;
                rot -= 90f;

                labels[0].SetPosition(pos);
                labels[0].rotation = rot;
            }
            else if (options.RotCysts.Value && self.drawableObject is DaddyGraphics daddyGraf)
            {
                // Main body chunk
                labels[0].SetPosition(daddyGraf.daddy.MiddleOfBody - camPos);
                labels[0].color = Color.LerpUnclamped(daddyGraf.daddy.eyeColor, daddyGraf.blackColor, Mathf.Lerp(daddyGraf.eyes[0].lastClosed, daddyGraf.eyes[0].closed, timeStacker));

                // Tentacles
                var tentacles = daddyGraf.daddy.tentacles;
                int k = 1;
                for (int i = 0; i < tentacles.Length; i++)
                {
                    var tentacle = tentacles[i];
                    var legGraf = daddyGraf.legGraphics[i];
                    int length = (int)(tentacle.idealLength / 20f);
                    for (int j = 0; j < length; j++, k++)
                    {
                        // Offset position by 1 to move away from center a bit
                        var index = Custom.LerpMap(j + 0.5f, 0, length, 0, legGraf.segments.Length);
                        var nextPos = Vector2.Lerp(legGraf.segments[Mathf.CeilToInt(index)].lastPos, legGraf.segments[Mathf.CeilToInt(index)].pos, timeStacker);
                        var prevPos = Vector2.Lerp(legGraf.segments[Mathf.FloorToInt(index)].lastPos, legGraf.segments[Mathf.FloorToInt(index)].pos, timeStacker);
                        labels[k].SetPosition(Vector2.Lerp(prevPos, nextPos, index % 1f) - camPos);
                        labels[k].rotation = Custom.AimFromOneVectorToAnother(nextPos, prevPos);
                    }
                }
            }
            else if (options.Noots.Value && self.drawableObject is NeedleWormGraphics nootGraf)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].SetPosition(nootGraf.worm.OnBodyPos((float)i / labels.Length, timeStacker) - camPos);
                    labels[i].rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, nootGraf.worm.OnBodyDir((float)i / labels.Length, timeStacker));

                    // Color = body color if not angry, white if fang out as warning
                    labels[i].color = Color.Lerp(nootGraf.bodyColor, Color.white, Mathf.Lerp(nootGraf.lastFangOut, nootGraf.fangOut, timeStacker));
                }
            }
        }
        private static void SpriteLeaser_CleanSpritesAndRemove(On.RoomCamera.SpriteLeaser.orig_CleanSpritesAndRemove orig, SpriteLeaser self)
        {
            orig(self);

            // Remove label
            var labels = CWTs.GetLabel(self);
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    label.RemoveFromContainer();
                }
            }
        }

    }
}
