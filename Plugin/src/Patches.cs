﻿using System;
using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameNetcodeStuff;
using JohnPaularatusEnemy.Util.Extensions;

namespace JohnPaularatusEnemy.Patches;

[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch {
	[HarmonyPatch(nameof(StartOfRound.Awake))]
	[HarmonyPostfix]
	public static void StartOfRound_Awake(ref StartOfRound __instance)
	{
		__instance.NetworkObject.OnSpawn(CreateNetworkManager);

	}

	private static void CreateNetworkManager()
	{
		if (StartOfRound.Instance.IsServer || StartOfRound.Instance.IsHost)
		{
			if (JohnPaularatusUtils.Instance == null) {
				GameObject utilsInstance = GameObject.Instantiate(Plugin.UtilsPrefab);
				SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
				utilsInstance.GetComponent<NetworkObject>().Spawn();
				Plugin.Logger.LogInfo($"Created JohnPaularatusUtils. Scene is: '{utilsInstance.scene.name}'");
			} else {
				Plugin.Logger.LogWarning("JohnPaularatusUtils already exists?");
			}
		}
	}
}

[HarmonyPatch(typeof(Landmine))]
static class LandminePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    static void PatchOnTriggerEnter(Landmine __instance, Collider other, ref float ___pressMineDebounceTimer)
    {
		ReadyJP component = other.gameObject.GetComponent<ReadyJP>();
		if (component != null && !component.isEnemyDead)
		{
			___pressMineDebounceTimer = 0.5f;
			__instance.PressMineServerRpc();
		}
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerExit")]
    static void PatchOnTriggerExit(Landmine __instance, Collider other, ref bool ___sendingExplosionRPC)
    {
		ReadyJP component = other.gameObject.GetComponent<ReadyJP>();
		if (component != null && !component.isEnemyDead)
		{
			if (!__instance.hasExploded)
			{
				__instance.SetOffMineAnimation();
				___sendingExplosionRPC = true;
				__instance.ExplodeMineServerRpc();
			}
		}
	}
}