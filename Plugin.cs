using System;
using System.Collections.Generic;
using System.Drawing;
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

            if (Options.Spiders.Value && self.drawableObject is SpiderGraphics)
            {
                string t = Options.SpidersFull.Value ? "Spider" : "S";
                return [new(font, t)];
            }
            else if (Options.Spiders.Value && self.drawableObject is BigSpiderGraphics bug)
            {
                string s = "Big Spider";
                if (bug.Spitter) s = $"Spitter{Environment.NewLine}Spider";
                else if (bug.Mother) s = $"Mother{Environment.NewLine}Spider";
                return [new(font, s)];
            }
            else if (Options.RotCysts.Value && self.drawableObject is DaddyGraphics daddyGraf)
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
            else if (Options.Noots.Value && self.drawableObject is NeedleWormGraphics nootGraf)
            {
                var type = nootGraf.worm.abstractCreature.creatureTemplate.type;
                int cut = type.value.IndexOf("Needle");
                if (cut == -1) cut = type.value.IndexOf("Noodle");
                if (cut == -1) cut = type.value.IndexOf("Noot");
                if (cut == -1) cut = type.value.Length;

                return [.. (type.value.Substring(0, cut) + "Noot").ToCharArray().Select(c => new FLabel(font, c.ToString()))];
            }
            else if (Options.Eggbugs.Value && self.drawableObject is EggBugGraphics eggBugGraf)
            {
                string str;
                var type = eggBugGraf.bug.abstractCreature.creatureTemplate.type;
                if (type == CreatureTemplate.Type.EggBug) str = "Eggbug";
                else if (ModManager.MSC && type == MoreSlugcatsEnums.CreatureTemplateType.FireBug) str = "Firebug";
                else str = pascalRegex.Replace(type.value, Environment.NewLine);

                List<FLabel> labels = [new(font, str) {
                    scale = (eggBugGraf.bug.bodyChunks[0].rad + eggBugGraf.bug.bodyChunks[1].rad + eggBugGraf.bug.bodyChunkConnections[0].distance) * 1.75f / LabelTest.GetWidth(str),
                    color = eggBugGraf.blackColor
                }];

                // Eggs
                for (int i = 0; i < 6; i++)
                {
                    labels.Add(new(font, "Egg")
                    {
                        scale = eggBugGraf.eggs[i / 3, i % 2].rad * 3f / LabelTest.GetWidth("Egg"),
                        color = eggBugGraf.eggColors[1]
                    });
                }

                return [.. labels];
            }
            else if (Options.Eggbugs.Value && self.drawableObject is EggBugEgg egg)
            {
                return [new FLabel(font, "Egg") { scale = egg.firstChunk.rad * 3f / LabelTest.GetWidth("Egg"), color = egg.eggColors[1] }];
            }
            else if (Options.Eggbugs.Value && self.drawableObject is FireEgg fireEgg)
            {
                return [new FLabel(font, "Egg") { scale = fireEgg.firstChunk.rad * 3f / LabelTest.GetWidth("Egg"), color = fireEgg.eggColors[1] }];
            }
            else if (Options.Dropwigs.Value && self.drawableObject is DropBugGraphics dropBugGraf)
            {
                return [new FLabel(font, "Dropwig") { scale = dropBugGraf.bug.mainBodyChunk.rad * 3f / 20f }];
            }
            return null;
        });
    }

    [BepInPlugin("alduris.arachnophobia", "Arachnophobia Mode", "1.0.5")]
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

            if (Options.Spiders.Value && self.drawableObject is SpiderGraphics spiderGraf)
            {
                // Coalescipede
                labels[0].scale = spiderGraf.spider.firstChunk.rad * 4f / LabelTest.GetWidth(labels[0].text, false);
                labels[0].color = rCam.currentPalette.blackColor;

                self.sprites[0].container.AddChild(labels[0]);
            }
            else if (Options.Spiders.Value && self.drawableObject is BigSpiderGraphics bigSpidGraf)
            {
                // Big spider
                labels[0].scale = (bigSpidGraf.bug.bodyChunks[0].rad + bigSpidGraf.bug.bodyChunks[1].rad + bigSpidGraf.bug.bodyChunkConnections[0].distance) * 1.5f / LabelTest.GetWidth(labels[0].text, false);
                if (!bigSpidGraf.bug.mother && !bigSpidGraf.bug.spitter) labels[0].scale *= 4f/3f;
                labels[0].color = bigSpidGraf.yellowCol;

                self.sprites[0].container.AddChild(labels[0]);
            }
            else if (Options.RotCysts.Value && self.drawableObject is DaddyGraphics daddyGraf)
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
            else if (Options.Noots.Value && self.drawableObject is NeedleWormGraphics nootGraf)
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
        private static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, SpriteLeaser sLeaser, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(sLeaser, timeStacker, rCam, camPos);
            var labels = CWTs.GetLabel(sLeaser);

            if (labels != null)
            {
                foreach (var sprite in sLeaser.sprites)
                {
                    sprite.isVisible = false;
                }
            }

            if (Options.Spiders.Value && sLeaser.drawableObject is SpiderGraphics spiderGraf)
            {
                // Coalescipede
                var pos = sLeaser.sprites[spiderGraf.BodySprite].GetPosition();
                var rot = sLeaser.sprites[spiderGraf.BodySprite].rotation;

                labels[0].SetPosition(pos);
                labels[0].rotation = rot;
            }
            else if (Options.Spiders.Value && sLeaser.drawableObject is BigSpiderGraphics bigSpidGraf)
            {
                // Big spider
                var pos = bigSpidGraf.bug.bodyChunks[1].pos - camPos;
                var rot = sLeaser.sprites[bigSpidGraf.HeadSprite].rotation;

                // Force rotation to be between 0 and 180 degrees
                if (rot < 0) rot += 180f * ((int)rot / 180 + 1);
                rot %= 180f;
                rot -= 90f;

                labels[0].SetPosition(pos);
                labels[0].rotation = rot;
            }
            else if (Options.RotCysts.Value && sLeaser.drawableObject is DaddyGraphics daddyGraf)
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
            else if (Options.Noots.Value && sLeaser.drawableObject is NeedleWormGraphics nootGraf)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].SetPosition(nootGraf.worm.OnBodyPos((float)i / labels.Length, timeStacker) - camPos);
                    labels[i].rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, nootGraf.worm.OnBodyDir((float)i / labels.Length, timeStacker));

                    // Color = body color if not angry, white if fang out as warning
                    labels[i].color = Color.Lerp(nootGraf.bodyColor, Color.white, Mathf.Lerp(nootGraf.lastFangOut, nootGraf.fangOut, timeStacker));
                }
            }
            else if (Options.Eggbugs.Value && sLeaser.drawableObject is EggBugGraphics eggBugGraf)
            {
                // Body
                labels[0].SetPosition(Vector2.Lerp(eggBugGraf.bug.bodyChunks[1].lastPos, eggBugGraf.bug.bodyChunks[1].pos, timeStacker) - camPos);
                float rot = sLeaser.sprites[eggBugGraf.HeadSprite].rotation;
                labels[0].rotation = ((rot < 0f ? rot + 180f * (Mathf.FloorToInt(rot) / 180 + 1) : rot) % 180f) + 90f;

                // Eggs
                for (int i = 0; i < 6; i++)
                {
                    var eggSprite = sLeaser.sprites[eggBugGraf.BackEggSprite(i % 2, i / 2, 2)];
                    labels[i + 1].x = eggSprite.x;
                    labels[i + 1].y = eggSprite.y;
                    labels[i + 1].rotation = eggSprite.rotation;
                    if (eggBugGraf.bug.FireBug && i >= eggBugGraf.bug.eggsLeft) labels[i + 1].isVisible = false;
                }
            }
            else if (Options.Eggbugs.Value && sLeaser.drawableObject is EggBugEgg egg)
            {
                labels[0].SetPosition(Vector2.Lerp(egg.firstChunk.lastPos, egg.firstChunk.pos, timeStacker) - camPos);
                labels[0].color = egg.blink > 1 && UnityEngine.Random.value > 0.5f ? egg.blinkColor : egg.color;
            }
            else if (Options.Eggbugs.Value && sLeaser.drawableObject is FireEgg fireEgg)
            {
                labels[0].SetPosition(Vector2.Lerp(fireEgg.firstChunk.lastPos, fireEgg.firstChunk.pos, timeStacker) - camPos);
                labels[0].scale = fireEgg.firstChunk.rad * 3f / LabelTest.GetWidth(labels[0].text);
                labels[0].color = sLeaser.sprites[1].color;
            }
            else if (Options.Dropwigs.Value && sLeaser.drawableObject is DropBugGraphics dropBugGraf)
            {
                labels[0].SetPosition(Vector2.Lerp(dropBugGraf.bug.bodyChunks[1].lastPos, dropBugGraf.bug.bodyChunks[1].pos, timeStacker) - camPos);
                float rot = sLeaser.sprites[dropBugGraf.HeadSprite].rotation;
                labels[0].rotation = ((rot < 0f ? rot + 180f * (Mathf.FloorToInt(rot) / 180 + 1) : rot) % 180f) + 90f;
                labels[0].color = dropBugGraf.currSkinColor;
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
