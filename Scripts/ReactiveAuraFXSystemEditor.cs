#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using ReactiveAuraFX.Core;
using VRC.SDK3.Avatars.Components;

#if MA_VRCSDK3_AVATARS
using nadena.dev.modular_avatar.core;
#endif

namespace ReactiveAuraFX.Editor
{
    /// <summary>
    /// ReactiveAuraFXSystem用カスタムエディタ
    /// VRChatアバター向け魅力的エフェクトシステムの設定UI
    /// </summary>
    [CustomEditor(typeof(ReactiveAuraFXSystem))]
    public class ReactiveAuraFXSystemEditor : UnityEditor.Editor
    {
        private ReactiveAuraFXSystem _target;
        private bool _showBasicSettings = true;
        private bool _showEmotionAura = true;
        private bool _showHeartbeatGlow = true;
        private bool _showEyeFocusRay = true;
        private bool _showLovePulse = true;
        private bool _showIdleBloom = true;
        private bool _showAnimatorSettings = true;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _enableButtonStyle;

        void OnEnable()
        {
            _target = (ReactiveAuraFXSystem)target;
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();
            
            // ヘッダー
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            // 基本設定
            DrawBasicSettings();
            
            EditorGUILayout.Space(10);
            
            // 各エフェクト設定
            DrawEffectSettings();
            
            EditorGUILayout.Space(10);
            
            // アニメーター設定
            DrawAnimatorSettings();
            
            EditorGUILayout.Space(10);
            
            // ユーティリティボタン
            DrawUtilityButtons();
            
            // 変更を適用
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_target);
            }
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel);
                _headerStyle.fontSize = 16;
                _headerStyle.alignment = TextAnchor.MiddleCenter;
                _headerStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);
            }
            
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.padding = new RectOffset(10, 10, 5, 5);
                _boxStyle.margin = new RectOffset(0, 0, 5, 5);
            }
            
            if (_enableButtonStyle == null)
            {
                _enableButtonStyle = new GUIStyle(GUI.skin.button);
                _enableButtonStyle.fontSize = 12;
                _enableButtonStyle.fontStyle = FontStyle.Bold;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            GUILayout.Label("🌟 Reactive Aura FX", _headerStyle);
            GUILayout.Label("魅力拡張アバター：VRChat用エフェクトシステム", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(5);
            
            // システム有効化トグル
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = _target.enableSystem ? Color.green : Color.red;
            
            if (GUILayout.Button(_target.enableSystem ? "✅ システム有効" : "❌ システム無効", 
                _enableButtonStyle, GUILayout.Width(150)))
            {
                _target.enableSystem = !_target.enableSystem;
            }
            
            GUI.backgroundColor = originalColor;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawBasicSettings()
        {
            _showBasicSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showBasicSettings, "🎯 基本システム設定");
            
            if (_showBasicSettings)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                _target.enableSystem = EditorGUILayout.Toggle("システム全体有効", _target.enableSystem);
                _target.vrchatCompatibilityMode = EditorGUILayout.Toggle("VRChat互換性モード", _target.vrchatCompatibilityMode);
                _target.debugMode = EditorGUILayout.Toggle("デバッグモード", _target.debugMode);
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawEffectSettings()
        {
            // EmotionAura
            DrawEmotionAuraSettings();
            EditorGUILayout.Space(5);
            
            // HeartbeatGlow
            DrawHeartbeatGlowSettings();
            EditorGUILayout.Space(5);
            
            // EyeFocusRay
            DrawEyeFocusRaySettings();
            EditorGUILayout.Space(5);
            
            // LovePulse
            DrawLovePulseSettings();
            EditorGUILayout.Space(5);
            
            // IdleBloom
            DrawIdleBloomSettings();
        }

        private void DrawEmotionAuraSettings()
        {
            _showEmotionAura = EditorGUILayout.BeginFoldoutHeaderGroup(_showEmotionAura, "💫 EmotionAura - 表情連動オーラ");
            
            if (_showEmotionAura)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                _target.enableEmotionAura = EditorGUILayout.Toggle("EmotionAura有効", _target.enableEmotionAura);
                
                if (_target.enableEmotionAura)
                {
                    _target.auraBaseIntensity = EditorGUILayout.Slider("オーラ基本強度", _target.auraBaseIntensity, 0f, 2f);
                    _target.auraRange = EditorGUILayout.Slider("オーラ範囲", _target.auraRange, 0.5f, 5f);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawHeartbeatGlowSettings()
        {
            _showHeartbeatGlow = EditorGUILayout.BeginFoldoutHeaderGroup(_showHeartbeatGlow, "💓 HeartbeatGlow - 鼓動波紋光");
            
            if (_showHeartbeatGlow)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                _target.enableHeartbeatGlow = EditorGUILayout.Toggle("HeartbeatGlow有効", _target.enableHeartbeatGlow);
                
                if (_target.enableHeartbeatGlow)
                {
                    _target.heartbeatSpeed = EditorGUILayout.Slider("鼓動速度", _target.heartbeatSpeed, 0.5f, 3f);
                    _target.heartbeatIntensity = EditorGUILayout.Slider("波紋強度", _target.heartbeatIntensity, 0.1f, 2f);
                    _target.heartbeatColor = EditorGUILayout.ColorField("波紋色", _target.heartbeatColor);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawEyeFocusRaySettings()
        {
            _showEyeFocusRay = EditorGUILayout.BeginFoldoutHeaderGroup(_showEyeFocusRay, "👁️ EyeFocusRay - 視線ビーム");
            
            if (_showEyeFocusRay)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                _target.enableEyeFocusRay = EditorGUILayout.Toggle("EyeFocusRay有効", _target.enableEyeFocusRay);
                
                if (_target.enableEyeFocusRay)
                {
                    _target.eyeRayLength = EditorGUILayout.Slider("ビーム長さ", _target.eyeRayLength, 1f, 10f);
                    _target.eyeRayThickness = EditorGUILayout.Slider("ビーム太さ", _target.eyeRayThickness, 0.01f, 0.1f);
                    _target.eyeRayColor = EditorGUILayout.ColorField("ビーム色", _target.eyeRayColor);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawLovePulseSettings()
        {
            _showLovePulse = EditorGUILayout.BeginFoldoutHeaderGroup(_showLovePulse, "💕 LovePulse - 愛情パーティクル");
            
            if (_showLovePulse)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                _target.enableLovePulse = EditorGUILayout.Toggle("LovePulse有効", _target.enableLovePulse);
                
                if (_target.enableLovePulse)
                {
                    _target.loveDetectionDistance = EditorGUILayout.Slider("反応距離", _target.loveDetectionDistance, 1f, 10f);
                    _target.loveParticleCount = EditorGUILayout.IntSlider("パーティクル数", _target.loveParticleCount, 5, 50);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawIdleBloomSettings()
        {
            _showIdleBloom = EditorGUILayout.BeginFoldoutHeaderGroup(_showIdleBloom, "🌸 IdleBloom - 静寂の花");
            
            if (_showIdleBloom)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                _target.enableIdleBloom = EditorGUILayout.Toggle("IdleBloom有効", _target.enableIdleBloom);
                
                if (_target.enableIdleBloom)
                {
                    _target.idleTimeThreshold = EditorGUILayout.Slider("静止判定時間", _target.idleTimeThreshold, 5f, 30f);
                    _target.bloomGrowthSpeed = EditorGUILayout.Slider("花成長速度", _target.bloomGrowthSpeed, 0.1f, 2f);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawAnimatorSettings()
        {
            _showAnimatorSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showAnimatorSettings, "🎛️ アニメーター・表情制御");
            
            if (_showAnimatorSettings)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                
                _target.avatarDescriptor = EditorGUILayout.ObjectField("アバターディスクリプター", 
                    _target.avatarDescriptor, typeof(VRC.SDK3.Avatars.Components.VRCAvatarDescriptor), true) 
                    as VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
                
                _target.faceAnimator = EditorGUILayout.ObjectField("表情アニメーター", 
                    _target.faceAnimator, typeof(Animator), true) as Animator;
                
                _target.headTransform = EditorGUILayout.ObjectField("頭Transform", 
                    _target.headTransform, typeof(Transform), true) as Transform;
                
                _target.chestTransform = EditorGUILayout.ObjectField("胸Transform", 
                    _target.chestTransform, typeof(Transform), true) as Transform;
                
                EditorGUILayout.Space(5);
                
                // 自動検出ボタン
                if (GUILayout.Button("🔍 コンポーネント自動検出"))
                {
                    AutoDetectComponents();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawUtilityButtons()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            GUILayout.Label("🛠️ ユーティリティ", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // 全エフェクト有効化
            if (GUILayout.Button("✅ 全エフェクト有効"))
            {
                _target.enableEmotionAura = true;
                _target.enableHeartbeatGlow = true;
                _target.enableEyeFocusRay = true;
                _target.enableLovePulse = true;
                _target.enableIdleBloom = true;
            }
            
            // 全エフェクト無効化
            if (GUILayout.Button("❌ 全エフェクト無効"))
            {
                _target.enableEmotionAura = false;
                _target.enableHeartbeatGlow = false;
                _target.enableEyeFocusRay = false;
                _target.enableLovePulse = false;
                _target.enableIdleBloom = false;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // プレハブ作成
            if (GUILayout.Button("📦 ReactiveAuraFXプレハブ作成"))
            {
                CreateReactiveAuraFXPrefab();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void AutoDetectComponents()
        {
            if (_target.avatarDescriptor == null)
            {
                _target.avatarDescriptor = _target.GetComponentInParent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                if (_target.avatarDescriptor == null)
                {
                    _target.avatarDescriptor = FindObjectOfType<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                }
            }
            
            if (_target.faceAnimator == null && _target.avatarDescriptor != null)
            {
                _target.faceAnimator = _target.avatarDescriptor.GetComponent<Animator>();
            }
            
            if (_target.faceAnimator != null)
            {
                if (_target.headTransform == null)
                {
                    _target.headTransform = _target.faceAnimator.GetBoneTransform(HumanBodyBones.Head);
                }
                
                if (_target.chestTransform == null)
                {
                    _target.chestTransform = _target.faceAnimator.GetBoneTransform(HumanBodyBones.Chest);
                    if (_target.chestTransform == null)
                    {
                        _target.chestTransform = _target.faceAnimator.GetBoneTransform(HumanBodyBones.Spine);
                    }
                }
            }
            
            EditorUtility.SetDirty(_target);
            Debug.Log("[ReactiveAuraFX] コンポーネント自動検出完了");
        }

        private void CreateReactiveAuraFXPrefab()
        {
            string path = EditorUtility.SaveFilePanel("ReactiveAuraFXプレハブ保存", "Assets", "ReactiveAuraFX", "prefab");
            
            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
                
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(_target.gameObject, path);
                
                if (prefab != null)
                {
                    EditorGUIUtility.PingObject(prefab);
                    Debug.Log($"[ReactiveAuraFX] プレハブ作成完了: {path}");
                }
                else
                {
                    Debug.LogError("[ReactiveAuraFX] プレハブ作成に失敗しました");
                }
            }
        }
    }
}
#endif 