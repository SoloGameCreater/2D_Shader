using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    class OutlineRenderPass : ScriptableRenderPass
    {
        private static readonly List<ShaderTagId> _shaderTags = new List<ShaderTagId>()
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        private static readonly int _shaderProp_OutlineMask = Shader.PropertyToID("_OutlineMask");

        private readonly Material _outlineMaterial;
        private RTHandle _outlineTexture;
        private readonly FilteringSettings _filteringSettings;
        private readonly MaterialPropertyBlock _materialPropertyBlock;

        public OutlineRenderPass(Material outlineMaterial)
        {
            // Configures where the render pass should be injected.
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            _outlineMaterial = outlineMaterial;
            _filteringSettings = new FilteringSettings(RenderQueueRange.all, renderingLayerMask: 1 << 8);
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ResetTarget();
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            RenderingUtils.ReAllocateIfNeeded(ref _outlineTexture, desc);
        }

        public void Dispose()
        {
            _outlineTexture?.Release();
            _outlineTexture = null;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Outline Command");

            cmd.SetRenderTarget(_outlineTexture);
            cmd.ClearRenderTarget(true, true, Color.clear);
            var drawingSettings = CreateDrawingSettings(_shaderTags, ref renderingData, SortingCriteria.None);
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);
            var list = context.CreateRendererList(ref rendererListParams);
            cmd.DrawRendererList(list);

            cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
            _materialPropertyBlock.SetTexture(_shaderProp_OutlineMask, _outlineTexture);
            cmd.DrawProcedural(Matrix4x4.identity, _outlineMaterial,
                0, MeshTopology.Triangles, 3, 1,
                _materialPropertyBlock);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    [SerializeField] private Material outlineMaterial;
    private OutlineRenderPass _scriptablePass;

    private bool IsMaterialValid => outlineMaterial && outlineMaterial.shader && outlineMaterial.shader.isSupported;

    /// <inheritdoc/>
    public override void Create()
    {
        if (!IsMaterialValid) return;

        _scriptablePass = new OutlineRenderPass(outlineMaterial);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_scriptablePass == null) return;

        renderer.EnqueuePass(_scriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        _scriptablePass?.Dispose();
    }
}