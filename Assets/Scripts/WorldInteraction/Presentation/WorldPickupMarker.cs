using UnityEngine;

namespace Train.WorldInteraction.Presentation
{
    /// <summary>
    /// 让地图拾取光标始终面向相机并做轻微上下浮动。
    /// 这是纯表现组件，不负责距离判断或写入背包。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldPickupMarker : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _bobAmplitude = 0.045f;
        [SerializeField, Min(0f)] private float _bobFrequency = 1.6f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 24f;

        private Vector3 _baseLocalPosition;
        private Camera _camera;
        private float _spinDegrees;

        /// <summary>
        /// 记录设计位置。
        /// </summary>
        private void Awake()
        {
            _baseLocalPosition = transform.localPosition;
        }

        /// <summary>
        /// 在相机更新后调整朝向、浮动和旋转。
        /// </summary>
        private void LateUpdate()
        {
            _camera ??= Camera.main;
            var offset = Mathf.Sin(
                Time.unscaledTime * _bobFrequency * Mathf.PI * 2f) *
                _bobAmplitude;
            transform.localPosition =
                _baseLocalPosition + Vector3.up * offset;

            if (_camera != null)
            {
                _spinDegrees = Mathf.Repeat(
                    _spinDegrees +
                    _rotationSpeed * Time.unscaledDeltaTime,
                    360f);
                transform.rotation =
                    _camera.transform.rotation *
                    Quaternion.AngleAxis(
                        _spinDegrees,
                        Vector3.forward);
            }
        }
    }
}
