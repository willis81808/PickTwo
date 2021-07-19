using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Photon.Pun;
using BepInEx;
using HarmonyLib;
using UnboundLib;
using System.Collections;
using BepInEx.Configuration;
using UnboundLib.GameModes;
using UnboundLib.Networking;

namespace PickTwoPlugin
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "1.0.2")]
    [BepInProcess("Rounds.exe")]
    public class PickTwo : BaseUnityPlugin
    {
        private const string ModId = "com.willis.rounds.picktwo";
        private const string ModName = "Pick Two Cards";
        public static bool ModActive = true;

        public static ConfigEntry<int> cardsToPick;
        
        struct NetworkEventType
        {
            public const string
                ToggleActive = ModId + "_ToggleActive";
        }

        void Awake()
        {
            cardsToPick = Config.Bind("PickN", "CardsToPick", 3);
            
            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            NetworkingManager.RegisterEvent(NetworkEventType.ToggleActive, e =>
            {
                ModActive = (bool)e[0];
            });
        }
        void Start()
        {
            Unbound.RegisterGUI(ModName, DrawGUI);
            Unbound.RegisterHandshake(ModId, OnHandShakeCompleted);

            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, PickExtraCard);
        }

        IEnumerator PickExtraCard(IGameModeHandler gm)
        {
            if (ModActive)
            {
                for (int i = 0; i < cardsToPick.Value - 1; i++)
                {
                    yield return new WaitForSecondsRealtime(0.5f);

                    CardChoiceVisuals.instance.Show(CardChoice_Data.i, true);
                    yield return CardChoice.instance.DoPick(1, CardChoice_Data.LastPickerID, CardChoice_Data.LastPickerType);
                }
            }

            yield return null;
        }

        void DrawGUI()
        {
            var isActive = GUILayout.Toggle(ModActive, "Enabled");

            if (isActive != ModActive)
            {
                NetworkingManager.RaiseEvent(NetworkEventType.ToggleActive, isActive);
            }

            ModActive = isActive;
        }
        void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
                NetworkingManager.RaiseEvent(NetworkEventType.ToggleActive, ModActive);
        }
    }

    [HarmonyPatch]
    class CardChoice_Data
    {
        public static int i;
        public static PickerType LastPickerType;
        public static int LastPickerID;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CardChoiceVisuals), "Show")]
        static void Show_Prefix(int pickerID, bool animateIn)
        {
            i = pickerID;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CardChoice), "StartPick")]
        static void Prefix(PickerType ___pickerType, int picksToSet, int pickerIDToSet)
        {
            LastPickerType = ___pickerType;
            LastPickerID = pickerIDToSet;
        }
    }
}
