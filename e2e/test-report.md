# E2E Test Report

実行日時: 2026-02-15 05:03 UTC
リージョン: us-east-1
アプリ名: e2e-test-log-base

## 機能要件テスト

| テストID | テスト名 | 結果 | 詳細 |
|----------|----------|------|------|
| F1 | Terraform apply 正常完了 | ✅ PASS | リソース数: 16 (Log Groups, S3, IAM, Lambda, Alarm, Metric Filter, Subscription Filter) |
| F2 | Lambda デプロイ＆正常実行 | ✅ PASS | StatusCode=200, Response="OK" |
| F3 | all-logs ログストリーム作成 | ✅ PASS | ストリーム数: 1, 名前: 2026/02/15/e2e-test-log-base/bd2c02725fc94ee483c3a90179c967d3 |
| F4 | error-logs ログストリーム作成 | ✅ PASS | ストリーム数: 1, エラーログが error-logs に書き込まれた |
| F5 | JSON構造化ログ形式 | ✅ PASS | level/message/timestamp/category フィールド確認済み |
| F6 | ログストリーム命名規則 | ✅ PASS | `{yyyy/MM/dd}/{FunctionName}/{GUID}` 形式確認済み |
| F7 | Metric Filter 動作確認 | ✅ PASS | ErrorCount データポイント: 1, Sum: 1 |
| F8 | 異常終了時のログFlush | ✅ PASS | InvalidOperationException 発生後、FlushAsync により error-logs にエラーログが記録された |

## 非機能要件テスト

| テストID | テスト名 | 結果 | 詳細 |
|----------|----------|------|------|
| N1 | S3バケット暗号化 | ✅ PASS | SSEAlgorithm: AES256 |
| N2 | S3パブリックアクセスブロック | ✅ PASS | BlockPublicAcls=true, BlockPublicPolicy=true, IgnorePublicAcls=true, RestrictPublicBuckets=true |
| N3 | S3ライフサイクル設定 | ✅ PASS | 30日 GLACIER 移行, 365日 削除 |
| N4 | S3ログ配信パス確認 | ✅ PASS | 配信パス: AWSLogs/{AccountId}/{Region}/{LogGroupName}/{yyyy}/{MM}/{dd}/{HH}/ |
| N5 | Terraform destroy クリーンアップ | ✅ PASS | 全リソース削除完了（S3バケットは事前に空にして削除） |

## 合計

**PASS: 13 / FAIL: 0**

## E2E テストで発見・修正した問題

### 1. NuGet パッケージのバージョン互換性

| パッケージ | 修正前 | 修正後 | 理由 |
|-----------|--------|--------|------|
| AWSSDK.CloudWatchLogs | 4.0.14.5 | 3.7.408.2 | v4 は .NET 9+ 必須、Lambda Runtime は .NET 8 |
| Microsoft.Extensions.DependencyInjection | 10.0.3 | 8.0.1 | v10 は .NET 10 Preview、.NET 8 非互換 |
| Microsoft.Extensions.Logging | 10.0.3 | 8.0.1 | 同上 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.3 | 8.0.2 | 同上 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.3 | 8.0.2 | 同上 |

### 2. csproj 設定不足

- `GenerateRuntimeConfigurationFiles` が未設定 → Lambda Runtime で exit status 106 エラー
- `AWSProjectType` が未設定 → Lambda プロジェクトとして認識されない

### 3. S3 配信設定の欠落

- DELIVERY class ロググループに S3 Subscription Filter が未設定だった
- IAM ロール + Subscription Filter を `terraform/s3_delivery.tf` として追加
