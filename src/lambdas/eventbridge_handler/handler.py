import json
import os

import boto3


def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    dynamodb = boto3.resource("dynamodb", endpoint_url=endpoint)
    table = dynamodb.Table(os.environ["DYNAMODB_TABLE"])

    table.put_item(
        Item={
            "id": context.aws_request_id,
            "source": event.get("source", "unknown"),
            "detail_type": event.get("detail-type", "unknown"),
            "detail": json.dumps(event.get("detail", {})),
        }
    )

    return {"statusCode": 200}
