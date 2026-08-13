using UnityEngine;

namespace Train.Gameplay.Enemy.Presentation
{
    /// <summary>
    /// 通过渲染器材质属性驱动冻结 Shader 表现，不复制材质、不破坏原有贴图。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyFreezeVisual : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");

        [SerializeField] private Color _freezeColor = new(.12f, .55f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float _tintStrength = .78f;
        [SerializeField, Min(0f)] private float _emissionStrength = .3f;
        [Header("受击闪烁")]
        [SerializeField] private Color _hitColor = new(1f, .04f, .04f, 1f);
        [SerializeField, Range(0f, 1f)] private float _hitTintStrength = .9f;
        [SerializeField, Min(0f)] private float _hitEmissionStrength = .45f;
        [SerializeField, Min(.01f)] private float _hitFlashSeconds = .12f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock[] _blocks;
        private Color[] _baseColors;
        private bool _cached;
        private bool _frozen;
        private bool _hitActive;
        private float _hitStartedAt;
        private GameObject _freezeShell;
        private Material _freezeMaterial;
        private readonly System.Collections.Generic.List<Transform> _iceShards = new();

        private void Awake() => Cache();

        public bool IsHitActive => _hitActive;

        public void SetHit(bool hit)
        {
            Cache();
            _hitActive = hit;
            if (hit)
            {
                _hitStartedAt = Time.time;
            }

            ApplyRendererColors();
        }

        public void SetFrozen(bool frozen)
        {
            Cache();
            _frozen = frozen;
            if (_freezeShell != null)
            {
                _freezeShell.SetActive(frozen);
            }
            ApplyRendererColors();
        }

        private void Update()
        {
            if (_hitActive)
            {
                ApplyRendererColors();
            }

            if (!_frozen)
            {
                return;
            }

            var pulse = Time.time * 1.8f;
            _freezeMaterial?.SetFloat(PulseId, pulse);
            for (var i = 0; i < _iceShards.Count; i++)
            {
                var shard = _iceShards[i];
                var angle = pulse * (.8f + i * .07f) + i * Mathf.PI * 2f / _iceShards.Count;
                shard.localRotation = Quaternion.Euler(
                    Mathf.Sin(pulse + i) * 35f,
                    angle * Mathf.Rad2Deg,
                    Mathf.Cos(pulse * 1.3f + i) * 35f);
                shard.localScale = Vector3.one * (.7f + Mathf.Sin(pulse * 2f + i) * .12f);
            }
        }

        private void ApplyRendererColors()
        {
            var hitStrength = 0f;
            var hitEmissionStrength = 0f;
            if (_hitActive)
            {
                var flashProgress = Mathf.Clamp01((Time.time - _hitStartedAt) / _hitFlashSeconds);
                var flash = Mathf.Lerp(1f, .72f, flashProgress);
                hitStrength = Mathf.Clamp01(_hitTintStrength * flash);
                hitEmissionStrength = _hitEmissionStrength * flash;
            }

            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var block = _blocks[i];
                block.Clear();
                if (_hitActive)
                {
                    var hitTint = Color.Lerp(_baseColors[i], _hitColor, hitStrength);
                    block.SetColor(BaseColorId, hitTint);
                    block.SetColor(ColorId, hitTint);
                    block.SetColor(EmissionColorId, _hitColor * hitEmissionStrength);
                }
                else if (_frozen)
                {
                    var frozenTint = Color.Lerp(_baseColors[i], _freezeColor, _tintStrength);
                    block.SetColor(BaseColorId, frozenTint);
                    block.SetColor(ColorId, frozenTint);
                    block.SetColor(EmissionColorId, _freezeColor * _emissionStrength);
                }

                renderer.SetPropertyBlock(block);
            }
        }

        private void Cache()
        {
            if (_cached)
            {
                return;
            }

            _renderers = GetComponentsInChildren<Renderer>(true);
            _blocks = new MaterialPropertyBlock[_renderers.Length];
            _baseColors = new Color[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
            {
                _blocks[i] = new MaterialPropertyBlock();
                var material = _renderers[i] != null ? _renderers[i].sharedMaterial : null;
                _baseColors[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : material != null && material.HasProperty(ColorId)
                        ? material.GetColor(ColorId)
                        : Color.white;
            }

            _cached = true;
            CreateFreezeShell();
        }

        private void CreateFreezeShell()
        {
            if (_renderers.Length == 0 || Shader.Find("Train/Freeze Overlay") == null)
            {
                return;
            }

            var bounds = _renderers[0].bounds;
            for (var i = 1; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) bounds.Encapsulate(_renderers[i].bounds);
            }

            _freezeShell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _freezeShell.name = "FreezeShaderShell";
            _freezeShell.transform.SetParent(transform, false);
            _freezeShell.transform.localPosition = transform.InverseTransformPoint(bounds.center);
            var diameter = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 1.08f;
            _freezeShell.transform.localScale = Vector3.one * diameter;
            Destroy(_freezeShell.GetComponent<Collider>());
            _freezeMaterial = new Material(Shader.Find("Train/Freeze Overlay"));
            _freezeMaterial.color = new Color(.08f, .55f, 1f, .32f);
            _freezeShell.GetComponent<MeshRenderer>().sharedMaterial = _freezeMaterial;
            _freezeShell.SetActive(false);

            for (var i = 0; i < 8; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"FreezeShard_{i:00}";
                shard.transform.SetParent(_freezeShell.transform, false);
                var angle = i * Mathf.PI * 2f / 8f;
                shard.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * .52f,
                    Mathf.Sin(i * 1.7f) * .42f,
                    Mathf.Sin(angle) * .52f);
                shard.transform.localScale = new Vector3(.035f, .16f, .035f);
                Destroy(shard.GetComponent<Collider>());
                shard.GetComponent<MeshRenderer>().sharedMaterial = _freezeMaterial;
                _iceShards.Add(shard.transform);
            }
        }

        private void OnDestroy()
        {
            if (_freezeMaterial != null)
            {
                Destroy(_freezeMaterial);
            }
        }
    }
}
