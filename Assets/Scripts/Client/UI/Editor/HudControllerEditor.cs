using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HudController))]
public class HudControllerEditor : Editor
{
    static readonly Color BarHi = new Color(42f / 255f, 35f / 255f, 24f / 255f, 1f);
    static readonly Color BarLo = new Color(20f / 255f, 16f / 255f,  9f / 255f, 1f);

    const float EnemyHit = 5f;

    private static Damage TestHit(float amount) => new Damage(DamageType.Physical, amount);

    static Health rapidTarget;
    static int rapidLeft;
    static double rapidNext;

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Reset Action Bar Colours (mockup)"))
        {
            serializedObject.Update();
            serializedObject.FindProperty("barTop").colorValue = BarHi;
            serializedObject.FindProperty("barBottom").colorValue = BarLo;
            serializedObject.FindProperty("slotTop").colorValue = BarLo;
            serializedObject.FindProperty("slotBottom").colorValue = BarHi;
            serializedObject.ApplyModifiedProperties();
        }

        DrawHealthTest();
    }

    void DrawHealthTest()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Health test", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode — Health has no value until Awake runs.", MessageType.Info);
            return;
        }

        Health h = ResolveHealth();
        if (h == null)
        {
            EditorGUILayout.HelpBox("No player Health found. Assign Player Health above, or check the player is tagged \"Player\".", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"{h.name}  {h.CurrentHealth:0.#} / {h.MaxHealth:0.#}" + (h.IsDead ? "   (dead)" : ""));

        using (new EditorGUI.DisabledScope(h.IsDead))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"Hit {EnemyHit:0.#}")) h.TakeDamage(TestHit(EnemyHit));
                if (GUILayout.Button("Hit 25")) h.TakeDamage(TestHit(25f));
                if (GUILayout.Button("Hit 50")) h.TakeDamage(TestHit(50f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"Rapid x3 ({EnemyHit:0.#} @ 0.15s)")) StartRapid(h);
                if (GUILayout.Button("Heal 25")) h.Heal(25f);
                if (GUILayout.Button("Full")) h.Heal(h.MaxHealth);
            }

            if (GUILayout.Button("Kill")) h.TakeDamage(TestHit(h.CurrentHealth));
        }

        if (h.IsDead)
            EditorGUILayout.HelpBox("Dead: PlayerRespawn brings the body back at the spawn point after its wait.", MessageType.Info);

        EditorGUILayout.HelpBox(
            "\"Hit 5\" and \"Hit 50\" should take the SAME time to drain (trailDuration, 0.55s) " +
            "— that's the mockup's transition. Rapid x3 lands hits inside the hold and the drain, " +
            "so the band should restart from where it is rather than jump.", MessageType.None);
    }

    Health ResolveHealth()
    {
        HudController hud = (HudController)target;
        if (hud.playerHealth != null) return hud.playerHealth;

        GameObject tagged = GameObject.FindWithTag("Player");
        return tagged != null ? tagged.GetComponentInChildren<Health>() : null;
    }

    static void StartRapid(Health h)
    {
        rapidTarget = h;
        rapidLeft = 3;
        rapidNext = 0.0;
        EditorApplication.update -= RapidTick;
        EditorApplication.update += RapidTick;
    }

    static void RapidTick()
    {
        if (rapidTarget == null || rapidLeft <= 0 || !Application.isPlaying)
        {
            EditorApplication.update -= RapidTick;
            rapidLeft = 0;
            return;
        }

        if (EditorApplication.timeSinceStartup < rapidNext) return;

        rapidTarget.TakeDamage(TestHit(EnemyHit));
        rapidNext = EditorApplication.timeSinceStartup + 0.15;
        if (--rapidLeft <= 0) EditorApplication.update -= RapidTick;
    }
}
