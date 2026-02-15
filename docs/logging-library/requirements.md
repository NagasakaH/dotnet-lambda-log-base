# ログライブラリ 要件定義

## 1. 背景と課題

### 1.1 背景

AWS Lambda (.NET 8) でアプリケーションログを管理する際、以下の要件を同時に満たす必要がある：

- **全ログの長期保管**: 監査・障害調査のために全ログを安価に長期保管
- **エラーログのリアルタイム監視**: エラー発生時に即座に検知・通知
- **Lambda 横断のエラー集約**: 複数 Lambda のエラーを一元監視

### 1.2 課題

AWS Lambda 標準の CloudWatch Logs 出力には以下の制限がある：

| 課題 | 詳細 |
|---|---|
| 単一ロググループ | 全ログが 1 つのロググループに出力され、用途別の管理ができない |
| コスト効率 | STANDARD class のみ使用可能で、大量ログの長期保管コストが高い |
| 構造化ログ | 標準の `Console.WriteLine` では構造化ログ（JSON）に対応しない |
| ログの自動振り分け | ログレベルに応じた振り分けが組み込まれていない |
| Lambda コンテナ再利用 | `IDisposable` パターンがコンテナ再利用と相性が悪い |

### 1.3 解決アプローチ

`Microsoft.Extensions.Logging` の `ILoggerProvider` / `ILogger` インターフェースを実装したカスタムプロバイダーを開発し、CloudWatch Logs の PutLogEvents API を直接使用してログを 2 つのロググループに振り分ける。

## 2. 機能要件

### FR-1: 構造化ログ出力

- `ILogger` インターフェースを通じて JSON 形式の構造化ログを出力できること
- ログエントリは `timestamp`、`level`、`category`、`message`、`exception`、`properties` を含むこと

### FR-2: 2 グループへのログ振り分け

- 全レベルのログを **all-logs グループ**（DELIVERY class）に送信すること
- Error 以上のログを **error-logs グループ**（STANDARD class）にも送信すること
- 振り分けはアプリケーション内で行い、CloudWatch の機能には依存しないこと

### FR-3: FlushAsync パターン

- Lambda コンテナの再利用に対応し、`FlushAsync()` でログを送信できること
- `DisposeAsync()` を呼ばずにログ送信を完了できること

### FR-4: PutLogEvents バッチ処理

- CloudWatch Logs API の制限に自動対応すること：
  - 1 バッチ最大 1MB
  - 1 バッチ最大 10,000 イベント
  - 1 イベント最大 256KB

### FR-5: DI コンテナ統合

- `IServiceCollection` / `ILoggingBuilder` への拡張メソッドで登録できること
- `AddCloudWatchLogger(Action<CloudWatchLoggerOptions>)` で設定可能なこと

### FR-6: ログストリーム自動作成

- ログストリームを `{yyyy/MM/dd}/{FunctionName}/{GUID}` 形式で自動作成すること
- 既存ストリームが存在する場合はエラーにならないこと

### FR-7: S3 へのログ長期保管

- all-logs グループから Subscription Filter 経由で S3 バケットにログを配信すること
- S3 バケットは AES256 暗号化、パブリックアクセス禁止であること
- Lifecycle ルール: 30 日後に GLACIER へ移行、365 日後に削除

### FR-8: エラー監視・通知

- error-logs グループに Metric Filter を設定し、Error/Critical ログをカウントすること
- CloudWatch Alarm でエラー発生を検知すること
- `alarm_email` 設定時のみ SNS Topic を作成し、メール通知すること

## 3. 非機能要件

### NFR-1: スレッドセーフティ

- `ConcurrentQueue` を使用し、マルチスレッド環境で安全にログをバッファリングすること

### NFR-2: 耐障害性

- ログ送信の失敗がアプリケーションの処理に影響しないこと（例外を swallow すること）

### NFR-3: バッファ管理

- バッファサイズの上限（デフォルト: 10,000 エントリ）を超えた場合、最も古いエントリから破棄すること

### NFR-4: 設定可能性

- 以下の項目を設定可能であること：
  - ロググループ名（all-logs / error-logs）
  - 最小ログレベル（全体 / エラーグループ）
  - バッファサイズ上限
  - Lambda 関数名

### NFR-5: セキュリティ

- S3 バケットのパブリックアクセスを完全ブロックすること
- S3 バケットの暗号化（AES256）を有効にすること
- IAM ロールは最小権限（`s3:PutObject` のみ）とすること

## 4. 受け入れ条件

| No | 条件 | 検証方法 |
|---|---|---|
| AC-1 | `ILogger` 経由で JSON 構造化ログが出力される | 単体テスト (FR-1) |
| AC-2 | 全ログが all-logs、Error+ が error-logs に振り分けられる | 単体テスト + E2E テスト (FR-2) |
| AC-3 | `FlushAsync()` でログが確実に送信される | 単体テスト + E2E テスト (FR-3) |
| AC-4 | API 制限を超えるバッチが自動分割される | 単体テスト (FR-4) |
| AC-5 | DI 拡張メソッドで簡単に登録できる | ビルドテスト (FR-5) |
| AC-6 | ログストリームが命名規則に従い自動作成される | E2E テスト (FR-6) |
| AC-7 | S3 にログが配信され、Lifecycle が適用されている | E2E テスト (FR-7) |
| AC-8 | エラー発生時にアラームが発火する | E2E テスト (FR-8) |
| AC-9 | マルチスレッドで安全に動作する | 単体テスト (NFR-1) |
| AC-10 | ログ送信失敗時にアプリが停止しない | 単体テスト (NFR-2) |

## 5. テスト結果

### 5.1 E2E テスト結果

E2E テストは実際の AWS 環境（ap-northeast-1）で実施。Terraform apply → Lambda デプロイ → テスト実行 → Terraform destroy の全サイクルを検証。

#### 機能要件テスト

| テストID | テスト名 | 関連要件 | 結果 |
|---|---|---|---|
| F1 | Terraform apply 正常完了 | FR-7, FR-8 | ✅ PASS |
| F2 | Lambda デプロイ＆正常実行 | FR-1 | ✅ PASS |
| F3 | all-logs ログストリーム作成 | FR-2, FR-6 | ✅ PASS |
| F4 | error-logs ログストリーム作成 | FR-2, FR-6 | ✅ PASS |
| F5 | JSON 構造化ログ形式 | FR-1 | ✅ PASS |
| F6 | ログストリーム命名規則 | FR-6 | ✅ PASS |
| F7 | Metric Filter 動作確認 | FR-8 | ✅ PASS |
| F8 | 異常終了時のログ Flush | FR-3 | ✅ PASS |

#### 非機能要件テスト

| テストID | テスト名 | 関連要件 | 結果 |
|---|---|---|---|
| N1 | S3 バケット暗号化（AES256） | NFR-5 | ✅ PASS |
| N2 | S3 パブリックアクセスブロック | NFR-5 | ✅ PASS |
| N3 | S3 ライフサイクル設定 | FR-7 | ✅ PASS |
| N4 | S3 ログ配信パス確認 | FR-7 | ✅ PASS |
| N5 | Terraform destroy クリーンアップ | — | ✅ PASS |

**E2E テスト合計: PASS 13 / FAIL 0**

詳細は [`e2e/test-report.md`](../../e2e/test-report.md) を参照。
