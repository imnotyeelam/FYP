
using UnityEngine;
using TMPro;

public class CallStackBlock : MonoBehaviour
{
    public TMP_Text functionLabel;
    public Renderer blockRenderer;

    private Material blockMaterial;

    void Awake()
    {
        if (blockRenderer == null)
            blockRenderer = GetComponent<Renderer>();

        if (blockRenderer != null)
            blockMaterial = blockRenderer.material;
    }

    public void Initialize(int n)
    {
        if (functionLabel != null)
            functionLabel.text = $"countdown({n})";
    }

    public void SetBaseCase()
    {
        if (blockMaterial != null)
            blockMaterial.color = Color.green;
    }

    public void SetReturning()
    {
        if (blockMaterial != null)
            blockMaterial.color = Color.yellow;
    }
}
