// EyeFocusRay - 視線フォーカスビーム
// Reactive Aura FX サブシステム
// VRChat + Modular Avatar完全対応

using UnityEngine;
using System.Collections;

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
    /// 視線がオブジェクトにフォーカスしたときに細いビーム状光を出すエフェクト
    /// VRChat Avatar 3.0 + Modular Avatar完全対応、AutoFIX安全設計
    /// </summary>
    [AddComponentMenu("ReactiveAuraFX/Effects/Eye Focus Ray Effect")]
    [System.Serializable]
    public class EyeFocusRayEffect : MonoBehaviour
    {
        [Header("👁️ EyeFocusRay設定")]
        [Tooltip("ビームの長さ")]
        [Range(1f, 10f)]
        public float rayLength = 5f;
        
        [Tooltip("ビームの太さ")]
        [Range(0.01f, 0.1f)]
        public float rayThickness = 0.02f;
        
        [Tooltip("ビームの色")]
        public Color rayColor = new Color(0.5f, 0.8f, 1f, 0.6f);
        
        [Tooltip("ビームの強度")]
        [Range(0.1f, 2f)]
        public float rayIntensity = 1f;
        
        [Tooltip("視線追跡感度")]
        [Range(0.1f, 1f)]
        public float gazeThreshold = 0.8f;
        
        [Tooltip("フォーカス維持時間")]
        [Range(0.1f, 2f)]
        public float focusHoldTime = 0.5f;
        
        [Tooltip("目のTransform（左右）")]
        public Transform[] eyeTransforms = new Transform[2];
        
        [Tooltip("視線レイヤーマスク")]
        public LayerMask gazeLayers = -1;

        // === エフェクトコンポーネント ===
        [Header("エフェクトコンポーネント")]
        [Tooltip("ビームのLineRenderer")]
        public LineRenderer beamRenderer;
        
        [Tooltip("フォーカス時のパーティクル")]
        public ParticleSystem focusParticles;
        
        [Tooltip("フォーカス時のライト")]
        public Light focusLight;
        
        [Tooltip("フォーカス時のオーディオ")]
        public AudioSource focusAudioSource;
        
        [Tooltip("フォーカス音クリップ")]
        public AudioClip focusClip;

#if MA_VRCSDK3_AVATARS
        [Space(10)]
        [Header("🔗 Modular Avatar完全連携")]
        [Tooltip("Animatorパラメータで強制発動")]
        public bool useAnimatorForceBeam = false;
        
        [Tooltip("強制ビームパラメータ名")]
        public string forceBeamParameterName = "EyeBeamForce";
        
        [Tooltip("エフェクト有効化パラメータ名")]
        public string enableParameterName = "ReactiveAuraFX/EyeFocusRay";
#endif

        // === 内部変数 ===
        private bool _isGazing = false;
        private bool _effectEnabled = true;
        private float _gazeStartTime = 0f;
        private Vector3 _gazeDirection = Vector3.forward;
        private GameObject _focusedObject = null;
        private Camera _eyeCamera;
        
        // ビーム制御
        private Vector3 _beamStartPos;
        private Vector3 _beamEndPos;
        private bool _beamActive = false;
        
        // パーティクル制御
        private ParticleSystem.MainModule _particleMain;
        private ParticleSystem.EmissionModule _particleEmission;

#if MA_VRCSDK3_AVATARS
        // Modular Avatar関連
        private Animator _avatarAnimator;
        private VRCAvatarDescriptor _avatarDescriptor;
        private bool _lastForceBeamValue = false;
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
            if (gameObject.name.Contains("EyeFocusRay") == false)
            {
                gameObject.name = "EyeFocusRay_Effect";
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
            // 目のTransform自動検出
            if (eyeTransforms[0] == null || eyeTransforms[1] == null)
            {
                var animator = GetComponentInParent<Animator>();
                if (animator != null)
                {
                    eyeTransforms[0] = animator.GetBoneTransform(HumanBodyBones.LeftEye);
                    eyeTransforms[1] = animator.GetBoneTransform(HumanBodyBones.RightEye);
                    
                    // 目がない場合は頭を使用
                    if (eyeTransforms[0] == null)
                    {
                        var head = animator.GetBoneTransform(HumanBodyBones.Head);
                        eyeTransforms[0] = head;
                        eyeTransforms[1] = head;
                    }
                }
            }
            
            // カメラ検出
            _eyeCamera = Camera.main;
            if (_eyeCamera == null)
            {
                _eyeCamera = FindObjectOfType<Camera>();
            }
            
            // LineRenderer初期化
            if (beamRenderer == null)
            {
                GameObject beamObj = new GameObject("EyeFocusBeam");
                beamObj.transform.SetParent(transform);
                beamRenderer = beamObj.AddComponent<LineRenderer>();
            }
            
            InitializeBeamRenderer();
            
            // パーティクルシステム初期化
            if (focusParticles != null)
            {
                _particleMain = focusParticles.main;
                _particleEmission = focusParticles.emission;
                
                _particleMain.startColor = rayColor;
                _particleEmission.enabled = false;
                focusParticles.Stop();
            }
            
            // ライト初期化
            if (focusLight != null)
            {
                focusLight.color = rayColor;
                focusLight.intensity = 0f;
                focusLight.enabled = false;
            }
            
            // オーディオ初期化
            if (focusAudioSource != null && focusClip != null)
            {
                focusAudioSource.clip = focusClip;
                focusAudioSource.loop = false;
            }

#if MA_VRCSDK3_AVATARS
            // Modular Avatar Animator検出
            if (useAnimatorForceBeam && _avatarAnimator == null)
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
            
            Debug.Log("[ReactiveAuraFX] EyeFocusRayEffect初期化完了");
        }

        private void InitializeBeamRenderer()
        {
            if (beamRenderer == null) return;
            
            beamRenderer.material = CreateBeamMaterial();
            beamRenderer.startWidth = rayThickness;
            beamRenderer.endWidth = rayThickness;
            beamRenderer.positionCount = 2;
            beamRenderer.useWorldSpace = true;
            beamRenderer.enabled = false;
            
            // ビームの詳細設定
            beamRenderer.numCapVertices = 4;
            beamRenderer.numCornerVertices = 4;
            beamRenderer.alignment = LineAlignment.TransformZ;
        }

        private Material CreateBeamMaterial()
        {
            Material beamMat = new Material(Shader.Find("Sprites/Default"));
            beamMat.color = rayColor;
            beamMat.SetFloat("_Mode", 3); // Transparent mode
            beamMat.renderQueue = 3000;
            
            return beamMat;
        }

        void Update()
        {
#if MA_VRCSDK3_AVATARS
            // Modular Avatar パラメータ監視
            if (useAnimatorForceBeam && _avatarAnimator != null)
            {
                UpdateAnimatorForceBeam();
            }
            
            if (_avatarAnimator != null)
            {
                UpdateEffectEnabledFromAnimator();
            }
#endif
            
            if (_effectEnabled)
            {
                UpdateGazeDetection();
                UpdateBeamEffect();
            }
        }

#if MA_VRCSDK3_AVATARS
        private void UpdateAnimatorForceBeam()
        {
            try
            {
                if (_avatarAnimator.parameters == null) return;
                
                // パラメータの存在チェック
                bool paramExists = false;
                foreach (var param in _avatarAnimator.parameters)
                {
                    if (param.name == forceBeamParameterName)
                    {
                        paramExists = true;
                        break;
                    }
                }
                
                if (!paramExists) return;
                
                bool forceBeamValue = _avatarAnimator.GetBool(forceBeamParameterName);
                
                if (forceBeamValue && !_lastForceBeamValue)
                {
                    ForceActivateBeam();
                }
                else if (!forceBeamValue && _lastForceBeamValue)
                {
                    DeactivateBeam();
                }
                
                _lastForceBeamValue = forceBeamValue;
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

        private void UpdateGazeDetection()
        {
            if (_eyeCamera == null) return;
            
            // 視線方向の計算
            Vector3 gazeDirection = GetGazeDirection();
            bool isLookingAtObject = PerformGazeRaycast(gazeDirection);
            
            if (isLookingAtObject && !_isGazing)
            {
                StartGazing();
            }
            else if (!isLookingAtObject && _isGazing)
            {
                StopGazing();
            }
            
            // フォーカス維持時間チェック
            if (_isGazing && Time.time - _gazeStartTime >= focusHoldTime)
            {
                if (!_beamActive)
                {
                    ActivateBeam();
                }
            }
        }

        private Vector3 GetGazeDirection()
        {
            // カメラの向きを基準にした視線方向
            if (_eyeCamera != null)
            {
                return _eyeCamera.transform.forward;
            }
            
            // 目のTransformがある場合はそれを使用
            if (eyeTransforms[0] != null)
            {
                return eyeTransforms[0].forward;
            }
            
            return transform.forward;
        }

        private bool PerformGazeRaycast(Vector3 direction)
        {
            Vector3 origin = GetGazeOrigin();
            RaycastHit hit;
            
            if (Physics.Raycast(origin, direction, out hit, rayLength, gazeLayers))
            {
                _focusedObject = hit.collider.gameObject;
                _beamEndPos = hit.point;
                
                // 視線の精度チェック
                float dotProduct = Vector3.Dot(direction.normalized, 
                    (hit.point - origin).normalized);
                
                return dotProduct >= gazeThreshold;
            }
            else
            {
                _focusedObject = null;
                _beamEndPos = origin + direction * rayLength;
                return false;
            }
        }

        private Vector3 GetGazeOrigin()
        {
            if (_eyeCamera != null)
            {
                return _eyeCamera.transform.position;
            }
            
            if (eyeTransforms[0] != null)
            {
                return eyeTransforms[0].position;
            }
            
            return transform.position;
        }

        private void StartGazing()
        {
            _isGazing = true;
            _gazeStartTime = Time.time;
            _beamStartPos = GetGazeOrigin();
            
            Debug.Log($"[ReactiveAuraFX] 視線フォーカス開始: {_focusedObject?.name}");
        }

        private void StopGazing()
        {
            _isGazing = false;
            DeactivateBeam();
            
            Debug.Log("[ReactiveAuraFX] 視線フォーカス終了");
        }

        private void ActivateBeam()
        {
            if (_beamActive || !_effectEnabled) return;
            
            _beamActive = true;
            
            // ビーム表示
            if (beamRenderer != null)
            {
                beamRenderer.enabled = true;
                beamRenderer.SetPosition(0, _beamStartPos);
                beamRenderer.SetPosition(1, _beamEndPos);
            }
            
            // パーティクル開始
            if (focusParticles != null)
            {
                focusParticles.transform.position = _beamEndPos;
                focusParticles.Play();
                _particleEmission.enabled = true;
            }
            
            // ライト点灯
            if (focusLight != null)
            {
                focusLight.transform.position = _beamEndPos;
                focusLight.enabled = true;
                focusLight.intensity = rayIntensity;
            }
            
            // フォーカス音再生
            if (focusAudioSource != null && focusClip != null)
            {
                focusAudioSource.PlayOneShot(focusClip);
            }
            
            Debug.Log("[ReactiveAuraFX] EyeFocusBeam発動");
        }

#if MA_VRCSDK3_AVATARS
        /// <summary>
        /// Animatorから強制的にビームを発動
        /// </summary>
        public void ForceActivateBeam()
        {
            if (!_effectEnabled) return;
            
            _beamStartPos = GetGazeOrigin();
            _beamEndPos = _beamStartPos + GetGazeDirection() * rayLength;
            ActivateBeam();
        }
#endif

        private void DeactivateBeam()
        {
            if (!_beamActive) return;
            
            _beamActive = false;
            
            // ビーム非表示
            if (beamRenderer != null)
            {
                beamRenderer.enabled = false;
            }
            
            // パーティクル停止
            if (focusParticles != null)
            {
                focusParticles.Stop();
                _particleEmission.enabled = false;
            }
            
            // ライト消灯
            if (focusLight != null)
            {
                focusLight.enabled = false;
            }
        }

        private void UpdateBeamEffect()
        {
            if (!_beamActive) return;
            
            // ビームの位置更新
            _beamStartPos = GetGazeOrigin();
            
            if (beamRenderer != null)
            {
                beamRenderer.SetPosition(0, _beamStartPos);
                beamRenderer.SetPosition(1, _beamEndPos);
                
                // アニメーション効果
                float pulseValue = Mathf.Sin(Time.time * 5f) * 0.2f + 0.8f;
                Color currentColor = rayColor;
                currentColor.a *= pulseValue;
                beamRenderer.material.color = currentColor;
            }
            
            // パーティクル位置更新
            if (focusParticles != null)
            {
                focusParticles.transform.position = _beamEndPos;
            }
            
            // ライト位置更新
            if (focusLight != null)
            {
                focusLight.transform.position = _beamEndPos;
                focusLight.intensity = rayIntensity * (Mathf.Sin(Time.time * 3f) * 0.3f + 0.7f);
            }
        }

        /// <summary>
        /// エフェクトの有効/無効を切り替え
        /// </summary>
        public void SetEffectEnabled(bool enabled)
        {
            _effectEnabled = enabled;
            
            if (!enabled && _beamActive)
            {
                DeactivateBeam();
            }
        }

        /// <summary>
        /// ビームの色を設定
        /// </summary>
        public void SetBeamColor(Color color)
        {
            rayColor = color;
            
            if (beamRenderer != null && beamRenderer.material != null)
            {
                beamRenderer.material.color = color;
            }
            
            if (focusLight != null)
            {
                focusLight.color = color;
            }
            
            if (focusParticles != null)
            {
                _particleMain.startColor = color;
            }
        }

        /// <summary>
        /// ビームの強度を設定
        /// </summary>
        public void SetBeamIntensity(float intensity)
        {
            rayIntensity = Mathf.Clamp(intensity, 0.1f, 2f);
        }

        /// <summary>
        /// ビームの長さを設定
        /// </summary>
        public void SetBeamLength(float length)
        {
            rayLength = Mathf.Clamp(length, 1f, 10f);
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
                Debug.Log("[ReactiveAuraFX] EyeFocusRay Modular Avatar統合完了");
            }
            else
            {
                Debug.LogWarning("[ReactiveAuraFX] VRCAvatarDescriptorが見つかりません");
            }
        }
#endif

        void OnDestroy()
        {
            if (beamRenderer != null && beamRenderer.material != null)
            {
                DestroyImmediate(beamRenderer.material);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!_isGazing) return;
            
            Gizmos.color = rayColor;
            Gizmos.DrawLine(_beamStartPos, _beamEndPos);
            
            if (_focusedObject != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_beamEndPos, 0.1f);
            }
        }
    }
} 