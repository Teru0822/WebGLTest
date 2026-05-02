# 開発履歴と実験の記録 (Development History & Experiment Logs)

このドキュメントは、プロジェクトの「初期状態（initial-state）」以降に実施された機能追加、アセットの導入、アーキテクチャの変更と、その結果（パフォーマンスへの影響、バグの有無など）を記録するためのログです。

新しい変更を加えるたびに、ここに追記してください。

---

## [2026-04-28] 初期状態の構築
* **変更内容**: 
  * Photon Fusion Server/Clientアーキテクチャの導入。
  * `GameStarter.cs` によるPキーでのマニュアル接続（エラー回避）の実装。
  * `EarthquakeManager.cs` におけるサーバー主導の物理演算と、GCアロケーションを防ぐためのRigidbodyキャッシュの導入。
  * WebGL向けのメモリ拡張 (`NetworkProjectConfig` の PageCount を 1024 に増加)。
* **結果 (Good/Bad)**:
  * **[Good]**: WebGLクライアントがエラー落ちせずに安定してサーバーへ接続可能になった。
  * **[Good]**: 地震を長時間発生させてもGCスパイクによるカクつき（FPS低下）が発生しなくなった。ベースラインとして非常に安定している。

---

## [2026-05-03] 標準物理演算への回帰とWebGLパフォーマンス最適化
* **変更内容**:
  * 独自物理演算（Custom XZ Physics）を破棄し、Photon Fusion本来の `NetworkRigidbody3D` とUnityの `MeshCollider` を使用する `StandardEarthquakeManager` ベースにアーキテクチャを回帰。
  * WebGLでの描画・物理負荷（16FPS問題）を解決するため、以下の最適化を導入。
    * マテリアルの GPU Instancing 有効化、およびそれを自動で行う `WebGLPerformanceOptimizer` (Editor拡張) の作成。
    * クライアント側でのみColliderを無効化し、Unity Physicsの再計算をスキップする `ClientColliderDisabler.cs` の導入。
    * `TargetFPS.cs` による4K解像度等の上限制限（フルHD化）。
  * ブラウザのハードウェアアクセラレーションを含む設定手順を `Docs/Rules/WebGLOptimization.md` にドキュメント化。
* **結果 (Good/Bad)**:
  * **[Good]**: WebGLクライアント環境においても、標準物理演算を用いながら60FPS付近を安定してキープできるようになった。
  * **[Good]**: 描画負荷（Draw Call）と物理同期負荷（SyncTransforms）の根本原因が排除され、どの環境でも一貫したパフォーマンスが出せるようになった。

---
