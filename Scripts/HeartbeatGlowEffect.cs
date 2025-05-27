// HeartbeatGlow - 鼓動波紋光エフェクト
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
    /// 胸元に手を置く動作で鼓動のような波紋光が広がるエフェクト
    /// VRChat Avatar 3.0対応、AutoFIX安全設計
    /// </summary>
    [AddComponentMenu("ReactiveAuraFX/Effects/Heartbeat Glow Effect")]
    [System.Serializable]
    public class HeartbeatGlowEffect : MonoBehaviour
    {
        [Header("💓 HeartbeatGlow設定")]
        [Tooltip("波紋の速度")]
        [Range(0.5f, 3.0f)]
        public float heartbeatSpeed = 1.2f;
        
        [Tooltip("波紋の強度")]
        [Range(0.1f, 2.0f)]
        public float heartbeatIntensity = 0.8f;
        
        [Tooltip("波紋の色")]
        public Color heartbeatColor = new Color(1f, 0.3f, 0.3f, 0.7f);
        
        [Tooltip("波紋の最大半径")]
        [Range(1f, 10f)]
        public float maxRippleRadius = 5f;
        
        [Tooltip("波紋の数")]
        [Range(1, 5)]
        public int rippleCount = 3;
        
        [Tooltip("トリガー検出範囲")]
        [Range(0.1f, 1f)]
        public float triggerDetectionRadius = 0.3f;
        
        [Tooltip("胸部位置Transform")]
        public Transform chestTransform;
        
        [Tooltip("手のTransform（左右）")]
        public Transform[] handTransforms = new Transform[2];

        // === エフェクトコンポーネント ===
        [Header("エフェクトコンポーネント")]
        [Tooltip("波紋マテリアル")]
        public Material rippleMaterial;
        
        [Tooltip("パーティクルシステム")]
        public ParticleSystem heartParticles;
        
        [Tooltip("ライトコンポーネント")]
        public Light heartLight;
        
        [Tooltip("オーディオソース")]
        public AudioSource heartAudioSource;
        
        [Tooltip("鼓動音クリップ")]
        public AudioClip heartbeatClip;

#if MA_VRCSDK3_AVATARS
        [Space(10)]
        [Header("🔗 Modular Avatar連携")]
        [Tooltip("Animatorパラメータで手動発動")]
        public bool useAnimatorTrigger = false;
        
        [Tooltip("発動トリガーパラメータ名")]
        public string triggerParameterName = "HeartbeatTrigger";
        
        [Tooltip("手の位置をAnimatorから取得")]
        public bool useAnimatorHandPositions = true;
        
        [Tooltip("左手パラメータ名（Vector3）")]
        public string leftHandParameterName = "LeftHandPosition";
        
        [Tooltip("右手パラメータ名（Vector3）")]
        public string rightHandParameterName = "RightHandPosition";
#endif

        // === 内部変数 ===
        private bool _isHeartbeatActive = false;
        private bool _isHandNearChest = false;
        private float _heartbeatPhase = 0f;
        private float _lastHeartbeatTime = 0f;
        private Coroutine _heartbeatCoroutine;
        
        // 波紋エフェクト用
        private RippleRenderer[] _rippleRenderers;
        private int _currentRippleIndex = 0;
        
        // パーティクル制御
        private ParticleSystem.MainModule _particleMain;
        private ParticleSystem.EmissionModule _particleEmission;
        
        // ライト制御
        private float _baseLightIntensity = 0.5f;
        private Color _baseLightColor;
        
#if MA_VRCSDK3_AVATARS
        // Modular Avatar関連
        private Animator _avatarAnimator;
        private bool _lastTriggerValue = false;
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
            if (gameObject.name.Contains("HeartbeatGlow") == false)
            {
                gameObject.name = "HeartbeatGlow_Effect";
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
            // 胸部Transform自動検出
            if (chestTransform == null)
            {
                var animator = GetComponentInParent<Animator>();
                if (animator != null)
                {
                    chestTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
                    if (chestTransform == null)
                    {
                        chestTransform = animator.GetBoneTransform(HumanBodyBones.Spine);
                    }
                }
            }
            
            // 手のTransform自動検出
            if (handTransforms[0] == null || handTransforms[1] == null)
            {
                var animator = GetComponentInParent<Animator>();
                if (animator != null)
                {
                    handTransforms[0] = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    handTransforms[1] = animator.GetBoneTransform(HumanBodyBones.RightHand);
                }
            }
            
            // 波紋レンダラー初期化
            InitializeRippleRenderers();
            
            // パーティクルシステム初期化
            if (heartParticles != null)
            {
                _particleMain = heartParticles.main;
                _particleEmission = heartParticles.emission;
                
                _particleMain.startColor = heartbeatColor;
                _particleEmission.enabled = false;
            }
            
            // ライト初期化
            if (heartLight != null)
            {
                _baseLightIntensity = heartLight.intensity;
                _baseLightColor = heartLight.color;
                heartLight.color = heartbeatColor;
                heartLight.enabled = false;
            }
            
            // オーディオ初期化
            if (heartAudioSource != null && heartbeatClip != null)
            {
                heartAudioSource.clip = heartbeatClip;
                heartAudioSource.loop = false;
            }

#if MA_VRCSDK3_AVATARS
            // Modular Avatar Animator検出
            if ((useAnimatorTrigger || useAnimatorHandPositions) && _avatarAnimator == null)
            {
                var avatarDescriptor = FindObjectOfType<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                if (avatarDescriptor != null)
                {
                    _avatarAnimator = avatarDescriptor.GetComponent<Animator>();
                }
            }
#endif
            
            Debug.Log("[ReactiveAuraFX] HeartbeatGlowEffect初期化完了");
        }

        private void InitializeRippleRenderers()
        {
            _rippleRenderers = new RippleRenderer[rippleCount];
            
            for (int i = 0; i < rippleCount; i++)
            {
                GameObject rippleObj = new GameObject($"HeartbeatRipple_{i}");
                rippleObj.transform.SetParent(transform);
                
                var rippleRenderer = rippleObj.AddComponent<RippleRenderer>();
                rippleRenderer.Initialize(rippleMaterial, maxRippleRadius, heartbeatColor);
                
                _rippleRenderers[i] = rippleRenderer;
            }
        }

        void Update()
        {
#if MA_VRCSDK3_AVATARS
            // Modular Avatar パラメータ監視
            if (useAnimatorTrigger && _avatarAnimator != null)
            {
                UpdateAnimatorTrigger();
            }
            
            if (useAnimatorHandPositions && _avatarAnimator != null)
            {
                UpdateAnimatorHandPositions();
            }
#endif
            
            if (chestTransform != null)
            {
                UpdateHandDetection();
                UpdateHeartbeatEffect();
            }
        }

#if MA_VRCSDK3_AVATARS
        private void UpdateAnimatorTrigger()
        {
            try
            {
                bool triggerValue = _avatarAnimator.GetBool(triggerParameterName);
                
                if (triggerValue && !_lastTriggerValue)
                {
                    StartHeartbeatEffect();
                }
                else if (!triggerValue && _lastTriggerValue)
                {
                    StopHeartbeatEffect();
                }
                
                _lastTriggerValue = triggerValue;
            }
            catch (System.Exception)
            {
                // パラメータが存在しない場合は無視
            }
        }

        private void UpdateAnimatorHandPositions()
        {
            try
            {
                // Animatorから手の位置を取得（実装は簡略化）
                // 実際の実装では、Animatorパラメータから手の位置を取得し、
                // 距離計算を行う必要があります
                
                // この例では、パラメータ存在チェックのみ
                if (_avatarAnimator.parameters != null)
                {
                    // パラメータ値に基づいた処理をここに実装
                }
            }
            catch (System.Exception)
            {
                // パラメータが存在しない場合は無視
            }
        }
#endif

        private void UpdateHandDetection()
        {
            bool handNearChest = false;
            
            foreach (var hand in handTransforms)
            {
                if (hand != null)
                {
                    float distance = Vector3.Distance(hand.position, chestTransform.position);
                    if (distance <= triggerDetectionRadius)
                    {
                        handNearChest = true;
                        break;
                    }
                }
            }
            
            if (handNearChest && !_isHandNearChest)
            {
                StartHeartbeatEffect();
            }
            else if (!handNearChest && _isHandNearChest)
            {
                StopHeartbeatEffect();
            }
            
            _isHandNearChest = handNearChest;
        }

        private void UpdateHeartbeatEffect()
        {
            if (!_isHeartbeatActive) return;
            
            _heartbeatPhase += Time.deltaTime * heartbeatSpeed;
            
            // 鼓動リズム生成
            float heartbeatValue = GenerateHeartbeatWave(_heartbeatPhase);
            
            // ライト制御
            if (heartLight != null)
            {
                heartLight.intensity = _baseLightIntensity + (heartbeatValue * heartbeatIntensity);
                heartLight.color = Color.Lerp(_baseLightColor, heartbeatColor, heartbeatValue);
            }
            
            // パーティクル制御
            if (heartParticles != null)
            {
                var emission = heartParticles.emission;
                emission.rateOverTime = heartbeatValue * 20f * heartbeatIntensity;
            }
            
            // 波紋トリガー
            if (heartbeatValue > 0.8f && Time.time - _lastHeartbeatTime > 60f / (heartbeatSpeed * 72f))
            {
                TriggerRipple();
                _lastHeartbeatTime = Time.time;
            }
        }

        private float GenerateHeartbeatWave(float phase)
        {
            // リアルな鼓動パターン（ドクン・ドクン）
            float beat1 = Mathf.Exp(-Mathf.Pow((phase % 1f) * 10f - 2f, 2f));
            float beat2 = Mathf.Exp(-Mathf.Pow((phase % 1f) * 10f - 4f, 2f)) * 0.7f;
            
            return Mathf.Clamp01(beat1 + beat2);
        }

        private void TriggerRipple()
        {
            if (_rippleRenderers == null || _rippleRenderers.Length == 0) return;
            
            var ripple = _rippleRenderers[_currentRippleIndex];
            if (ripple != null)
            {
                ripple.StartRipple(chestTransform.position);
            }
            
            _currentRippleIndex = (_currentRippleIndex + 1) % _rippleRenderers.Length;
            
            // 鼓動音再生
            if (heartAudioSource != null && heartbeatClip != null)
            {
                heartAudioSource.pitch = heartbeatSpeed;
                heartAudioSource.volume = heartbeatIntensity * 0.5f;
                heartAudioSource.PlayOneShot(heartbeatClip);
            }
        }

        public void StartHeartbeatEffect()
        {
            if (_isHeartbeatActive) return;
            
            _isHeartbeatActive = true;
            _heartbeatPhase = 0f;
            
            if (heartLight != null)
            {
                heartLight.enabled = true;
            }
            
            if (heartParticles != null)
            {
                heartParticles.Play();
                _particleEmission.enabled = true;
            }
            
            Debug.Log("[ReactiveAuraFX] HeartbeatGlow開始");
        }

        public void StopHeartbeatEffect()
        {
            if (!_isHeartbeatActive) return;
            
            _isHeartbeatActive = false;
            
            if (heartLight != null)
            {
                heartLight.enabled = false;
            }
            
            if (heartParticles != null)
            {
                heartParticles.Stop();
                _particleEmission.enabled = false;
            }
            
            // 全ての波紋を停止
            if (_rippleRenderers != null)
            {
                foreach (var ripple in _rippleRenderers)
                {
                    if (ripple != null)
                    {
                        ripple.StopRipple();
                    }
                }
            }
            
            Debug.Log("[ReactiveAuraFX] HeartbeatGlow停止");
        }

        /// <summary>
        /// 鼓動速度を設定
        /// </summary>
        public void SetHeartbeatSpeed(float speed)
        {
            heartbeatSpeed = Mathf.Clamp(speed, 0.5f, 3.0f);
        }

        /// <summary>
        /// 鼓動強度を設定
        /// </summary>
        public void SetHeartbeatIntensity(float intensity)
        {
            heartbeatIntensity = Mathf.Clamp(intensity, 0.1f, 2.0f);
        }

        /// <summary>
        /// 鼓動色を設定
        /// </summary>
        public void SetHeartbeatColor(Color color)
        {
            heartbeatColor = color;
            
            if (heartLight != null)
            {
                heartLight.color = color;
            }
            
            if (heartParticles != null)
            {
                _particleMain.startColor = color;
            }
        }

        void OnDestroy()
        {
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
            }
        }
    }

    /// <summary>
    /// 波紋レンダリング用クラス
    /// </summary>
    public class RippleRenderer : MonoBehaviour
    {
        private Material _material;
        private float _maxRadius;
        private Color _color;
        private bool _isActive = false;
        private float _currentRadius = 0f;
        private float _rippleSpeed = 5f;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public void Initialize(Material material, float maxRadius, Color color)
        {
            _material = material;
            _maxRadius = maxRadius;
            _color = color;
            
            // クワッドメッシュ作成
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
            CreateQuadMesh();
            
            if (_material != null)
            {
                _meshRenderer.material = _material;
            }
            
            gameObject.SetActive(false);
        }

        private void CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "RippleQuad";
            
            Vector3[] vertices = new Vector3[4];
            Vector2[] uv = new Vector2[4];
            int[] triangles = new int[6];
            
            float size = _maxRadius * 2f;
            
            vertices[0] = new Vector3(-size * 0.5f, 0, -size * 0.5f);
            vertices[1] = new Vector3(size * 0.5f, 0, -size * 0.5f);
            vertices[2] = new Vector3(-size * 0.5f, 0, size * 0.5f);
            vertices[3] = new Vector3(size * 0.5f, 0, size * 0.5f);
            
            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(1, 0);
            uv[2] = new Vector2(0, 1);
            uv[3] = new Vector2(1, 1);
            
            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 2; triangles[4] = 3; triangles[5] = 1;
            
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            _meshFilter.mesh = mesh;
        }

        public void StartRipple(Vector3 position)
        {
            transform.position = position;
            _currentRadius = 0f;
            _isActive = true;
            gameObject.SetActive(true);
            
            StartCoroutine(RippleAnimation());
        }

        public void StopRipple()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        private IEnumerator RippleAnimation()
        {
            while (_isActive && _currentRadius < _maxRadius)
            {
                _currentRadius += Time.deltaTime * _rippleSpeed;
                
                float progress = _currentRadius / _maxRadius;
                float alpha = (1f - progress) * _color.a;
                
                if (_meshRenderer.material != null)
                {
                    Color currentColor = _color;
                    currentColor.a = alpha;
                    _meshRenderer.material.color = currentColor;
                    
                    // シェーダープロパティがあれば設定
                    if (_meshRenderer.material.HasProperty("_Radius"))
                    {
                        _meshRenderer.material.SetFloat("_Radius", _currentRadius);
                    }
                }
                
                yield return null;
            }
            
            StopRipple();
        }
    }
} 