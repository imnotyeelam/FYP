
using UnityEngine;

public class RuinGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    public Color glowColor = Color.cyan;

    [Range(0f, 10f)]
    public float glowIntensity = 3f;

    private Renderer[] ruinRenderers;
    private Material[][] materials;

    void Awake()
    {
        // Find all Mesh Renderers, including LOD0, LOD1, LOD2
        ruinRenderers = GetComponentsInChildren<MeshRenderer>(true);

        materials = new Material[ruinRenderers.Length][];

        for (int i = 0; i < ruinRenderers.Length; i++)
        {
            materials[i] = ruinRenderers[i].materials;

            foreach (Material mat in materials[i])
            {
                if (mat == null ||
                    !mat.HasProperty("_EmissionColor"))
                    continue;

                mat.EnableKeyword("_EMISSION");

                // Initially no emission
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    public void ActivateGlow()
    {
        if (materials == null)
            return;

        foreach (Material[] rendererMaterials in materials)
        {
            foreach (Material mat in rendererMaterials)
            {
                if (mat == null ||
                    !mat.HasProperty("_EmissionColor"))
                    continue;

                mat.EnableKeyword("_EMISSION");

                mat.SetColor(
                    "_EmissionColor",
                    glowColor * glowIntensity
                );
            }
        }

        Debug.Log(gameObject.name + " magic activated!");
    }

    void OnDestroy()
    {
        if (materials == null)
            return;

        foreach (Material[] rendererMaterials in materials)
        {
            foreach (Material mat in rendererMaterials)
            {
                if (mat != null)
                    Destroy(mat);
            }
        }
    }
}
