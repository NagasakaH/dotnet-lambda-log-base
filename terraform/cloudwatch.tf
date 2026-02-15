# All-logs group (DELIVERY class - 2 day fixed retention, for S3 delivery)
resource "aws_cloudwatch_log_group" "all_logs" {
  name              = "/lambda/${var.app_name}/all-logs"
  log_group_class   = "DELIVERY"
  skip_destroy      = false

  tags = merge(var.tags, {
    Purpose = "all-logs"
    Class   = "DELIVERY"
  })
}

# Error-only log group (STANDARD class - configurable retention, shared across Lambdas)
resource "aws_cloudwatch_log_group" "error_logs" {
  name              = "/lambda/shared/error-logs"
  log_group_class   = "STANDARD"
  retention_in_days = var.error_log_retention_days
  skip_destroy      = false

  tags = merge(var.tags, {
    Purpose = "error-logs"
    Class   = "STANDARD"
  })
}

# Metric filter for error count on error log group
resource "aws_cloudwatch_log_metric_filter" "error_count" {
  name           = "${var.app_name}-error-count"
  pattern        = "{ $.level = \"Error\" || $.level = \"Critical\" }"
  log_group_name = aws_cloudwatch_log_group.error_logs.name

  metric_transformation {
    name          = "ErrorCount"
    namespace     = "Lambda/${var.app_name}"
    value         = "1"
    default_value = "0"
  }
}

# CloudWatch Alarm for error rate
resource "aws_cloudwatch_metric_alarm" "error_alarm" {
  alarm_name          = "${var.app_name}-error-alarm"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "ErrorCount"
  namespace           = "Lambda/${var.app_name}"
  period              = 300
  statistic           = "Sum"
  threshold           = 0
  alarm_description   = "Alarm when Lambda function logs errors"
  treat_missing_data  = "notBreaching"

  alarm_actions = var.alarm_email != "" ? [aws_sns_topic.alarm[0].arn] : []

  tags = var.tags
}
