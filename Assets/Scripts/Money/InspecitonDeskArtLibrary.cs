using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InspectionDeskArtLibrary : MonoBehaviour
{
    [Serializable]
    public struct ArtEntry
    {
        public string key;
        public Sprite baseSprite;
        public Sprite overlaySprite;
        public Vector2 size;
        public bool hideText;
        public Color tint;
    }

    [Header("Assign your desk sprites here")]
    [SerializeField] private List<ArtEntry> entries = new List<ArtEntry>();

    private readonly Dictionary<string, ArtEntry> cache = new Dictionary<string, ArtEntry>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        RebuildCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildCache();
    }
#endif

    public bool TryGetArt(
        InspectionDeskItemState state,
        out Sprite baseSprite,
        out Sprite overlaySprite,
        out Vector2 size,
        out bool hideText,
        out Color tint)
    {
        baseSprite = null;
        overlaySprite = null;
        size = Vector2.zero;
        hideText = false;
        tint = Color.white;

        if (state == null)
            return false;

        string key = !string.IsNullOrWhiteSpace(state.artKey) ? state.artKey : GetDefaultArtKey(state);
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (!cache.TryGetValue(key, out ArtEntry entry))
            return false;

        if (entry.baseSprite == null && entry.overlaySprite == null)
            return false;

        baseSprite = entry.baseSprite;
        overlaySprite = entry.overlaySprite;
        size = entry.size.sqrMagnitude > 0.01f ? entry.size : GetFallbackSizeForKey(key, state.moneyValuePence);
        hideText = entry.hideText || state.preferArtOnly;
        tint = entry.tint.a <= 0f ? Color.white : entry.tint;
        return true;
    }

    public static string GetMoneyArtKey(int valuePence)
    {
        return $"money_{Mathf.Max(0, valuePence)}";
    }

    public static string GetIdArtKey(PassengerIdVisual visual)
    {
        return visual switch
        {
            PassengerIdVisual.Real => "id_real",
            PassengerIdVisual.ObviousFake => "id_obvious_fake",
            PassengerIdVisual.FakeAlt1 => "id_fake_alt1",
            PassengerIdVisual.FakeAlt2 => "id_fake_alt2",
            PassengerIdVisual.FakeAlt3 => "id_fake_alt3",
            _ => "id_real"
        };
    }

    public static string GetTicketArtKey(PassengerTicketState state)
    {
        return state switch
        {
            PassengerTicketState.Valid => "ticket_valid",
            PassengerTicketState.Old => "ticket_old",
            PassengerTicketState.Fake => "ticket_fake",
            _ => "ticket_valid"
        };
    }

    private string GetDefaultArtKey(InspectionDeskItemState state)
    {
        if (state.kind == InspectionDeskItemKind.Cash)
            return GetMoneyArtKey(state.moneyValuePence);

        return state.kind switch
        {
            InspectionDeskItemKind.IdCard => "id_real",
            InspectionDeskItemKind.Ticket => "ticket_valid",
            _ => string.Empty
        };
    }

    private Vector2 GetFallbackSizeForKey(string key, int moneyValuePence)
    {
        if (key.StartsWith("money_", StringComparison.OrdinalIgnoreCase))
            return moneyValuePence >= 500 ? new Vector2(136f, 92f) : new Vector2(64f, 64f);

        if (key.StartsWith("id_", StringComparison.OrdinalIgnoreCase))
            return new Vector2(250f, 150f);

        if (key.StartsWith("ticket_", StringComparison.OrdinalIgnoreCase))
            return new Vector2(180f, 90f);

        return new Vector2(180f, 100f);
    }

    private void RebuildCache()
    {
        cache.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            ArtEntry entry = entries[i];
            if (string.IsNullOrWhiteSpace(entry.key))
                continue;

            if (entry.tint.a <= 0f)
                entry.tint = Color.white;

            cache[entry.key.Trim()] = entry;
        }
    }
}