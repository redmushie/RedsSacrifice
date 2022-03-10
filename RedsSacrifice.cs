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

namespace RedsSacrifice
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("redssacrifice", "RedsSacrifice", "0.0.1")]
    [R2APISubmoduleDependency("CommandHelper")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class RedsSacrifice : BaseUnityPlugin
    {

        private Dictionary<CombatDirector, DirectorTracker> directors;
        private double nextMonsterCost;

        public RedsSacrifice()
        {
            directors = new Dictionary<CombatDirector, DirectorTracker>();
            nextMonsterCost = 0;

            Hook();

            Debugging = false;
            WaveBaselineMult = 100d;
            ShrineBaselineMult = 2d;
            GlobalBaselineMult = 100d;
            SimulacrumBaselineMult = 100d;
            CommandHelper.AddToConsoleWhenReady();
        }

        #region Config

        public static bool Debugging { get; private set; }
        public static double WaveBaselineMult { get; private set; }
        public static double ShrineBaselineMult { get; private set; }
        public static double SimulacrumBaselineMult { get; private set; }
        public static double GlobalBaselineMult { get; private set; }

        #endregion

        #region Commands

        [ConCommand(commandName = "rs_Debugging", flags = ConVarFlags.None, helpText = "Sets the debugging flag.")]
        private static void CmdDebugging(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log($"{Debugging}");
                return;
            }

            bool flag;
            if (!bool.TryParse(args[0], out flag))
            {
                Debug.Log($"Not a valid boolean: ${args[0]}");
                return;
            }

            Debugging = flag;
        }

        [ConCommand(commandName = "rs_WaveMult", flags = ConVarFlags.None, helpText = "Sets the wave baseline multiplier.")]
        private static void CmdWaveMult(ConCommandArgs args)
        {
            if (args.Count == 0) {
                Debug.Log($"{WaveBaselineMult}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Debug.Log($"Not a valid double: ${args[0]}");
                return;
            }

            WaveBaselineMult = mult;
        }

        [ConCommand(commandName = "rs_ShrineMult", flags = ConVarFlags.None, helpText = "Sets the combat shrine baseline multiplier.")]
        private static void CmdShrineMult(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log($"{ShrineBaselineMult}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Debug.Log($"Not a valid double: ${args[0]}");
                return;
            }

            ShrineBaselineMult = mult;
        }

        [ConCommand(commandName = "rs_SimulacrumMult", flags = ConVarFlags.None, helpText = "Sets the Simulacrum baseline multiplier.")]
        private static void CmdSimulacrumMult(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log($"{SimulacrumBaselineMult}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Debug.Log($"Not a valid double: ${args[0]}");
                return;
            }

            SimulacrumBaselineMult = mult;
        }

        [ConCommand(commandName = "rs_GlobalMult", flags = ConVarFlags.None, helpText = "Sets the global chance multiplier.")]
        private static void CmdGlobalMult(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log($"{GlobalBaselineMult}");
                return;
            }

            double mult;
            if (!double.TryParse(args[0], out mult))
            {
                Debug.Log($"Not a valid double: ${args[0]}");
                return;
            }

            GlobalBaselineMult = mult;
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
                Debug.LogWarning("Detected Simulacrum wave controller; using SimulacrumBaseline");
                directorTracker.Baseline = new SimulacrumBaseline(infiniteTowerWaveController);
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
            Debug.Log("CombatDirector_SetNextSpawnAsBoss was called, initiating TeleporterBossBaseline");
            GetOrCreateTracker(self).Baseline = new TeleporterBossBaseline();
            orig(self);
        }

        private void CombatDirector_CombatShrineActivation(On.RoR2.CombatDirector.orig_CombatShrineActivation orig, CombatDirector self, Interactor interactor, float monsterCredit, DirectorCard chosenDirectorCard)
        {
            Debug.Log("CombatDirector_SetNextSpawnAsBoss was called, initiating CombatShrineBaseline");
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
            CharacterMaster master = obj.spawnedInstance.GetComponent<CharacterMaster>();
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
                    Debug.Log($"Monster cost: {monsterTracker.Cost}");
                }

                DirectorTracker directorTracker = monsterTracker.DirectorTracker;
                if (directorTracker.Baseline == null)
                {
                    Debug.LogWarning("No baseline detected. Initiating MoneyWaveBaseline");
                    directorTracker.Baseline = new MoneyWaveBaseline(directorTracker);
                }
            } else
            {
                if (Debugging)
                {
                    Debug.LogError($"No monster tracker for {bodyObj}!");
                }
            }
        }

        private float Util_GetExpAdjustedDropChancePercent(On.RoR2.Util.orig_GetExpAdjustedDropChancePercent orig, float chance, GameObject bodyObj)
        {
            MonsterTracker monsterTracker = bodyObj.GetComponent<MonsterTracker>();
            if (monsterTracker == null)
            {
                if (Debugging) {
                    Debug.LogError($"GetExpAdj: MonsterTracker is null for {bodyObj}");
                }
                Dump("GetExpAdj-GetMonsterInfo", bodyObj);
                return 0;
            }

            DirectorTracker directorTracker = monsterTracker.DirectorTracker;
            
            // Handle exceptional cases where no baseline has been specified.
            if (directorTracker.Baseline == null)
            {
                Debug.LogError($"No baseline found?");
                Dump("GetExpAdj-GetBaseline", directorTracker.Director);
                return 0;
            }

            // Calculate!
            double creditCost = monsterTracker.Cost;
            double creditBaseline = directorTracker.Baseline.GetBaseline(directorTracker, monsterTracker);

            // Do nothing if the credit baseline is zero.
            if (Debugging && creditBaseline == 0)
            {
                Debug.LogWarning($"Ignoring {creditCost} boss credits for {bodyObj}.");
                return 0;
            }

            if (Debugging)
            {
                Debug.Log($"Credit cost: ${creditBaseline}");
                Debug.Log($"Determined baseline: ${creditBaseline}");
            }

            // creditCost / creditBaseline is the fraction. We multiply by 100 to get the percentage.
            // Then we multiply by GlobalBaselineMult/100, since that's also a percentage.
            double fraction = creditCost / creditBaseline * 100 * (GlobalBaselineMult / 100.0d);
            if (Debugging)
            {
                Debug.Log($"Killed monster worth {creditCost} credits (baseline {creditBaseline:0.00}). Fraction: {fraction:0.00}");
            }
            return (float)fraction;
        }

        #endregion

        #region Debugging

        private void Dump(string source, Component obj)
        {
            if (!Debugging)
                return;

            Debug.Log(Pad($" {source} ", 64, '='));
            Debug.Log($"Dumping GameObject: " + obj.ToString());

            Debug.Log("Components:");
            foreach (var component in obj.GetComponents<Component>())
            {
                Debug.Log($"- {component.GetType().Name}");
            }

            CharacterBody body = obj.GetComponent<CharacterBody>();
            if (body != null)
            {
                Debug.Log($"Has CharacterBody!");

                Debug.Log("Buffs:");
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    BuffDef def = BuffCatalog.GetBuffDef((BuffIndex)i);
                    int stacks = body.GetBuffCount((BuffIndex)i);
                    if (stacks > 0)
                    {
                        Debug.Log($"- Has {stacks} stacks of buff {def.name}");
                    }
                }

                MonsterTracker monsterTracker = body.GetComponent<MonsterTracker>();
                if (monsterTracker != null)
                {
                    Debug.Log($"Body has cost: {monsterTracker.Cost}");
                }
            }

            Debug.Log("================================================================");
        }

        private void Dump(string source, GameObject obj)
        {
            if (!Debugging)
                return;
            Debug.Log(Pad($" {source} ", 64, '='));
            Debug.Log($"Dumping GameObject: " + obj.ToString());

            Debug.Log("Components:");
            foreach (var component in obj.GetComponents<Component>())
            {
                Debug.Log($"- {component.GetType().Name}");
            }

            CharacterBody body = obj.GetComponent<CharacterBody>();
            if (body != null)
            {
                Debug.Log($"Has CharacterBody!");

                Debug.Log("Buffs:");
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    BuffDef def = BuffCatalog.GetBuffDef((BuffIndex)i);
                    int stacks = body.GetBuffCount((BuffIndex)i);
                    if (stacks > 0)
                    {
                        Debug.Log($"- Has {stacks} stacks of buff {def.name}");
                    }
                }

                MonsterTracker monsterTracker = body.GetComponent<MonsterTracker>();
                if (monsterTracker != null)
                {
                    Debug.Log($"Body has cost: {monsterTracker.Cost}");
                }
            }

            Debug.Log("================================================================");
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
