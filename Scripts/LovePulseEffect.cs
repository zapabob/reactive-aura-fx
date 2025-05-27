// LovePulse - 愛情パーティクルエフェクト
// Reactive Aura FX サブシステム
// VRChat + Modular Avatar完全対応

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if VRC_SDK_VRCSDK3 && !UDON
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

#if MA_VRCSDK3_AVATARS
using nadena.dev.modular_avatar.core;
#endif

namespace ReactiveAuraFX.Core
{
    /// <summary>
    /// 特定ユーザーとの距離と注視でハート型パーティクルとSEを発生させるエフェクト
    /// VRChat Avatar 3.0 + Modular Avatar完全対応、AutoFIX安全設計
    /// </summary>
    [AddComponentMenu("ReactiveAuraFX/Effects/Love Pulse Effect")]
    [System.Serializable]
    public class LovePulseEffect : MonoBehaviour
    {
        [Header("💕 LovePulse設定")]
        [Tooltip("愛情検出距離")]
        [Range(1f, 10f)]
        public float loveDetectionDistance = 3f;
        
        [Tooltip("視線角度閾値")]
        [Range(0f, 90f)]
        public float gazeAngleThreshold = 30f;
        
        [Tooltip("愛情蓄積時間")]
        [Range(1f, 10f)]
        public float loveAccumulationTime = 3f;
        
        [Tooltip("パーティクル数")]
        [Range(5, 50)]
        public int loveParticleCount = 15;
        
        [Tooltip("パーティクルの色")]
        public Color loveColor = new Color(1f, 0.4f, 0.7f, 0.8f);
        
        [Tooltip("エフェクト強度")]
        [Range(0.1f, 3f)]
        public float effectIntensity = 1f;
        
        [Tooltip("対象プレイヤータグ")]
        public string[] targetPlayerTags = { "Player", "RemotePlayer" };

        // === エフェクトコンポーネント ===
        [Header("エフェクトコンポーネント")]
        [Tooltip("ハートパーティクルシステム")]
        public ParticleSystem heartParticles;
        
        [Tooltip("愛情オーラライト")]
        public Light loveLight;
        
        [Tooltip("愛情オーディオソース")]
        public AudioSource loveAudioSource;
        
        [Tooltip("ハートビート音")]
        public AudioClip heartbeatClip;
        
        [Tooltip("愛情発動音")]
        public AudioClip loveActivationClip;

#if MA_VRCSDK3_AVATARS
        [Space(10)]
        [Header("🔗 Modular Avatar完全連携")]
        [Tooltip("Animatorパラメータで手動発動")]
        public bool useAnimatorManualTrigger = false;
        
        [Tooltip("手動発動パラメータ名")]
        public string manualTriggerParameterName = "LovePulseTrigger";
        
        [Tooltip("エフェクト有効化パラメータ名")]
        public string enableParameterName = "ReactiveAuraFX/LovePulse";
#endif

        // === 内部変数 ===
        private bool _isLovePulseActive = false;
        private bool _effectEnabled = true;
        private float _loveAccumulation = 0f;
        private List<Transform> _nearbyPlayers = new List<Transform>();
        private Transform _currentLoveTarget = null;
        private Camera _playerCamera;
        
        // パーティクル制御
        private ParticleSystem.MainModule _particleMain;
        private ParticleSystem.EmissionModule _particleEmission;
        private ParticleSystem.ShapeModule _particleShape;
        private ParticleSystem.VelocityOverLifetimeModule _particleVelocity;
        
        // ライト制御
        private float _baseLightIntensity = 0.3f;
        private Color _baseLightColor;
        
        // エフェクトタイマー
        private float _pulseTimer = 0f;
        private float _nextPulseTime = 0f;

#if MA_VRCSDK3_AVATARS
        // Modular Avatar関連
        private Animator _avatarAnimator;
        private VRCAvatarDescriptor _avatarDescriptor;
        private bool _lastManualTriggerValue = false;
        private bool _lastEffectEnabledValue = true;
#endif

        void Awake()
        {
            // AutoFIX対策：Awakeで基本設定
            EnsureAutoFixSafety();
        }

        void Start()
        {
            InitializeComponents();
        }

        private void EnsureAutoFixSafety()
        {
            // オブジェクト名をAutoFIXが無視する形式に
            if (gameObject.name.Contains("LovePulse") == false)
            {
                gameObject.name = "LovePulse_Effect";
            }
            
            // タグ設定
            if (gameObject.tag == "Untagged")
            {
                try
                {
                    gameObject.tag = "EditorOnly";
                }
                catch
                {
                    // タグが存在しない場合は無視
                }
            }
        }

        private void InitializeComponents()
        {
            // カメラ検出
            _playerCamera = Camera.main;
            if (_playerCamera == null)
            {
                _playerCamera = FindObjectOfType<Camera>();
            }
            
            // パーティクルシステム初期化
            if (heartParticles == null)
            {
                CreateHeartParticleSystem();
            }
            else
            {
                InitializeParticleSystem();
            }
            
            // ライト初期化
            if (loveLight != null)
            {
                _baseLightIntensity = loveLight.intensity;
                _baseLightColor = loveLight.color;
                loveLight.color = loveColor;
                loveLight.enabled = false;
            }
            
            // オーディオ初期化
            if (loveAudioSource != null)
            {
                loveAudioSource.loop = false;
            }

#if MA_VRCSDK3_AVATARS
            // Modular Avatar Animator検出
            if (useAnimatorManualTrigger && _avatarAnimator == null)
            {
                _avatarDescriptor = GetComponentInParent<VRCAvatarDescriptor>();
                if (_avatarDescriptor == null)
                {
                    _avatarDescriptor = FindObjectOfType<VRCAvatarDescriptor>();
                }
                
                if (_avatarDescriptor != null)
                {
                    _avatarAnimator = _avatarDescriptor.GetComponent<Animator>();
                }
            }
#endif
            
            Debug.Log("[ReactiveAuraFX] LovePulseEffect初期化完了");
        }

        private void CreateHeartParticleSystem()
        {
            GameObject particleObj = new GameObject("LoveHeartParticles");
            particleObj.transform.SetParent(transform);
            heartParticles = particleObj.AddComponent<ParticleSystem>();
            
            InitializeParticleSystem();
        }

        private void InitializeParticleSystem()
        {
            if (heartParticles == null) return;
            
            _particleMain = heartParticles.main;
            _particleEmission = heartParticles.emission;
            _particleShape = heartParticles.shape;
            _particleVelocity = heartParticles.velocityOverLifetime;
            
            // メイン設定
            _particleMain.startLifetime = 3f;
            _particleMain.startSpeed = 2f;
            _particleMain.startSize = 0.2f;
            _particleMain.startColor = loveColor;
            _particleMain.maxParticles = loveParticleCount * 2;
            _particleMain.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // エミッション設定
            _particleEmission.enabled = false;
            _particleEmission.rateOverTime = 0;
            
            // 形状設定（ハート型に近づける）
            _particleShape.enabled = true;
            _particleShape.shapeType = ParticleSystemShapeType.Circle;
            _particleShape.radius = 0.5f;
            
            // 速度設定
            _particleVelocity.enabled = true;
            _particleVelocity.space = ParticleSystemSimulationSpace.World;
            
            // カラーオーバーライフタイム
            var colorOverLifetime = heartParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(loveColor, 0.0f), 
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(loveColor, 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.0f, 0.0f), 
                    new GradientAlphaKey(1.0f, 0.3f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = gradient;
            
            // サイズオーバーライフタイム
            var sizeOverLifetime = heartParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.3f, 1.2f);
            sizeCurve.AddKey(1f, 0.2f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        }

        void Update()
        {
#if MA_VRCSDK3_AVATARS
            // Modular Avatar パラメータ監視
            if (useAnimatorManualTrigger && _avatarAnimator != null)
            {
                UpdateAnimatorManualTrigger();
            }
            
            if (_avatarAnimator != null)
            {
                UpdateEffectEnabledFromAnimator();
            }
#endif
            
            if (_playerCamera == null || !_effectEnabled) return;
            
            UpdateNearbyPlayersDetection();
            UpdateLoveAccumulation();
            UpdateLovePulseEffect();
        }

#if MA_VRCSDK3_AVATARS
        private void UpdateAnimatorManualTrigger()
        {
            try
            {
                if (_avatarAnimator.parameters == null) return;
                
                // パラメータの存在チェック
                bool paramExists = false;
                foreach (var param in _avatarAnimator.parameters)
                {
                    if (param.name == manualTriggerParameterName)
                    {
                        paramExists = true;
                        break;
                    }
                }
                
                if (!paramExists) return;
                
                bool manualTriggerValue = _avatarAnimator.GetBool(manualTriggerParameterName);
                
                if (manualTriggerValue && !_lastManualTriggerValue)
                {
                    ManualTriggerLovePulse();
                }
                
                _lastManualTriggerValue = manualTriggerValue;
            }
            catch (System.Exception)
            {
                // パラメータが存在しない場合は無視
            }
        }

        private void UpdateEffectEnabledFromAnimator()
        {
            try
            {
                if (_avatarAnimator.parameters == null) return;
                
                // エフェクト有効化パラメータの存在チェック
                bool paramExists = false;
                foreach (var param in _avatarAnimator.parameters)
                {
                    if (param.name == enableParameterName)
                    {
                        paramExists = true;
                        break;
                    }
                }
                
                if (!paramExists) return;
                
                bool effectEnabled = _avatarAnimator.GetBool(enableParameterName);
                
                if (effectEnabled != _lastEffectEnabledValue)
                {
                    _lastEffectEnabledValue = effectEnabled;
                    SetEffectEnabled(effectEnabled);
                }
            }
            catch (System.Exception)
            {
                // パラメータが存在しない場合は無視
            }
        }
#endif

        private void UpdateNearbyPlayersDetection()
        {
            _nearbyPlayers.Clear();
            
            // 指定タグのプレイヤーを検索
            foreach (string tag in targetPlayerTags)
            {
                GameObject[] players = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject player in players)
                {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance <= loveDetectionDistance && player.transform != transform)
                    {
                        _nearbyPlayers.Add(player.transform);
                    }
                }
            }
        }

        private void UpdateLoveAccumulation()
        {
            _currentLoveTarget = GetLookingAtPlayer();
            
            if (_currentLoveTarget != null)
            {
                _loveAccumulation += Time.deltaTime;
                
                if (_loveAccumulation >= loveAccumulationTime && !_isLovePulseActive)
                {
                    TriggerLovePulse();
                }
            }
            else
            {
                _loveAccumulation = Mathf.Max(0f, _loveAccumulation - Time.deltaTime * 2f);
                
                if (_loveAccumulation <= 0f && _isLovePulseActive)
                {
                    StopLovePulse();
                }
            }
        }

        private Transform GetLookingAtPlayer()
        {
            if (_playerCamera == null || _nearbyPlayers.Count == 0) return null;
            
            Vector3 cameraForward = _playerCamera.transform.forward;
            
            foreach (Transform player in _nearbyPlayers)
            {
                Vector3 directionToPlayer = (player.position - _playerCamera.transform.position).normalized;
                float angle = Vector3.Angle(cameraForward, directionToPlayer);
                
                if (angle <= gazeAngleThreshold)
                {
                    return player;
                }
            }
            
            return null;
        }

        private void TriggerLovePulse()
        {
            if (_isLovePulseActive || !_effectEnabled) return;
            
            _isLovePulseActive = true;
            _pulseTimer = 0f;
            _nextPulseTime = 0f;
            
            // パーティクル開始
            if (heartParticles != null)
            {
                heartParticles.Play();
                _particleEmission.enabled = true;
            }
            
            // ライト点灯
            if (loveLight != null)
            {
                loveLight.enabled = true;
            }
            
            // 愛情発動音再生
            if (loveAudioSource != null && loveActivationClip != null)
            {
                loveAudioSource.PlayOneShot(loveActivationClip);
            }
            
            Debug.Log($"[ReactiveAuraFX] LovePulse発動: {_currentLoveTarget?.name ?? "Manual"}");
        }

        private void StopLovePulse()
        {
            if (!_isLovePulseActive) return;
            
            _isLovePulseActive = false;
            
            // パーティクル停止
            if (heartParticles != null)
            {
                heartParticles.Stop();
                _particleEmission.enabled = false;
            }
            
            // ライト消灯
            if (loveLight != null)
            {
                loveLight.enabled = false;
            }
            
            Debug.Log("[ReactiveAuraFX] LovePulse停止");
        }

        private void UpdateLovePulseEffect()
        {
            if (!_isLovePulseActive) return;
            
            _pulseTimer += Time.deltaTime;
            
            // ハートビートパルス
            if (_pulseTimer >= _nextPulseTime)
            {
                CreateHeartPulse();
                _nextPulseTime = _pulseTimer + (60f / 72f); // 72 BPM
                
                // ハートビート音再生
                if (loveAudioSource != null && heartbeatClip != null)
                {
                    loveAudioSource.pitch = 1f + (_loveAccumulation / loveAccumulationTime) * 0.3f;
                    loveAudioSource.volume = effectIntensity * 0.4f;
                    loveAudioSource.PlayOneShot(heartbeatClip);
                }
            }
            
            // ライト強度調整
            if (loveLight != null)
            {
                float pulseValue = Mathf.Sin(_pulseTimer * 4f) * 0.3f + 0.7f;
                loveLight.intensity = _baseLightIntensity * pulseValue * effectIntensity;
            }
            
            // パーティクル向き調整
            if (_currentLoveTarget != null && heartParticles != null)
            {
                Vector3 directionToTarget = (_currentLoveTarget.position - transform.position).normalized;
                _particleVelocity.x = directionToTarget.x * 2f;
                _particleVelocity.y = directionToTarget.y * 2f + 1f; // 上向き成分追加
                _particleVelocity.z = directionToTarget.z * 2f;
            }
        }

        private void CreateHeartPulse()
        {
            if (heartParticles == null) return;
            
            // バーストエミッション
            var burstParams = new ParticleSystem.EmitParams();
            burstParams.position = transform.position;
            burstParams.startColor = loveColor;
            burstParams.startSize = 0.15f * effectIntensity;
            burstParams.startLifetime = 2.5f;
            
            heartParticles.Emit(burstParams, loveParticleCount);
        }

        /// <summary>
        /// エフェクトの有効/無効を切り替え
        /// </summary>
        public void SetEffectEnabled(bool enabled)
        {
            _effectEnabled = enabled;
            
            if (!enabled && _isLovePulseActive)
            {
                StopLovePulse();
            }
        }

        /// <summary>
        /// 愛情色を設定
        /// </summary>
        public void SetLoveColor(Color color)
        {
            loveColor = color;
            
            if (heartParticles != null)
            {
                _particleMain.startColor = color;
            }
            
            if (loveLight != null)
            {
                loveLight.color = color;
            }
        }

        /// <summary>
        /// エフェクト強度を設定
        /// </summary>
        public void SetEffectIntensity(float intensity)
        {
            effectIntensity = Mathf.Clamp(intensity, 0.1f, 3f);
        }

        /// <summary>
        /// 検出距離を設定
        /// </summary>
        public void SetDetectionDistance(float distance)
        {
            loveDetectionDistance = Mathf.Clamp(distance, 1f, 10f);
        }

        /// <summary>
        /// 愛情蓄積をリセット
        /// </summary>
        public void ResetLoveAccumulation()
        {
            _loveAccumulation = 0f;
            if (_isLovePulseActive)
            {
                StopLovePulse();
            }
        }

        /// <summary>
        /// 手動で愛情パルスを発動
        /// </summary>
        public void ManualTriggerLovePulse()
        {
            if (!_effectEnabled) return;
            
            _loveAccumulation = loveAccumulationTime;
            TriggerLovePulse();
        }

#if MA_VRCSDK3_AVATARS
        /// <summary>
        /// Modular Avatar統合のセットアップ
        /// </summary>
        [ContextMenu("Modular Avatar統合セットアップ")]
        public void SetupModularAvatarIntegration()
        {
            if (_avatarDescriptor == null)
            {
                _avatarDescriptor = GetComponentInParent<VRCAvatarDescriptor>();
                if (_avatarDescriptor == null)
                {
                    _avatarDescriptor = FindObjectOfType<VRCAvatarDescriptor>();
                }
            }
            
            if (_avatarDescriptor != null)
            {
                _avatarAnimator = _avatarDescriptor.GetComponent<Animator>();
                Debug.Log("[ReactiveAuraFX] LovePulse Modular Avatar統合完了");
            }
            else
            {
                Debug.LogWarning("[ReactiveAuraFX] VRCAvatarDescriptorが見つかりません");
            }
        }
#endif

        void OnDrawGizmosSelected()
        {
            // 検出範囲を表示
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, loveDetectionDistance);
            
            // 現在のターゲットを表示
            if (_currentLoveTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentLoveTarget.position);
                Gizmos.DrawWireSphere(_currentLoveTarget.position, 0.3f);
            }
            
            // 愛情蓄積度を表示
            if (_loveAccumulation > 0f)
            {
                Gizmos.color = Color.Lerp(Color.white, Color.red, _loveAccumulation / loveAccumulationTime);
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, 
                    Vector3.one * (_loveAccumulation / loveAccumulationTime));
            }
        }
    }
} 