variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "ap-northeast-1"
}

variable "app_name" {
  description = "Application name (used for all-logs group naming)"
  type        = string
}

variable "error_log_retention_days" {
  description = "Error log group retention in days"
  type        = number
  default     = 7
}

variable "s3_bucket_prefix" {
  description = "S3 bucket name prefix for log delivery"
  type        = string
  default     = "lambda-logs"
}

variable "alarm_email" {
  description = "Email address for alarm notifications (optional)"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}
