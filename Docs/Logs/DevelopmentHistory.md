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
