# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-27

### ✨ Added - 新機能
- **🌟 ReactiveAuraFX Core System**: 表情・視線・動作連動エフェクトシステム
- **💫 EmotionAura Effect**: 表情に応じたオーラエフェクト
- **💓 HeartbeatGlow Effect**: 胸に手を置くと鼓動波紋光エフェクト
- **👁️ EyeFocusRay Effect**: 視線フォーカスビームエフェクト
- **💕 LovePulse Effect**: 特定ユーザーとの距離と注視でハート型パーティクル
- **🌸 IdleBloom Effect**: 静止状態で足元に花が咲く演出

### 🔗 Modular Avatar Complete Integration
- **Expression Parameters**: 全エフェクトの制御パラメータ自動生成
- **Expression Menu**: 階層化されたメニュー自動作成
- **Merge Animator**: AnimatorControllerの自動統合
- **Real-time Parameter Monitoring**: Animatorパラメータのリアルタイム監視
- **Auto Setup**: 一発インストールでMA完全統合

### 🛠️ Technical Features
- **AutoFIX Protection**: 自動削除回避機能
- **Custom Unity Editor**: MA統合状態を表示するカスタムInspector
- **VRChat SDK3 Compatibility**: VRChat Avatar 3.0完全対応
- **Non-Destructive Workflow**: Modular Avatarによる非破壊的ワークフロー

### 📋 Expression Parameters List
- `ReactiveAuraFX/SystemEnabled` (Bool) - システム全体制御
- `ReactiveAuraFX/EmotionAura` (Bool) - EmotionAura制御
- `ReactiveAuraFX/HeartbeatGlow` (Bool) - HeartbeatGlow制御
- `ReactiveAuraFX/EyeFocusRay` (Bool) - EyeFocusRay制御
- `ReactiveAuraFX/LovePulse` (Bool) - LovePulse制御
- `ReactiveAuraFX/IdleBloom` (Bool) - IdleBloom制御
- `Emotion` (Int) - 表情値 (0-7: Neutral, Happy, Love, Shy, Angry, Sad, Excited, Calm)
- `HeartbeatTrigger` (Bool) - 鼓動手動発動

### 📁 File Structure
```
Assets/ReactiveAuraFX/
├── Scripts/
│   ├── ReactiveAuraFXSystem.cs (主要システム)
│   ├── EmotionAuraEffect.cs (表情連動オーラ)
│   ├── HeartbeatGlowEffect.cs (鼓動波紋光)
│   ├── EyeFocusRayEffect.cs (視線ビーム)
│   ├── LovePulseEffect.cs (愛情パーティクル)
│   ├── IdleBloomEffect.cs (静寂の花)
│   ├── ReactiveAuraFXSystemEditor.cs (カスタムエディタ)
│   └── ReactiveAuraFXInstaller.cs (インストーラー)
├── package.json (Unityパッケージ設定)
├── README.md (包括的ドキュメント)
├── LICENSE (MITライセンス)
├── CHANGELOG.md (変更履歴)
└── .gitignore (Git設定)
```

### 🎯 Target Users
- VTuber配信者
- 恋愛RPユーザー  
- 癒し系アバター利用者
- VRChatクリエイター

### 🚀 Installation
1. Modular Avatarをインストール（VCC推奨）
2. アバターのルートオブジェクトを選択
3. `ReactiveAuraFX > 🌟 アバターにReactiveAuraFXを追加`
4. 完了！

## [Unreleased] - 今後の予定

### Phase 2: センサー連携
- OSC Sensor Integration（心拍センサー等）
- リアルタイム生体情報連携

### Phase 3: AI機能
- AI-driven Emotion Mapping
- チャット内容からの自動エフェクト発動

### Phase 4: マルチプラットフォーム
- Cluster対応
- VR以外のプラットフォーム対応 