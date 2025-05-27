#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;

#if MA_VRCSDK3_AVATARS
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace ReactiveAuraFX.Core
{
    /// <summary>
    /// ReactiveAuraFX完全自動インストーラー
    /// VRChat + Modular Avatar対応
    /// </summary>
    public static class ReactiveAuraFXInstaller
    {
        private const string MENU_PREFIX = "ReactiveAuraFX/";
        private const string SYSTEM_NAME = "ReactiveAuraFX_System";
        
        [MenuItem(MENU_PREFIX + "🌟 アバターにReactiveAuraFXを自動インストール", false, 0)]
        public static void AutoInstallToSelectedAvatar()
        {
            GameObject selectedObject = Selection.activeGameObject;
            
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("ReactiveAuraFX", 
                    "アバターのGameObjectを選択してから実行してください。", "OK");
                return;
            }
            
            VRCAvatarDescriptor avatarDescriptor = selectedObject.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor == null)
            {
                avatarDescriptor = selectedObject.GetComponentInChildren<VRCAvatarDescriptor>();
                if (avatarDescriptor == null)
                {
                    EditorUtility.DisplayDialog("ReactiveAuraFX", 
                        "選択されたオブジェクトまたはその子にVRCAvatarDescriptorが見つかりません。", "OK");
                    return;
                }
            }
            
            AutoInstallReactiveAuraFX(avatarDescriptor.gameObject, avatarDescriptor);
        }
        
        [MenuItem(MENU_PREFIX + "🌟 アバターにReactiveAuraFXを自動インストール", true)]
        public static bool ValidateAutoInstallToSelectedAvatar()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(MENU_PREFIX + "⚙️ カスタムインストール（設定選択）", false, 1)]
        public static void CustomInstallToSelectedAvatar()
        {
            ReactiveAuraFXInstallWindow.ShowWindow();
        }

        [MenuItem(MENU_PREFIX + "📦 ReactiveAuraFXプレハブ作成", false, 100)]
        public static void CreateReactiveAuraFXPrefab()
        {
            // プレハブ保存パス選択
            string path = EditorUtility.SaveFilePanel(
                "ReactiveAuraFXプレハブ保存", 
                "Assets/ReactiveAuraFX", 
                SYSTEM_NAME, 
                "prefab");
            
            if (string.IsNullOrEmpty(path)) return;
            
            path = FileUtil.GetProjectRelativePath(path);
            
            // ベースオブジェクト作成
            GameObject prefabObj = CreateReactiveAuraFXSystem(null, null);
            
            // プレハブとして保存
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabObj, path);
            
            // 作成したオブジェクトを削除
            Object.DestroyImmediate(prefabObj);
            
            if (prefab != null)
            {
                EditorGUIUtility.PingObject(prefab);
                EditorUtility.DisplayDialog("ReactiveAuraFX", 
                    $"ReactiveAuraFXプレハブが作成されました！\n\n" +
                    $"保存先: {path}\n\n" +
                    $"このプレハブをアバターの直下にドラッグ&ドロップして使用してください。", "OK");
                
                Debug.Log($"[ReactiveAuraFX] プレハブ作成完了: {path}");
            }
        }

        [MenuItem(MENU_PREFIX + "🔧 設定とトラブルシューティング", false, 200)]
        public static void OpenSettingsWindow()
        {
            ReactiveAuraFXSettingsWindow.ShowWindow();
        }

        [MenuItem(MENU_PREFIX + "📖 ドキュメントを開く", false, 300)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/your-repo/ReactiveAuraFX/wiki");
        }

        public static void AutoInstallReactiveAuraFX(GameObject avatarRoot, VRCAvatarDescriptor avatarDescriptor)
        {
            // 既存のReactiveAuraFXSystemをチェック
            ReactiveAuraFXSystem existingSystem = avatarRoot.GetComponentInChildren<ReactiveAuraFXSystem>();
            if (existingSystem != null)
            {
                bool replace = EditorUtility.DisplayDialog("ReactiveAuraFX", 
                    "既にReactiveAuraFXSystemが存在します。置き換えますか？", "置き換える", "キャンセル");
                
                if (!replace) return;
                
                Object.DestroyImmediate(existingSystem.gameObject);
            }
            
            // ReactiveAuraFXSystem作成
            GameObject reactiveAuraFXObj = CreateReactiveAuraFXSystem(avatarRoot, avatarDescriptor);
            
            // Modular Avatar完全セットアップ
#if MA_VRCSDK3_AVATARS
            SetupModularAvatarIntegration(reactiveAuraFXObj, avatarDescriptor);
#endif
            
            // オブジェクトを選択
            Selection.activeGameObject = reactiveAuraFXObj;
            
            // 成功メッセージ
            string message = $"ReactiveAuraFXが正常にインストールされました！\n\n" +
                           $"設定されたアバター: {avatarRoot.name}\n" +
                           $"作成されたオブジェクト: {reactiveAuraFXObj.name}\n\n";
            
#if MA_VRCSDK3_AVATARS
            message += "✅ Modular Avatar統合完了\n" +
                      "• パラメータ自動設定\n" +
                      "• メニュー自動統合\n" +
                      "• アニメーター自動マージ\n\n";
#else
            message += "⚠️ Modular Avatarが見つかりません\n" +
                      "より簡単なセットアップのためModular Avatarの導入を推奨します。\n\n";
#endif
            
            message += "Inspectorで各エフェクトの設定を調整してください。";
            
            EditorUtility.DisplayDialog("ReactiveAuraFX", message, "OK");
            
            // ログ出力
            Debug.Log($"[ReactiveAuraFX] {avatarRoot.name}にReactiveAuraFXをインストール完了");
            
            // エディタを更新
            EditorUtility.SetDirty(reactiveAuraFXObj);
            EditorUtility.SetDirty(avatarRoot);
        }

        private static GameObject CreateReactiveAuraFXSystem(GameObject avatarRoot, VRCAvatarDescriptor avatarDescriptor)
        {
            // ReactiveAuraFXSystemオブジェクト作成
            GameObject reactiveAuraFXObj = new GameObject(SYSTEM_NAME);
            if (avatarRoot != null)
            {
                reactiveAuraFXObj.transform.SetParent(avatarRoot.transform);
            }
            reactiveAuraFXObj.transform.localPosition = Vector3.zero;
            reactiveAuraFXObj.transform.localRotation = Quaternion.identity;
            reactiveAuraFXObj.transform.localScale = Vector3.one;
            
            // ReactiveAuraFXSystemコンポーネント追加
            ReactiveAuraFXSystem auraSystem = reactiveAuraFXObj.AddComponent<ReactiveAuraFXSystem>();
            
            // 基本設定
            auraSystem.enableSystem = true;
            auraSystem.vrchatCompatibilityMode = true;
            auraSystem.autoFixSafeMode = true;
            
            // 自動設定
            if (avatarDescriptor != null)
            {
                auraSystem.avatarDescriptor = avatarDescriptor;
                auraSystem.faceAnimator = avatarDescriptor.GetComponent<Animator>();
                
                // ボーン自動検出
                Animator animator = avatarDescriptor.GetComponent<Animator>();
                if (animator != null)
                {
                    auraSystem.headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
                    auraSystem.chestTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
                    if (auraSystem.chestTransform == null)
                    {
                        auraSystem.chestTransform = animator.GetBoneTransform(HumanBodyBones.Spine);
                    }
                }
            }
            
            return reactiveAuraFXObj;
        }

#if MA_VRCSDK3_AVATARS
        private static void SetupModularAvatarIntegration(GameObject reactiveAuraFXObj, VRCAvatarDescriptor avatarDescriptor)
        {
            var auraSystem = reactiveAuraFXObj.GetComponent<ReactiveAuraFXSystem>();
            
            // Modular Avatar Parameters追加
            SetupMAParameters(reactiveAuraFXObj, auraSystem);
            
            // Modular Avatar Menu Installer追加
            SetupMAMenuInstaller(reactiveAuraFXObj, auraSystem);
            
            // Modular Avatar Merge Animator追加
            SetupMAMergeAnimator(reactiveAuraFXObj);
            
            Debug.Log("[ReactiveAuraFX] Modular Avatar完全統合完了");
        }

        private static void SetupMAParameters(GameObject obj, ReactiveAuraFXSystem system)
        {
            var maParameters = obj.GetComponent<ModularAvatarParameters>();
            if (maParameters == null)
            {
                maParameters = obj.AddComponent<ModularAvatarParameters>();
            }
            
            var paramList = new List<ParameterConfig>();
            
            // 全体制御パラメータ
            paramList.Add(new ParameterConfig
            {
                nameOrPrefix = "ReactiveAuraFX/SystemEnabled",
                syncType = ParameterSyncType.Bool,
                defaultValue = system.enableSystem ? 1f : 0f,
                saved = true,
                localOnly = false
            });
            
            // 各エフェクト制御パラメータ
            if (system.enableEmotionAura)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/EmotionAura",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 1f,
                    saved = true,
                    localOnly = false
                });
                
                // 表情パラメータ
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "Emotion",
                    syncType = ParameterSyncType.Int,
                    defaultValue = 0f,
                    saved = false,
                    localOnly = false
                });
            }
            
            if (system.enableHeartbeatGlow)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/HeartbeatGlow",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 1f,
                    saved = true,
                    localOnly = false
                });
                
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "HeartbeatTrigger",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 0f,
                    saved = false,
                    localOnly = false
                });
            }
            
            if (system.enableEyeFocusRay)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/EyeFocusRay",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 1f,
                    saved = true,
                    localOnly = false
                });
                
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "EyeBeamForce",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 0f,
                    saved = false,
                    localOnly = false
                });
            }
            
            if (system.enableLovePulse)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/LovePulse",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 1f,
                    saved = true,
                    localOnly = false
                });
                
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "LovePulseTrigger",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 0f,
                    saved = false,
                    localOnly = false
                });
            }
            
            if (system.enableIdleBloom)
            {
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "ReactiveAuraFX/IdleBloom",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 1f,
                    saved = true,
                    localOnly = false
                });
                
                paramList.Add(new ParameterConfig
                {
                    nameOrPrefix = "IdleBloomTrigger",
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 0f,
                    saved = false,
                    localOnly = false
                });
            }
            
            maParameters.parameters = paramList;
        }

        private static void SetupMAMenuInstaller(GameObject obj, ReactiveAuraFXSystem system)
        {
            var menuInstaller = obj.GetComponent<ModularAvatarMenuInstaller>();
            if (menuInstaller == null)
            {
                menuInstaller = obj.AddComponent<ModularAvatarMenuInstaller>();
            }
            
            // Expression Menu作成
            var menu = CreateReactiveAuraFXMenu(system);
            menuInstaller.menuToAppend = menu;
            menuInstaller.installTargetMenu = null; // ルートメニューに追加
        }

        private static VRCExpressionsMenu CreateReactiveAuraFXMenu(ReactiveAuraFXSystem system)
        {
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = "ReactiveAuraFX Menu";
            
            var controls = new List<VRCExpressionsMenu.Control>();
            
            // 全体ON/OFF
            controls.Add(new VRCExpressionsMenu.Control
            {
                name = "🌟 ReactiveAuraFX",
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/SystemEnabled" }
            });
            
            // サブメニュー作成
            var subMenu = CreateEffectsSubMenu(system);
            
            // サブメニューへのリンク
            controls.Add(new VRCExpressionsMenu.Control
            {
                name = "⚙️ エフェクト設定",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = subMenu
            });
            
            menu.controls = controls;
            
            // アセットとして保存
            SaveMenuAssets(menu, subMenu);
            
            return menu;
        }

        private static VRCExpressionsMenu CreateEffectsSubMenu(ReactiveAuraFXSystem system)
        {
            var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            subMenu.name = "ReactiveAuraFX Effects";
            
            var subControls = new List<VRCExpressionsMenu.Control>();
            
            if (system.enableEmotionAura)
            {
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💫 EmotionAura",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/EmotionAura" }
                });
            }
            
            if (system.enableHeartbeatGlow)
            {
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💓 HeartbeatGlow",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/HeartbeatGlow" }
                });
                
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💓 Heartbeat Trigger",
                    type = VRCExpressionsMenu.Control.ControlType.Button,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "HeartbeatTrigger" }
                });
            }
            
            if (system.enableEyeFocusRay)
            {
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "👁️ EyeFocusRay",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/EyeFocusRay" }
                });
                
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "👁️ Force Eye Beam",
                    type = VRCExpressionsMenu.Control.ControlType.Button,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "EyeBeamForce" }
                });
            }
            
            if (system.enableLovePulse)
            {
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💕 LovePulse",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/LovePulse" }
                });
                
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💕 Love Trigger",
                    type = VRCExpressionsMenu.Control.ControlType.Button,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "LovePulseTrigger" }
                });
            }
            
            if (system.enableIdleBloom)
            {
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "🌸 IdleBloom",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/IdleBloom" }
                });
                
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "🌸 Force Bloom",
                    type = VRCExpressionsMenu.Control.ControlType.Button,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "IdleBloomTrigger" }
                });
            }
            
            subMenu.controls = subControls;
            
            return subMenu;
        }

        private static void SaveMenuAssets(VRCExpressionsMenu menu, VRCExpressionsMenu subMenu)
        {
            string menuPath = "Assets/ReactiveAuraFX/Generated/ReactiveAuraFX_Menu.asset";
            string subMenuPath = "Assets/ReactiveAuraFX/Generated/ReactiveAuraFX_SubMenu.asset";
            
            // ディレクトリ作成
            string dirPath = "Assets/ReactiveAuraFX/Generated";
            if (!AssetDatabase.IsValidFolder(dirPath))
            {
                AssetDatabase.CreateFolder("Assets/ReactiveAuraFX", "Generated");
            }
            
            AssetDatabase.CreateAsset(subMenu, subMenuPath);
            AssetDatabase.CreateAsset(menu, menuPath);
            AssetDatabase.SaveAssets();
        }

        private static void SetupMAMergeAnimator(GameObject obj)
        {
            var mergeAnimator = obj.GetComponent<ModularAvatarMergeAnimator>();
            if (mergeAnimator == null)
            {
                mergeAnimator = obj.AddComponent<ModularAvatarMergeAnimator>();
            }
            
            mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            mergeAnimator.deleteAttachedAnimator = false;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            mergeAnimator.matchAvatarWriteDefaults = true;
            
            // 基本的なAnimatorController作成
            CreateReactiveAuraFXAnimatorController(mergeAnimator);
        }

        private static void CreateReactiveAuraFXAnimatorController(ModularAvatarMergeAnimator mergeAnimator)
        {
            string controllerPath = "Assets/ReactiveAuraFX/Generated/ReactiveAuraFX_Animator.controller";
            
            // ディレクトリ作成
            string dirPath = "Assets/ReactiveAuraFX/Generated";
            if (!AssetDatabase.IsValidFolder(dirPath))
            {
                AssetDatabase.CreateFolder("Assets/ReactiveAuraFX", "Generated");
            }
            
            // 既存のコントローラーをチェック
            var existingController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            if (existingController != null)
            {
                mergeAnimator.animator = existingController;
                return;
            }
            
            // 新しいAnimatorController作成
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            // パラメータ追加
            controller.AddParameter("ReactiveAuraFX/SystemEnabled", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/EmotionAura", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/HeartbeatGlow", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/EyeFocusRay", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/LovePulse", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/IdleBloom", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Emotion", AnimatorControllerParameterType.Int);
            controller.AddParameter("HeartbeatTrigger", AnimatorControllerParameterType.Bool);
            controller.AddParameter("EyeBeamForce", AnimatorControllerParameterType.Bool);
            controller.AddParameter("LovePulseTrigger", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IdleBloomTrigger", AnimatorControllerParameterType.Bool);
            
            // レイヤー作成
            var layer = new UnityEditor.Animations.AnimatorControllerLayer
            {
                name = "ReactiveAuraFX Layer",
                defaultWeight = 1f,
                stateMachine = new UnityEditor.Animations.AnimatorStateMachine()
            };
            
            controller.AddLayer(layer);
            
            // 基本状態を作成
            var idleState = layer.stateMachine.AddState("Idle");
            layer.stateMachine.defaultState = idleState;
            
            mergeAnimator.animator = controller;
            
            AssetDatabase.SaveAssets();
        }
#endif
    }

    /// <summary>
    /// ReactiveAuraFXカスタムインストールウィンドウ
    /// </summary>
    public class ReactiveAuraFXInstallWindow : EditorWindow
    {
        private VRCAvatarDescriptor targetAvatar;
        private bool enableEmotionAura = true;
        private bool enableHeartbeatGlow = true;
        private bool enableEyeFocusRay = true;
        private bool enableLovePulse = true;
        private bool enableIdleBloom = true;
        private bool autoFixSafeMode = true;
        private Vector2 scrollPos;
        
        public static void ShowWindow()
        {
            var window = GetWindow<ReactiveAuraFXInstallWindow>("ReactiveAuraFX インストール");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }
        
        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("🌟 ReactiveAuraFX カスタムインストール", EditorStyles.largeLabel);
            GUILayout.Space(10);
            
            // ターゲットアバター選択
            GUILayout.Label("📋 対象アバター", EditorStyles.boldLabel);
            targetAvatar = EditorGUILayout.ObjectField("Target Avatar", targetAvatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            
            if (targetAvatar == null && Selection.activeGameObject != null)
            {
                var descriptor = Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();
                if (descriptor == null)
                {
                    descriptor = Selection.activeGameObject.GetComponentInChildren<VRCAvatarDescriptor>();
                }
                if (descriptor != null)
                {
                    targetAvatar = descriptor;
                }
            }
            
            GUILayout.Space(10);
            
            // エフェクト選択
            GUILayout.Label("🎬 インストールするエフェクト", EditorStyles.boldLabel);
            enableEmotionAura = EditorGUILayout.Toggle("💫 EmotionAura - 表情連動オーラ", enableEmotionAura);
            enableHeartbeatGlow = EditorGUILayout.Toggle("💓 HeartbeatGlow - 鼓動波紋光", enableHeartbeatGlow);
            enableEyeFocusRay = EditorGUILayout.Toggle("👁️ EyeFocusRay - 視線ビーム", enableEyeFocusRay);
            enableLovePulse = EditorGUILayout.Toggle("💕 LovePulse - 愛情パーティクル", enableLovePulse);
            enableIdleBloom = EditorGUILayout.Toggle("🌸 IdleBloom - 静寂の花", enableIdleBloom);
            
            GUILayout.Space(10);
            
            // 高度な設定
            GUILayout.Label("⚙️ 高度な設定", EditorStyles.boldLabel);
            autoFixSafeMode = EditorGUILayout.Toggle("AutoFIX安全モード", autoFixSafeMode);
            
            GUILayout.Space(10);
            
            // Modular Avatar状態表示
#if MA_VRCSDK3_AVATARS
            EditorGUILayout.HelpBox("✅ Modular Avatar検出済み\n完全統合機能が利用可能です。", MessageType.Info);
#else
            EditorGUILayout.HelpBox("⚠️ Modular Avatarが見つかりません\nより簡単なセットアップのためModular Avatarの導入を推奨します。", MessageType.Warning);
#endif
            
            GUILayout.Space(10);
            
            // インストールボタン
            GUI.enabled = targetAvatar != null;
            
            if (GUILayout.Button("🚀 インストール実行", GUILayout.Height(40)))
            {
                PerformCustomInstall();
                Close();
            }
            
            GUI.enabled = true;
            
            GUILayout.Space(10);
            
            // 情報表示
            EditorGUILayout.HelpBox(
                "カスタムインストールでは、必要なエフェクトのみを選択してインストールできます。\n" +
                "後からInspectorで設定を変更することも可能です。",
                MessageType.Info);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void PerformCustomInstall()
        {
            if (targetAvatar == null) return;
            
            // 既存のReactiveAuraFXSystemをチェック
            ReactiveAuraFXSystem existingSystem = targetAvatar.GetComponentInChildren<ReactiveAuraFXSystem>();
            if (existingSystem != null)
            {
                bool replace = EditorUtility.DisplayDialog("ReactiveAuraFX", 
                    "既にReactiveAuraFXSystemが存在します。置き換えますか？", "置き換える", "キャンセル");
                
                if (!replace) return;
                
                Object.DestroyImmediate(existingSystem.gameObject);
            }
            
            // ReactiveAuraFXSystem作成
            GameObject reactiveAuraFXObj = new GameObject("ReactiveAuraFX_System");
            reactiveAuraFXObj.transform.SetParent(targetAvatar.transform);
            reactiveAuraFXObj.transform.localPosition = Vector3.zero;
            reactiveAuraFXObj.transform.localRotation = Quaternion.identity;
            reactiveAuraFXObj.transform.localScale = Vector3.one;
            
            // ReactiveAuraFXSystemコンポーネント追加
            ReactiveAuraFXSystem auraSystem = reactiveAuraFXObj.AddComponent<ReactiveAuraFXSystem>();
            
            // カスタム設定適用
            auraSystem.enableSystem = true;
            auraSystem.vrchatCompatibilityMode = true;
            auraSystem.autoFixSafeMode = autoFixSafeMode;
            auraSystem.enableEmotionAura = enableEmotionAura;
            auraSystem.enableHeartbeatGlow = enableHeartbeatGlow;
            auraSystem.enableEyeFocusRay = enableEyeFocusRay;
            auraSystem.enableLovePulse = enableLovePulse;
            auraSystem.enableIdleBloom = enableIdleBloom;
            
            // 自動設定
            auraSystem.avatarDescriptor = targetAvatar;
            auraSystem.faceAnimator = targetAvatar.GetComponent<Animator>();
            
            // ボーン自動検出
            Animator animator = targetAvatar.GetComponent<Animator>();
            if (animator != null)
            {
                auraSystem.headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
                auraSystem.chestTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (auraSystem.chestTransform == null)
                {
                    auraSystem.chestTransform = animator.GetBoneTransform(HumanBodyBones.Spine);
                }
            }
            
#if MA_VRCSDK3_AVATARS
            // Modular Avatar統合
            ReactiveAuraFXInstaller.SetupModularAvatarIntegration(reactiveAuraFXObj, targetAvatar);
#endif
            
            // 完了メッセージ
            EditorUtility.DisplayDialog("ReactiveAuraFX", 
                "カスタムインストールが完了しました！\n\n" +
                "選択されたエフェクトがインストールされ、Modular Avatar統合も完了しています。", "OK");
            
            // オブジェクトを選択
            Selection.activeGameObject = reactiveAuraFXObj;
            
            Debug.Log($"[ReactiveAuraFX] カスタムインストール完了: {targetAvatar.name}");
        }
    }

    /// <summary>
    /// ReactiveAuraFX設定・トラブルシューティングウィンドウ
    /// </summary>
    public class ReactiveAuraFXSettingsWindow : EditorWindow
    {
        private Vector2 scrollPos;
        
        public static void ShowWindow()
        {
            var window = GetWindow<ReactiveAuraFXSettingsWindow>("ReactiveAuraFX設定");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }
        
        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("🌟 ReactiveAuraFX 設定とトラブルシューティング", EditorStyles.largeLabel);
            GUILayout.Space(10);
            
            // システム状態
            GUILayout.Label("📊 システム状態", EditorStyles.boldLabel);
            CheckAndDisplaySystemStatus();
            
            GUILayout.Space(10);
            
            // 基本設定
            GUILayout.Label("📋 基本設定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. アバターのルートオブジェクトを選択\n" +
                "2. メニューから「ReactiveAuraFX > アバターにReactiveAuraFXを自動インストール」を実行\n" +
                "3. Inspectorで各エフェクトの設定を調整",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // AutoFIX対策
            GUILayout.Label("🛡️ AutoFIX対策", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• オブジェクト名に「ReactiveAuraFX」が含まれるよう命名\n" +
                "• エフェクトオブジェクトは「EditorOnly」タグを設定\n" +
                "• Awakeでの自動安全設定により削除を回避",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // トラブルシューティング
            GUILayout.Label("🔧 トラブルシューティング", EditorStyles.boldLabel);
            
            if (GUILayout.Button("全ReactiveAuraFXオブジェクトを検索"))
            {
                SearchAllReactiveAuraFXObjects();
            }
            
            if (GUILayout.Button("VRChat SDK状態確認"))
            {
                CheckVRChatSDKStatus();
            }
            
            if (GUILayout.Button("Modular Avatar状態確認"))
            {
                CheckModularAvatarStatus();
            }
            
            GUILayout.Space(10);
            
            // パフォーマンス設定
            GUILayout.Label("⚡ パフォーマンス設定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "VRChatでのパフォーマンス最適化:\n" +
                "• パーティクル数を控えめに設定\n" +
                "• 不要なエフェクトは無効化\n" +
                "• AutoFIX安全モードを有効化",
                MessageType.Info);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void CheckAndDisplaySystemStatus()
        {
            var systems = FindObjectsOfType<ReactiveAuraFXSystem>();
            
            if (systems.Length == 0)
            {
                EditorGUILayout.HelpBox("ReactiveAuraFXSystemが見つかりません。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"ReactiveAuraFXSystem: {systems.Length}個検出", MessageType.Info);
            }
            
            CheckVRChatSDKStatus();
            CheckModularAvatarStatus();
        }
        
        private void SearchAllReactiveAuraFXObjects()
        {
            var systems = FindObjectsOfType<ReactiveAuraFXSystem>();
            Debug.Log($"[ReactiveAuraFX] 検索結果: {systems.Length}個のシステムが見つかりました");
            
            foreach (var system in systems)
            {
                Debug.Log($"- {system.gameObject.name} (親: {system.transform.parent?.name ?? "なし"})");
            }
            
            if (systems.Length > 0)
            {
                Selection.objects = System.Array.ConvertAll(systems, s => s.gameObject);
                EditorUtility.DisplayDialog("ReactiveAuraFX", 
                    $"{systems.Length}個のReactiveAuraFXSystemが見つかりました。\nヒエラルキーで選択されています。", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("ReactiveAuraFX", 
                    "ReactiveAuraFXSystemが見つかりませんでした。", "OK");
            }
        }
        
        private void CheckVRChatSDKStatus()
        {
#if VRC_SDK_VRCSDK3
            EditorGUILayout.HelpBox("✅ VRChat SDK3検出済み", MessageType.Info);
#else
            EditorGUILayout.HelpBox("❌ VRChat SDK3が見つかりません\nVRChat Creator Companionからインポートしてください。", MessageType.Error);
#endif
        }
        
        private void CheckModularAvatarStatus()
        {
#if MA_VRCSDK3_AVATARS
            EditorGUILayout.HelpBox("✅ Modular Avatar検出済み", MessageType.Info);
#else
            EditorGUILayout.HelpBox("⚠️ Modular Avatarが見つかりません\n完全統合機能を利用するには、Modular Avatarの導入を推奨します。", MessageType.Warning);
#endif
        }
    }
}
#endif 