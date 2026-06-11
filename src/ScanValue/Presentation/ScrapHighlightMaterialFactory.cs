using UnityEngine;
using UnityEngine.Rendering;

namespace Auuueser.ScanValue.Presentation;

internal static class ScrapHighlightMaterialFactory
{
    public static Material Create(ScrapHighlightStyle style)
    {
        var shader = Shader.Find("HDRP/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Hidden/Internal-Colored");
        var material = new Material(shader)
        {
            name = "ScanValue.ScanHighlight",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = 3000,
        };

        Apply(material, style);
        return material;
    }

    public static void Apply(Material material, ScrapHighlightStyle style)
    {
        material.SetColor("_Color", style.Color);
        material.SetColor("_BaseColor", style.Color);
        material.SetColor("_UnlitColor", style.Color);
        material.SetInt("_Cull", (int)CullMode.Front);
        material.SetInt("_CullMode", (int)CullMode.Front);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetFloat("_SurfaceType", 1f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }
}
