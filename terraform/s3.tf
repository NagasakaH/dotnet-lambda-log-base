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

data "aws_caller_identity" "current" {}
