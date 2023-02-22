using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class RayTracingMaster : MonoBehaviour
{
    [Range(2, 8)] 
    [SerializeField] private int rayBounces; 
    
    [Header("References")]
    [SerializeField] private ComputeShader rayTracingShader;
    [SerializeField] private Texture skyboxTexture;
    [SerializeField] private Light directionalLight;

    private RenderTexture target;

    private Camera _camera;
    
    // Antialiasing
    private uint _currentSample = 0;
    private Material _addMaterial;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }

        if(directionalLight.transform.hasChanged)
        {
            _currentSample = 0;
            directionalLight.transform.hasChanged = false;
        }
    }

    private void SetShaderParameters()
    {
        // Set the matrices of the camera on the shader
        rayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        
        rayTracingShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
        
        // Set parameters
        rayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetInt("_RayMaxBounces", rayBounces);

        // Set lightning in compute shader
        Vector3 lightDirection = directionalLight.transform.forward;
        rayTracingShader.SetVector("_DirectionalLight", 
            new Vector4(lightDirection.x, lightDirection.y, lightDirection.z, directionalLight.intensity));


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
        
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        
        // Blit the result texture to the screen
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(target, destination, _addMaterial);
        _currentSample++;
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

            // Reset antialiasing
            _currentSample = 0;
            
            target.enableRandomWrite = true;
            target.Create();
        }
    }
}
