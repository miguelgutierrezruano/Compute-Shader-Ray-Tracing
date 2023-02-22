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

    [Header("Sphere parameters")]
    [SerializeField] Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    [SerializeField] uint SpheresMax = 100;
    [SerializeField] float SpherePlacementRadius = 50.0f;

    private ComputeBuffer _sphereBuffer;

    private RenderTexture target;

    private Camera _camera;
    
    // Antialiasing
    private uint _currentSample = 0;
    private Material _addMaterial;

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };

    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();

        // Add a number of random spheres
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();

            // Radius and radius
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);

            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y + SpherePlacementRadius * 2);

            bool valid = true;

            // Reject spheres that are intersecting others
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid) continue;

            // Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.2f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;

            // Add the sphere to the list
            spheres.Add(sphere);
        }

        // Assign to compute buffer
        _sphereBuffer = new ComputeBuffer(spheres.Count, 40); // 40 is stride - byte lenght of struct
        _sphereBuffer.SetData(spheres);
    }

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

        rayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
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
