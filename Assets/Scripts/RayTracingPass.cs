using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;
using Random = UnityEngine.Random;

public partial class RayTracingFeature
{
    private struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };
    
    private class RayTracingPass : ScriptableRenderPass, IDisposable
    {
        private ComputeShader _computeShader;
        private int _computeShaderRenderTargetId;
        private RenderTargetIdentifier _computeShaderRenderTargetIdentifier;

        private int _renderTargetId;
        
        private int _width;
        private int _height;
        private uint _xGroupSize;
        private uint _yGroupSize;
        private int _kernelId;

        private Camera _camera;

        private Texture2D _skybox;
        private RenderTargetIdentifier _skyboxRenderTargetIdentifier;
        
        private Material _addMaterial;

        private Vector4 _directionalLightDirection;

        private Vector2 _sphereRadius = new(3.0f, 8.0f);
        private uint _sphereMax = 100;
        private float _spherePlacementRadius = 100.0f;
        private ComputeBuffer _sphereBuffer;
        
        public RayTracingPass(ComputeShader shader, Texture2D skybox, Material addMaterial, Vector4 directionalLightDirection)
        {
            _computeShader = shader;
            _skybox = skybox;
            _skyboxRenderTargetIdentifier = new RenderTargetIdentifier(_skybox);

            _addMaterial = addMaterial;
            
            _camera = Camera.main;

            _directionalLightDirection = directionalLightDirection;
            
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            SetupScene();
        }

        private void SetupScene()
        {
            List<Sphere> spheres = new List<Sphere>();
            for (int i = 0; i < _sphereMax; ++i)
            {
                var sphere = new Sphere
                {
                    radius = _sphereRadius.x + Random.value * (_sphereRadius.y - _sphereRadius.x)
                };

                Vector2 randomPos = Random.insideUnitCircle * _spherePlacementRadius;
                sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

                foreach (var other in spheres)
                {
                    float minDist = sphere.radius + other.radius;
                    if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                        goto SkipSphere;
                }

                Color color = Random.ColorHSV();
                bool metal = Random.value < 0.5f;
                sphere.albedo = metal ? 
                    Vector3.zero : new Vector3(color.r, color.g, color.b);
                sphere.specular = metal ? 
                    new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
                
                spheres.Add(sphere);
                
            SkipSphere:
                continue;
            }

            _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
            _sphereBuffer.SetData(spheres);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTargetDescriptor.enableRandomWrite = true;
            cameraTargetDescriptor.depthBufferBits = 0;

            //컴퓨트 쉐이더를 위한 렌더타겟을 얻음
            _computeShaderRenderTargetId = Shader.PropertyToID("Result");
            _computeShaderRenderTargetIdentifier = new RenderTargetIdentifier(_computeShaderRenderTargetId);
            cmd.GetTemporaryRT(_computeShaderRenderTargetId, cameraTargetDescriptor);

            //일반 쉐이더를 위한 렌더타겟을 얻음
            _renderTargetId = Shader.PropertyToID("_CustomRenderBuffer");
            cmd.GetTemporaryRT(_renderTargetId, cameraTargetDescriptor);
            
            _kernelId = _computeShader.FindKernel("CSMain");
            
            _computeShader.GetKernelThreadGroupSizes(_kernelId, out _xGroupSize, out _yGroupSize, out _);

            _width = cameraTargetDescriptor.width;
            _height = cameraTargetDescriptor.height;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, new ProfilingSampler("Ray Tracing")))
            {
                cmd.SetComputeBufferParam(_computeShader, _kernelId, "_Spheres", _sphereBuffer);
                cmd.SetComputeVectorParam(_computeShader, "_DirectionalLight",
                    new Vector4(_directionalLightDirection.x, _directionalLightDirection.y, _directionalLightDirection.z, _directionalLightDirection.w));
                cmd.SetComputeVectorParam(_computeShader, "PixelOffset", new Vector2(Random.value, Random.value));
                cmd.SetComputeTextureParam(_computeShader, _kernelId, "_SkyboxTexture", _skyboxRenderTargetIdentifier);
                cmd.SetComputeMatrixParam(_computeShader, "_CameraToWorld", _camera.cameraToWorldMatrix);
                cmd.SetComputeMatrixParam(_computeShader, "_CameraInverseProjection", _camera.projectionMatrix.inverse);
                cmd.SetComputeTextureParam(_computeShader, _kernelId, _computeShaderRenderTargetId, _computeShaderRenderTargetIdentifier);
                cmd.DispatchCompute(_computeShader, _kernelId, _width / (int)_xGroupSize, _height / (int)_yGroupSize,
                    1);

                cmd.Blit(_renderTargetId, renderingData.cameraData.renderer.cameraColorTarget, _addMaterial);
            }
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_computeShaderRenderTargetId);
            cmd.ReleaseTemporaryRT(_renderTargetId);
        }

        public void Dispose()
        {
            _sphereBuffer?.Release();
        }
    }
}
