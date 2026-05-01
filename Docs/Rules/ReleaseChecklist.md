# 本番ビルド用チェックリスト (Release Checklist)

このドキュメントは、大学の研究発表や本番公開に向けて「最終ビルド」を行う前に確認・設定を戻すべき項目をまとめたリストです。
開発中のテストビルドを高速化するために設定した項目を、品質優先に戻す作業になります。

---

## WebGLビルドの本番設定（元に戻す項目）

### 1. 圧縮設定を本番用（Brotli）に戻す
開発中はビルド時間を短縮するために圧縮を切っていましたが、本番ではファイルサイズを小さくしてロードを速くするために強力な圧縮をかけます。
* **場所**: `Edit > Project Settings > Player > WebGLタブ > Publishing Settings`
* **設定**: **`Compression Format`** を `Disabled` から **`Brotli`** (または `Gzip`) に戻す。

### 2. C++コンパイラを最高速度（Master）に戻す
開発中はビルド時間を優先してDebug設定にしていましたが、本番では実行時のFPS（フレームレート）を最高にするためにMasterに戻します。
* **場所**: `Edit > Project Settings > Player > WebGLタブ > Other Settings` の下の方
* **設定**: **`C++ Compiler Configuration`** を `Debug` から **`Master`** に戻す。

### 3. コードの削除（Stripping）設定を見直す
これはエラーが出なければそのままでも良いですが、不要なコードを削って容量を軽くするための設定です。
* **場所**: 同じく `Other Settings` の中
* **設定**: **`Managed Stripping Level`** を `Minimal` から **`Low`**（または必要に応じて `Medium` / `High`）に戻す。

---

> [!WARNING]
> これらの設定をすべて本番用（Brotli + Master）に戻すと、**ビルドに数十分〜1時間近くかかる可能性があります。**
> 研究発表の直前に慌ててビルドするのではなく、前日など時間に余裕がある時に最終書き出しを行うようにしてください！
