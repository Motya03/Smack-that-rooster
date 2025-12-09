using UnityEngine;

public class FaceCameraMultiplayer : MonoBehaviour
{
    void LateUpdate()
    {
        transform.LookAt(Camera.main.transform);
    }
}
