# カスタムXZ物理演算セットアップ手順 (Custom Physics Setup Guide)

シーンにオブジェクトが何もない状態から、独自の物理演算オブジェクト（机・椅子）を配置し、Photonサーバーを起動して地震（物理演算）を発生させるまでの手順です。

## 1. シーンの基本準備
1. Unityで新しいシーンを作成します。
2. 床となるオブジェクト（Cubeなど）を配置し、スケールを広げます（例: `Scale (20, 1, 20)`）。
   - **注意**: 床オブジェクトには `CustomPhysicsObject` を**アタッチしないでください**。床は動かない静的な背景オブジェクトとして扱われます。

## 2. ネットワークとマネージャーの配置
1. 空のGameObjectを作成し、名前を `NetworkManager` とします。
2. `NetworkManager` に以下のスクリプトをアタッチします。
   - `GameStarter` (Pキーでサーバーを起動するためのスクリプト)
3. **プレハブから**（または新規の空GameObjectとして）`CustomPhysicsManager` をシーンに配置します。
4. `CustomPhysicsManager` GameObjectに以下のコンポーネントがアタッチされていることを確認します。
   - `NetworkObject` (Photonが同期するために必須)
   - `CustomPhysicsManager` (独自物理ロジックの本体)

## 3. 物理オブジェクト（机・椅子）の配置
1. シーン上に机や椅子となるオブジェクト（UnityデフォルトのCubeでも可）を配置します。
2. **既存の物理コンポーネントの削除**:
   - 配置したオブジェクトに標準の `Rigidbody` や `BoxCollider` （またはMeshCollider）が付いている場合は**すべて削除（Remove Component）**します。本システムでは使用しません。
3. **カスタム物理コンポーネントの追加**:
   - 対象オブジェクトに `CustomPhysicsObject` スクリプトをアタッチします。
4. **コライダー（メッシュ）の設定**:
   - インスペクターの `CustomPhysicsObject` コンポーネントにある `Collision Mesh` 枠に、**当たり判定用に使いたいローポリメッシュ**（Cubeのメッシュ等）をドラッグ＆ドロップで割り当てます。
   - `Mass`（重さ）や `Topple Threshold`（倒れやすさ）の数値を必要に応じて調整します。
5. （任意）設定が完了したオブジェクトを複数コピーして、シーン上にばら撒きます。

## 4. 実行と地震のテスト
1. エディタの再生（Play）ボタンを押します。
2. Consoleウィンドウに `Ready. Press 'P' key to start Fusion...` と表示されたら、**キーボードの「P」キー**を押します。
3. `GameStarter` が作動し、Fusionが起動（エディタの場合はHostモードで起動）します。
4. 起動すると、サーバー権限で `CustomPhysicsManager` が作動し、シーン内のすべての `CustomPhysicsObject` を自動的に配列へ収集します。
5. **地震発生**: 現在のプロトタイプでは、起動直後から `FixedUpdateNetwork` 内の「仮の地震加速度（Sin波による横揺れ）」が適用されます。机が滑る、ぶつかり合って反発する、一定確率で倒れるといった物理挙動が同期して確認できます。

---
> [!TIP]
> **トラブルシューティング**:
> * オブジェクトが全く動かない場合： `CustomPhysicsManager` に `NetworkObject` がアタッチされているか、PキーでFusionが正しく起動できたかを確認してください。
> * Consoleに「Collision Mesh が設定されていません」と出る場合： 対象オブジェクトのインスペクターでメッシュがアサインされているか確認してください。アサインされていない場合、そのオブジェクトは物理演算から除外されます。
