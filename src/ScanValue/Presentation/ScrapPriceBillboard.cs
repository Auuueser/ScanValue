using UnityEngine;

namespace Auuueser.ScanValue.Presentation;

internal sealed class ScrapPriceBillboard : MonoBehaviour
{
    private Camera? targetCamera;

    public void SetCamera(Camera? camera)
    {
        targetCamera = camera;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        var cameraForward = targetCamera.transform.forward;
        var cameraUp = targetCamera.transform.up;
        if (cameraForward == Vector3.zero || cameraUp == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(cameraForward, cameraUp);
    }
}
