output "all_logs_group_name" {
  description = "CloudWatch Log Group name for all logs (DELIVERY class)"
  value       = aws_cloudwatch_log_group.all_logs.name
}

output "all_logs_group_arn" {
  description = "CloudWatch Log Group ARN for all logs"
  value       = aws_cloudwatch_log_group.all_logs.arn
}

output "error_logs_group_name" {
  description = "CloudWatch Log Group name for error logs (STANDARD class)"
  value       = aws_cloudwatch_log_group.error_logs.name
}

output "error_logs_group_arn" {
  description = "CloudWatch Log Group ARN for error logs"
  value       = aws_cloudwatch_log_group.error_logs.arn
}

output "s3_bucket_name" {
  description = "S3 bucket name for log delivery"
  value       = aws_s3_bucket.log_delivery.bucket
}

output "s3_bucket_arn" {
  description = "S3 bucket ARN for log delivery"
  value       = aws_s3_bucket.log_delivery.arn
}

output "sns_topic_arn" {
  description = "SNS topic ARN for alarms (empty if no email configured)"
  value       = var.alarm_email != "" ? aws_sns_topic.alarm[0].arn : ""
}
