using BepInEx;
using R2API.Utils;
using RoR2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RedsSacrifice.Baseline;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace RedsSacrifice
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("redssacrifice", "Red's Sacrifice", "1.1.2")]
    [R2APISubmoduleDependency("CommandHelper")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class RedsSacrifice : BaseUnityPlugin
    {

        private ConfigFile config;
        private Dictionary<CombatDirector, DirectorTracker> directors;
        private double nextMonsterCost;

        public RedsSacrifice()
        {
            directors = new Dictionary<CombatDirector, DirectorTracker>();
            nextMonsterCost = 0;
            Logger = base.Logger;

            InitConfig();
            Hook();

            CommandHelper.AddToConsoleWhenReady();
        }

        #region Config

        private void InitConfig()
        {
            config = new ConfigFile(Paths.ConfigPath + "\\me.RedMushie.RedsSacrifice.cfg", true);
            const double maxDouble = 100000000.0d;

            var enabled = config.Bind(new ConfigDefinition("Common", "Enabled"), true,
                new ConfigDescription("Whether the mod is enabled or not.", new AcceptableValueList<bool>(true, false)));
            var debugging = config.Bind(new ConfigDefinition("Common", "Debugging"), false,
                new ConfigDescription("If set to true, the mod will output loads of messages to the console. Not recommended unless you're debugging.", new AcceptableValueList<bool>(true, false)));

            var globalMult = config.Bind(new ConfigDefinition("Multipliers (All games)", "Global"), 100.0d,
                new ConfigDescription("Percentage multiplier of the global drop chance.", new AcceptableValueRange<double>(0.0d, maxDouble)));
            var shrineMult = config.Bind(new ConfigDefinition("Multipliers (All games)", "Shrine of Combat"), 200.0d,
                new ConfigDescription("Drop chance multiplier (in percent) for monsters spawned by a Shrine of Combat.", new AcceptableValueRange<double>(0.0d, maxDouble)));

            var normalWaveMult = config.Bind(new ConfigDefinition("Multipliers (Classic games)", "Normal wave"), 100.0d,
                new ConfigDescription("Drop chance multiplier (in percent) for monsters spawned during a Classic game non-boss wave.", new AcceptableValueRange<double>(0.0d, maxDouble)));
            var bossWaveMult = config.Bind(new ConfigDefinition("Multipliers (Classic games)", "Boss wave"), 0.0d,
                new ConfigDescription("Drop chance multiplier (in percent) for monsters spawned during a Classic game boss wave.", new AcceptableValueRange<double>(0.0d, maxDouble)));

            var simulacrumNormalWaveMult = config.Bind(new ConfigDefinition("Multipliers (Simulacrum)", "Normal wave"), 150.0d,
                new ConfigDescription("Drop chance multiplier (in percent) for monsters spawned during a Simulacrum game non-boss wave.", new AcceptableValueRange<double>(0.0d, maxDouble)));
            var simulacrumBossWaveMult = config.Bind(new ConfigDefinition("Multipliers (Simulacrum)", "Boss wave"), 150.0d,
                new ConfigDescription("Drop chance multiplier (in percent) for monsters spawned during a Simulacrum game boss wave.", new AcceptableValueRange<double>(0.0d, maxDouble)));

            Enabled = enabled.Value;
            Debugging = debugging.Value;

            GlobalMultiplier = globalMult.Value;

            NormalWaveMultiplier = normalWaveMult.Value;
            BossWaveMultiplier = bossWaveMult.Value;

            CombatShrineMultiplier = shrineMult.Value;

            SimulacrumNormalWaveMultiplier = simulacrumNormalWaveMult.Value;
            SimulacrumBossWaveMultiplier = simulacrumBossWaveMult.Value;
        }

        public static new ManualLogSource Logger { get; private set; }

        public static bool Enabled { get; private set; }
        public static bool Debugging { get; private set; }

        public static double GlobalMultiplier { get; private set; }

        public static double NormalWaveMultiplier { get; private set; }
        public static double BossWaveMultiplier { get; private set; }

        public static double CombatShrineMultiplier { get; private set; }
        
        public static double SimulacrumNormalWaveMultiplier { get; private set; }

        public static double SimulacrumBossWaveMultiplier { get; private set; }

        #endregion

        #region Commands

        [ConCommand(commandName = "rs_enabled", flags = ConVarFlags.None, helpText = "Sets the enabled flag.")]
        private static void CmdEnabled(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Logger.LogInfo($"{Enabled}");
                return;
            }

            bool flag;
            if (!bool.TryParse(args[0], out flag))
            {
                Logger.LogInfo($"Not a valid boolean: ${args[0]}");
                return;
            }

            Enabled = flag;
        }

        [ConCommand(commandName = "rs_debug", flags = ConVarFlags.None, helpText = "Sets the debugging flag.")]
        private static void CmdDebugging(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Logger.LogInfo($"{Debugging}");
                return;
            }

            bool flag;
            if (!bool.TryParse(args[0], out flag))
            {
                Logger.LogInfo($"Not a valid boolean: ${args[0]}");
                return;
            }

            Debugging = flag;
        }

        [ConCommand(commandName = "rs_global_mult", flags = ConVarFlags.None, helpText = "Sets the global drop chance multiplier.")]
        private static void CmdGlobalMult(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Logger.LogInfo($"{GlobalMultiplier}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Logger.LogInfo($"Not a valid double: ${args[0]}");
                return;
            }

            GlobalMultiplier = mult;
        }

        
        [ConCommand(commandName = "rs_shrine_mult", flags = ConVarFlags.None, helpText = "Sets the Combat Shrine drop chance multiplier.")]
        private static void CmdShrineMult(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Logger.LogInfo($"{CombatShrineMultiplier}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Logger.LogInfo($"Not a valid double: ${args[0]}");
                return;
            }

            CombatShrineMultiplier = mult;
        }


        [ConCommand(commandName = "rs_classic_normal_mult", flags = ConVarFlags.None, helpText = "Sets the Classic game non-boss wave drop chance multiplier.")]
        private static void CmdNormalWaveMult(ConCommandArgs args)
        {
            if (args.Count == 0) {
                Logger.LogInfo($"{NormalWaveMultiplier}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Logger.LogInfo($"Not a valid double: ${args[0]}");
                return;
            }

            NormalWaveMultiplier = mult;
        }

        [ConCommand(commandName = "rs_classic_boss_mult", flags = ConVarFlags.None, helpText = "Sets the Classic game boss wave drop chance multiplier.")]
        private static void CmdBossWaveMult(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Logger.LogInfo($"{BossWaveMultiplier}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Logger.LogInfo($"Not a valid double: ${args[0]}");
                return;
            }

            BossWaveMultiplier = mult;
        }


        [ConCommand(commandName = "rs_sim_normal_mult", flags = ConVarFlags.None, helpText = "Sets the Simulacrum non-boss wave drop chance multiplier.")]
        private static void CmdSimulacrumNormalMult(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Logger.LogInfo($"{SimulacrumNormalWaveMultiplier}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Logger.LogInfo($"Not a valid double: ${args[0]}");
                return;
            }

            SimulacrumNormalWaveMultiplier = mult;
        }

        [ConCommand(commandName = "rs_sim_boss_mult", flags = ConVarFlags.None, helpText = "Sets the Simulacrum boss wave drop chance multiplier.")]
        private static void CmdSimulacrumBossMult(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Logger.LogInfo($"{SimulacrumBossWaveMultiplier}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Logger.LogInfo($"Not a valid double: ${args[0]}");
                return;
            }

            SimulacrumBossWaveMultiplier = mult;
        }

        #endregion

        #region Hooks

        private void Hook()
        {
            On.RoR2.CombatDirector.OnEnable += CombatDirector_OnEnable;
            On.RoR2.CombatDirector.OnDisable += CombatDirector_OnDisable;
            On.RoR2.CombatDirector.SetNextSpawnAsBoss += CombatDirector_SetNextSpawnAsBoss;
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget += CombatDirector_AttemptSpawnOnTarget;
            On.RoR2.CombatDirector.CombatShrineActivation += CombatDirector_CombatShrineActivation;
            On.RoR2.Util.GetExpAdjustedDropChancePercent += Util_GetExpAdjustedDropChancePercent;
            SpawnCard.onSpawnedServerGlobal += SpawnCard_onSpawnedServerGlobal;
        }

        private DirectorTracker GetOrCreateTracker(CombatDirector director)
        {
            if (directors.ContainsKey(director))
                return directors[director];

            DirectorTracker tracker = new DirectorTracker(director);
            directors[director] = tracker;
            return tracker;
        }

        /// <summary>
        /// Whenever a CombatDirector gets enabled, we begin tracking it by mapping the instance to a DirectorTracker.
        /// We also begin listening for any monster spawn events by this Director, so we can attach MonsterTrackers to them.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void CombatDirector_OnEnable(On.RoR2.CombatDirector.orig_OnEnable orig, CombatDirector self)
        {
            Dump("CombatDirector_OnEnable", self);

            // Track this Director as soon as it's activated.
            DirectorTracker directorTracker = GetOrCreateTracker(self);

            // Configure the baseline correctly for the Simulacrum.
            InfiniteTowerWaveController infiniteTowerWaveController = self.GetComponent<InfiniteTowerWaveController>() ?? self.GetComponent<InfiniteTowerBossWaveController>();
            if (infiniteTowerWaveController != null)
            {
                Logger.LogWarning("Detected Simulacrum wave controller; initiating SimulacrumNormalWaveBaseline");
                directorTracker.Baseline = new SimulacrumNormalWaveBaseline(infiniteTowerWaveController);
            }

            if (directorTracker.Listener == null)
            {
                // Create a listener for whenever this director spawns a monster.
                directorTracker.Listener = new UnityAction<GameObject>(obj =>
                {
                    CharacterMaster master = obj.GetComponent<CharacterMaster>();
                    GameObject bodyObj = master.GetBodyObject();

                    // Add a MonsterTracker to every single monster that spawns as a result of this
                    // CombatDirector. In SpawnCard_onSpawnedServerGlobal we fill in the missing Cost field.
                    MonsterTracker monsterTracker = bodyObj.AddComponent<MonsterTracker>();
                    monsterTracker.DirectorTracker = directorTracker;
                    monsterTracker.DifficultyCoefficient = Run.instance.compensatedDifficultyCoefficient;
                });

                // Bind the listener to the CombatDirector.
                self.onSpawnedServer.AddListener(directorTracker.Listener);
            }
            
            orig(self);
        }

        /// <summary>
        /// Stop tracking CombatDirectors when they are disabled. Removes the spawn listener and removes the
        /// DirectorTracker from the map.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void CombatDirector_OnDisable(On.RoR2.CombatDirector.orig_OnDisable orig, CombatDirector self)
        {
            Dump("CombatDirector_OnDisable", self);

            // Get the tracker, remove the listener, and remove any references to this.
            DirectorTracker directorTracker = directors[self];
            if (directorTracker.Listener != null)
                self.onSpawnedServer.RemoveListener(directorTracker.Listener);
            directors.Remove(self);

            orig(self);
        }

        private void CombatDirector_SetNextSpawnAsBoss(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            var tracker = GetOrCreateTracker(self);

            if (tracker.Baseline == null)
            {
                if (Debugging)
                {
                    Logger.LogInfo("CombatDirector_SetNextSpawnAsBoss was called and no baseline was specified, initiating BossWaveBaseline");
                }
                tracker.Baseline = new BossWaveBaseline(tracker);
            } else if (tracker.Baseline is SimulacrumNormalWaveBaseline)
            {
                if (Debugging)
                {
                    Logger.LogInfo("CombatDirector_SetNextSpawnAsBoss was called on a SimulacrumNormalWaveBaseline, upgrading to SimulacrumBossWaveBaseline!");
                }
                tracker.Baseline = new SimulacrumBossWaveBaseline(self.GetComponent<InfiniteTowerBossWaveController>());
            } else
            {
                // ???
            }

            orig(self);
        }

        private void CombatDirector_CombatShrineActivation(On.RoR2.CombatDirector.orig_CombatShrineActivation orig, CombatDirector self, Interactor interactor, float monsterCredit, DirectorCard chosenDirectorCard)
        {
            Logger.LogInfo("CombatDirector_SetNextSpawnAsBoss was called, initiating CombatShrineBaseline");
            GetOrCreateTracker(self).Baseline = new CombatShrineBaseline(monsterCredit);
            orig(self, interactor, monsterCredit, chosenDirectorCard);
        }

        private void CombatDirector_AttemptSpawnOnTarget(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Find the field that stores the monster credits.
            FieldInfo costField = typeof(CombatDirector)
                .GetField("monsterCredit");

            // Begin at the back.
            c.Index = c.Instrs.Count - 1;

            // Find when we write to the monsterCredits field.
            c.GotoPrev(MoveType.Before, x => x.MatchStfld<CombatDirector>("monsterCredit"));

            // Grab the cost of this monster (stored in ldloc.0)
            c.Emit(OpCodes.Ldloc_0);
            c.Emit(OpCodes.Conv_R4);
            c.EmitDelegate<Action<float>>((monsterCost) =>
            {
                nextMonsterCost = monsterCost;
            });
        }

        /// <summary>
        /// Assign the cost of a monster when it is spawned.
        /// </summary>
        /// <param name="obj"></param>
        private void SpawnCard_onSpawnedServerGlobal(SpawnCard.SpawnResult obj)
        {
            // Get the CharacterMaster. If there is none, this is not a Monster.
            CharacterMaster master = obj.spawnedInstance?.GetComponent<CharacterMaster>();
            if (master == null)
                return;

            // Get the "physical body" GameObject and check if it has a MonsterTracker.
            // If it does, it was spawned through a CombatDirector, and is a monster.
            GameObject bodyObj = master.GetBodyObject();
            MonsterTracker monsterTracker = bodyObj.GetComponent<MonsterTracker>();
            if (monsterTracker != null)
            {
                monsterTracker.Cost = nextMonsterCost;
                if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.swarmsArtifactDef))
                {
                    monsterTracker.Cost /= 2;
                }

                if (Debugging)
                {
                    Logger.LogInfo($"Monster cost: {monsterTracker.Cost}");
                }

                DirectorTracker directorTracker = monsterTracker.DirectorTracker;
                if (directorTracker.Baseline == null)
                {
                    if (Debugging)
                    {
                        Logger.LogWarning("No baseline detected. Initiating NormalWaveBaseline");
                    }
                    directorTracker.Baseline = new NormalWaveBaseline(directorTracker);
                }
            } else
            {
                if (Debugging)
                {
                    Logger.LogError($"No monster tracker for {bodyObj}!");
                }
            }
        }

        private float Util_GetExpAdjustedDropChancePercent(On.RoR2.Util.orig_GetExpAdjustedDropChancePercent orig, float chance, GameObject bodyObj)
        {
            if (!Enabled)
            {
                return orig(chance, bodyObj);
            }

            MonsterTracker monsterTracker = bodyObj.GetComponent<MonsterTracker>();
            if (monsterTracker == null)
            {
                if (Debugging) {
                    Logger.LogError($"GetExpAdj: MonsterTracker is null for {bodyObj}");
                }
                Dump("GetExpAdj-GetMonsterInfo", bodyObj);
                return 0;
            }

            DirectorTracker directorTracker = monsterTracker.DirectorTracker;
            
            // Handle exceptional cases where no baseline has been specified.
            if (directorTracker.Baseline == null)
            {
                Logger.LogError($"No baseline found?");
                Dump("GetExpAdj-GetBaseline", directorTracker.Director);
                return 0;
            }

            // Calculate!
            double creditCost = monsterTracker.Cost;
            double creditBaseline = directorTracker.Baseline.GetBaseline(directorTracker, monsterTracker);

            // Do nothing if the credit baseline is zero.
            if (creditBaseline == 0)
            {
                if (Debugging)
                {
                    Logger.LogWarning($"Ignoring {creditCost} boss credits for {bodyObj}.");
                }
                return 0;
            }

            if (Debugging)
            {
                Logger.LogInfo($"Credit cost: {creditCost}");
                Logger.LogInfo($"Determined baseline: {creditBaseline}");
            }

            // creditCost / creditBaseline is the fraction. We multiply by 100 to get the percentage.
            // Then we multiply by GlobalBaselineMult/100, since that's also a percentage.
            double fraction = (creditCost / creditBaseline) * 100 * (GlobalMultiplier / 100.0d);
            if (Debugging)
            {
                Logger.LogInfo($"Killed monster worth {creditCost} credits (baseline {creditBaseline:0.00}). Fraction: {fraction:0.00}");
            }
            return (float)fraction;
        }

        #endregion

        #region Debugging

        private void Dump(string source, Component obj)
        {
            if (!Debugging)
                return;

            Logger.LogInfo(Pad($" {source} ", 64, '='));
            Logger.LogInfo($"Dumping GameObject: " + obj.ToString());

            Logger.LogInfo("Components:");
            foreach (var component in obj.GetComponents<Component>())
            {
                Logger.LogInfo($"- {component.GetType().Name}");
            }

            CharacterBody body = obj.GetComponent<CharacterBody>();
            if (body != null)
            {
                Logger.LogInfo($"Has CharacterBody!");

                Logger.LogInfo("Buffs:");
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    BuffDef def = BuffCatalog.GetBuffDef((BuffIndex)i);
                    int stacks = body.GetBuffCount((BuffIndex)i);
                    if (stacks > 0)
                    {
                        Logger.LogInfo($"- Has {stacks} stacks of buff {def.name}");
                    }
                }

                MonsterTracker monsterTracker = body.GetComponent<MonsterTracker>();
                if (monsterTracker != null)
                {
                    Logger.LogInfo($"Body has cost: {monsterTracker.Cost}");
                }
            }

            Logger.LogInfo("================================================================");
        }

        private void Dump(string source, GameObject obj)
        {
            if (!Debugging)
                return;
            Logger.LogInfo(Pad($" {source} ", 64, '='));
            Logger.LogInfo($"Dumping GameObject: " + obj.ToString());

            Logger.LogInfo("Components:");
            foreach (var component in obj.GetComponents<Component>())
            {
                Logger.LogInfo($"- {component.GetType().Name}");
            }

            CharacterBody body = obj.GetComponent<CharacterBody>();
            if (body != null)
            {
                Logger.LogInfo($"Has CharacterBody!");

                Logger.LogInfo("Buffs:");
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    BuffDef def = BuffCatalog.GetBuffDef((BuffIndex)i);
                    int stacks = body.GetBuffCount((BuffIndex)i);
                    if (stacks > 0)
                    {
                        Logger.LogInfo($"- Has {stacks} stacks of buff {def.name}");
                    }
                }

                MonsterTracker monsterTracker = body.GetComponent<MonsterTracker>();
                if (monsterTracker != null)
                {
                    Logger.LogInfo($"Body has cost: {monsterTracker.Cost}");
                }
            }

            Logger.LogInfo("================================================================");
        }

        private string Pad(string str, int len, char pad)
        {
            int delta = Math.Abs(len - str.Length);
            if (delta == 0)
                return str;

            return new string(pad, delta).Insert(delta / 2, str);            
        }

        #endregion

    }
}
