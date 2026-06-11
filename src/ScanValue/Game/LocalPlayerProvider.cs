using GameNetcodeStuff;
using UnityEngine;

namespace Auuueser.ScanValue.Game;

internal sealed class LocalPlayerProvider
{
    public bool TryGet(out Vector3 playerPosition, out Camera playerCamera, out string failureReason)
    {
        playerPosition = Vector3.zero;
        playerCamera = null!;
        failureReason = string.Empty;

        var localPlayerController = ResolveLocalPlayer();
        if (localPlayerController == null)
        {
            failureReason = "No local player";
            return false;
        }

        var round = StartOfRound.Instance;
        if (TryUseCamera(localPlayerController.gameplayCamera, localPlayerController.transform.position, out playerPosition, out playerCamera))
        {
            return true;
        }

        if (round != null && round.activeCamera != null &&
            TryUseCamera(round.activeCamera, round.activeCamera.transform.position, out playerPosition, out playerCamera))
        {
            return true;
        }

        if (round != null && round.spectateCamera != null &&
            TryUseCamera(round.spectateCamera, round.spectateCamera.transform.position, out playerPosition, out playerCamera))
        {
            return true;
        }

        failureReason = "No usable camera";
        return false;
    }

    private static PlayerControllerB? ResolveLocalPlayer()
    {
        var round = StartOfRound.Instance;
        if (round != null && round.localPlayerController != null)
        {
            return round.localPlayerController;
        }

        var hud = HUDManager.Instance;
        if (hud != null && hud.localPlayer != null)
        {
            return hud.localPlayer;
        }

        var gameNetworkManager = GameNetworkManager.Instance;
        return gameNetworkManager != null ? gameNetworkManager.localPlayerController : null;
    }

    private static bool TryUseCamera(Camera? camera, Vector3 distanceCenter, out Vector3 playerPosition, out Camera playerCamera)
    {
        if (camera != null && camera.isActiveAndEnabled)
        {
            playerPosition = distanceCenter;
            playerCamera = camera;
            return true;
        }

        playerPosition = Vector3.zero;
        playerCamera = null!;
        return false;
    }
}
