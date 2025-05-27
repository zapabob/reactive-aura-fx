#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

#if MA_VRCSDK3_AVATARS
using nadena.dev.modular_avatar.core;
#endif

namespace ReactiveAuraFX.Core
{
    /// <summary>
    /// ReactiveAuraFX一発インストーラー
    /// VRChat + Modular Avatar対応
    /// </summary>
    public static class ReactiveAuraFXInstaller
    {
        [MenuItem("ReactiveAuraFX/🌟 アバターにReactiveAuraFXを追加", false, 0)]
        public static void InstallToSelectedAvatar()
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
                EditorUtility.DisplayDialog("ReactiveAuraFX", 
                    "選択されたオブジェクトにVRCAvatarDescriptorが見つかりません。", "OK");
                return;
            }
            
            InstallReactiveAuraFX(selectedObject, avatarDescriptor);
        }
        
        [MenuItem("ReactiveAuraFX/🌟 アバターにReactiveAuraFXを追加", true)]
        public static bool ValidateInstallToSelectedAvatar()
        {
            return Selection.activeGameObject != null;
        }

        public static void InstallReactiveAuraFX(GameObject avatarRoot, VRCAvatarDescriptor avatarDescriptor)
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
            
            // ReactiveAuraFXSystemオブジェクト作成
            GameObject reactiveAuraFXObj = new GameObject("ReactiveAuraFX_System");
            reactiveAuraFXObj.transform.SetParent(avatarRoot.transform);
            reactiveAuraFXObj.transform.localPosition = Vector3.zero;
            reactiveAuraFXObj.transform.localRotation = Quaternion.identity;
            reactiveAuraFXObj.transform.localScale = Vector3.one;
            
            // ReactiveAuraFXSystemコンポーネント追加
            ReactiveAuraFXSystem auraSystem = reactiveAuraFXObj.AddComponent<ReactiveAuraFXSystem>();
            
            // 自動設定
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
            
            // Modular Avatar対応
#if MA_VRCSDK3_AVATARS
            SetupModularAvatarIntegration(reactiveAuraFXObj, avatarDescriptor);
#endif
            
            // オブジェクトを選択
            Selection.activeGameObject = reactiveAuraFXObj;
            
            // 成功メッセージ
            EditorUtility.DisplayDialog("ReactiveAuraFX", 
                $"ReactiveAuraFXが正常にインストールされました！\n\n" +
                $"設定されたアバター: {avatarRoot.name}\n" +
                $"作成されたオブジェクト: {reactiveAuraFXObj.name}\n\n" +
                $"Inspectorで各エフェクトの設定を調整してください。", "OK");
            
            // ログ出力
            Debug.Log($"[ReactiveAuraFX] {avatarRoot.name}にReactiveAuraFXをインストール完了");
            
            // エディタを更新
            EditorUtility.SetDirty(reactiveAuraFXObj);
            EditorUtility.SetDirty(avatarRoot);
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
            
            var paramList = new System.Collections.Generic.List<ParameterConfig>();
            
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
            
            var controls = new System.Collections.Generic.List<VRCExpressionsMenu.Control>();
            
            // 全体ON/OFF
            controls.Add(new VRCExpressionsMenu.Control
            {
                name = "🌟 ReactiveAuraFX",
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/SystemEnabled" }
            });
            
            // サブメニュー作成
            var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            subMenu.name = "ReactiveAuraFX Effects";
            
            var subControls = new System.Collections.Generic.List<VRCExpressionsMenu.Control>();
            
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
            }
            
            if (system.enableLovePulse)
            {
                subControls.Add(new VRCExpressionsMenu.Control
                {
                    name = "💕 LovePulse",
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = "ReactiveAuraFX/LovePulse" }
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
            }
            
            subMenu.controls = subControls;
            
            // サブメニューへのリンク
            controls.Add(new VRCExpressionsMenu.Control
            {
                name = "⚙️ エフェクト設定",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = subMenu
            });
            
            menu.controls = controls;
            
            // アセットとして保存
            string menuPath = "Assets/ReactiveAuraFX/Generated/ReactiveAuraFX_Menu.asset";
            string subMenuPath = "Assets/ReactiveAuraFX/Generated/ReactiveAuraFX_SubMenu.asset";
            
            // ディレクトリ作成
            string dirPath = "Assets/ReactiveAuraFX/Generated";
            if (!UnityEditor.AssetDatabase.IsValidFolder(dirPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/ReactiveAuraFX", "Generated");
            }
            
            UnityEditor.AssetDatabase.CreateAsset(subMenu, subMenuPath);
            UnityEditor.AssetDatabase.CreateAsset(menu, menuPath);
            UnityEditor.AssetDatabase.SaveAssets();
            
            return menu;
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
            
            // 基本的なAnimatorController作成（必要に応じて）
            CreateBasicAnimatorController(mergeAnimator);
        }

        private static void CreateBasicAnimatorController(ModularAvatarMergeAnimator mergeAnimator)
        {
            // 基本的なAnimatorControllerを作成
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(
                "Assets/ReactiveAuraFX/Generated/ReactiveAuraFX_Animator.controller");
            
            // パラメータ追加
            controller.AddParameter("ReactiveAuraFX/SystemEnabled", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/EmotionAura", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/HeartbeatGlow", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/EyeFocusRay", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/LovePulse", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ReactiveAuraFX/IdleBloom", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Emotion", AnimatorControllerParameterType.Int);
            controller.AddParameter("HeartbeatTrigger", AnimatorControllerParameterType.Bool);
            
            // レイヤー作成
            var layer = new UnityEditor.Animations.AnimatorControllerLayer
            {
                name = "ReactiveAuraFX Layer",
                defaultWeight = 1f,
                stateMachine = new UnityEditor.Animations.AnimatorStateMachine()
            };
            
            controller.AddLayer(layer);
            
            mergeAnimator.animator = controller;
            
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif

        [MenuItem("ReactiveAuraFX/📦 ReactiveAuraFXプレハブ作成", false, 100)]
        public static void CreateReactiveAuraFXPrefab()
        {
            // プレハブ保存パス選択
            string path = EditorUtility.SaveFilePanel(
                "ReactiveAuraFXプレハブ保存", 
                "Assets/ReactiveAuraFX", 
                "ReactiveAuraFX_System", 
                "prefab");
            
            if (string.IsNullOrEmpty(path)) return;
            
            path = FileUtil.GetProjectRelativePath(path);
            
            // ベースオブジェクト作成
            GameObject prefabObj = new GameObject("ReactiveAuraFX_System");
            ReactiveAuraFXSystem system = prefabObj.AddComponent<ReactiveAuraFXSystem>();
            
            // デフォルト設定
            system.enableSystem = true;
            system.vrchatCompatibilityMode = true;
            system.autoFixSafeMode = true;
            
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
            else
            {
                EditorUtility.DisplayDialog("ReactiveAuraFX", 
                    "プレハブの作成に失敗しました。", "OK");
            }
        }

        [MenuItem("ReactiveAuraFX/🔧 設定とトラブルシューティング", false, 200)]
        public static void OpenSettingsWindow()
        {
            ReactiveAuraFXSettingsWindow.ShowWindow();
        }

        [MenuItem("ReactiveAuraFX/📖 ドキュメントを開く", false, 300)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/your-repo/ReactiveAuraFX/wiki");
        }
    }

    /// <summary>
    /// ReactiveAuraFX設定ウィンドウ
    /// </summary>
    public class ReactiveAuraFXSettingsWindow : EditorWindow
    {
        private Vector2 scrollPos;
        
        public static void ShowWindow()
        {
            var window = GetWindow<ReactiveAuraFXSettingsWindow>("ReactiveAuraFX設定");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("🌟 ReactiveAuraFX 設定とトラブルシューティング", EditorStyles.largeLabel);
            GUILayout.Space(10);
            
            // 基本設定
            GUILayout.Label("📋 基本設定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. アバターのルートオブジェクトを選択\n" +
                "2. メニューから「ReactiveAuraFX > アバターにReactiveAuraFXを追加」を実行\n" +
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
                var systems = FindObjectsOfType<ReactiveAuraFXSystem>();
                Debug.Log($"[ReactiveAuraFX] 検索結果: {systems.Length}個のシステムが見つかりました");
                
                foreach (var system in systems)
                {
                    Debug.Log($"- {system.gameObject.name} (親: {system.transform.parent?.name ?? "なし"})");
                }
                
                if (systems.Length > 0)
                {
                    Selection.objects = System.Array.ConvertAll(systems, s => s.gameObject);
                }
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
        
        private void CheckVRChatSDKStatus()
        {
#if VRC_SDK_VRCSDK3
            EditorUtility.DisplayDialog("VRChat SDK", "VRChat SDK3が検出されました ✅", "OK");
#else
            EditorUtility.DisplayDialog("VRChat SDK", "VRChat SDK3が見つかりません ❌\n\nVRChat Creator Companionからインポートしてください。", "OK");
#endif
        }
        
        private void CheckModularAvatarStatus()
        {
#if MA_VRCSDK3_AVATARS
            EditorUtility.DisplayDialog("Modular Avatar", "Modular Avatarが検出されました ✅", "OK");
#else
            EditorUtility.DisplayDialog("Modular Avatar", "Modular Avatarが見つかりません ⚠️\n\n必須ではありませんが、推奨ツールです。", "OK");
#endif
        }
    }
}
#endif 