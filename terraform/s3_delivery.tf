# IAM role for CloudWatch Logs to deliver logs to S3
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

# Subscription filter to deliver all-logs to S3
resource "aws_cloudwatch_log_subscription_filter" "s3_delivery" {
  name            = "${var.app_name}-s3-delivery"
  log_group_name  = aws_cloudwatch_log_group.all_logs.name
  filter_pattern  = ""
  destination_arn = aws_s3_bucket.log_delivery.arn
  role_arn        = aws_iam_role.cwl_to_s3.arn
}
