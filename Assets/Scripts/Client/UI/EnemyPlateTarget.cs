using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyPlateTarget : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private EnemyRank rank = EnemyRank.Elite;
    [SerializeField] private int level = 12;
    [SerializeField] private float headClearance = 0.25f;

    public Health Health { get; private set; }
    public EnemyRank Rank => rank;
    public int Level => level;

    public float VisionFade => vision != null ? vision.Fade : 0f;
    public float HeightOffset
    {
        get
        {
            if (rend == null) rend = GetComponentInChildren<Renderer>();
            if (rend == null) return 1f + headClearance;
            return (rend.bounds.max.y - transform.position.y) + headClearance;
        }
    }

    private Renderer rend;

    private VisionHidden vision;

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? Clean(gameObject.name) : displayName.ToUpperInvariant();

    public float Fraction =>
        Health != null && Health.MaxHealth > 0f
            ? Mathf.Clamp01(Health.CurrentHealth / Health.MaxHealth)
            : 0f;

    private void Awake()
    {
        Health = GetComponent<Health>();
        rend = GetComponentInChildren<Renderer>();
        vision = GetComponent<VisionHidden>();
        if (vision == null) vision = GetComponentInChildren<VisionHidden>(true);
    }

    private void OnEnable() => EnemyPlates.Register(this);

    private void OnDisable() => EnemyPlates.Unregister(this);

    private static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        int paren = raw.IndexOf('(');
        if (paren > 0) raw = raw.Substring(0, paren);
        return raw.Trim().ToUpperInvariant();
    }
}
