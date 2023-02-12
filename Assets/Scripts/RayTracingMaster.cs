using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    [SerializeField] private ComputeShader rayTracingShader;

    private RenderTexture target;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
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
