// EmotionAura - 表情連動オーラエフェクト
// Reactive Aura FX サブシステム
// VRChat + Modular Avatar完全対応

using UnityEngine;
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
    /// 表情に応じてオーラの色と形を変更するエフェクト
    /// VRChat Avatar 3.0 + Modular Avatar完全対応、AutoFIX安全設計
    /// </summary>
    [AddComponentMenu("ReactiveAuraFX/Effects/Emotion Aura Effect")]
    [RequireComponent(typeof(ParticleSystem))]
    public class EmotionAuraEffect : MonoBehaviour
    {
        [Header("💫 EmotionAura設定")]
        [Tooltip("オーラの基本プレハブ")]
        public GameObject auraBasePrefab;
        
        [Tooltip("パーティクルシステム")]
        public ParticleSystem auraParticles;
        
        [Tooltip("ライトコンポーネント")]
        public Light auraLight;
        
        [Tooltip("表情別色設定")]
        public EmotionAuraColors emotionColors = new EmotionAuraColors();
        
        [Tooltip("アニメーション速度")]
        [Range(0.1f, 5f)]
        public float animationSpeed = 1f;
        
        [Tooltip("強度倍率")]
        [Range(0.1f, 3f)]
        public float intensityMultiplier = 1f;

#if MA_VRCSDK3_AVATARS
        [Space(10)]
        [Header("🔗 Modular Avatar完全連携")]
        [Tooltip("Animatorパラメータから表情を検出")]
        public bool useAnimatorEmotionDetection = true;
        
        [Tooltip("表情パラメータ名")]
        public string emotionParameterName = "Emotion";
        
        [Tooltip("エフェクト有効化パラメータ名")]
        public string enableParameterName = "ReactiveAuraFX/EmotionAura";
        
        [Tooltip("表情値マッピング")]
        public EmotionValueMapping[] emotionMappings = new EmotionValueMapping[]
        {
            new EmotionValueMapping { emotionType = EmotionType.Neutral, parameterValue = 0 },
            new EmotionValueMapping { emotionType = EmotionType.Happy, parameterValue = 1 },
            new EmotionValueMapping { emotionType = EmotionType.Love, parameterValue = 2 },
            new EmotionValueMapping { emotionType = EmotionType.Shy, parameterValue = 3 },
            new EmotionValueMapping { emotionType = EmotionType.Angry, parameterValue = 4 },
            new EmotionValueMapping { emotionType = EmotionType.Sad, parameterValue = 5 },
            new EmotionValueMapping { emotionType = EmotionType.Excited, parameterValue = 6 },
            new EmotionValueMapping { emotionType = EmotionType.Calm, parameterValue = 7 }
        };

        [System.Serializable]
        public class EmotionValueMapping
        {
            public EmotionType emotionType;
            public int parameterValue;
        }
#endif

        // === 内部変数 ===
        private EmotionType _currentEmotion = EmotionType.Neutral;
        private EmotionType _previousEmotion = EmotionType.Neutral;
        private float _transitionProgress = 0f;
        private bool _isTransitioning = false;
        private bool _effectEnabled = true;
        
        // パーティクル設定
        private ParticleSystem.MainModule _mainModule;
        private ParticleSystem.ColorOverLifetimeModule _colorModule;
        private ParticleSystem.SizeOverLifetimeModule _sizeModule;
        private ParticleSystem.VelocityOverLifetimeModule _velocityModule;
        
        // ライト設定
        private Color _targetLightColor;
        private float _targetLightIntensity;
        
#if MA_VRCSDK3_AVATARS
        // Modular Avatar関連
        private Animator _avatarAnimator;
        private int _lastEmotionParameterValue = 0;
        private bool _lastEffectEnabledValue = true;
        private VRCAvatarDescriptor _avatarDescriptor;
#endif

        public enum EmotionType
        {
            Neutral,    // 😐 中性
            Happy,      // 😊 喜び
            Love,       // 😍 愛情
            Shy,        // 😳 恥ずかしがり
            Angry,      // 😠 怒り
            Sad,        // 😢 悲しみ
            Excited,    // 🤩 興奮
            Calm        // 😌 穏やか
        }

        [System.Serializable]
        public class EmotionAuraColors
        {
            [Header("感情別オーラ色設定")]
            public Color neutralColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            public Color happyColor = new Color(1f, 1f, 0.3f, 0.5f);
            public Color loveColor = new Color(1f, 0.3f, 0.5f, 0.6f);
            public Color shyColor = new Color(1f, 0.7f, 0.7f, 0.4f);
            public Color angryColor = new Color(1f, 0.2f, 0.2f, 0.7f);
            public Color sadColor = new Color(0.4f, 0.4f, 1f, 0.4f);
            public Color excitedColor = new Color(1f, 0.5f, 0f, 0.8f);
            public Color calmColor = new Color(0.3f, 0.8f, 0.5f, 0.3f);
            
            public Color GetColorForEmotion(EmotionType emotion)
            {
                switch (emotion)
                {
                    case EmotionType.Happy: return happyColor;
                    case EmotionType.Love: return loveColor;
                    case EmotionType.Shy: return shyColor;
                    case EmotionType.Angry: return angryColor;
                    case EmotionType.Sad: return sadColor;
                    case EmotionType.Excited: return excitedColor;
                    case EmotionType.Calm: return calmColor;
                    default: return neutralColor;
                }
            }
        }

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
            if (gameObject.name.Contains("EmotionAura") == false)
            {
                gameObject.name = "EmotionAura_Effect";
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
            // パーティクルシステムの初期化
            if (auraParticles == null)
            {
                auraParticles = GetComponent<ParticleSystem>();
                if (auraParticles == null)
                {
                    auraParticles = gameObject.AddComponent<ParticleSystem>();
                }
            }

#if MA_VRCSDK3_AVATARS
            // Modular Avatar Animator検出
            if (useAnimatorEmotionDetection && _avatarAnimator == null)
            {
                // 親オブジェクトからVRCAvatarDescriptorを検索
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
            
            if (auraParticles != null)
            {
                _mainModule = auraParticles.main;
                _colorModule = auraParticles.colorOverLifetime;
                _sizeModule = auraParticles.sizeOverLifetime;
                _velocityModule = auraParticles.velocityOverLifetime;
                
                // パーティクル基本設定
                _mainModule.loop = true;
                _mainModule.startLifetime = 2f;
                _mainModule.startSpeed = 1f;
                _mainModule.maxParticles = 100;
                
                _colorModule.enabled = true;
                _sizeModule.enabled = true;
                _velocityModule.enabled = true;
            }
            
            // ライトの初期化
            if (auraLight == null)
            {
                auraLight = GetComponent<Light>();
            }
            
            if (auraLight != null)
            {
                auraLight.type = LightType.Point;
                auraLight.range = 5f;
                auraLight.intensity = 0.5f;
            }
            
            Debug.Log("[ReactiveAuraFX] EmotionAuraEffect初期化完了");
        }

        void Update()
        {
            if (_isTransitioning)
            {
                UpdateTransition();
            }
            
#if MA_VRCSDK3_AVATARS
            // Modular Avatar パラメータ監視
            if (useAnimatorEmotionDetection && _avatarAnimator != null)
            {
                UpdateEmotionFromAnimator();
                UpdateEffectEnabledFromAnimator();
            }
#endif
            
            if (_effectEnabled)
            {
                UpdateAuraEffect();
            }
        }

#if MA_VRCSDK3_AVATARS
        private void UpdateEmotionFromAnimator()
        {
            try
            {
                if (_avatarAnimator.parameters == null) return;
                
                // パラメータの存在チェック
                bool paramExists = false;
                foreach (var param in _avatarAnimator.parameters)
                {
                    if (param.name == emotionParameterName)
                    {
                        paramExists = true;
                        break;
                    }
                }
                
                if (!paramExists) return;
                
                int emotionValue = _avatarAnimator.GetInteger(emotionParameterName);
                
                if (emotionValue != _lastEmotionParameterValue)
                {
                    _lastEmotionParameterValue = emotionValue;
                    
                    // パラメータ値から表情タイプを検索
                    foreach (var mapping in emotionMappings)
                    {
                        if (mapping.parameterValue == emotionValue)
                        {
                            SetEmotion(mapping.emotionType);
                            break;
                        }
                    }
                }
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

        /// <summary>
        /// 感情を設定してオーラを変更
        /// </summary>
        public void SetEmotion(EmotionType emotion)
        {
            if (_currentEmotion != emotion)
            {
                _previousEmotion = _currentEmotion;
                _currentEmotion = emotion;
                StartTransition();
            }
        }

        private void StartTransition()
        {
            _isTransitioning = true;
            _transitionProgress = 0f;
            
            Debug.Log($"[ReactiveAuraFX] 感情変化: {_previousEmotion} → {_currentEmotion}");
        }

        private void UpdateTransition()
        {
            _transitionProgress += Time.deltaTime * animationSpeed;
            
            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _isTransitioning = false;
            }
            
            // 色の補間
            Color previousColor = emotionColors.GetColorForEmotion(_previousEmotion);
            Color currentColor = emotionColors.GetColorForEmotion(_currentEmotion);
            Color blendedColor = Color.Lerp(previousColor, currentColor, _transitionProgress);
            
            ApplyColorToParticles(blendedColor);
            ApplyColorToLight(blendedColor);
        }

        private void UpdateAuraEffect()
        {
            // パルス効果
            float pulseValue = Mathf.Sin(Time.time * 2f * animationSpeed) * 0.2f + 0.8f;
            
            if (auraParticles != null)
            {
                var emission = auraParticles.emission;
                emission.rateOverTime = GetEmissionRateForEmotion(_currentEmotion) * pulseValue * intensityMultiplier;
            }
            
            if (auraLight != null)
            {
                auraLight.intensity = _targetLightIntensity * pulseValue * intensityMultiplier;
            }
        }

        private void ApplyColorToParticles(Color color)
        {
            if (auraParticles == null) return;
            
            // メインカラー設定
            _mainModule.startColor = color;
            
            // カラーオーバーライフタイム設定
            if (_colorModule.enabled)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { 
                        new GradientColorKey(color, 0.0f), 
                        new GradientColorKey(color, 1.0f) 
                    },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(0.0f, 0.0f), 
                        new GradientAlphaKey(color.a, 0.2f), 
                        new GradientAlphaKey(color.a * 0.8f, 0.8f), 
                        new GradientAlphaKey(0.0f, 1.0f) 
                    }
                );
                _colorModule.color = gradient;
            }
        }

        private void ApplyColorToLight(Color color)
        {
            if (auraLight == null) return;
            
            _targetLightColor = color;
            _targetLightIntensity = GetLightIntensityForEmotion(_currentEmotion);
            
            auraLight.color = Color.Lerp(auraLight.color, _targetLightColor, Time.deltaTime * 2f);
        }

        private float GetEmissionRateForEmotion(EmotionType emotion)
        {
            switch (emotion)
            {
                case EmotionType.Excited: return 50f;
                case EmotionType.Love: return 40f;
                case EmotionType.Happy: return 35f;
                case EmotionType.Angry: return 45f;
                case EmotionType.Shy: return 20f;
                case EmotionType.Sad: return 15f;
                case EmotionType.Calm: return 25f;
                default: return 30f; // Neutral
            }
        }

        private float GetLightIntensityForEmotion(EmotionType emotion)
        {
            switch (emotion)
            {
                case EmotionType.Excited: return 1.2f;
                case EmotionType.Love: return 0.8f;
                case EmotionType.Happy: return 0.9f;
                case EmotionType.Angry: return 1.0f;
                case EmotionType.Shy: return 0.4f;
                case EmotionType.Sad: return 0.3f;
                case EmotionType.Calm: return 0.6f;
                default: return 0.5f; // Neutral
            }
        }

        /// <summary>
        /// エフェクトの有効/無効を切り替え
        /// </summary>
        public void SetEffectEnabled(bool enabled)
        {
            _effectEnabled = enabled;
            
            if (auraParticles != null)
            {
                if (enabled)
                    auraParticles.Play();
                else
                    auraParticles.Stop();
            }
            
            if (auraLight != null)
            {
                auraLight.enabled = enabled;
            }
        }

        /// <summary>
        /// 強度を設定
        /// </summary>
        public void SetIntensity(float intensity)
        {
            intensityMultiplier = Mathf.Clamp(intensity, 0.1f, 3f);
        }

        /// <summary>
        /// アニメーション速度を設定
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            animationSpeed = Mathf.Clamp(speed, 0.1f, 5f);
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
                Debug.Log("[ReactiveAuraFX] EmotionAura Modular Avatar統合完了");
            }
            else
            {
                Debug.LogWarning("[ReactiveAuraFX] VRCAvatarDescriptorが見つかりません");
            }
        }

        /// <summary>
        /// 表情マッピングをリセット
        /// </summary>
        [ContextMenu("表情マッピングをリセット")]
        public void ResetEmotionMappings()
        {
            emotionMappings = new EmotionValueMapping[]
            {
                new EmotionValueMapping { emotionType = EmotionType.Neutral, parameterValue = 0 },
                new EmotionValueMapping { emotionType = EmotionType.Happy, parameterValue = 1 },
                new EmotionValueMapping { emotionType = EmotionType.Love, parameterValue = 2 },
                new EmotionValueMapping { emotionType = EmotionType.Shy, parameterValue = 3 },
                new EmotionValueMapping { emotionType = EmotionType.Angry, parameterValue = 4 },
                new EmotionValueMapping { emotionType = EmotionType.Sad, parameterValue = 5 },
                new EmotionValueMapping { emotionType = EmotionType.Excited, parameterValue = 6 },
                new EmotionValueMapping { emotionType = EmotionType.Calm, parameterValue = 7 }
            };
            Debug.Log("[ReactiveAuraFX] 表情マッピングがリセットされました");
        }
#endif
    }
} 