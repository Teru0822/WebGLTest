# Photon Fusion Architecture

本プロジェクトはWebGLで動作し、物理演算を伴う大規模な同期を行うため、Photon Fusionのアーキテクチャ設計を以下のように定義します。

## 1. ネットワークトポロジー (Server/Client Mode)
- **WebGLの制約**: WebGLビルドはPhoton Fusionにおいて「Host」や「Server」として機能することができません（ブラウザのソケット通信制約のため）。
- **採用モード**: **Server/Client Mode**
- **サーバー (Dedicated Server)**:
  - WindowsまたはLinux向けのHeadless Buildとして作成します。
  - すべての物理演算（机や椅子の挙動）、衝突判定、地震のトリガー管理はこのサーバーが `State Authority` を持って実行します。
- **クライアント (WebGL)**:
  - プレイヤーは常に「Client」としてサーバーに接続します。
  - クライアントは物理演算を行わず、サーバーから送られてくる Transform 情報を補間（Interpolation）して描画します。

## 2. 物理演算と同期 (Server-Auth Physics)
- **NetworkRigidbody**:
  - 動くオブジェクト（机、椅子、避難用具など）には `NetworkRigidbody` （または `NetworkRigidbody3D`）をアタッチします。
  - 物理演算はサーバーのみで実行されるため、Rigidbodyの `isKinematic` は原則として、サーバー上では `false`、クライアント上では `true` になるように制御されます（Fusionが自動的に処理しますが、明示的な操作が必要な場合は権限を確認します）。
- **NetworkObject**:
  - `State Authority` は常にサーバー（Dedicated Server）が持ちます。

## 3. クライアント側の入力 (Input)
- クライアント（プレイヤー）の移動などの操作は `NetworkInput` を通じてサーバーに送信されます。
- サーバー上で入力に基づいてプレイヤーのTransformを更新し、その結果が全クライアントに同期されます（クライアントサイド・プレディクションを使用するかどうかはプレイヤーの要件によって決定しますが、物理オブジェクトはプレディクションなしの補間で十分です）。

## 4. 起動時の振る舞い
- ビルドターゲットやコマンドライン引数（例: `-batchmode -nographics` 等）を判定し、自動的に `StartGame` のモードを `GameMode.Server` または `GameMode.Client` に振り分ける仕組みを実装します。
