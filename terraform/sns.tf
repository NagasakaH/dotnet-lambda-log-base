resource "aws_sns_topic" "alarm" {
  count = var.alarm_email != "" ? 1 : 0

  name = "${var.app_name}-alarm-topic"

  tags = var.tags
}

resource "aws_sns_topic_subscription" "alarm_email" {
  count = var.alarm_email != "" ? 1 : 0

  topic_arn = aws_sns_topic.alarm[0].arn
  protocol  = "email"
  endpoint  = var.alarm_email
}
