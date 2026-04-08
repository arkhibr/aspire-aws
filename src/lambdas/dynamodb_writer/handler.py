import json
import os

import boto3


def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    dynamodb = boto3.resource("dynamodb", endpoint_url=endpoint)
    table = dynamodb.Table(os.environ["DYNAMODB_TABLE"])

    payload = event if isinstance(event, dict) else json.loads(event)
    table.put_item(Item={"id": payload["id"], "data": json.dumps(payload)})

    return {"statusCode": 200, "id": payload["id"]}
