# WebGL Earthquake Simulation - Vibe Coding Hub (AGENTS.md)

このドキュメントは、バイブコーディング（AIエージェントを活用した開発）を円滑に進めるためのハブドキュメントです。
プロンプト実行時にAIエージェント（主にClaude、次にGemini）はこのファイルを起点とし、関連する詳細ルールを参照して開発を進めてください。

## 1. プロジェクト概要
- **目的**: WebGL環境で動作する地震避難訓練アプリケーションの開発。
- **コア技術**: Unity, Photon Fusion (Server/Client), WebGL
- **主な特徴**:
  - 大量のオブジェクト（机、椅子など）の物理演算・衝突判定は**専用サーバー（Dedicated Server）**で処理。
  - WebGLクライアントはサーバーから同期された状態を受け取り描画。

## 2. 詳細ルールへのリンク (Docs/Rules/)

各ジャンルごとの詳細なルールや設計思想は以下のドキュメントに分割されています。作業内容に応じて適切なドキュメントを参照してください。

* [**Workflow.md**](./Docs/Rules/Workflow.md)
  * AIエージェントとの協調開発ルール、コーディングスタイル、タスク進行方法について。
* [**PhotonArchitecture.md**](./Docs/Rules/PhotonArchitecture.md)
  * Photon Fusionの構成（Server/Clientモード）、サーバー側物理演算の設計、ネットワーク同期のルールについて。
* [**PhotonFusionKnowledge.md**](./Docs/Rules/PhotonFusionKnowledge.md)
  * Photon Fusionに関する仕様や、プロジェクト進行で得られた重要な情報、不要・非推奨なアプローチなどを蓄積するナレッジベース。
* [**SimulationDesign.md**](./Docs/Rules/SimulationDesign.md)
  * 地震のシミュレーション仕様、オブジェクト配置、揺れの制御、プレイヤーのインタラクションについて。
* [**CustomPhysicsSetup.md**](./Docs/Rules/CustomPhysicsSetup.md)
  * 今回独自に実装した超軽量な物理演算システム（Custom XZ Physics）のセットアップ手順。

## 3. エージェントへの指示（AIプロンプト時の注意事項）
* **優先事項**: 変更を加える前に、必ず上記の関連ルールファイルを確認してください。
* **ドキュメントの更新**: 新しい設計やルールが決定した場合は、該当する `.md` ファイルを更新し、知識をドキュメント化してください。
* **Photon Fusionの制約**: WebGLは「Host/Server」になれないため、常に「専用サーバー」と「WebGLクライアント」の構成であることを前提にコードを書いてください。

## 4. 初期状態への復帰 (Revert to Initial State)
* ユーザーから「初期状態に戻して」またはそれに類する指示があった場合、AIエージェントは直ちにGitの `initial-state` タグへ状態をリセットしてください。
* **復帰手順**:
  1. `git reset --hard initial-state` を実行する。
  2. 未追跡ファイルが残っている場合は `git clean -fd` でクリーンアップする。
  3. （必要に応じて）リモートリポジトリへの強制プッシュを行うかユーザーに確認する。

## 5. 開発履歴とアプローチ検証のルール (Development & Evaluation Workflow)
今後の開発、特にパフォーマンス最適化や新機能の導入においては、必ず以下のサイクルで進めてください。

1. **アプローチの提案**: 改善案や新手法をエージェントから複数提案する。
2. **導入して検証**: 提案したアプローチを単体、または組み合わせて実装し検証する。
3. **良し悪しの判定**: パフォーマンスや安定性への影響（単体・組み合わせによる相乗効果など）を評価する。
4. **導入決定**: 最も優れたアプローチ（またはその組み合わせ）を正式に採用する。

**関連ドキュメント（検証ごとに必ず更新すること）**:
* `Docs/Logs/DevelopmentHistory.md`: 正式に導入が決定した機能や確定した歴史の記録。
* `Docs/Logs/ActiveApproaches.md`: 現在遂行中・検証予定のアプローチ一覧とそのステータス。
* `Docs/Logs/ApproachEvaluations.md`: 検証結果、単体・組み合わせの良し悪し、および最終的な判定の記録。
