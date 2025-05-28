using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Material outlineMaterial;
    private OutlineRenderPass _scriptablePass;

    private bool IsMaterialValid => outlineMaterial && outlineMaterial.shader && outlineMaterial.shader.isSupported;

    /// <inheritdoc/>
    public override void Create()
    {
        if (!IsMaterialValid) return;

        _scriptablePass = new OutlineRenderPass(outlineMaterial);
#if !UNITY_6000_0_OR_NEWER
        _scriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
#endif
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
        private readonly FilteringSettings _filteringSettings;
        private readonly MaterialPropertyBlock _materialPropertyBlock;

#if !UNITY_6000_0_OR_NEWER
        private RTHandle _outlineTexture;
#endif

        public OutlineRenderPass(Material outlineMaterial)
        {
#if !UNITY_6000_0_OR_NEWER
            // Configures where the render pass should be injected.
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
#else
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
#endif

            _outlineMaterial = outlineMaterial;
            _filteringSettings = new FilteringSettings(RenderQueueRange.all, renderingLayerMask: 1 << 8);
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        public void Dispose()
        {
#if !UNITY_6000_0_OR_NEWER
            _outlineTexture?.Release();
            _outlineTexture = null;
#else
            // RenderGraph handles texture lifecycle, no manual cleanup needed
#endif
        }

#if !UNITY_6000_0_OR_NEWER
        // Legacy Unity versions implementation using Execute method
        
        // This method is called before executing the render pass.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ResetTarget();
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            RenderingUtils.ReAllocateIfNeeded(ref _outlineTexture, desc);
        }

        // Here you can implement the rendering logic for legacy Unity versions
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Outline Command");

            // First: Render outline mask
            cmd.SetRenderTarget(_outlineTexture);
            cmd.ClearRenderTarget(true, true, Color.clear);
            var drawingSettings = CreateDrawingSettings(_shaderTags, ref renderingData, SortingCriteria.None);
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, _filteringSettings);
            var list = context.CreateRendererList(ref rendererListParams);
            cmd.DrawRendererList(list);

            // Second: Render outline effect to camera color
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

#else
        // Unity 6+ implementation using RenderGraph

        // This class stores the data needed by the RenderGraph pass.
        private class PassData
        {
            public Material outlineMaterial;
            public MaterialPropertyBlock materialPropertyBlock;
            public FilteringSettings filteringSettings;
            public RendererListHandle rendererListHandle;
            public TextureHandle outlineTexture;
            public TextureHandle cameraColorTexture;
        }

        // Static method to execute the outline mask rendering
        static void ExecuteOutlineMaskPass(PassData data, RasterGraphContext context)
        {
            var cmd = context.cmd;
            
            // Clear the outline texture
            cmd.ClearRenderTarget(true, true, Color.clear);
            
            // Draw objects to outline mask
            cmd.DrawRendererList(data.rendererListHandle);
        }

        // Static method to execute the outline rendering
        static void ExecuteOutlineRenderPass(PassData data, RasterGraphContext context)
        {
            var cmd = context.cmd;
            
            // Set the outline mask texture and draw the outline effect
            data.materialPropertyBlock.SetTexture(_shaderProp_OutlineMask, data.outlineTexture);
            cmd.DrawProcedural(Matrix4x4.identity, data.outlineMaterial,
                0, MeshTopology.Triangles, 3, 1,
                data.materialPropertyBlock);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();

            // Create outline mask texture
            var outlineTextureDesc = cameraData.cameraTargetDescriptor;
            outlineTextureDesc.msaaSamples = 1;
            outlineTextureDesc.depthBufferBits = 0;
            outlineTextureDesc.colorFormat = RenderTextureFormat.ARGB32;
            
            TextureHandle outlineTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, outlineTextureDesc, "OutlineTexture", false);

            // First pass: Render outline mask
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Outline Mask Pass", out var passData))
            {
                passData.outlineMaterial = _outlineMaterial;
                passData.materialPropertyBlock = _materialPropertyBlock;
                passData.filteringSettings = _filteringSettings;
                passData.outlineTexture = outlineTexture;

                // Create drawing settings manually for RenderGraph
                var drawingSettings = new DrawingSettings();
                for (int i = 0; i < _shaderTags.Count; ++i)
                {
                    drawingSettings.SetShaderPassName(i, _shaderTags[i]);
                }
                drawingSettings.sortingSettings = new SortingSettings(cameraData.camera) { criteria = SortingCriteria.None };
                drawingSettings.perObjectData = universalRenderingData.perObjectData;
                
                var rendererListParams = new RendererListParams(universalRenderingData.cullResults, drawingSettings, _filteringSettings);
                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderAttachment(outlineTexture, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteOutlineMaskPass(data, context));
            }

            // Second pass: Render outline effect to camera color
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Outline Render Pass", out var passData))
            {
                passData.outlineMaterial = _outlineMaterial;
                passData.materialPropertyBlock = _materialPropertyBlock;
                passData.outlineTexture = outlineTexture;
                passData.cameraColorTexture = resourceData.activeColorTexture;

                builder.UseTexture(outlineTexture);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteOutlineRenderPass(data, context));
            }
        }
#endif
    }
}