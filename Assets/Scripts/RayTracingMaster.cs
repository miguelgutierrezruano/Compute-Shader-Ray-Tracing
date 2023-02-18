using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    [SerializeField] private ComputeShader rayTracingShader;
    [SerializeField] private Texture skyboxTexture;

    private RenderTexture target;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    // Set the matrices of the camera on the shader
    private void SetShaderParameters()
    {
        rayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        
        rayTracingShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitializeRenderTexture();
        
        // Set the target and dispatch the compute shader
        rayTracingShader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        // Blit the result texture to the screen
        Graphics.Blit(target, destination);
    }

    private void InitializeRenderTexture()
    {
        if (!(target != null) || target.width != Screen.width || target.height != Screen.height)
        {
            // Release render texture if exist
            if(target != null)
                target.Release();
            
            // Get a render target for RayTracing
            target = new RenderTexture(Screen.width, Screen.height, 0,
                         RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear); 

            target.enableRandomWrite = true;
            target.Create();
        }
    }
}
