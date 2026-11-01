using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    [Serializable]
    public sealed class Ruin02StaffRestorationEntry
    {
        [Range(1, 4)] public int absorbMagicParameter;
        public string label;
        public Transform staff;
        public Transform focusMarker;
        public Transform cameraEndView;
        public GameObject chaoticOrb;
        public GameObject projectilePrefab;
        [Min(.1f)] public float flightSeconds = 1.7f;
        [Min(0)] public float flightArcHeight = .45f;
    }

    // Legacy component name retained so D1 scene serialization and tools remain valid.
    // Phase D2 turns it into one data-driven controller for all four return restorations.
    public sealed class Ruin02AirRestoration : MonoBehaviour
    {
        public Ruin02PracticeController practice;
        public Ruin02CinematicCamera cameraRig;

        // D1 compatibility fields. They also seed the Air entry if an older scene is opened.
        public Transform airStaff, projectileOrigin, airStaffFocus;
        public GameObject purpleOrb, violetProjectilePrefab;
        [Min(.1f)] public float enterSeconds = .55f, flightSeconds = 1.7f, focusSeconds = .6f, returnSeconds = .55f;
        [Min(0)] public float flightArcHeight = .45f;

        [Header("Phase D2 restoration mapping")]
        public Ruin02StaffRestorationEntry[] restorations = Array.Empty<Ruin02StaffRestorationEntry>();

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        struct MaterialSlot
        {
            public Renderer renderer;
            public int index;
            public MaterialPropertyBlock original;
            public Material material;
        }

        sealed class RuntimeEntry
        {
            public Ruin02StaffRestorationEntry definition;
            public readonly List<MaterialSlot> slots = new List<MaterialSlot>();
            public GameObject projectileHost;
            public bool restored;
        }

        readonly Dictionary<int, RuntimeEntry> entries = new Dictionary<int, RuntimeEntry>();
        Coroutine sequence;
        RuntimeEntry activeEntry;

        public bool IsRestored => IsStaffRestored(1);
        public bool IsPlaying => sequence != null;
        public int RestoredCount => entries.Values.Count(e => e.restored);
        public bool IsStaffRestored(int parameter) => entries.TryGetValue(parameter, out var entry) && entry.restored;

        void Start()
        {
            EnsureAirCompatibilityEntry();
            if (practice == null || cameraRig == null || projectileOrigin == null || cameraRig.gameplayCamera == null ||
                cameraRig.flightStartView == null || cameraRig.flightEndView == null || restorations.Length != 4)
            { Debug.LogError("Ruin 2 staff restoration references are incomplete.", this); enabled = false; return; }

            foreach (var definition in restorations)
            {
                if (definition == null || definition.absorbMagicParameter < 1 || definition.absorbMagicParameter > 4 ||
                    definition.staff == null || definition.focusMarker == null || definition.chaoticOrb == null ||
                    definition.projectilePrefab == null || entries.ContainsKey(definition.absorbMagicParameter))
                { Debug.LogError("Ruin 2 staff restoration mapping is invalid.", this); enabled = false; return; }
                var entry = new RuntimeEntry { definition = definition };
                CacheMaterials(entry);
                SetInactiveAppearance(entry);
                definition.chaoticOrb.SetActive(true);
                entries.Add(definition.absorbMagicParameter, entry);
            }
            practice.AbsorbMagic += OnAbsorbMagic;
        }

        void EnsureAirCompatibilityEntry()
        {
            if (restorations != null && restorations.Length > 0) return;
            restorations = new[] { new Ruin02StaffRestorationEntry {
                absorbMagicParameter = 1, label = "Air", staff = airStaff, focusMarker = airStaffFocus,
                chaoticOrb = purpleOrb, projectilePrefab = violetProjectilePrefab,
                flightSeconds = flightSeconds, flightArcHeight = flightArcHeight
            }};
        }

        void CacheMaterials(RuntimeEntry entry)
        {
            foreach (var renderer in entry.definition.staff.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original, i);
                    entry.slots.Add(new MaterialSlot { renderer = renderer, index = i, original = original, material = materials[i] });
                }
            }
        }

        void SetInactiveAppearance(RuntimeEntry entry)
        {
            foreach (var slot in entry.slots)
            {
                if (slot.renderer == null || slot.material == null) continue;
                var block = new MaterialPropertyBlock(); slot.renderer.GetPropertyBlock(block, slot.index);
                if (slot.material.HasProperty(BaseColor))
                {
                    float alpha = slot.material.GetColor(BaseColor).a;
                    block.SetColor(BaseColor, new Color(.38f, .40f, .43f, alpha));
                }
                if (slot.material.HasProperty(EmissionColor)) block.SetColor(EmissionColor, Color.black);
                slot.renderer.SetPropertyBlock(block, slot.index);
            }
        }

        static void RestoreAppearance(RuntimeEntry entry)
        {
            foreach (var slot in entry.slots)
                if (slot.renderer != null) slot.renderer.SetPropertyBlock(slot.original, slot.index);
        }

        void OnAbsorbMagic(int parameter)
        {
            if (sequence != null || !entries.TryGetValue(parameter, out var entry) || entry.restored) return;
            activeEntry = entry; sequence = StartCoroutine(PlaySequence(entry));
        }

        IEnumerator PlaySequence(RuntimeEntry entry)
        {
            var definition = entry.definition;
            practice.BeginCinematic(); cameraRig.Capture();
            entry.projectileHost = new GameObject(definition.label + "ProjectileRuntime");
            entry.projectileHost.transform.SetParent(transform, false); entry.projectileHost.SetActive(false);
            var projectile = Instantiate(definition.projectilePrefab, entry.projectileHost.transform);
            Vector3 direction = definition.focusMarker.position - projectileOrigin.position;
            projectile.transform.SetPositionAndRotation(projectileOrigin.position,
                direction.sqrMagnitude > .001f ? Quaternion.LookRotation(direction) : Quaternion.identity);

            // Vendor prefabs are dynamic effects. This scene owns their transform path, so movement,
            // Rigidbody simulation and all colliders are disabled only on the runtime instance.
            foreach (var movement in projectile.GetComponentsInChildren<ProjectileMoveScript>(true)) movement.enabled = false;
            foreach (var body in projectile.GetComponentsInChildren<Rigidbody>(true))
            { body.isKinematic = true; body.detectCollisions = false; body.useGravity = false; }
            foreach (var collider in projectile.GetComponentsInChildren<Collider>(true)) collider.enabled = false;

            yield return cameraRig.Enter(projectileOrigin.position, enterSeconds);
            entry.projectileHost.SetActive(true);
            foreach (var particles in projectile.GetComponentsInChildren<ParticleSystem>(true))
                if (!particles.isPlaying) particles.Play();
            for (float elapsed = 0; elapsed < definition.flightSeconds; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / definition.flightSeconds);
                float eased = t * t * (3f - 2f * t);
                Vector3 point = Vector3.Lerp(projectileOrigin.position, definition.focusMarker.position, eased) +
                    Vector3.up * (Mathf.Sin(t * Mathf.PI) * definition.flightArcHeight);
                projectile.transform.position = point;
                cameraRig.Track(t, point, definition.cameraEndView);
                yield return null;
            }
            projectile.transform.position = definition.focusMarker.position;
            cameraRig.Track(1f, definition.focusMarker.position, definition.cameraEndView);
            RestoreAppearance(entry); definition.chaoticOrb.SetActive(false); entry.restored = true;
            foreach (var particles in projectile.GetComponentsInChildren<ParticleSystem>(true))
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            yield return cameraRig.Focus(definition.focusMarker.position, focusSeconds);
            if (entry.projectileHost != null) Destroy(entry.projectileHost); entry.projectileHost = null;
            yield return cameraRig.Return(returnSeconds);
            sequence = null; activeEntry = null; practice.EndCinematic();
        }

        // Development-only menu can finish the active cinematic without leaving a modal locked.
        public void SkipCurrentSequence()
        {
            if (sequence == null || activeEntry == null) return;
            StopCoroutine(sequence); sequence = null;
            if (activeEntry.projectileHost != null) Destroy(activeEntry.projectileHost);
            activeEntry.projectileHost = null; RestoreAppearance(activeEntry);
            activeEntry.definition.chaoticOrb.SetActive(false); activeEntry.restored = true;
            activeEntry = null; cameraRig.RestoreImmediately();
            if (practice != null) practice.EndCinematic();
        }

        void OnDisable()
        {
            if (practice != null) practice.AbsorbMagic -= OnAbsorbMagic;
            if (sequence != null) SkipCurrentSequence();
        }
    }
}
