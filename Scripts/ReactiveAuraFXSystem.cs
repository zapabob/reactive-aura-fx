// 魅力拡張アバター：Reactive Aura FX
// VRChatアバター用魅力的エフェクトシステム
// 作成日: 2025年1月27日

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;

#if VRC_SDK_VRCSDK3 && !UDON
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

#if MA_VRCSDK3_AVATARS
using nadena.dev.modular_avatar.core;
#endif

namespace ReactiveAuraFX.Core
{
    /// <summary>
    /// VRChatアバター用魅力的なエフェクトシステム
    /// 表情・視線・動作に連動してリアルタイムでエフェクトを発動
    /// Modular Avatar対応、AutoFIX安全設計
    /// </summary>
    [AddComponentMenu("ReactiveAuraFX/Reactive Aura FX System")]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)] // 他のコンポーネントより先に実行
    public class ReactiveAuraFXSystem : MonoBehaviour
    {
        [Header("🌟 Reactive Aura FX - 魅力拡張アバター")]
        [SerializeField, HideInInspector]
        private string systemVersion = "1.0.0";
        
        [SerializeField, HideInInspector]
        private bool systemInitialized = false;
        
        [SerializeField, HideInInspector]
        private bool modularAvatarDetected = false;

        // === 🎯 基本設定 ===
        [Header("🎯 基本システム設定")]
        [Tooltip("システム全体の有効/無効")]
        public bool enableSystem = true;
        
        [Tooltip("VRChat互換性モード")]
        public bool vrchatCompatibilityMode = true;
        
        [Tooltip("デバッグモード")]
        public bool debugMode = false;
        
        [Tooltip("AutoFIX安全モード")]
        public bool autoFixSafeMode = true;

        // === 💫 EmotionAura設定 ===
        [Header("💫 EmotionAura - 表情連動オーラ")]
        [Tooltip("表情連動オーラエフェクトを有効化")]
        public bool enableEmotionAura = true;
        
        [Tooltip("オーラの基本強度")]
        [Range(0f, 2f)]
        public float auraBaseIntensity = 1.0f;
        
        [Tooltip("オーラの範囲")]
        [Range(0.5f, 5.0f)]
        public float auraRange = 2.0f;

        // === 💓 HeartbeatGlow設定 ===
        [Header("💓 HeartbeatGlow - 鼓動波紋光")]
        [Tooltip("鼓動波紋光エフェクトを有効化")]
        public bool enableHeartbeatGlow = true;
        
        [Tooltip("鼓動の速度")]
        [Range(0.5f, 3.0f)]
        public float heartbeatSpeed = 1.2f;
        
        [Tooltip("波紋の強度")]
        [Range(0.1f, 2.0f)]
        public float heartbeatIntensity = 0.8f;
        
        [Tooltip("波紋の色")]
        public Color heartbeatColor = new Color(1f, 0.3f, 0.3f, 0.7f);

        // === 👁️ EyeFocusRay設定 ===
        [Header("👁️ EyeFocusRay - 視線ビーム")]
        [Tooltip("視線ビームエフェクトを有効化")]
        public bool enableEyeFocusRay = true;
        
        [Tooltip("ビームの長さ")]
        [Range(1f, 10f)]
        public float eyeRayLength = 5f;
        
        [Tooltip("ビームの太さ")]
        [Range(0.01f, 0.1f)]
        public float eyeRayThickness = 0.02f;
        
        [Tooltip("ビームの色")]
        public Color eyeRayColor = new Color(0.5f, 0.8f, 1f, 0.6f);

        // === 💕 LovePulse設定 ===
        [Header("💕 LovePulse - 愛情パーティクル")]
        [Tooltip("愛情パーティクルエフェクトを有効化")]
        public bool enableLovePulse = true;
        
        [Tooltip("反応距離")]
        [Range(1f, 10f)]
        public float loveDetectionDistance = 3f;
        
        [Tooltip("パーティクル数")]
        [Range(5, 50)]
        public int loveParticleCount = 15;

        // === 🌸 IdleBloom設定 ===
        [Header("🌸 IdleBloom - 静寂の花")]
        [Tooltip("静寂の花エフェクトを有効化")]
        public bool enableIdleBloom = true;
        
        [Tooltip("静止判定時間（秒）")]
        [Range(5f, 30f)]
        public float idleTimeThreshold = 10f;
        
        [Tooltip("花の成長速度")]
        [Range(0.1f, 2f)]
        public float bloomGrowthSpeed = 0.5f;

        // === 🎛️ アニメーター・表情制御設定 ===
        [Header("🎛️ アニメーター・表情制御")]
        [Tooltip("対象アバターディスクリプター")]
        public VRCAvatarDescriptor avatarDescriptor;
        
        [Tooltip("表情検出用アニメーター")]
        public Animator faceAnimator;
        
        [Tooltip("視線検出用Transform（通常は頭のボーン）")]
        public Transform headTransform;
        
        [Tooltip("胸元検出用Transform")]
        public Transform chestTransform;

        // === エフェクトコンポーネント参照 ===
        [Header("🎬 エフェクトコンポーネント")]
        [SerializeField, HideInInspector]
        private EmotionAuraEffect emotionAuraEffect;
        
        [SerializeField, HideInInspector]
        private HeartbeatGlowEffect heartbeatGlowEffect;
        
        [SerializeField, HideInInspector]
        private EyeFocusRayEffect eyeFocusRayEffect;
        
        [SerializeField, HideInInspector]
        private LovePulseEffect lovePulseEffect;
        
        [SerializeField, HideInInspector]
        private IdleBloomEffect idleBloomEffect;

        // === 内部変数 ===
        private float _lastMotionTime = 0f;
        private bool _isIdleBloomActive = false;
        private Vector3 _lastPosition;
        private Camera _mainCamera;
        
        // Modular Avatar Animatorパラメータ監視
#if MA_VRCSDK3_AVATARS
        private bool _lastSystemEnabledParam = true;
        private bool _lastEmotionAuraParam = true;
        private bool _lastHeartbeatGlowParam = true;
        private bool _lastEyeFocusRayParam = true;
        private bool _lastLovePulseParam = true;
        private bool _lastIdleBloomParam = true;
#endif

        // === AutoFIX対策 ===
        [Space(10)]
        [Header("🛡️ AutoFIX対策")]
        [Tooltip("エフェクト用GameObject（AutoFIX回避）")]
        [SerializeField, HideInInspector]
        private GameObject effectContainer;

        // === 初期化 ===
        void Awake()
        {
            // AutoFIX対策：Awakeで基本設定
            if (autoFixSafeMode)
            {
                EnsureAutoFixSafety();
            }
        }

        void Start()
        {
            try
            {
                InitializeSystem();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReactiveAuraFX] システム初期化エラー: {e.Message}");
            }
        }

        private void EnsureAutoFixSafety()
        {
            // オブジェクト名をAutoFIXが無視する形式に
            if (gameObject.name.Contains("ReactiveAuraFX") == false)
            {
                gameObject.name = "ReactiveAuraFX_System";
            }
            
            // タグとレイヤーの設定
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
            
            // Modular Avatar検出
#if MA_VRCSDK3_AVATARS
            modularAvatarDetected = true;
#endif
        }

        private void InitializeSystem()
        {
            if (systemInitialized) return;
            
            Debug.Log("[ReactiveAuraFX] Reactive Aura FXシステムを初期化中...");
            
            // カメラ参照取得
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindObjectOfType<Camera>();
            }
            
            // 自動検出の実行
            if (avatarDescriptor == null)
                AutoDetectAvatarComponents();
            
            // エフェクトコンテナ作成
            CreateEffectContainer();
            
            // 各エフェクト初期化
            InitializeEffects();
            
            // Modular Avatar自動セットアップ
#if MA_VRCSDK3_AVATARS
            if (modularAvatarDetected)
            {
                SetupModularAvatarComponents();
            }
#endif
            
            _lastPosition = transform.position;
            systemInitialized = true;
            
            Debug.Log("[ReactiveAuraFX] システム初期化完了！");
        }

        private void CreateEffectContainer()
        {
            if (effectContainer == null)
            {
                effectContainer = new GameObject("ReactiveAuraFX_Effects");
                effectContainer.transform.SetParent(transform);
                effectContainer.transform.localPosition = Vector3.zero;
                effectContainer.transform.localRotation = Quaternion.identity;
                effectContainer.transform.localScale = Vector3.one;
                
                // AutoFIX対策
                effectContainer.tag = "EditorOnly";
            }
        }

        private void InitializeEffects()
        {
            // EmotionAura初期化
            if (enableEmotionAura && emotionAuraEffect == null)
            {
                var emotionObj = new GameObject("EmotionAura_Effect");
                emotionObj.transform.SetParent(effectContainer.transform);
                emotionAuraEffect = emotionObj.AddComponent<EmotionAuraEffect>();
                InitializeEmotionAura();
            }
            
            // HeartbeatGlow初期化
            if (enableHeartbeatGlow && heartbeatGlowEffect == null)
            {
                var heartbeatObj = new GameObject("HeartbeatGlow_Effect");
                heartbeatObj.transform.SetParent(effectContainer.transform);
                heartbeatGlowEffect = heartbeatObj.AddComponent<HeartbeatGlowEffect>();
                InitializeHeartbeatGlow();
            }
            
            // EyeFocusRay初期化
            if (enableEyeFocusRay && eyeFocusRayEffect == null)
            {
                var eyeRayObj = new GameObject("EyeFocusRay_Effect");
                eyeRayObj.transform.SetParent(effectContainer.transform);
                eyeFocusRayEffect = eyeRayObj.AddComponent<EyeFocusRayEffect>();
                InitializeEyeFocusRay();
            }
            
            // LovePulse初期化
            if (enableLovePulse && lovePulseEffect == null)
            {
                var loveObj = new GameObject("LovePulse_Effect");
                loveObj.transform.SetParent(effectContainer.transform);
                lovePulseEffect = loveObj.AddComponent<LovePulseEffect>();
                InitializeLovePulse();
            }
            
            // IdleBloom初期化
            if (enableIdleBloom && idleBloomEffect == null)
            {
                var bloomObj = new GameObject("IdleBloom_Effect");
                bloomObj.transform.SetParent(effectContainer.transform);
                idleBloomEffect = bloomObj.AddComponent<IdleBloomEffect>();
                InitializeIdleBloom();
            }
        }

        private void InitializeEmotionAura()
        {
            if (emotionAuraEffect == null) return;
            
            emotionAuraEffect.animationSpeed = 1f;
            emotionAuraEffect.intensityMultiplier = auraBaseIntensity;
        }

        private void InitializeHeartbeatGlow()
        {
            if (heartbeatGlowEffect == null) return;
            
            heartbeatGlowEffect.heartbeatSpeed = heartbeatSpeed;
            heartbeatGlowEffect.heartbeatIntensity = heartbeatIntensity;
            heartbeatGlowEffect.heartbeatColor = heartbeatColor;
            heartbeatGlowEffect.chestTransform = chestTransform;
        }

        private void InitializeEyeFocusRay()
        {
            if (eyeFocusRayEffect == null) return;
            
            eyeFocusRayEffect.rayLength = eyeRayLength;
            eyeFocusRayEffect.rayThickness = eyeRayThickness;
            eyeFocusRayEffect.rayColor = eyeRayColor;
        }

        private void InitializeLovePulse()
        {
            if (lovePulseEffect == null) return;
            
            lovePulseEffect.loveDetectionDistance = loveDetectionDistance;
            lovePulseEffect.loveParticleCount = loveParticleCount;
        }

        private void InitializeIdleBloom()
        {
            if (idleBloomEffect == null) return;
            
            idleBloomEffect.idleTimeThreshold = idleTimeThreshold;
            idleBloomEffect.bloomGrowthSpeed = bloomGrowthSpeed;
        }

        private void AutoDetectAvatarComponents()
        {
            // VRCAvatarDescriptorの自動検出
            if (avatarDescriptor == null)
            {
                avatarDescriptor = GetComponentInParent<VRCAvatarDescriptor>();
                if (avatarDescriptor == null)
                {
                    avatarDescriptor = FindObjectOfType<VRCAvatarDescriptor>();
                }
            }
            
            // アニメーターの自動検出
            if (faceAnimator == null && avatarDescriptor != null)
            {
                faceAnimator = avatarDescriptor.GetComponent<Animator>();
            }
            
            // 重要なTransformの自動検出
            AutoDetectImportantTransforms();
            
            Debug.Log($"[ReactiveAuraFX] アバターコンポーネント自動検出完了");
        }

        private void AutoDetectImportantTransforms()
        {
            if (avatarDescriptor == null) return;
            
            var animator = avatarDescriptor.GetComponent<Animator>();
            if (animator == null) return;
            
            // 頭のTransform検出
            if (headTransform == null)
            {
                headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
            }
            
            // 胸のTransform検出
            if (chestTransform == null)
            {
                chestTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chestTransform == null)
                {
                    chestTransform = animator.GetBoneTransform(HumanBodyBones.Spine);
                }
            }
        }

        // === メインアップデートループ ===
        void Update()
        {
            if (!enableSystem || !systemInitialized) return;
            
            try
            {
                UpdateMotionDetection();
                UpdateEffectStates();
                
                if (debugMode)
                {
                    UpdateDebugDisplay();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReactiveAuraFX] Update()エラー: {e.Message}");
            }
        }

        private void UpdateMotionDetection()
        {
            Vector3 currentPosition = transform.position;
            bool isMoving = Vector3.Distance(currentPosition, _lastPosition) > 0.01f;
            
            if (isMoving)
            {
                _lastMotionTime = Time.time;
            }
            
            _lastPosition = currentPosition;
        }

        private void UpdateEffectStates()
        {
            // IdleBloom更新
            UpdateIdleBloom();
            
            // Modular Avatar Animatorパラメータ監視
#if MA_VRCSDK3_AVATARS
            if (enableAnimatorParameters && faceAnimator != null)
            {
                MonitorAnimatorParameters();
            }
#endif
            
            // その他のエフェクト状態同期
            SynchronizeEffectSettings();
        }

#if MA_VRCSDK3_AVATARS
        private void MonitorAnimatorParameters()
        {
            // システム全体制御
            bool systemEnabledParam = GetAnimatorBoolParameter("ReactiveAuraFX/SystemEnabled", enableSystem);
            if (systemEnabledParam != _lastSystemEnabledParam)
            {
                enableSystem = systemEnabledParam;
                _lastSystemEnabledParam = systemEnabledParam;
                ToggleAllEffects();
            }
            
            // EmotionAura制御
            bool emotionAuraParam = GetAnimatorBoolParameter("ReactiveAuraFX/EmotionAura", enableEmotionAura);
            if (emotionAuraParam != _lastEmotionAuraParam)
            {
                _lastEmotionAuraParam = emotionAuraParam;
                SetEmotionAuraEnabled(emotionAuraParam);
            }
            
            // HeartbeatGlow制御
            bool heartbeatGlowParam = GetAnimatorBoolParameter("ReactiveAuraFX/HeartbeatGlow", enableHeartbeatGlow);
            if (heartbeatGlowParam != _lastHeartbeatGlowParam)
            {
                _lastHeartbeatGlowParam = heartbeatGlowParam;
                SetHeartbeatGlowEnabled(heartbeatGlowParam);
            }
            
            // EyeFocusRay制御
            bool eyeFocusRayParam = GetAnimatorBoolParameter("ReactiveAuraFX/EyeFocusRay", enableEyeFocusRay);
            if (eyeFocusRayParam != _lastEyeFocusRayParam)
            {
                _lastEyeFocusRayParam = eyeFocusRayParam;
                SetEyeFocusRayEnabled(eyeFocusRayParam);
            }
            
            // LovePulse制御
            bool lovePulseParam = GetAnimatorBoolParameter("ReactiveAuraFX/LovePulse", enableLovePulse);
            if (lovePulseParam != _lastLovePulseParam)
            {
                _lastLovePulseParam = lovePulseParam;
                SetLovePulseEnabled(lovePulseParam);
            }
            
            // IdleBloom制御
            bool idleBloomParam = GetAnimatorBoolParameter("ReactiveAuraFX/IdleBloom", enableIdleBloom);
            if (idleBloomParam != _lastIdleBloomParam)
            {
                _lastIdleBloomParam = idleBloomParam;
                SetIdleBloomEnabled(idleBloomParam);
            }
        }

        private bool GetAnimatorBoolParameter(string paramName, bool defaultValue)
        {
            if (faceAnimator == null) return defaultValue;
            
            try
            {
                return faceAnimator.GetBool(paramName);
            }
            catch
            {
                return defaultValue;
            }
        }
#endif

        private void UpdateIdleBloom()
        {
            if (!enableIdleBloom) return;
            
            float timeSinceLastMotion = Time.time - _lastMotionTime;
            
            if (timeSinceLastMotion >= idleTimeThreshold && !_isIdleBloomActive)
            {
                TriggerIdleBloom();
            }
            else if (timeSinceLastMotion < idleTimeThreshold / 2f && _isIdleBloomActive)
            {
                StopIdleBloom();
            }
        }

        private void SynchronizeEffectSettings()
        {
            // 設定変更を各エフェクトに同期
            if (heartbeatGlowEffect != null)
            {
                heartbeatGlowEffect.heartbeatSpeed = heartbeatSpeed;
                heartbeatGlowEffect.heartbeatIntensity = heartbeatIntensity;
                heartbeatGlowEffect.heartbeatColor = heartbeatColor;
            }
            
            if (eyeFocusRayEffect != null)
            {
                eyeFocusRayEffect.rayLength = eyeRayLength;
                eyeFocusRayEffect.rayThickness = eyeRayThickness;
                eyeFocusRayEffect.rayColor = eyeRayColor;
            }
            
            if (lovePulseEffect != null)
            {
                lovePulseEffect.loveDetectionDistance = loveDetectionDistance;
                lovePulseEffect.loveParticleCount = loveParticleCount;
            }
            
            if (idleBloomEffect != null)
            {
                idleBloomEffect.idleTimeThreshold = idleTimeThreshold;
                idleBloomEffect.bloomGrowthSpeed = bloomGrowthSpeed;
            }
        }

        private void TriggerIdleBloom()
        {
            _isIdleBloomActive = true;
            if (idleBloomEffect != null)
            {
                idleBloomEffect.ManualTriggerBloom();
            }
            Debug.Log("[ReactiveAuraFX] IdleBloom発動！");
        }

        private void StopIdleBloom()
        {
            _isIdleBloomActive = false;
            if (idleBloomEffect != null)
            {
                idleBloomEffect.ResetMotionTimer();
            }
            Debug.Log("[ReactiveAuraFX] IdleBloom終了");
        }

        private void UpdateDebugDisplay()
        {
            float timeSinceLastMotion = Time.time - _lastMotionTime;
            if (timeSinceLastMotion > 1f)
            {
                Debug.Log($"[ReactiveAuraFX] 静止時間: {timeSinceLastMotion:F1}秒");
            }
        }

        // === パブリックAPI ===
        public void SetEmotionAuraEnabled(bool enabled)
        {
            enableEmotionAura = enabled;
            if (emotionAuraEffect != null)
            {
                emotionAuraEffect.SetEffectEnabled(enabled);
            }
            Debug.Log($"[ReactiveAuraFX] EmotionAura: {(enabled ? "有効" : "無効")}");
        }

        public void SetHeartbeatGlowEnabled(bool enabled)
        {
            enableHeartbeatGlow = enabled;
            if (heartbeatGlowEffect != null)
            {
                if (enabled)
                    heartbeatGlowEffect.StartHeartbeatEffect();
                else
                    heartbeatGlowEffect.StopHeartbeatEffect();
            }
            Debug.Log($"[ReactiveAuraFX] HeartbeatGlow: {(enabled ? "有効" : "無効")}");
        }

        public void SetEyeFocusRayEnabled(bool enabled)
        {
            enableEyeFocusRay = enabled;
            if (eyeFocusRayEffect != null)
            {
                eyeFocusRayEffect.enabled = enabled;
            }
            Debug.Log($"[ReactiveAuraFX] EyeFocusRay: {(enabled ? "有効" : "無効")}");
        }

        public void SetLovePulseEnabled(bool enabled)
        {
            enableLovePulse = enabled;
            if (lovePulseEffect != null)
            {
                if (!enabled)
                    lovePulseEffect.ResetLoveAccumulation();
                lovePulseEffect.enabled = enabled;
            }
            Debug.Log($"[ReactiveAuraFX] LovePulse: {(enabled ? "有効" : "無効")}");
        }

        public void SetIdleBloomEnabled(bool enabled)
        {
            enableIdleBloom = enabled;
            if (idleBloomEffect != null)
            {
                if (!enabled)
                    idleBloomEffect.ResetMotionTimer();
                idleBloomEffect.enabled = enabled;
            }
            Debug.Log($"[ReactiveAuraFX] IdleBloom: {(enabled ? "有効" : "無効")}");
        }

        public void ToggleAllEffects()
        {
            bool newState = !enableSystem;
            enableSystem = newState;
            
            SetEmotionAuraEnabled(newState && enableEmotionAura);
            SetHeartbeatGlowEnabled(newState && enableHeartbeatGlow);
            SetEyeFocusRayEnabled(newState && enableEyeFocusRay);
            SetLovePulseEnabled(newState && enableLovePulse);
            SetIdleBloomEnabled(newState && enableIdleBloom);
            
            Debug.Log($"[ReactiveAuraFX] 全エフェクト: {(newState ? "有効" : "無効")}");
        }

        // === Modular Avatar対応 ===
#if MA_VRCSDK3_AVATARS
        [Space(10)]
        [Header("🔗 Modular Avatar統合")]
        [Tooltip("Expression Menuからの制御を有効化")]
        public bool enableExpressionMenuControl = true;
        
        [Tooltip("Animatorパラメータとの連携を有効化")]
        public bool enableAnimatorParameters = true;
        
        [Tooltip("MA Merge Animatorを自動セットアップ")]
        public bool autoSetupMergeAnimator = true;

        [ContextMenu("Modular Avatar セットアップ")]
        public void SetupModularAvatar()
        {
            SetupModularAvatarComponents();
        }

        private void SetupModularAvatarComponents()
        {
            // Modular Avatar Merge Animator設定
            if (autoSetupMergeAnimator)
            {
                var mergeAnimator = GetComponent<ModularAvatarMergeAnimator>();
                if (mergeAnimator == null)
                {
                    mergeAnimator = gameObject.AddComponent<ModularAvatarMergeAnimator>();
                }
                
                mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                mergeAnimator.deleteAttachedAnimator = false;
                mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
                mergeAnimator.matchAvatarWriteDefaults = true;
                
                Debug.Log("[ReactiveAuraFX] Modular Avatar Merge Animator設定完了");
            }
            
            // Modular Avatar Parameters設定
            if (enableAnimatorParameters)
            {
                SetupModularAvatarParameters();
            }
            
            // Expression Menu設定
            if (enableExpressionMenuControl)
            {
                SetupExpressionMenuIntegration();
            }
        }

        private void SetupModularAvatarParameters()
        {
            var maParameters = GetComponent<ModularAvatarParameters>();
            if (maParameters == null)
            {
                maParameters = gameObject.AddComponent<ModularAvatarParameters>();
            }
            
            // 各エフェクト用パラメータを設定
            var paramList = new System.Collections.Generic.List<ParameterConfig>();
            
            if (enableEmotionAura)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/EmotionAura",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = enableEmotionAura ? 1f : 0f,
                    saved = true,
                    localOnly = false
                });
            }
            
            if (enableHeartbeatGlow)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/HeartbeatGlow",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = enableHeartbeatGlow ? 1f : 0f,
                    saved = true,
                    localOnly = false
                });
            }
            
            if (enableEyeFocusRay)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/EyeFocusRay",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = enableEyeFocusRay ? 1f : 0f,
                    saved = true,
                    localOnly = false
                });
            }
            
            if (enableLovePulse)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/LovePulse",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = enableLovePulse ? 1f : 0f,
                    saved = true,
                    localOnly = false
                });
            }
            
            if (enableIdleBloom)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/IdleBloom",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = enableIdleBloom ? 1f : 0f,
                    saved = true,
                    localOnly = false
                });
            }
            
            // 全体制御パラメータ
            paramList.Add(new ParameterConfig
            {
                nameOrPrefix = "ReactiveAuraFX/SystemEnabled",
                syncType = ParameterSyncType.Bool,
                defaultValue = enableSystem ? 1f : 0f,
                saved = true,
                localOnly = false
            });
            
            maParameters.parameters = paramList;
            
            Debug.Log("[ReactiveAuraFX] Modular Avatar Parameters設定完了");
        }

        private void SetupExpressionMenuIntegration()
        {
            var menuInstaller = GetComponent<ModularAvatarMenuInstaller>();
            if (menuInstaller == null)
            {
                menuInstaller = gameObject.AddComponent<ModularAvatarMenuInstaller>();
            }
            
            // Expression Menu用のMenuGroupを設定
            CreateReactiveAuraFXMenu(menuInstaller);
            
            Debug.Log("[ReactiveAuraFX] Expression Menu統合設定完了");
        }

        private void CreateReactiveAuraFXMenu(ModularAvatarMenuInstaller menuInstaller)
        {
            // ReactiveAuraFXメニューアイテム作成
            var menuGroup = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menuGroup.name = "ReactiveAuraFX Menu";
            
            var controls = new System.Collections.Generic.List<VRCExpressionsMenu.Control>();
            
            // 全体ON/OFF
            controls.Add(new VRCExpressionsMenu.Control
            {
                name = "🌟 ReactiveAuraFX",
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/SystemEnabled" }
            });
            
            // 各エフェクトのトグル
            if (enableEmotionAura)
            {
                controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💫 EmotionAura",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/EmotionAura" }
                });
            }
            
            if (enableHeartbeatGlow)
            {
                controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💓 HeartbeatGlow",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/HeartbeatGlow" }
                });
            }
            
            if (enableEyeFocusRay)
            {
                controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "👁️ EyeFocusRay",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/EyeFocusRay" }
                });
            }
            
            if (enableLovePulse)
            {
                controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💕 LovePulse",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/LovePulse" }
                });
            }
            
            if (enableIdleBloom)
            {
                controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "🌸 IdleBloom",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/IdleBloom" }
                });
            }
            
            menuGroup.controls = controls;
            menuInstaller.menuToAppend = menuGroup;
            
            // アセットとして保存
#if UNITY_EDITOR
            string assetPath = "Assets/ReactiveAuraFX/ReactiveAuraFX_Menu.asset";
            UnityEditor.AssetDatabase.CreateAsset(menuGroup, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
#endif

        // === Unity Event Functions ===
        void OnValidate()
        {
            // Inspector値変更時の同期
            if (Application.isPlaying && systemInitialized)
            {
                SynchronizeEffectSettings();
            }
        }

        void OnDestroy()
        {
            Debug.Log("[ReactiveAuraFX] ReactiveAuraFXSystem破棄");
        }

#if UNITY_EDITOR
        [ContextMenu("🔍 コンポーネント自動検出")]
        public void AutoDetectComponents()
        {
            AutoDetectAvatarComponents();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("🎬 全エフェクト再初期化")]
        public void ReinitializeAllEffects()
        {
            if (effectContainer != null)
            {
                DestroyImmediate(effectContainer);
            }
            systemInitialized = false;
            InitializeSystem();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
} 