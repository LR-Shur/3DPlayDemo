using System;
using System.Collections.Generic;
using TMPro;
using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.GameFlow.Runtime;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Application.Events;
using UnityEngine;

namespace Train.Composition
{
    /// <summary>
    /// 统一处理战斗命中的轻量表现：伤害数字和命中粒子。
    /// 表现层订阅事件，不把 UI 或特效逻辑塞回伤害结算代码。
    /// </summary>
    [DefaultExecutionOrder(-8400)]
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackRuntimeController : MonoBehaviour
    {
        private const float FloatingTextLifetime = 0.72f;
        private const int MaxFloatingTexts = 20;

        private readonly List<FloatingDamageText> _floatingTexts = new();
        private IEventBus _events;
        private IDisposable _damageSubscription;
        private Material _particleMaterial;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _events = GameBootstrap.EnsureExists().Context.Events;
            _damageSubscription = _events.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
        }

        private void Update()
        {
            var camera = Camera.main;
            for (var i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var floating = _floatingTexts[i];
                if (floating.Root == null || floating.Text == null)
                {
                    _floatingTexts.RemoveAt(i);
                    continue;
                }

                var normalized = Mathf.InverseLerp(
                    floating.ExpiresAt,
                    floating.StartedAt,
                    Time.unscaledTime);
                if (normalized >= 1f)
                {
                    Destroy(floating.Root);
                    _floatingTexts.RemoveAt(i);
                    continue;
                }

                floating.Root.transform.position =
                    floating.StartPosition +
                    Vector3.up * Mathf.Lerp(0f, 1.15f, normalized);
                if (camera != null)
                {
                    floating.Root.transform.rotation =
                        Quaternion.LookRotation(
                            floating.Root.transform.position - camera.transform.position,
                            camera.transform.up);
                }

                var color = floating.BaseColor;
                color.a = Mathf.Clamp01(1f - normalized * 1.15f);
                floating.Text.color = color;
                floating.Text.fontSize = Mathf.Lerp(2.35f, 2.8f, normalized);
            }
        }

        private void OnEntityDamaged(EntityDamagedEvent message)
        {
            if (message.Health == null ||
                message.Result.AppliedDamage <= 0f ||
                message.Health.GetComponentInParent<EnemyIdentity>() == null)
            {
                return;
            }

            var color = message.Damage.DamageType == DamageType.Electric
                ? new Color32(125, 238, 255, 255)
                : new Color32(255, 235, 190, 255);
            SpawnFloatingText(
                message.Damage.HitPoint,
                Mathf.RoundToInt(message.Result.AppliedDamage),
                color);
            SpawnImpactBurst(message.Damage.HitPoint, color);
        }

        private void SpawnFloatingText(Vector3 position, int amount, Color color)
        {
            if (_floatingTexts.Count >= MaxFloatingTexts)
            {
                var oldest = _floatingTexts[0];
                if (oldest.Root != null)
                {
                    Destroy(oldest.Root);
                }

                _floatingTexts.RemoveAt(0);
            }

            var root = new GameObject("[CombatDamageNumber]");
            var text = root.AddComponent<TextMeshPro>();
            text.text = amount.ToString();
            text.fontSize = 2.35f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(5, 12, 22, 255);
            text.color = color;
            root.transform.position = position + Vector3.up * 0.18f;
            _floatingTexts.Add(new FloatingDamageText(
                root,
                text,
                root.transform.position,
                color,
                Time.unscaledTime,
                Time.unscaledTime + FloatingTextLifetime));
        }

        private void SpawnImpactBurst(Vector3 position, Color color)
        {
            var root = new GameObject("[CombatHitBurst]");
            root.transform.position = position;
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = 0.28f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            main.startColor = color;
            main.maxParticles = 12;

            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.04f;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetParticleMaterial();
            particles.Play();
            Destroy(root, 0.8f);
        }

        private Material GetParticleMaterial()
        {
            if (_particleMaterial != null)
            {
                return _particleMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                         Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                _particleMaterial = new Material(shader)
                {
                    name = "CombatHitBurstMaterial"
                };
            }

            return _particleMaterial;
        }

        private void OnDestroy()
        {
            _damageSubscription?.Dispose();
            if (_particleMaterial != null)
            {
                Destroy(_particleMaterial);
            }
        }

        private sealed class FloatingDamageText
        {
            public FloatingDamageText(
                GameObject root,
                TextMeshPro text,
                Vector3 startPosition,
                Color baseColor,
                float startedAt,
                float expiresAt)
            {
                Root = root;
                Text = text;
                StartPosition = startPosition;
                BaseColor = baseColor;
                StartedAt = startedAt;
                ExpiresAt = expiresAt;
            }

            public GameObject Root { get; }
            public TextMeshPro Text { get; }
            public Vector3 StartPosition { get; }
            public Color BaseColor { get; }
            public float StartedAt { get; }
            public float ExpiresAt { get; }
        }
    }
}
