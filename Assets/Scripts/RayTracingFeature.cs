using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering.Universal;
using LightType = UnityEngine.LightType;

public partial class RayTracingFeature : ScriptableRendererFeature
{
    [SerializeField] private ComputeShader _rayTracingShader;
    [SerializeField] private Texture2D _skybox;
    [SerializeField] private Material _addMaterial;
    [SerializeField] private Vector4 _directionalLightDirection;
    
    private RayTracingPass _pass;
    
    public override void Create()
    {
        if (_rayTracingShader == null)
            return;

        _pass = new RayTracingPass(_rayTracingShader, _skybox, _addMaterial, _directionalLightDirection);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_rayTracingShader == null)
            return;
        
        renderer.EnqueuePass(_pass);
    }
}
