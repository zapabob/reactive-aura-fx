// EyeFocusRay - 視線フォーカスビーム
// Reactive Aura FX サブシステム
// VRChat + Modular Avatar対応

using UnityEngine;
using System.Collections;

#if VRC_SDK_VRCSDK3 && !UDON
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace ReactiveAuraFX.Core
{
    /// <summary>
    /// 視線がオブジェクトにフォーカスしたときに細いビーム状光を出すエフェクト
    /// VRChat Avatar 3.0対応、AutoFIX安全設計
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
        [Header("🔗 Modular Avatar連携")]
        [Tooltip("Animatorパラメータで強制発動")]
        public bool useAnimatorForceBeam = false;
        
        [Tooltip("強制ビームパラメータ名")]
        public string forceBeamParameterName = "EyeBeamForce";
#endif

        // === 内部変数 ===
        private bool _isGazing = false;
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
            UpdateGazeDetection();
            UpdateBeamEffect();
        }

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
            if (_beamActive) return;
            
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