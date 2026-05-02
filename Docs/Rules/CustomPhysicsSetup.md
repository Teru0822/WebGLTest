# 標準物理演算セットアップ手順 (Standard Physics Setup Guide)

Photon Fusionの `NetworkRigidbody3D` とUnity標準の `MeshCollider` を用いて、物理演算による地震シミュレーションを行うためのセットアップ手順です。旧来のカスタム物理エンジンは非推奨・削除されました。

## 1. シーンの基本準備
1. 床となるオブジェクト（Cubeなど）を配置し、スケールを広げます（例: `Scale (20, 1, 20)`）。
2. 床には標準の `BoxCollider` が付いていることを確認してください。

## 2. ネットワークとマネージャーの配置
1. 空のGameObjectを作成し、名前を `NetworkManager` とします。
2. `NetworkManager` に以下のスクリプトをアタッチします。
   - `GameStarter` (Pキーでサーバーを起動)
   - `StandardEarthquakeManager` (標準物理エンジンを利用した地震発生ロジック)
   - `NetworkObject` (同期用)

## 3. 物理オブジェクト（机・椅子）のプレハブ設定
シーン上に配置する（あるいは配置されている）机や椅子のプレハブに対して、以下のコンポーネントがアタッチされている必要があります。

1. **`MeshCollider` (または BoxCollider)**
   - 形状に合わせたColliderが必要です。MeshColliderを使用する場合は必ず **`Convex`** にチェックを入れてください（Rigidbodyで動かすため必須です）。
2. **`Rigidbody`**
   - 質量（Mass）を適切な値（机なら10〜20など）に設定します。
3. **`NetworkRigidbody3D`**
   - Photon FusionがRigidbodyの座標と回転をネットワーク同期するためのコンポーネントです。
   - 自動的に `NetworkObject` も追加されます。
4. **`ClientColliderDisabler`** (非常に重要)
   - WebGL環境でのパフォーマンス低下（16FPS問題）を防ぐため、**必ずこのスクリプトをアタッチしてください**。
   - クライアント側で不要な当たり判定計算をスキップし、60FPS描画を維持します。

## 4. 実行と地震のテスト
1. エディタの再生（Play）ボタンを押します。
2. Consoleウィンドウに `Ready. Press 'P' key to start Fusion...` と表示されたら、キーボードの「P」キーを押します。
3. Fusionが起動したら、`StandardEarthquakeManager` の機能により、**「Eキー」**でリアルなパターン地震、**「Tキー」**で単純な横揺れが発生します。
