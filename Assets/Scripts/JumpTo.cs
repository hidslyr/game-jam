using System;
// using JellyClash.Scripts.Components.Gameplay;
using MoreMountains.Tools;
// using DG.Tweening;
using UnityEngine;

namespace JellyClash.Scripts.Components
{
    public class JumpTo : MonoBehaviour
    {

        public bool AdjustRotation = true;
        // Jump state
        private bool _isJumping;
        private Transform _jumpTarget;
        private Vector3 _jumpStartPosition;

        private Quaternion _jumpStartRotation;
        private float _jumpPower;
        private float _jumpDuration;
        private float _jumpElapsedTime;
        private Action _onComplete;

        public bool IsJumping => _isJumping;

        internal Transform JumpTarget => _jumpTarget;
        internal Vector3 JumpStartPosition => _jumpStartPosition;
        internal Quaternion JumpStartRotation => _jumpStartRotation;
        internal float JumpPower => _jumpPower;
        internal float JumpDuration => _jumpDuration;
        internal float JumpElapsedTime => _jumpElapsedTime;

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;
        }

        public void Jump(Transform target, float jumpPower, float duration)
        {
            Jump(target, jumpPower, duration, null);
        }

        public void Jump(Transform target, float jumpPower, float duration, Action onComplete)
        {
            _isJumping = true;
            _jumpTarget = target;
            _jumpStartPosition = transform.position;
            if (AdjustRotation)
            {
                _jumpStartRotation = transform.rotation;
            }
            _jumpPower = jumpPower;
            _jumpDuration = Mathf.Max(duration, 0.0001f);
            _jumpElapsedTime = 0f;
            _onComplete = onComplete;
        }

        public void Stop()
        {
            _isJumping = false;
            _jumpTarget = null;
            _onComplete = null;
        }

        private void Update()
        {

            if (!_isJumping || _jumpTarget == null)
                return;

            TickJump(Time.deltaTime * 1.0f);
        }

        private void TickJump(float deltaTime)
        {
            _jumpElapsedTime += deltaTime;
            float t = Mathf.Clamp01(_jumpElapsedTime / _jumpDuration);
            t = MMTween.EaseInSinusoidal(t);

            // Get current target position (dynamic)
            Vector3 targetPosition = _jumpTarget.position;

            // Horizontal movement
            Vector3 currentPos = Vector3.Lerp(_jumpStartPosition, targetPosition, MMTween.EaseOutSinusoidal(t));

            // Vertical arc using parabola: 4 * h * t * (1 - t) gives max height h at t=0.5
            float arc = 4f * _jumpPower * t * (1f - t);
            currentPos.y += arc;

            transform.position = currentPos;
            if (AdjustRotation)
            {
                Quaternion currentRotation = Quaternion.Lerp(_jumpStartRotation, _jumpTarget.rotation, t);
                transform.rotation = currentRotation;
            }

            // Check if jump is complete
            if (t >= 1f)
            {
                transform.position = _jumpTarget.position;
                _isJumping = false;
                var onComplete = _onComplete;
                _onComplete = null;
                _jumpTarget = null;
                onComplete?.Invoke();
            }
        }

        internal void SetElapsedTime(float elapsed)
        {
            _jumpElapsedTime = elapsed;
        }

        internal void CompleteFromJob()
        {
            if (_jumpTarget != null)
            {
                transform.position = _jumpTarget.position;
                if (AdjustRotation)
                {
                    transform.rotation = _jumpTarget.rotation;
                }
            }

            _isJumping = false;
            var onComplete = _onComplete;
            _onComplete = null;
            _jumpTarget = null;
            onComplete?.Invoke();
        }

        public void StopJump()
        {
            _isJumping = false;
            _jumpTarget = null;
            _onComplete = null;
        }
    }
}