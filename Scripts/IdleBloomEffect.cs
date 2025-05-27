// IdleBloom - 静寂の花エフェクト
// Reactive Aura FX サブシステム
// VRChat + Modular Avatar対応

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if VRC_SDK_VRCSDK3 && !UDON
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace ReactiveAuraFX.Core
{
    /// <summary>
    /// 静止状態が続いたときに足元に花が咲くような演出エフェクト
    /// VRChat Avatar 3.0対応、AutoFIX安全設計
    /// </summary>
    [AddComponentMenu("ReactiveAuraFX/Effects/Idle Bloom Effect")]
    [System.Serializable]
    public class IdleBloomEffect : MonoBehaviour
    {
        [Header("🌸 IdleBloom設定")]
        [Tooltip("静止判定時間（秒）")]
        [Range(5f, 30f)]
        public float idleTimeThreshold = 10f;
        
        [Tooltip("花の成長速度")]
        [Range(0.1f, 2f)]
        public float bloomGrowthSpeed = 0.5f;
        
        [Tooltip("花の最大サイズ")]
        [Range(0.5f, 3f)]
        public float maxBloomSize = 1.5f;
        
        [Tooltip("花の数")]
        [Range(3, 15)]
        public int flowerCount = 8;
        
        [Tooltip("花の展開半径")]
        [Range(0.5f, 3f)]
        public float bloomRadius = 1.2f;
        
        [Tooltip("花の色")]
        public Color[] flowerColors = {
            new Color(1f, 0.7f, 0.8f, 0.8f),  // ピンク
            new Color(0.8f, 0.9f, 1f, 0.8f),  // 薄青
            new Color(1f, 1f, 0.7f, 0.8f),    // 薄黄
            new Color(0.9f, 0.8f, 1f, 0.8f)   // 薄紫
        };
        
        [Tooltip("動作検出感度")]
        [Range(0.01f, 0.1f)]
        public float motionSensitivity = 0.02f;

        // === エフェクトコンポーネント ===
        [Header("エフェクトコンポーネント")]
        [Tooltip("花パーティクルシステム")]
        public ParticleSystem flowerParticles;
        
        [Tooltip("環境ライト")]
        public Light ambientLight;
        
        [Tooltip("静寂オーディオソース")]
        public AudioSource ambientAudioSource;
        
        [Tooltip("花咲き音")]
        public AudioClip bloomClip;
        
        [Tooltip("環境音")]
        public AudioClip ambientClip;

#if MA_VRCSDK3_AVATARS
        [Space(10)]
        [Header("🔗 Modular Avatar連携")]
        [Tooltip("Animatorパラメータで手動発動")]
        public bool useAnimatorBloomTrigger = false;
        
        [Tooltip("開花トリガーパラメータ名")]
        public string bloomTriggerParameterName = "IdleBloomTrigger";
        
        [Tooltip("動作検出をAnimatorから取得")]
        public bool useAnimatorMotionDetection = false;
        
        [Tooltip("動作パラメータ名")]
        public string motionParameterName = "IsMoving";
#endif

        // === 内部変数 ===
        private bool _isIdleBloomActive = false;
        private float _idleTimer = 0f;
        private Vector3 _lastPosition;
        private float _lastMotionTime = 0f;
        
        // 花オブジェクト制御
        private List<FlowerRenderer> _flowerRenderers = new List<FlowerRenderer>();
        private bool _flowersCreated = false;
        
        // パーティクル制御
        private ParticleSystem.MainModule _particleMain;
        private ParticleSystem.EmissionModule _particleEmission;
        private ParticleSystem.ShapeModule _particleShape;
        
        // ライト制御
        private float _baseLightIntensity = 0.2f;
        private Color _baseLightColor;
        
        // 成長制御
        private float _growthProgress = 0f;
        private Coroutine _bloomCoroutine;

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
            if (gameObject.name.Contains("IdleBloom") == false)
            {
                gameObject.name = "IdleBloom_Effect";
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
            _lastPosition = transform.position;
            _lastMotionTime = Time.time;
            
            // パーティクルシステム初期化
            if (flowerParticles == null)
            {
                CreateFlowerParticleSystem();
            }
            else
            {
                InitializeParticleSystem();
            }
            
            // ライト初期化
            if (ambientLight != null)
            {
                _baseLightIntensity = ambientLight.intensity;
                _baseLightColor = ambientLight.color;
                ambientLight.enabled = false;
            }
            
            // オーディオ初期化
            if (ambientAudioSource != null)
            {
                ambientAudioSource.loop = true;
                ambientAudioSource.volume = 0.3f;
            }
            
            Debug.Log("[ReactiveAuraFX] IdleBloomEffect初期化完了");
        }

        private void CreateFlowerParticleSystem()
        {
            GameObject particleObj = new GameObject("IdleBloomParticles");
            particleObj.transform.SetParent(transform);
            flowerParticles = particleObj.AddComponent<ParticleSystem>();
            
            InitializeParticleSystem();
        }

        private void InitializeParticleSystem()
        {
            if (flowerParticles == null) return;
            
            _particleMain = flowerParticles.main;
            _particleEmission = flowerParticles.emission;
            _particleShape = flowerParticles.shape;
            
            // メイン設定
            _particleMain.startLifetime = 8f;
            _particleMain.startSpeed = 0.2f;
            _particleMain.startSize = 0.1f;
            _particleMain.startColor = flowerColors[0];
            _particleMain.maxParticles = 50;
            _particleMain.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // エミッション設定
            _particleEmission.enabled = false;
            _particleEmission.rateOverTime = 2f;
            
            // 形状設定
            _particleShape.enabled = true;
            _particleShape.shapeType = ParticleSystemShapeType.Circle;
            _particleShape.radius = bloomRadius;
            _particleShape.position = Vector3.zero;
            
            // カラーオーバーライフタイム
            var colorOverLifetime = flowerParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(flowerColors[0], 0.0f), 
                    new GradientColorKey(Color.white, 0.3f),
                    new GradientColorKey(flowerColors[0], 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.0f, 0.0f), 
                    new GradientAlphaKey(0.8f, 0.4f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = gradient;
            
            // サイズオーバーライフタイム
            var sizeOverLifetime = flowerParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.1f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(1f, 0.3f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        }

        void Update()
        {
            UpdateMotionDetection();
            UpdateIdleTimer();
            UpdateBloomEffect();
        }

        private void UpdateMotionDetection()
        {
            Vector3 currentPosition = transform.position;
            float motionMagnitude = Vector3.Distance(currentPosition, _lastPosition);
            
            if (motionMagnitude > motionSensitivity)
            {
                _lastMotionTime = Time.time;
                
                // 動作検出時に花を終了
                if (_isIdleBloomActive)
                {
                    StopIdleBloom();
                }
            }
            
            _lastPosition = currentPosition;
        }

        private void UpdateIdleTimer()
        {
            _idleTimer = Time.time - _lastMotionTime;
            
            if (_idleTimer >= idleTimeThreshold && !_isIdleBloomActive)
            {
                StartIdleBloom();
            }
        }

        private void UpdateBloomEffect()
        {
            if (!_isIdleBloomActive) return;
            
            // 環境ライトの調整
            if (ambientLight != null)
            {
                float lightPulse = Mathf.Sin(Time.time * 0.5f) * 0.1f + 0.9f;
                ambientLight.intensity = _baseLightIntensity * lightPulse * _growthProgress;
            }
            
            // パーティクルエミッション調整
            if (flowerParticles != null)
            {
                _particleEmission.rateOverTime = 3f * _growthProgress;
            }
        }

        private void StartIdleBloom()
        {
            if (_isIdleBloomActive) return;
            
            _isIdleBloomActive = true;
            _growthProgress = 0f;
            
            // 花オブジェクト作成
            CreateFlowers();
            
            // パーティクル開始
            if (flowerParticles != null)
            {
                flowerParticles.transform.position = GetGroundPosition();
                flowerParticles.Play();
                _particleEmission.enabled = true;
            }
            
            // ライト点灯
            if (ambientLight != null)
            {
                ambientLight.transform.position = transform.position + Vector3.up * 0.5f;
                ambientLight.enabled = true;
            }
            
            // 花咲き音再生
            if (ambientAudioSource != null && bloomClip != null)
            {
                ambientAudioSource.PlayOneShot(bloomClip);
            }
            
            // 環境音開始
            if (ambientAudioSource != null && ambientClip != null)
            {
                ambientAudioSource.clip = ambientClip;
                ambientAudioSource.Play();
            }
            
            // 成長アニメーション開始
            if (_bloomCoroutine != null)
            {
                StopCoroutine(_bloomCoroutine);
            }
            _bloomCoroutine = StartCoroutine(BloomGrowthAnimation());
            
            Debug.Log("[ReactiveAuraFX] IdleBloom開始");
        }

        private void StopIdleBloom()
        {
            if (!_isIdleBloomActive) return;
            
            _isIdleBloomActive = false;
            
            // 成長アニメーション停止
            if (_bloomCoroutine != null)
            {
                StopCoroutine(_bloomCoroutine);
                _bloomCoroutine = null;
            }
            
            // パーティクル停止
            if (flowerParticles != null)
            {
                flowerParticles.Stop();
                _particleEmission.enabled = false;
            }
            
            // ライト消灯
            if (ambientLight != null)
            {
                ambientLight.enabled = false;
            }
            
            // 環境音停止
            if (ambientAudioSource != null)
            {
                ambientAudioSource.Stop();
            }
            
            // 花消失アニメーション
            StartCoroutine(FlowerDisappearAnimation());
            
            Debug.Log("[ReactiveAuraFX] IdleBloom停止");
        }

        private Vector3 GetGroundPosition()
        {
            Vector3 groundPos = transform.position;
            
            // レイキャストで地面を検出
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 2f))
            {
                groundPos = hit.point;
            }
            else
            {
                groundPos.y -= 1f; // デフォルトで足元より下
            }
            
            return groundPos;
        }

        private void CreateFlowers()
        {
            if (_flowersCreated) return;
            
            Vector3 centerPos = GetGroundPosition();
            
            for (int i = 0; i < flowerCount; i++)
            {
                float angle = (360f / flowerCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * bloomRadius,
                    0f,
                    Mathf.Sin(angle) * bloomRadius
                );
                
                Vector3 flowerPos = centerPos + offset;
                Color flowerColor = flowerColors[i % flowerColors.Length];
                
                FlowerRenderer flower = CreateFlowerRenderer(flowerPos, flowerColor);
                _flowerRenderers.Add(flower);
            }
            
            _flowersCreated = true;
        }

        private FlowerRenderer CreateFlowerRenderer(Vector3 position, Color color)
        {
            GameObject flowerObj = new GameObject($"IdleFlower_{_flowerRenderers.Count}");
            flowerObj.transform.SetParent(transform);
            flowerObj.transform.position = position;
            
            FlowerRenderer flower = flowerObj.AddComponent<FlowerRenderer>();
            flower.Initialize(color, maxBloomSize);
            
            return flower;
        }

        private IEnumerator BloomGrowthAnimation()
        {
            float startTime = Time.time;
            
            while (_isIdleBloomActive && _growthProgress < 1f)
            {
                float elapsed = Time.time - startTime;
                _growthProgress = Mathf.Clamp01(elapsed * bloomGrowthSpeed);
                
                // 花の成長
                foreach (var flower in _flowerRenderers)
                {
                    if (flower != null)
                    {
                        flower.SetGrowthProgress(_growthProgress);
                    }
                }
                
                yield return null;
            }
        }

        private IEnumerator FlowerDisappearAnimation()
        {
            float duration = 2f;
            float startTime = Time.time;
            
            while (Time.time - startTime < duration)
            {
                float progress = (Time.time - startTime) / duration;
                float disappearProgress = 1f - progress;
                
                foreach (var flower in _flowerRenderers)
                {
                    if (flower != null)
                    {
                        flower.SetGrowthProgress(disappearProgress);
                    }
                }
                
                yield return null;
            }
            
            // 花オブジェクト削除
            foreach (var flower in _flowerRenderers)
            {
                if (flower != null && flower.gameObject != null)
                {
                    DestroyImmediate(flower.gameObject);
                }
            }
            
            _flowerRenderers.Clear();
            _flowersCreated = false;
        }

        /// <summary>
        /// 静止時間閾値を設定
        /// </summary>
        public void SetIdleTimeThreshold(float threshold)
        {
            idleTimeThreshold = Mathf.Clamp(threshold, 5f, 30f);
        }

        /// <summary>
        /// 花の成長速度を設定
        /// </summary>
        public void SetBloomGrowthSpeed(float speed)
        {
            bloomGrowthSpeed = Mathf.Clamp(speed, 0.1f, 2f);
        }

        /// <summary>
        /// 花の色を設定
        /// </summary>
        public void SetFlowerColors(Color[] colors)
        {
            if (colors != null && colors.Length > 0)
            {
                flowerColors = colors;
            }
        }

        /// <summary>
        /// 手動で花を咲かせる
        /// </summary>
        public void ManualTriggerBloom()
        {
            _lastMotionTime = Time.time - idleTimeThreshold;
            StartIdleBloom();
        }

        /// <summary>
        /// 動作をリセット
        /// </summary>
        public void ResetMotionTimer()
        {
            _lastMotionTime = Time.time;
            if (_isIdleBloomActive)
            {
                StopIdleBloom();
            }
        }

        void OnDestroy()
        {
            if (_bloomCoroutine != null)
            {
                StopCoroutine(_bloomCoroutine);
            }
        }

        void OnDrawGizmosSelected()
        {
            // 花の展開範囲を表示
            Gizmos.color = Color.green;
            Vector3 groundPos = GetGroundPosition();
            Gizmos.DrawWireSphere(groundPos, bloomRadius);
            
            // 静止時間表示
            if (_idleTimer > 0f)
            {
                float idleProgress = Mathf.Clamp01(_idleTimer / idleTimeThreshold);
                Gizmos.color = Color.Lerp(Color.white, Color.green, idleProgress);
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, 
                    Vector3.one * idleProgress);
            }
        }
    }

    /// <summary>
    /// 個別の花レンダリング用クラス
    /// </summary>
    public class FlowerRenderer : MonoBehaviour
    {
        private Color _flowerColor;
        private float _maxSize;
        private float _currentGrowth = 0f;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public void Initialize(Color color, float maxSize)
        {
            _flowerColor = color;
            _maxSize = maxSize;
            
            CreateFlowerMesh();
            SetGrowthProgress(0f);
        }

        private void CreateFlowerMesh()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
            // 簡単な花形状メッシュを作成
            CreateFlowerMeshData();
            
            // マテリアル設定
            Material flowerMat = new Material(Shader.Find("Standard"));
            flowerMat.color = _flowerColor;
            flowerMat.SetFloat("_Mode", 3); // Transparent mode
            flowerMat.renderQueue = 3000;
            _meshRenderer.material = flowerMat;
        }

        private void CreateFlowerMeshData()
        {
            Mesh mesh = new Mesh();
            mesh.name = "FlowerMesh";
            
            // 5枚の花びらを持つ花形状
            int petals = 5;
            int verticesPerPetal = 3;
            int totalVertices = petals * verticesPerPetal + 1; // 中心点追加
            
            Vector3[] vertices = new Vector3[totalVertices];
            Vector2[] uv = new Vector2[totalVertices];
            int[] triangles = new int[petals * 3 * 2];
            
            // 中心点
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            
            // 花びら生成
            for (int i = 0; i < petals; i++)
            {
                float angle = (360f / petals) * i * Mathf.Deg2Rad;
                float nextAngle = (360f / petals) * (i + 1) * Mathf.Deg2Rad;
                
                int baseIndex = i * verticesPerPetal + 1;
                
                // 花びらの外側の点
                vertices[baseIndex] = new Vector3(Mathf.Cos(angle) * 0.3f, 0, Mathf.Sin(angle) * 0.3f);
                vertices[baseIndex + 1] = new Vector3(Mathf.Cos(angle + 0.2f) * 0.5f, 0.1f, Mathf.Sin(angle + 0.2f) * 0.5f);
                vertices[baseIndex + 2] = new Vector3(Mathf.Cos(nextAngle) * 0.3f, 0, Mathf.Sin(nextAngle) * 0.3f);
                
                uv[baseIndex] = new Vector2(0.3f, 0.3f);
                uv[baseIndex + 1] = new Vector2(0.8f, 0.8f);
                uv[baseIndex + 2] = new Vector2(0.3f, 0.3f);
                
                // 三角形
                int triIndex = i * 6;
                triangles[triIndex] = 0;
                triangles[triIndex + 1] = baseIndex;
                triangles[triIndex + 2] = baseIndex + 1;
                
                triangles[triIndex + 3] = 0;
                triangles[triIndex + 4] = baseIndex + 1;
                triangles[triIndex + 5] = baseIndex + 2;
            }
            
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            _meshFilter.mesh = mesh;
        }

        public void SetGrowthProgress(float progress)
        {
            _currentGrowth = Mathf.Clamp01(progress);
            
            float scale = _currentGrowth * _maxSize;
            transform.localScale = Vector3.one * scale;
            
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                Color currentColor = _flowerColor;
                currentColor.a = _currentGrowth * 0.8f;
                _meshRenderer.material.color = currentColor;
            }
        }
    }
} 