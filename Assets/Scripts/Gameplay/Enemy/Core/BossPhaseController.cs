using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Combat;
using UnityEngine;

namespace Train.Gameplay.Enemy.Core
{
    /// <summary>
    /// Boss 三阶段控制器：根据生命比例改变攻击节奏和过载材质。
    /// 只负责表现与数值调谐，不接管 EnemyController 的状态机。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class BossPhaseController : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField, Range(0.1f, 0.9f)] private float _phaseTwoThreshold = 0.66f;
        [SerializeField, Range(0.05f, 0.8f)] private float _phaseThreeThreshold = 0.33f;
        [SerializeField] private Color _phaseTwoColor = new(0.95f, 0.18f, 0.72f, 1f);
        [SerializeField] private Color _phaseThreeColor = new(1f, 0.34f, 0.08f, 1f);

        private Health _health;
        private EnemyMeleeCombat _combat;
        private Renderer[] _renderers;
        private Vector3 _baseScale;
        private int _phase;

        public int Phase => _phase;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _combat = GetComponent<EnemyMeleeCombat>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _baseScale = transform.localScale;
            _phase = 1;
            ApplyPhase(1);
        }

        private void OnEnable()
        {
            _health ??= GetComponent<Health>();
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
            }
        }

        private void OnDamaged(DamageInfo damage, DamageResult result)
        {
            if (result.Killed || _health == null)
            {
                return;
            }

            var ratio = _health.CurrentHealth / Mathf.Max(1f, _health.MaxHealth);
            var nextPhase = ratio <= _phaseThreeThreshold
                ? 3
                : ratio <= _phaseTwoThreshold
                    ? 2
                    : 1;
            if (nextPhase > _phase)
            {
                ApplyPhase(nextPhase);
            }
        }

        private void ApplyPhase(int phase)
        {
            _phase = phase;
            var damageMultiplier = phase switch
            {
                3 => 1.55f,
                2 => 1.25f,
                _ => 1f
            };
            var cooldownMultiplier = phase switch
            {
                3 => 0.65f,
                2 => 0.82f,
                _ => 1f
            };
            _combat?.ApplyPhaseModifiers(damageMultiplier, cooldownMultiplier);

            transform.localScale = _baseScale * (phase == 3 ? 1.08f : phase == 2 ? 1.04f : 1f);
            var color = phase == 3
                ? _phaseThreeColor
                : phase == 2
                    ? _phaseTwoColor
                    : Color.black;
            var intensity = phase == 3 ? 4.5f : phase == 2 ? 2.8f : 0f;
            for (var i = 0; i < _renderers.Length; i++)
            {
                var materials = _renderers[i].materials;
                for (var j = 0; j < materials.Length; j++)
                {
                    var material = materials[j];
                    if (material == null || !material.HasProperty(EmissionColorId))
                    {
                        continue;
                    }

                    if (phase > 1)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor(EmissionColorId, color * intensity);
                    }
                }
            }
        }
    }
}
