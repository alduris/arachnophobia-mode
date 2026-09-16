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
using Watcher;
using Color = UnityEngine.Color;
using SpriteLeaser = RoomCamera.SpriteLeaser;

#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SpiderMod
{
    [BepInPlugin("alduris.arachnophobia", "Arachnophobia Mode", "1.0.7")]
    internal class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;
        private static readonly ConditionalWeakTable<SpriteLeaser, List<FLabel>> sleaserCWT = new();

        // https://stackoverflow.com/questions/3216085/split-a-pascalcase-string-into-separate-words
        internal static readonly Regex pascalRegex = new(@"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])");

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
            var font = Custom.GetFont();
            List<FLabel> labels = null;

            if (Options.Spiders.Value && self.drawableObject is SpiderGraphics spiderGraf)
            {
                // Coalescipede
                string t = Options.SpidersFull.Value ? "Spider" : "S";
                FLabel label = new(font, t);
                label.scale = spiderGraf.spider.firstChunk.rad * 4f / LabelTest.GetWidth(label.text, false);
                label.color = rCam.currentPalette.blackColor;
                labels = [label];
            }
            else if (Options.Spiders.Value && self.drawableObject is BigSpiderGraphics bigSpidGraf)
            {
                // Big spider
                string s = string.Join("\n", pascalRegex.Split(bigSpidGraf.bug.abstractCreature.creatureTemplate.type.value));
                FLabel label = new(font, s);
                label.scale = (bigSpidGraf.bug.bodyChunks[0].rad + bigSpidGraf.bug.bodyChunks[1].rad + bigSpidGraf.bug.bodyChunkConnections[0].distance) * 1.5f / LabelTest.GetWidth(label.text, false);
                if (!bigSpidGraf.bug.mother && !bigSpidGraf.bug.spitter) label.scale *= 4f/3f;
                label.color = bigSpidGraf.yellowCol;
                labels = [label];
            }
            else if (Options.RotCysts.Value && self.drawableObject is DaddyGraphics daddyGraf)
            {
                var type = daddyGraf.daddy.abstractCreature.creatureTemplate.type;
                int cut = type.value.IndexOf("LongLegs");
                string shortname = cut <= 0 ? pascalRegex.Replace(type.value, "\n") : type.value.Substring(0, cut);
                if (ModManager.MSC)
                {
                    if (type == MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy) shortname = "Hunter";
                    else if (type == DLCSharedEnums.CreatureTemplateType.TerrorLongLegs) shortname = $"Your\nMother";
                }

                // Main body chunk
                FLabel bodyLabel = new(font, shortname)
                {
                    scale = Mathf.Sqrt(daddyGraf.daddy.bodyChunks.Length) * daddyGraf.daddy.bodyChunks.Average(c => c.rad) * 2f / 20f,
                    color = daddyGraf.daddy.eyeColor
                };

                labels = [bodyLabel];

                // Tentacles
                for (int i = 0; i < daddyGraf.daddy.tentacles.Length; i++)
                {
                    var tentacle = daddyGraf.daddy.tentacles[i];
                    int length = (int)(tentacle.idealLength / 20f);
                    int numOfOs = length - 7; // len("LongLegs") = 7
                    for (int j = 0; j < length; j++)
                    {
                        int k = (j >= 1 && j < 1 + numOfOs) ? 1 : (j < 1 ? j : j - numOfOs);
                        FLabel label = new(font, "LongLeg"[k].ToString())
                        {
                            scale = 1.5f,
                            color = Color.Lerp(daddyGraf.blackColor, daddyGraf.daddy.eyeColor, Custom.LerpMap(j, 0, length, 0f, 1f, 1.5f))
                        };
                        labels.Add(label);
                    }
                }
            }
            else if (Options.Noots.Value && self.drawableObject is NeedleWormGraphics nootGraf)
            {
                var type = nootGraf.worm.abstractCreature.creatureTemplate.type;
                int cut = type.value.IndexOf("Needle");
                if (cut == -1) cut = type.value.IndexOf("Noodle");
                if (cut == -1) cut = type.value.IndexOf("Noot");
                if (cut == -1) cut = type.value.Length;

                labels = [.. (type.value.Substring(0, cut) + "Noot").ToCharArray().Select(c => new FLabel(font, c.ToString()))];

                for (int i = 0; i < labels.Count; i++)
                {
                    labels[i].scale = nootGraf.worm.OnBodyRad(0) * 8f / 20f;
                }
            }
            else if (Options.Eggbugs.Value && self.drawableObject is EggBugGraphics eggBugGraf)
            {
                string str;
                var type = eggBugGraf.bug.abstractCreature.creatureTemplate.type;
                if (type == CreatureTemplate.Type.EggBug) str = "Eggbug";
                else if (ModManager.MSC && type == MoreSlugcatsEnums.CreatureTemplateType.FireBug) str = "Firebug";
                else str = pascalRegex.Replace(type.value, "\n");

                FLabel bodyLabel = new(font, str)
                {
                    scale = (eggBugGraf.bug.bodyChunks[0].rad + eggBugGraf.bug.bodyChunks[1].rad + eggBugGraf.bug.bodyChunkConnections[0].distance) * 1.75f / LabelTest.GetWidth(str),
                    color = eggBugGraf.blackColor
                };

                labels = [bodyLabel];

                // Eggs
                for (int i = 0; i < 6; i++)
                {
                    labels.Add(new(font, "Egg")
                    {
                        scale = eggBugGraf.eggs[i / 3, i % 2].rad * 3f / LabelTest.GetWidth("Egg"),
                        color = eggBugGraf.eggColors[1]
                    });
                }
            }
            else if (Options.Eggbugs.Value && self.drawableObject is EggBugEgg egg)
            {
                labels = [new FLabel(font, "Egg") { scale = egg.firstChunk.rad * 3f / LabelTest.GetWidth("Egg"), color = egg.eggColors[1] }];
            }
            else if (Options.Eggbugs.Value && self.drawableObject is FireEgg fireEgg)
            {
                labels = [new FLabel(font, "Egg") { scale = fireEgg.firstChunk.rad * 3f / LabelTest.GetWidth("Egg"), color = fireEgg.eggColors[1] }];
            }
            else if (Options.Dropwigs.Value && self.drawableObject is DropBugGraphics dropBugGraf)
            {
                var type = dropBugGraf.bug.abstractCreature.creatureTemplate.type;
                labels = [new FLabel(font, type == CreatureTemplate.Type.DropBug ? "Dropwig" : pascalRegex.Replace(type.value, "\n")) { scale = dropBugGraf.bug.mainBodyChunk.rad * 3f / 20f }];
            }
            else if (Options.Centipedes.Value && self.drawableObject is CentipedeGraphics centiGraf)
            {
                labels = [];

                var type = centiGraf.centipede.abstractCreature.creatureTemplate.type;
                float scale = centiGraf.centipede.bodyChunks.Max(c => c.rad) * 3f / 20f;
                if (type == CreatureTemplate.Type.SmallCentipede)
                {
                    // Special case for babypede
                    labels.AddRange("Babypede".ToCharArray().Select(c => new FLabel(font, c.ToString())));
                }
                else
                {
                    string name = type.value;
                    if (ModManager.DLCShared && type == DLCSharedEnums.CreatureTemplateType.AquaCenti)
                    {
                        name = "Aquapede";
                    }

                    int numChunks = centiGraf.centipede.bodyChunks.Length;

                    bool stretch = numChunks >= name.Length;
                    if (stretch)
                    {
                        // Stretch the name to match segments (calculate how many letters we need to add)
                        int nameE = name.IndexOf("Centi") + 1;
                        if (nameE == 0) nameE = name.IndexOf("pede") + 1;
                        if (nameE == 0) nameE = name.IndexOf("e") + 1;
                        var chars = name.ToCharArray();
                        int numOfEs = numChunks - chars.Length;

                        for (int i = 0; i < numChunks; i++)
                        {
                            int j = (i >= nameE && i < nameE + numOfEs) ? nameE : (i < nameE ? i : i - numOfEs);
                            labels.Add(new(font, chars[j].ToString()));
                        }
                    }
                    else
                    {
                        // Fit name in segments
                        labels.AddRange(name.ToCharArray().Select(c => new FLabel(font, c.ToString())));
                        scale *= (float)centiGraf.centipede.bodyChunks.Length / labels.Count;
                    }
                }

                // Scale and color labels
                foreach (var label in labels)
                {
                    label.scale = scale * 1.5f;
                    label.color = centiGraf.ShellColor;
                }
            }
            else if (Options.Spiders.Value && self.drawableObject is RippleSpiderGraphics rippleSpiderGraf)
            {
                float width = rippleSpiderGraf.Spider.bodyChunks.Sum(x => x.rad) + rippleSpiderGraf.Spider.bodyChunkConnections.Sum(x => x.distance);
                string text = "Ripple\nSpider";
                FLabel label = new(font, text) 
                {
                    scale = width * 1.5f / LabelTest.GetWidth(text),
                    shader = self.sprites[rippleSpiderGraf.tailSprite].shader
                };
                labels = [label];
            }
            else if (Options.Crabs.Value && self.drawableObject is DrillCrabGraphics crabGraf)
            {
                string[] splitName = pascalRegex.Split(crabGraf.Crab.abstractCreature.creatureTemplate.type.value);
                var bodyLabel = new FLabel(font, string.Join("\n", splitName))
                {
                    scale = crabGraf.Crab.bodyChunks.Max(x => x.rad) * 4f / 20f / splitName.Length,
                    shader = self.sprites[0].shader
                };

                labels = [bodyLabel];

                // todo: maybe legs?
            }
            else if (Options.Barnacles.Value && self.drawableObject is BarnacleGraphics barnacleGraf)
            {
                var type = barnacleGraf.Barnacle.abstractCreature.creatureTemplate.type;
                string text = pascalRegex.Replace(type.value, "\n");
                labels = [new FLabel(font, text) { scale = barnacleGraf.Barnacle.mainBodyChunk.rad * 3f / LabelTest.GetWidth(text) }];
            }

            if (labels != null)
            {
                sleaserCWT.Add(self, labels);
                foreach (var label in labels)
                {
                    self.sprites[0].container.AddChild(label);
                }
            }
        }

        private static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, SpriteLeaser sLeaser, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(sLeaser, timeStacker, rCam, camPos);
            if (sleaserCWT.TryGetValue(sLeaser, out var labels))
            {
                // Hide all sprites
                foreach (var sprite in sLeaser.sprites)
                {
                    sprite.isVisible = false;
                }

                // Run display logic
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
                    for (int i = 0; i < labels.Count; i++)
                    {
                        labels[i].SetPosition(nootGraf.worm.OnBodyPos((float)i / labels.Count, timeStacker) - camPos);
                        labels[i].rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, nootGraf.worm.OnBodyDir((float)i / labels.Count, timeStacker));

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
                else if (Options.Centipedes.Value && sLeaser.drawableObject is CentipedeGraphics centiGraf)
                {
                    var chunks = centiGraf.centipede.bodyChunks;
                    bool fit = labels.Count > chunks.Length;
                    for (int i = 0; i < labels.Count; i++)
                    {
                        if (fit)
                        {
                            labels[i].SetPosition(Utils.PointAlongChunks(i, labels.Count, chunks, timeStacker) - camPos);
                            labels[i].rotation = Utils.RotationAlongSprites(i, labels.Count, chunks.Length, sLeaser.sprites, x => x);
                        }
                        else
                        {
                            labels[i].SetPosition(chunks[i].pos - camPos);
                            labels[i].rotation = sLeaser.sprites[i].rotation;
                        }
                    }
                }
                else if (Options.Spiders.Value && sLeaser.drawableObject is RippleSpiderGraphics rippleSpiderGraf)
                {
                    labels[0].SetPosition(Utils.AvgBodyChunkPos(rippleSpiderGraf.Spider.bodyChunks[0], rippleSpiderGraf.Spider.bodyChunks[1], timeStacker) - camPos);
                    labels[0].rotation = Utils.FixRotation(Utils.AngleBtwnChunks(rippleSpiderGraf.Spider.bodyChunks[0], rippleSpiderGraf.Spider.bodyChunks[1], timeStacker)) - 90f;
                    labels[0].color = sLeaser.sprites[rippleSpiderGraf.headSprite].color;

                    sLeaser.sprites[rippleSpiderGraf.firstEffectSprite].isVisible = true;
                    sLeaser.sprites[rippleSpiderGraf.firstEffectSprite + 1].isVisible = true;
                }
                else if (Options.Crabs.Value && sLeaser.drawableObject is DrillCrabGraphics drillCrabGraf)
                {
                    var blobSprite = sLeaser.sprites[drillCrabGraf.blobSprite];
                    labels[0].SetPosition(blobSprite.GetPosition());
                    labels[0].rotation = blobSprite.rotation + 180f;
                    labels[0].color = blobSprite.color;
                }
                else if (Options.Barnacles.Value && sLeaser.drawableObject is BarnacleGraphics barnacleGraf)
                {
                    labels[0].SetPosition(Utils.AvgBodyChunkPos(barnacleGraf.Barnacle.bodyChunks[0], barnacleGraf.Barnacle.bodyChunks[1], timeStacker) - camPos);
                    labels[0].rotation = Utils.FixRotation(Utils.AngleBtwnChunks(barnacleGraf.Barnacle.bodyChunks[0], barnacleGraf.Barnacle.bodyChunks[1], timeStacker)) - 90f;
                    labels[0].color = barnacleGraf.blackColor;
                    labels[0].MoveBehindOtherNode(sLeaser.sprites[barnacleGraf.firstConeSprite]);

                    if (barnacleGraf.Barnacle.hasShell)
                    {
                        for (int i = 0; i < barnacleGraf.cones.Length; i++)
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                int k = barnacleGraf.firstConeSprite + i * 5 + j;
                                sLeaser.sprites[k].isVisible = true;
                            }
                        }
                    }
                }
            }

        }

        private static void SpriteLeaser_CleanSpritesAndRemove(On.RoomCamera.SpriteLeaser.orig_CleanSpritesAndRemove orig, SpriteLeaser self)
        {
            orig(self);

            // Remove label
            if (sleaserCWT.TryGetValue(self, out var labels))
            {
                foreach (var label in labels)
                {
                    label.RemoveFromContainer();
                }
                labels.Clear();
                sleaserCWT.Remove(self);
            }
        }

    }
}
