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
        private const float LightHitStopDuration = .022f;
        private const float HeavyHitStopDuration = .05f;
        private const float LightHitStopScale = .2f;
        private const float HeavyHitStopScale = .09f;
        private const float HitStopRetriggerInterval = .03f;
        private const float LightShakeStrength = .025f;
        private const float HeavyShakeStrength = .055f;

        private const float FloatingTextLifetime = 0.72f;
        private const int MaxFloatingTexts = 20;

        private readonly List<FloatingDamageText> _floatingTexts = new();
        private IEventBus _events;
        private IDisposable _damageSubscription;
        private Material _particleMaterial;
        private float _hitStopUntil;
        private float _hitStopScale = 1f;
        private bool _hitStopIsHeavy;
        private float _lastHitStopTriggerAt = float.NegativeInfinity;
        private float _previousTimeScale = 1f;
        private float _shakeUntil;
        private float _shakeStrength;
        private Vector3 _lastShakeOffset;
        private Camera _shakeCamera;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _events = GameBootstrap.EnsureExists().Context.Events;
            _damageSubscription = _events.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
        }

        private void Update()
        {
            UpdateHitStop();
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
                    floating.StartedAt,
                    floating.ExpiresAt,
                    Time.unscaledTime);
                if (normalized >= 1f)
                {
                    Destroy(floating.Root);
                    _floatingTexts.RemoveAt(i);
                    continue;
                }

                floating.Root.transform.position =
                floating.StartPosition +
                    Vector3.up * Mathf.Lerp(0f, 1.35f, normalized);
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
                floating.Text.fontSize = Mathf.Lerp(3.6f, 4.2f, normalized);
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
                color,
                message.Health);
            SpawnImpactBurst(message.Damage.HitPoint, color);
            TriggerImpactFeedback(
                message.Damage.HitPoint,
                message.Result.AppliedDamage,
                message.Damage.DamageType);
        }

        /// <summary>命中时短暂停顿并轻微震动镜头，强化动作游戏的命中确认感。</summary>
        private void TriggerImpactFeedback(
            Vector3 hitPoint,
            float damage,
            DamageType damageType)
        {
            var heavy = damage >= 40f || damageType == DamageType.Earth;
            var now = Time.unscaledTime;
            var hitStopActive = _hitStopUntil > now;
            var canRetrigger = now - _lastHitStopTriggerAt >= HitStopRetriggerInterval;
            var canUpgrade = heavy && hitStopActive && !_hitStopIsHeavy;
            if (!canRetrigger && !canUpgrade)
            {
                return;
            }

            if (!hitStopActive)
            {
                _previousTimeScale = Mathf.Max(.01f, Time.timeScale);
                _hitStopIsHeavy = false;
            }

            _hitStopIsHeavy |= heavy;
            _hitStopScale = _hitStopIsHeavy ? HeavyHitStopScale : LightHitStopScale;
            var hitStopSeconds = heavy ? HeavyHitStopDuration : LightHitStopDuration;
            _hitStopUntil = Mathf.Max(_hitStopUntil, now + hitStopSeconds);
            Time.timeScale = _hitStopScale;
            _lastHitStopTriggerAt = now;
            _shakeUntil = Mathf.Max(_shakeUntil, now + (heavy ? .12f : .08f));
            _shakeStrength = Mathf.Max(
                _shakeStrength,
                heavy ? HeavyShakeStrength : LightShakeStrength);
        }

        private void UpdateHitStop()
        {
            if (_hitStopUntil > 0f && Time.unscaledTime >= _hitStopUntil)
            {
                Time.timeScale = _previousTimeScale;
                _hitStopUntil = 0f;
                _hitStopIsHeavy = false;
            }
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (_shakeCamera != null && _shakeCamera != camera)
            {
                _shakeCamera.transform.position -= _lastShakeOffset;
                _lastShakeOffset = Vector3.zero;
            }

            if (_shakeCamera == camera && _lastShakeOffset.sqrMagnitude > 0f)
            {
                camera.transform.position -= _lastShakeOffset;
                _lastShakeOffset = Vector3.zero;
            }

            if (camera == null || Time.unscaledTime >= _shakeUntil)
            {
                _shakeStrength = 0f;
                _shakeCamera = camera;
                return;
            }

            _shakeCamera = camera;
            var fade = Mathf.Clamp01((_shakeUntil - Time.unscaledTime) / .12f);
            var planarOffset = UnityEngine.Random.insideUnitCircle * (_shakeStrength * fade);
            _lastShakeOffset = camera.transform.right * planarOffset.x +
                camera.transform.up * planarOffset.y;
            camera.transform.position += _lastShakeOffset;
        }

        private void SpawnFloatingText(
            Vector3 position,
            int amount,
            Color color,
            Health ownerHealth)
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
            text.fontSize = 3.6f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(5, 12, 22, 255);
            text.color = color;
            var anchor = ownerHealth != null ? ownerHealth.transform.position : position;
            var collider = ownerHealth != null
                ? ownerHealth.GetComponentInChildren<Collider>()
                : null;
            if (collider != null)
            {
                anchor = collider.bounds.center;
                anchor.y = collider.bounds.max.y;
            }

            var side = ownerHealth != null && (ownerHealth.GetInstanceID() & 1) == 0
                ? -1f
                : 1f;
            root.transform.position = anchor +
                (ownerHealth != null ? ownerHealth.transform.right * (side * .6f) : Vector3.zero) +
                Vector3.up * .75f;
            _floatingTexts.Add(new FloatingDamageText(
                root,
                text,
                root.transform.position,
                color,
                Time.unscaledTime,
                Time.unscaledTime + FloatingTextLifetime,
                ownerHealth));
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
            if (_hitStopUntil > 0f)
            {
                Time.timeScale = _previousTimeScale;
            }
            if (_shakeCamera != null && _lastShakeOffset.sqrMagnitude > 0f)
            {
                _shakeCamera.transform.position -= _lastShakeOffset;
            }
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
                float expiresAt,
                Health ownerHealth)
            {
                Root = root;
                Text = text;
                StartPosition = startPosition;
                BaseColor = baseColor;
                StartedAt = startedAt;
                ExpiresAt = expiresAt;
                OwnerHealth = ownerHealth;
            }

            public GameObject Root { get; }
            public TextMeshPro Text { get; }
            public Vector3 StartPosition { get; }
            public Color BaseColor { get; }
            public float StartedAt { get; }
            public float ExpiresAt { get; }
            public Health OwnerHealth { get; }
        }
    }
}
