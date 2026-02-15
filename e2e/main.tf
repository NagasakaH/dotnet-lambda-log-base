terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "app_name" {
  description = "Application name for E2E test"
  type        = string
  default     = "e2e-test-log-base"
}

variable "error_log_retention_days" {
  description = "Error log group retention in days"
  type        = number
  default     = 1
}

variable "s3_bucket_prefix" {
  description = "S3 bucket name prefix"
  type        = string
  default     = "lambda-logs"
}

variable "tags" {
  description = "Tags for all resources"
  type        = map(string)
  default = {
    Environment = "e2e-test"
    ManagedBy   = "terraform"
  }
}

data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

# ============================================================
# CloudWatch Log Groups
# ============================================================

resource "aws_cloudwatch_log_group" "all_logs" {
  name            = "/lambda/${var.app_name}/all-logs"
  log_group_class = "DELIVERY"
  skip_destroy    = false

  tags = merge(var.tags, {
    Purpose = "all-logs"
    Class   = "DELIVERY"
  })
}

resource "aws_cloudwatch_log_group" "error_logs" {
  name              = "/lambda/${var.app_name}/error-logs"
  log_group_class   = "STANDARD"
  retention_in_days = var.error_log_retention_days
  skip_destroy      = false

  tags = merge(var.tags, {
    Purpose = "error-logs"
    Class   = "STANDARD"
  })
}

# ============================================================
# Metric Filter & Alarm
# ============================================================

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

  tags = var.tags
}

# ============================================================
# S3 Bucket for Log Delivery
# ============================================================

resource "aws_s3_bucket" "log_delivery" {
  bucket = "${var.s3_bucket_prefix}-${var.app_name}-${data.aws_caller_identity.current.account_id}"

  tags = merge(var.tags, {
    Purpose = "log-delivery"
  })
}

resource "aws_s3_bucket_public_access_block" "log_delivery" {
  bucket = aws_s3_bucket.log_delivery.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_lifecycle_configuration" "log_delivery" {
  bucket = aws_s3_bucket.log_delivery.id

  rule {
    id     = "archive-logs"
    status = "Enabled"
    filter {}

    transition {
      days          = 30
      storage_class = "GLACIER"
    }

    expiration {
      days = 365
    }
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "log_delivery" {
  bucket = aws_s3_bucket.log_delivery.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# ============================================================
# S3 Delivery (Subscription Filter)
# ============================================================

resource "aws_iam_role" "cwl_to_s3" {
  name = "${var.app_name}-cwl-to-s3-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect    = "Allow"
        Principal = { Service = "logs.amazonaws.com" }
        Action    = "sts:AssumeRole"
        Condition = {
          StringLike = {
            "aws:SourceArn" = "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:*"
          }
        }
      }
    ]
  })

  tags = var.tags
}

resource "aws_iam_role_policy" "cwl_to_s3" {
  name = "${var.app_name}-cwl-to-s3-policy"
  role = aws_iam_role.cwl_to_s3.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["s3:PutObject"]
        Resource = ["${aws_s3_bucket.log_delivery.arn}/*"]
      }
    ]
  })
}

resource "aws_cloudwatch_log_subscription_filter" "s3_delivery" {
  name            = "${var.app_name}-s3-delivery"
  log_group_name  = aws_cloudwatch_log_group.all_logs.name
  filter_pattern  = ""
  destination_arn = aws_s3_bucket.log_delivery.arn
  role_arn        = aws_iam_role.cwl_to_s3.arn
}

# ============================================================
# Lambda Function
# ============================================================

resource "aws_iam_role" "lambda" {
  name = "${var.app_name}-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect    = "Allow"
        Principal = { Service = "lambda.amazonaws.com" }
        Action    = "sts:AssumeRole"
      }
    ]
  })

  tags = var.tags
}

resource "aws_iam_role_policy" "lambda" {
  name = "${var.app_name}-lambda-policy"
  role = aws_iam_role.lambda.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "logs:DescribeLogStreams"
        ]
        Resource = [
          "${aws_cloudwatch_log_group.all_logs.arn}:*",
          "${aws_cloudwatch_log_group.error_logs.arn}:*"
        ]
      }
    ]
  })
}

resource "aws_lambda_function" "test" {
  function_name = var.app_name
  role          = aws_iam_role.lambda.arn
  handler       = "DotnetLambdaLogBase::DotnetLambdaLogBase.Function::FunctionHandler"
  runtime       = "dotnet8"
  timeout       = 30
  memory_size   = 256
  filename      = "${path.module}/lambda.zip"

  source_code_hash = filebase64sha256("${path.module}/lambda.zip")

  environment {
    variables = {
      ALL_LOGS_GROUP   = aws_cloudwatch_log_group.all_logs.name
      ERROR_LOGS_GROUP = aws_cloudwatch_log_group.error_logs.name
    }
  }

  tags = var.tags
}

# ============================================================
# Outputs
# ============================================================

output "function_name" {
  value = aws_lambda_function.test.function_name
}

output "function_arn" {
  value = aws_lambda_function.test.arn
}

output "all_logs_group_name" {
  value = aws_cloudwatch_log_group.all_logs.name
}

output "error_logs_group_name" {
  value = aws_cloudwatch_log_group.error_logs.name
}

output "s3_bucket_name" {
  value = aws_s3_bucket.log_delivery.bucket
}

output "s3_bucket_arn" {
  value = aws_s3_bucket.log_delivery.arn
}

output "metric_filter_name" {
  value = aws_cloudwatch_log_metric_filter.error_count.name
}

output "alarm_name" {
  value = aws_cloudwatch_metric_alarm.error_alarm.alarm_name
}
