# Unity URP 轮廓线系统文档

## 概述

这是一个基于Unity Universal Render Pipeline (URP)的轮廓线渲染系统，通过自定义Renderer Feature实现了2D物体的轮廓线效果。该系统使用Sobel边缘检测算法来生成平滑的轮廓线。

## 系统架构

```
轮廓线系统
├── OutlineRendererFeature.cs (渲染功能主类)
├── OutlineFeature.shader (轮廓线着色器)
├── Renderer2D.asset (2D渲染器配置)
└── UniversalRenderPipelineGlobalSettings.asset (全局渲染设置)
```

## 文件详细说明

### 1. OutlineRendererFeature.cs

**文件路径**: `Assets/Scripts/URPFeature/OutlineRendererFeature.cs`

**功能**: URP自定义渲染功能的主要实现类

#### 主要组件

##### OutlineRendererFeature 类
- **继承**: `ScriptableRendererFeature`
- **作用**: 管理轮廓线渲染功能的生命周期
- **关键字段**:
  - `outlineMaterial`: 轮廓线材质球
  - `_scriptablePass`: 轮廓线渲染通道实例

##### OutlineRenderPass 类
- **继承**: `ScriptableRenderPass`
- **作用**: 执行具体的轮廓线渲染逻辑

#### 关键技术实现

##### 渲染层级过滤
```csharp
_filteringSettings = new FilteringSettings(RenderQueueRange.all, renderingLayerMask: 1 << 8);
```
- 使用第8个渲染层级(Rendering Layer 8 - "Outline")
- 只有在"Outline"层的对象会被渲染到轮廓遮罩中

##### 双通道渲染流程
1. **第一通道**: 渲染对象到轮廓遮罩纹理
   ```csharp
   cmd.SetRenderTarget(_outlineTexture);
   cmd.ClearRenderTarget(true, true, Color.clear);
   cmd.DrawRendererList(list);
   ```

2. **第二通道**: 使用轮廓着色器绘制最终轮廓
   ```csharp
   cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
   _materialPropertyBlock.SetTexture(_shaderProp_OutlineMask, _outlineTexture);
   cmd.DrawProcedural(Matrix4x4.identity, _outlineMaterial, 0, MeshTopology.Triangles, 3, 1, _materialPropertyBlock);
   ```

##### 渲染时机
- **执行时机**: `RenderPassEvent.BeforeRenderingPostProcessing`
- 在后处理之前执行，确保轮廓线不受后处理影响

### 2. OutlineFeature.shader

**文件路径**: `Assets/Scripts/Self/OutlineFeature.shader`

**功能**: 实现轮廓线的视觉效果

#### 着色器属性

```hlsl
Properties
{
    [HDR] _OutlineColor ("Outline Color", Color) = (0,1,1,1)
    _OutlineSize ("Outline Thickness", Range(0,0.005)) = 0.002
}
```

- `_OutlineColor`: HDR轮廓线颜色，支持发光效果
- `_OutlineSize`: 轮廓线粗细，范围0-0.005

#### 渲染设置

```hlsl
Cull Off       // 不进行面剔除
ZWrite Off     // 不写入深度缓冲
Blend SrcAlpha OneMinusSrcAlpha  // Alpha混合
```

#### 核心算法 - Sobel边缘检测

##### 顶点着色器
- 使用全屏三角形渲染
- 计算8个方向的采样偏移
- 考虑屏幕长宽比的校正

```hlsl
const half aspectRatio = _ScreenParams.x / _ScreenParams.y;
const half diagonalCo = 0.707;  // cos(45°)
```

##### 片段着色器
使用Sobel算子进行边缘检测：

**X方向卷积核**:
```
-1  0  1
-2  0  2
-1  0  1
```

**Y方向卷积核**:
```
-1 -2 -1
 0  0  0
 1  2  1
```

**边缘强度计算**:
```hlsl
col.a = saturate(abs(gx) + abs(gy)) * (1 - alpha);
```

- `gx`, `gy`: X和Y方向的梯度
- `(1 - alpha)`: 确保原物体内部不显示轮廓线

### 3. Renderer2D.asset

**文件路径**: `Assets/URP/Renderer2D.asset`

**功能**: 2D渲染器配置文件

#### 关键配置

```yaml
m_RendererFeatures:
- {fileID: 9202806106724859400}  # OutlineRendererFeature引用

# OutlineRendererFeature配置
m_Script: {fileID: 11500000, guid: 76e4487f631b8b249b013e25700a624c, type: 3}
m_Name: OutlineRendererFeature
m_Active: 1
outlineMaterial: {fileID: 2100000, guid: 12f7668bdf733fb4797171ee3dd088c0, type: 2}
```

- 将`OutlineRendererFeature`添加到渲染器功能列表
- 指定轮廓线材质球的引用

### 4. UniversalRenderPipelineGlobalSettings.asset

**文件路径**: `Assets/URP/UniversalRenderPipelineGlobalSettings.asset`

**功能**: URP全局渲染设置

#### 渲染层级配置

```yaml
m_RenderingLayerNames:
- Light Layer default
- Light Layer 1
- Light Layer 2
- Light Layer 3
- Light Layer 4
- Light Layer 5
- Light Layer 6
- Light Layer 7
- Outline              # 第8层 - 轮廓线专用层级
```

- 定义了9个渲染层级
- 第8层"Outline"专门用于轮廓线系统
- `m_ValidRenderingLayers: 511` 表示所有9层都有效(2^9-1=511)

## 使用方法

### 1. 设置物体轮廓线

1. 选择需要轮廓线的游戏对象
2. 在Inspector中找到Renderer组件
3. 将"Rendering Layer Mask"设置为"Outline"
4. 轮廓线会自动显示

### 2. 调整轮廓线效果

1. 找到轮廓线材质球
2. 调整`_OutlineColor`改变颜色和亮度
3. 调整`_OutlineSize`改变粗细

### 3. 启用/禁用轮廓线系统

1. 在项目中找到`Renderer2D.asset`
2. 在Inspector中找到"OutlineRendererFeature"
3. 勾选/取消勾选"Active"选项

## 技术特点

### 优势

1. **高性能**: 使用双通道渲染，避免了多次采样
2. **质量高**: Sobel边缘检测产生平滑的轮廓线
3. **灵活性**: 支持HDR颜色和可调节粗细
4. **兼容性**: 完全基于URP，与Unity渲染管线无缝集成
5. **选择性**: 只对指定渲染层级的对象生效

### 性能考虑

1. **内存开销**: 需要额外的RT纹理存储轮廓遮罩
2. **填充率**: 全屏后处理需要较高的像素填充率
3. **批处理**: 双通道渲染可能影响批处理效率

## 扩展建议

1. **多重轮廓线**: 支持不同颜色的多层轮廓
2. **动画效果**: 添加轮廓线颜色或粗细的动画
3. **距离淡化**: 根据距离调整轮廓线透明度
4. **遮挡处理**: 添加被遮挡物体的不同轮廓样式

## 故障排除

### 常见问题

1. **轮廓线不显示**:
   - 检查对象是否在"Outline"渲染层级
   - 确认轮廓线材质球已正确配置
   - 验证RendererFeature是否已启用

2. **轮廓线太粗/太细**:
   - 调整材质球中的`_OutlineSize`参数
   - 考虑屏幕分辨率对轮廓线粗细的影响

3. **性能问题**:
   - 监控RT纹理的内存使用
   - 考虑降低轮廓线渲染分辨率
   - 优化需要轮廓线的对象数量

## 版本信息

- **Unity版本**: 2022.3+
- **URP版本**: 12.0+
- **支持平台**: 所有URP支持的平台
- **最后更新**: 2024

---

*该文档涵盖了轮廓线系统的完整实现细节，如有疑问请参考Unity URP官方文档或相关技术资料。* 