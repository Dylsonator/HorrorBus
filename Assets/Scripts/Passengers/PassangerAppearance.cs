using UnityEngine;

public sealed class PassengerAppearance : MonoBehaviour
{
    [Header("Renderers (assign in prefab)")]
    [SerializeField] private Renderer shirtRenderer;
    [SerializeField] private Renderer hairRenderer;
    [SerializeField] private Renderer skinRenderer;
    [SerializeField] private Renderer[] eyesRenderers;


    [Header("Optional palettes (leave empty to tint only)")]
    [SerializeField] private Material[] shirtMaterials;
    [SerializeField] private Material[] hairMaterials;
    [SerializeField] private Material[] skinMaterials;
    [SerializeField] private Material[] eyesMaterials;

    [Header("Tint mode (MaterialPropertyBlock)")]
    [SerializeField] private bool useTint = true;
    [SerializeField] private string colorProperty = "_BaseColor"; // URP Lit: _BaseColor, Standard: _Color

    [Header("Random ranges")]
    [SerializeField] private Color shirtMin = new(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color shirtMax = new(0.95f, 0.95f, 0.95f, 1f);

    [SerializeField] private Color hairMin = new(0.08f, 0.05f, 0.03f, 1f);
    [SerializeField] private Color hairMax = new(0.45f, 0.35f, 0.25f, 1f);

    [SerializeField] private Color skinMin = new(0.60f, 0.40f, 0.30f, 1f);
    [SerializeField] private Color skinMax = new(0.97f, 0.83f, 0.72f, 1f);

    [SerializeField] private Color eyesMin = new(0.10f, 0.10f, 0.10f, 1f);
    [SerializeField] private Color eyesMax = new(0.35f, 0.70f, 0.90f, 1f);

    // Saved appearance state (for copying)
    public int ShirtIndex { get; private set; } = -1;
    public int HairIndex { get; private set; } = -1;
    public int SkinIndex { get; private set; } = -1;
    public int EyesIndex { get; private set; } = -1;

    public Color ShirtColor { get; private set; }
    public Color HairColor { get; private set; }
    public Color SkinColor { get; private set; }
    public Color EyesColor { get; private set; }

    private MaterialPropertyBlock mpb;
    private int colorId;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        colorId = Shader.PropertyToID(colorProperty);
    }

    public void Randomize(int seed)
    {
        var rng = new System.Random(seed);

        ShirtIndex = PickIndex(rng, shirtMaterials);
        HairIndex = PickIndex(rng, hairMaterials);
        SkinIndex = PickIndex(rng, skinMaterials);
        EyesIndex = PickIndex(rng, eyesMaterials);

        ShirtColor = RandomColor(rng, shirtMin, shirtMax);
        HairColor = RandomColor(rng, hairMin, hairMax);
        SkinColor = RandomColor(rng, skinMin, skinMax);
        EyesColor = RandomColor(rng, eyesMin, eyesMax);

        Apply();
    }

    public void CopyFrom(PassengerAppearance other)
    {
        if (other == null) return;

        ShirtIndex = other.ShirtIndex;
        HairIndex = other.HairIndex;
        SkinIndex = other.SkinIndex;
        EyesIndex = other.EyesIndex;

        ShirtColor = other.ShirtColor;
        HairColor = other.HairColor;
        SkinColor = other.SkinColor;
        EyesColor = other.EyesColor;

        Apply();
    }

    public void Apply()
    {
        ApplyPart(shirtRenderer, ShirtIndex, shirtMaterials, ShirtColor);
        ApplyPart(hairRenderer, HairIndex, hairMaterials, HairColor);
        ApplyPart(skinRenderer, SkinIndex, skinMaterials, SkinColor);
        ApplyPartMulti(eyesRenderers, EyesIndex, eyesMaterials, EyesColor);

    }

    private void ApplyPart(Renderer r, int idx, Material[] palette, Color tint)
    {
        if (r == null) return;

        // Material swap (optional)
        if (palette != null && palette.Length > 0 && idx >= 0 && idx < palette.Length && palette[idx] != null)
        {
            // Use sharedMaterial so we don't instantiate
            r.sharedMaterial = palette[idx];
        }

        // Tint (safe per-instance)
        if (useTint)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(colorId, tint);
            r.SetPropertyBlock(mpb);
        }
    }
    private void ApplyPartMulti(Renderer[] rs, int idx, Material[] palette, Color tint)
    {
        if (rs == null || rs.Length == 0) return;

        for (int i = 0; i < rs.Length; i++)
            ApplyPart(rs[i], idx, palette, tint);
    }


    private static int PickIndex(System.Random rng, Material[] palette)
    {
        if (palette == null || palette.Length == 0) return -1;
        return rng.Next(0, palette.Length);
    }

    private static Color RandomColor(System.Random rng, Color min, Color max)
    {
        float r = (float)rng.NextDouble();
        float g = (float)rng.NextDouble();
        float b = (float)rng.NextDouble();

        return new Color(
            Mathf.Lerp(min.r, max.r, r),
            Mathf.Lerp(min.g, max.g, g),
            Mathf.Lerp(min.b, max.b, b),
            1f
        );
    }
}
