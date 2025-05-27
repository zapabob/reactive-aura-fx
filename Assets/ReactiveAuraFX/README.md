# ReactiveAuraFX System

## 🌟 概要

ReactiveAuraFXは、VRChatアバター用の感情応答型ビジュアルエフェクトシステムです。プレイヤーの感情状態に応じて美しい5種類のエフェクトが自動で作動し、没入感のあるVR体験を提供します。

## ✨ 主要機能

### 📊 5つの感情エフェクト

1. **🔵 EmotionAura (感情オーラ)**
   - 青色のソフトなパーティクルオーラ
   - 継続的な感情表現

2. **❤️ HeartbeatGlow (心拍グロー)**
   - 赤色パーティクル + 心拍リップル効果
   - 興奮・緊張状態の可視化

3. **👁️ EyeFocusRay (視線フォーカス)**
   - 目からの青い光線 + フォーカスパーティクル
   - 集中・注視状態の表現

4. **💕 LovePulse (愛のパルス)**
   - ピンク色の温かい波動エフェクト
   - 愛情・感動の表現

5. **🌸 IdleBloom (アイドル開花)**
   - 8個の花が円形に配置され開花
   - 瞑想・リラックス状態の表現

## 📁 ファイル構成

```
ReactiveAuraFX/
├── Scripts/              # C#スクリプト
├── Materials/            # エフェクト用マテリアル
├── Prefabs/             # プレハブファイル
├── Audio/               # 音響ファイル
├── Textures/            # テクスチャファイル
├── Animators/           # アニメーター設定
├── VRCHAT_INTEGRATION.txt
├── USAGE_GUIDE.txt
└── README.md
```

## 🚀 セットアップ手順

### 1. 基本導入
1. `ReactiveAuraFX_System.prefab` をシーンに配置
2. アバターのルートオブジェクトの子オブジェクトとして設置

### 2. VRChat統合
詳細は `VRCHAT_INTEGRATION.txt` を参照

### 3. カスタマイズ
詳細は `USAGE_GUIDE.txt` を参照

## 🎮 操作方法

### Expression Parameters
- **EmotionState**: 0-4 (感情レベル)
- **LoveLevel**: 0-1 (愛情レベル)
- **FocusIntensity**: 0-1 (集中レベル)
- **HeartRate**: 60-120 (心拍数)
- **IdleMode**: On/Off (瞑想モード)

## ⚡ パフォーマンス

- **軽量設計**: VRChat推奨設定準拠
- **最適化済み**: パーティクル数・描画負荷調整済み
- **3D Audio**: 空間音響対応

## 🛠️ 技術仕様

- **Unity 2022.3.6f1** 以上
- **VRChat SDK 3.0** 対応
- **VRCSDK Base + Worlds/Avatars**
- **EditorOnly** タグ設定済み

## 📝 ライセンス

このプロジェクトは教育・研究目的で作成されています。
VRChatでの使用は各自の責任において行ってください。

## 🔧 サポート

- バグ報告やご質問はIssuesまでお願いします
- カスタマイズ相談も歓迎です

## 🎯 更新履歴

### v1.0.0 (完成版)
- 5つの感情エフェクト実装
- VRChat統合機能完成
- マテリアル・オーディオ最適化
- ドキュメント完備

---

**ReactiveAuraFXで感情豊かなVRChatライフを楽しんでください！** ✨ 