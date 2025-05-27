ReactiveAuraFXシステム v1.0
================================

## 概要
ReactiveAuraFXは、VRChatアバター用の感情応答型オーラエフェクトシステムです。
プレイヤーの感情状態に応じて美しいビジュアルエフェクトとオーディオが作動します。

## エフェクトの種類

### 1. EmotionAura (感情オーラ)
- 用途: 基本感情状態の表示
- エフェクト: 青色のパーティクルオーラ + ポイントライト
- 特徴: 穏やかな発光、継続的なエフェクト

### 2. HeartbeatGlow (心拍グロー)
- 用途: 興奮・恋愛感情の表示
- エフェクト: 赤色パーティクル + ハートビートリップル + オーディオ
- 特徴: 心拍に同期したパルス効果

### 3. EyeFocusRay (視線フォーカス)
- 用途: 集中・注視状態の表示
- エフェクト: ビームレンダラー + フォーカスパーティクル + ブルーライト
- 特徴: 目の位置から発射される集中光線

### 4. LovePulse (愛のパルス)
- 用途: 強い愛情・幸福感の表示
- エフェクト: ピンク色パーティクル + パルスライト + オーディオ
- 特徴: 温かみのあるピンク色の波動

### 5. IdleBloom (アイドル開花)
- 用途: 平穏・瞑想状態の表示
- エフェクト: 8個の花オブジェクト + パーティクル + アンビエントライト
- 特徴: 円形配置の花が徐々に開花するアニメーション

## システム構成

ReactiveAuraFX_System/
├── Effects/                    (全エフェクトの親)
│   ├── EmotionAura_Effect     (基本オーラ)
│   ├── HeartbeatGlow_Effect   (心拍エフェクト)
│   ├── EyeFocusRay_Effect     (視線エフェクト)
│   ├── LovePulse_Effect       (愛のパルス)
│   └── IdleBloom_Effect       (開花エフェクト)
├── VRChatReferences/          (VRChat連携用)
│   ├── LeftEyeReference       (左目参照)
│   ├── RightEyeReference      (右目参照)
│   └── HeadReference          (頭部参照)
├── ReactiveAuraFX_Controller  (アニメーション制御)
└── ReactiveAuraFX_AudioSettings (オーディオ設定)

## 技術仕様

### パーティクルシステム
- EmotionAura: 100個、2秒ライフタイム
- HeartbeatGlow: 50個、パルス発光
- FocusParticles: 20個、円形配置
- LovePulse: 15個、3秒ライフタイム
- IdleBloomParticles: 50個、8秒ライフタイム

### ライティング
- 全てのライトは初期状態で無効
- スクリプト制御により動的に有効化
- 3D空間音響対応

### マテリアル
- AuraParticle.mat (青色透明)
- HeartbeatRipple.mat (赤色透明)
- EyeBeam.mat (青色透明)
- FlowerPetal.mat (ピンク色透明)
- 全て透明度対応レンダリング

## VRChat連携

### 必要なコンポーネント
- VRC Avatar Descriptor
- VRC Avatar Parameters
- VRC Playable Layers (FX Layer)

### セットアップ手順
1. アバターの頭部ボーンにReactiveAuraFX_Systemを配置
2. VRChatReferencesの位置をアバターに合わせて調整
3. アニメーターコントローラーでエフェクトの切り替えを設定
4. 表情メニューにトリガーを追加

## カスタマイズ

### 色の変更
各マテリアルのColorプロパティを調整してください。

### サイズ調整
Transform.scaleでエフェクト全体のサイズを変更できます。

### 音量調整
AudioSourceコンポーネントのVolumeを調整してください。

## 注意事項

- 全オブジェクトはEditorOnlyタグが設定されています
- VRChatビルド時に適切に処理されることを確認してください
- パフォーマンス向上のため、使用しないエフェクトは無効化することを推奨します
- プレハブとして保存されているため、複数のアバターで再利用可能です

## 更新履歴

v1.0 (2024)
- 初回リリース
- 5種類の基本エフェクト実装
- VRChat完全対応
- プレハブシステム導入

## サポート

質問や不具合報告は、プロジェクトのIssueトラッカーまでお願いします。

(C) 2024 ReactiveAuraFX Project. All rights reserved. 