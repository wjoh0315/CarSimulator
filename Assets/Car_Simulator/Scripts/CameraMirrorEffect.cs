using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMirrorEffect : MonoBehaviour
{
    Camera mirrorCamera;

    void Awake()
    {
        mirrorCamera = GetComponent<Camera>();
    }

    void OnPreCull()
    {
        mirrorCamera.ResetProjectionMatrix();

        Matrix4x4 cameraMatrix = mirrorCamera.projectionMatrix;
        cameraMatrix *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
        mirrorCamera.projectionMatrix = cameraMatrix;
    }

    void OnPreRender()
    {
        GL.invertCulling = true;
    }

    void OnPostRender()
    {
        GL.invertCulling = false;
    }
}
