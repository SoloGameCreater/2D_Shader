# Unity URP 轮廓线系统

## 概述

这是一个高度兼容的Unity Universal Render Pipeline (URP)轮廓线渲染系统，支持从Unity 2022.3到Unity 6.0+的所有版本。系统采用Sobel边缘检测算法生成高质量的2D物体轮廓线效果。

## 🚀 最新特性

### 多版本兼容性优化
- ✅ **Unity 2022.3 - 2023.x**: 使用传统的Execute模式渲染
- ✅ **Unity 6.0+**: 使用新的RenderGraph系统
- ✅ **自动版本检测**: 编译时自动选择适配的渲染路径

### 兼容性特点
- **向前兼容**: 旧项目升级到Unity 6.0+时无需修改代码
- **向后兼容**: 新项目可在旧版Unity中正常运行
- **性能优化**: 在Unity 6.0+中享受RenderGraph的性能优势

## 📁 系统架构

```
轮廓线系统
├── OutlineRendererFeature.cs     # 主要渲染功能 (支持多版本)
├── OutlineFeature.shader         # 轮廓线着色器
├── Renderer2D.asset             # 2D渲染器配置
└── UniversalRenderPipelineGlobalSettings.asset  # 全局设置
```

## 🔧 技术实现

### 版本检测机制

```csharp
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
```

系统使用预处理指令自动检测Unity版本，确保代码在不同版本中的正确执行。

### 双渲染路径

#### 传统模式 (Unity 2022.3 - 2023.x)
```csharp
#if !UNITY_6000_0_OR_NEWER
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    // 传统Execute方法实现
    // 手动管理RT纹理生命周期
}
#endif
```

**特点:**
- 使用`Execute`方法进行渲染
- 手动管理`RTHandle`资源
- 兼容所有旧版Unity项目

#### RenderGraph模式 (Unity 6.0+)
```csharp
#if UNITY_6000_0_OR_NEWER
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    // RenderGraph实现
    // 自动资源管理和优化
}
#endif
```

**特点:**
- 使用`RecordRenderGraph`方法
- 自动纹理生命周期管理
- 更好的性能和内存优化
- GPU调试支持增强

### 核心渲染流程

无论哪个版本，都采用相同的双通道渲染逻辑：

1. **轮廓遮罩通道**: 将目标对象渲染到专用RT纹理
2. **轮廓生成通道**: 使用Sobel算子生成最终轮廓效果

### 渲染层级系统

```csharp
_filteringSettings = new FilteringSettings(RenderQueueRange.all, renderingLayerMask: 1 << 8);
```

- 使用第8个渲染层级 ("Outline")
- 只渲染指定层级的对象
- 精确控制轮廓线显示范围

## 🎯 使用指南

### 快速开始

1. **添加RendererFeature**
   - 在URP Renderer Asset中添加`OutlineRendererFeature`
   - 分配轮廓线材质球

2. **设置对象轮廓**
   - 选择需要轮廓线的游戏对象
   - 将Renderer的"Rendering Layer Mask"设为"Outline"

3. **调整效果**
   - `_OutlineColor`: 调整轮廓颜色(支持HDR)
   - `_OutlineSize`: 调整轮廓粗细(0-0.005)

### 高级配置

#### 材质球参数
```hlsl
[HDR] _OutlineColor ("Outline Color", Color) = (0,1,1,1)
_OutlineSize ("Outline Thickness", Range(0,0.005)) = 0.002
```

#### 性能优化建议
- **分辨率缩放**: 考虑降低轮廓RT分辨率以提升性能
- **层级管理**: 只对必要对象使用轮廓层级
- **批处理优化**: 尽量减少状态切换

## 📊 版本对比

| 特性 | Unity 2022.3-2023.x | Unity 6.0+ |
|------|---------------------|-------------|
| 渲染模式 | Execute | RenderGraph |
| 资源管理 | 手动 | 自动 |
| 性能 | 标准 | 优化 |
| 调试支持 | 基础 | 增强 |
| GPU内存 | 手动优化 | 自动优化 |

## 🔍 故障排除

### 常见问题

**轮廓线不显示**
```
✓ 检查对象是否在"Outline"渲染层级
✓ 确认RendererFeature已启用
✓ 验证材质球配置正确
✓ 检查Unity版本兼容性
```

**版本升级问题**
```
✓ Unity 6.0+升级后重新编译
✓ 检查RenderGraph功能是否正常
✓ 确认URP版本匹配
```

**性能问题**
```
✓ 监控RT纹理内存使用
✓ 考虑分辨率缩放
✓ 优化轮廓对象数量
✓ 在Unity 6.0+中启用RenderGraph优化
```

## 📋 系统要求

### 支持的Unity版本
- **最低版本**: Unity 2022.3 LTS
- **推荐版本**: Unity 2023.3 LTS 或 Unity 6.0+
- **URP版本**: 12.0+ (Unity 2022.3+), 17.0+ (Unity 6.0+)

### 支持的平台
- Windows、macOS、Linux
- iOS、Android
- WebGL
- 主机平台 (PlayStation、Xbox、Switch)

## 🛠️ 开发信息

### 技术栈
- **渲染管线**: Universal Render Pipeline
- **着色器语言**: HLSL
- **边缘检测**: Sobel算子
- **兼容性**: 条件编译指令

### 关键类说明

#### `OutlineRendererFeature`
- 主要的ScriptableRendererFeature实现
- 管理渲染通道生命周期
- 处理版本兼容性

#### `OutlineRenderPass`
- 具体的渲染逻辑实现
- 双路径渲染支持
- 资源管理和清理

### 代码结构
```
OutlineRendererFeature.cs
├── 版本检测 (#if UNITY_6000_0_OR_NEWER)
├── 传统渲染路径 (Execute方法)
├── RenderGraph路径 (RecordRenderGraph方法)
└── 通用资源管理
```

## 🚀 未来规划

### 计划功能
- [ ] 多重轮廓线支持
- [ ] 轮廓线动画效果
- [ ] 距离自适应轮廓
- [ ] 自定义轮廓形状
- [ ] 实时轮廓颜色调整

### 性能优化
- [ ] 更高效的边缘检测算法
- [ ] GPU Compute Shader支持
- [ ] 移动端特化优化
- [ ] Unity 6.0+ RenderGraph深度优化

## 📞 技术支持

如遇到问题，请按以下步骤排查：

1. **检查Unity版本**: 确认使用支持的Unity版本
2. **验证URP设置**: 确保URP配置正确
3. **查看控制台**: 检查是否有编译错误或警告
4. **性能分析**: 使用Unity Profiler分析性能
5. **版本兼容**: 确认代码适配当前Unity版本

---

**📝 更新日志**
- **v2.0**: 添加Unity 6.0+ RenderGraph支持
- **v1.5**: 优化传统渲染路径性能
- **v1.0**: 初始版本，支持基础轮廓线功能

*该系统已在多个Unity版本中测试验证，确保稳定性和兼容性。* 