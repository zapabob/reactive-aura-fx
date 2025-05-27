# 🌟 Reactive Aura FX - 魅力拡張アバター

VRChatアバター向け魅力的エフェクトシステム
表情・視線・動作に連動してリアルタイムでエフェクトを発動し、自己表現力と没入感を向上

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![Unity](https://img.shields.io/badge/Unity-2022.3_LTS-green.svg)
![VRChat](https://img.shields.io/badge/VRChat-SDK3-orange.svg)

## ✨ 主要機能

### 💫 EmotionAura - 表情連動オーラ
- **機能**: アバターの表情（笑顔・怒り・悲しみ）に応じたオーラ発光
- **トリガー**: Animatorパラメータによる表情変化
- **エフェクト**: 色と形が変化するオーラエフェクト
- **設定可能項目**: オーラ色、強度、範囲、アニメーション速度

### 💓 HeartbeatGlow - 鼓動波紋光
- **機能**: 胸元に手を置く動作で鼓動のような波紋光が広がる
- **トリガー**: 手と胸の位置関係検出
- **エフェクト**: 波紋状の光エフェクト、鼓動音再生
- **設定可能項目**: 鼓動速度、波紋強度、色、最大半径

### 👁️ EyeFocusRay - 視線フォーカスビーム
- **機能**: 視線がオブジェクトにフォーカスしたときに細いビーム状光を発生
- **トリガー**: カメラ視線方向とRaycast判定
- **エフェクト**: LineRendererによるビーム、フォーカス時パーティクル
- **設定可能項目**: ビーム長さ、太さ、色、フォーカス感度

### 💕 LovePulse - 愛情パーティクル
- **機能**: 特定ユーザーとの距離と注視でハート型パーティクルとSE発生
- **トリガー**: プレイヤー距離 + 視線角度判定
- **エフェクト**: ハート型パーティクル、愛情ライト、鼓動音
- **設定可能項目**: 検出距離、パーティクル数、色、蓄積時間

### 🌸 IdleBloom - 静寂の花
- **機能**: 静止状態が一定時間継続すると足元に花が咲く演出
- **トリガー**: 位置変化の検出による静止判定
- **エフェクト**: 3D花オブジェクト、パーティクル、環境ライト
- **設定可能項目**: 静止時間閾値、花の成長速度、色、数

## 🔧 システム要件

- **Unity**: 2022.3.22f1 LTS以上
- **VRChat SDK**: VRChat SDK3 (Avatar 3.0)
- **対応プラットフォーム**: PCVR, Desktop VRChat
- **必須**: Modular Avatar (完全統合対応)
- **推奨**: VRChat Creator Companion (VCC)

## 📦 インストール方法

### 方法1: Unityパッケージインポート
1. リリースページから最新のunitypackageをダウンロード
2. Unity上で`Assets > Import Package > Custom Package`
3. ダウンロードしたパッケージを選択してインポート

### 方法2: GitHubからクローン
```bash
git clone https://github.com/zapabob/ReactiveAuraFX.git
```

## 🚀 使用方法

### 💡 超簡単！一発セットアップ（Modular Avatar完全統合）
1. **Modular Avatarをインストール**（VCC推奨）
2. **アバターのルートオブジェクト**（VRCAvatarDescriptorがあるオブジェクト）を選択
3. Unityメニューから「**ReactiveAuraFX > 🌟 アバターにReactiveAuraFXを追加**」をクリック
4. **完了！** Modular Avatarが自動統合されます

### 🔗 Modular Avatar統合内容
- **Expression Parameters**: 全エフェクトのON/OFF制御
- **Expression Menu**: 階層化されたメニュー自動生成
- **Merge Animator**: AnimatorControllerの自動統合
- **リアルタイム制御**: VRChat内での即座のエフェクト制御

### 🛡️ AutoFIX対策済み
- **オブジェクト名**: 自動的に「ReactiveAuraFX_System」に設定
- **タグ**: 「EditorOnly」タグで保護
- **Modular Avatar完全統合**: MA Parameters, Menu Installer, Merge Animator自動追加

### 詳細設定

#### EmotionAura設定
```csharp
// 表情変化の検出
EmotionAuraEffect emotionAura = GetComponent<EmotionAuraEffect>();
emotionAura.SetEmotion(EmotionType.Happy);
```

#### HeartbeatGlow設定
```csharp
// 手動でハートビート発動
HeartbeatGlowEffect heartbeat = GetComponent<HeartbeatGlowEffect>();
heartbeat.StartHeartbeatEffect();
```

#### EyeFocusRay設定
```csharp
// ビーム設定カスタマイズ
EyeFocusRayEffect eyeRay = GetComponent<EyeFocusRayEffect>();
eyeRay.SetBeamColor(Color.cyan);
eyeRay.SetBeamLength(8f);
```

#### LovePulse設定
```csharp
// 愛情パルス手動発動
LovePulseEffect lovePulse = GetComponent<LovePulseEffect>();
lovePulse.ManualTriggerLovePulse();
```

#### IdleBloom設定
```csharp
// 花を手動で咲かせる
IdleBloomEffect idleBloom = GetComponent<IdleBloomEffect>();
idleBloom.ManualTriggerBloom();
```

## 🎨 カスタマイズ

### カラーテーマの変更
各エフェクトはInspectorから簡単に色を変更できます：

```csharp
// プログラムから色を変更
public void ChangeThemeColor(Color newColor)
{
    emotionAura.SetEmotionColor(newColor);
    heartbeat.SetHeartbeatColor(newColor);
    eyeRay.SetBeamColor(newColor);
    lovePulse.SetLoveColor(newColor);
}
```

### エフェクト強度の調整
```csharp
// 全体的な強度調整
public void SetGlobalIntensity(float intensity)
{
    emotionAura.SetIntensity(intensity);
    heartbeat.SetHeartbeatIntensity(intensity);
    eyeRay.SetBeamIntensity(intensity);
    lovePulse.SetEffectIntensity(intensity);
}
```

## 🔌 Modular Avatar完全統合機能

### Expression Menu構造
自動生成されるExpression Menu構造：
```
🌟 ReactiveAuraFX (Toggle) - システム全体ON/OFF
└── ⚙️ エフェクト設定 (SubMenu)
    ├── 💫 EmotionAura (Toggle)
    ├── 💓 HeartbeatGlow (Toggle)  
    ├── 💓 Heartbeat Trigger (Button) - 手動発動
    ├── 👁️ EyeFocusRay (Toggle)
    ├── 💕 LovePulse (Toggle)
    └── 🌸 IdleBloom (Toggle)
```

### Expression Parameters
自動生成されるAnimatorパラメータ：
```csharp
// システム制御
"ReactiveAuraFX/SystemEnabled" (Bool)
"ReactiveAuraFX/EmotionAura" (Bool)
"ReactiveAuraFX/HeartbeatGlow" (Bool)
"ReactiveAuraFX/EyeFocusRay" (Bool)
"ReactiveAuraFX/LovePulse" (Bool)
"ReactiveAuraFX/IdleBloom" (Bool)

// エフェクト連動
"Emotion" (Int) - 表情値 (0-7)
"HeartbeatTrigger" (Bool) - 鼓動手動発動
```

### Animatorパラメータ連動
表情に応じた自動エフェクト発動：
```csharp
// EmotionAura自動連動
0: Neutral, 1: Happy, 2: Love, 3: Shy
4: Angry, 5: Sad, 6: Excited, 7: Calm
```

## 📊 パフォーマンス最適化

### LOD (Level of Detail) 設定
- 距離に応じてエフェクトの詳細度を自動調整
- VRChatのパフォーマンス基準に対応

### GPU最適化
- パーティクルシステムのGPU Instancing使用
- シェーダーレベルでの最適化

### メモリ使用量最適化
- オブジェクトプールパターンの使用
- 不要時のリソース自動解放

## 🛠️ トラブルシューティング

### 🚨 AutoFIXで消された場合
1. Unityメニューから「**ReactiveAuraFX > 🔧 設定とトラブルシューティング**」を開く
2. 「**全ReactiveAuraFXオブジェクトを検索**」ボタンをクリック
3. 見つからない場合は再インストール：「**ReactiveAuraFX > 🌟 アバターにReactiveAuraFXを追加**」

### よくある問題

**Q: エフェクトが表示されない**
A: 以下を確認してください：
- ReactiveAuraFXSystemが有効になっているか
- 対象のアバターにAnimatorが設定されているか
- VRChat SDKが正しくインポートされているか
- AutoFIX安全モードが有効になっているか

**Q: AutoFIXで削除される**
A: **対策済み！** 以下の保護機能があります：
- オブジェクト名に「ReactiveAuraFX」を含む命名
- 「EditorOnly」タグによる保護
- Awakeでの自動安全設定

**Q: パフォーマンスが重い**
A: 以下を試してください：
- パーティクル数を減らす
- 不要なエフェクトを無効化
- 「設定とトラブルシューティング」ウィンドウでパフォーマンス設定を確認

**Q: VRChatでエフェクトが動作しない**
A: 以下を確認してください：
- VRChat互換性モードが有効になっているか
- Modular Avatarが正しく統合されているか
- アバターのパフォーマンスランクが適切か

## 📝 変更履歴

### v1.0.0 (2025-01-27)
- 初回リリース
- EmotionAura、HeartbeatGlow、EyeFocusRay、LovePulse、IdleBloom実装
- **Modular Avatar完全統合**（Expression Parameters/Menu/Merge Animator）
- リアルタイムAnimatorパラメータ監視機能
- Unityカスタムエディタ実装（MA統合状態表示）
- VRChat SDK3対応

## 🤝 コントリビューション

プルリクエストやイシューの報告を歓迎します！

1. このリポジトリをフォーク
2. 機能ブランチを作成 (`git checkout -b feature/amazing-feature`)
3. 変更をコミット (`git commit -m 'Add amazing feature'`)
4. ブランチにプッシュ (`git push origin feature/amazing-feature`)
5. プルリクエストを作成

## 📄 ライセンス

### 商用利用について
- **個人利用**: 自由に使用可能
- **配信での使用**: 許可
- **Boothでの再販**: 禁止
- **改造・カスタマイズ**: 個人利用の範囲で許可

### 必要表記
プロジェクトを使用する際は、以下の表記を含めてください：
```
Powered by Reactive Aura FX
```

## 📞 サポート

- **ドキュメント**: [Wiki](https://github.com/zapabob/reactive-aura-fx/wiki)
- **Modular Avatar公式**: [https://modular-avatar.nadena.dev/](https://modular-avatar.nadena.dev/)
- **Issues**: [GitHub Issues](https://github.com/zapabob/reactive-aura-fx/issues)
- **VRChat Ask**: [Non-Destructive Workflows Guide](https://ask.vrchat.com/t/go-the-modular-way-a-guide-to-non-destructive-workflows-on-avatars/23084)

## 🚀 今後の拡張予定

### Phase 2: センサー連携
- OSC Sensor Integration（心拍センサー等）
- リアルタイム生体情報連携

### Phase 3: AI機能
- AI-driven Emotion Mapping
- チャット内容からの自動エフェクト発動

### Phase 4: マルチプラットフォーム
- Cluster対応
- VR以外のプラットフォーム対応

---

💖 **Reactive Aura FXで、あなたのアバターをもっと魅力的に！**

Made with ❤️ for VRChat Community 