using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class GradientText : MonoBehaviour
{
    public Gradient gradient;

    [Range(0f, 360f)]
    public float gradientAngle = 315f;

    private TextMeshProUGUI _tmp;
    private bool _dirty;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        ApplyGradientSafe();
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    void OnTextChanged(Object obj)
    {
        if (obj == _tmp)
            _dirty = true;
    }

    void LateUpdate()
    {
        if (_dirty)
        {
            ApplyGradientSafe();
            _dirty = false;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
                ApplyGradientSafe();
        };
    }
#endif

    private void ApplyGradientSafe()
    {
        // 避免 TMP 还没初始化
        if (_tmp == null || gradient == null)
            return;

        _tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = _tmp.textInfo;
        if (textInfo == null || textInfo.characterCount == 0)
            return;

        // --- Collect bounds ---
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var c = textInfo.characterInfo[i];
            if (!c.isVisible) continue;

            Vector3 bl = c.bottomLeft;
            Vector3 tr = c.topRight;

            if (bl.x < min.x) min.x = bl.x;
            if (bl.y < min.y) min.y = bl.y;
            if (tr.x > max.x) max.x = tr.x;
            if (tr.y > max.y) max.y = tr.y;
        }

        if (min.x == float.MaxValue) return;

        // --- calc projection direction ---
        float rad = gradientAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

        float minProj = float.MaxValue;
        float maxProj = float.MinValue;

        Vector2[] corners = {
            new Vector2(min.x, min.y),
            new Vector2(max.x, min.y),
            new Vector2(min.x, max.y),
            new Vector2(max.x, max.y)
        };

        foreach (var p in corners)
        {
            float proj = Vector2.Dot(p, dir);
            if (proj < minProj) minProj = proj;
            if (proj > maxProj) maxProj = proj;
        }

        // --- apply to all meshes ---
        var meshInfos = textInfo.meshInfo;
        if (meshInfos == null || meshInfos.Length == 0)
            return;

        for (int m = 0; m < meshInfos.Length; m++)
        {
            var meshInfo = meshInfos[m];

            if (meshInfo.vertices == null ||
                meshInfo.vertices.Length == 0)
                continue;

            if (meshInfo.colors32 == null ||
                meshInfo.colors32.Length != meshInfo.vertices.Length)
            {
                meshInfo.colors32 = new Color32[meshInfo.vertices.Length];
            }

            var verts = meshInfo.vertices;
            var cols = meshInfo.colors32;

            for (int v = 0; v < verts.Length; v++)
            {
                float proj = Vector2.Dot(verts[v], dir);
                float t = Mathf.InverseLerp(minProj, maxProj, proj);
                cols[v] = gradient.Evaluate(t);
            }

            textInfo.meshInfo[m].colors32 = cols;
        }

        _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
