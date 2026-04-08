import json
import os

import boto3


def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    dynamodb = boto3.resource("dynamodb", endpoint_url=endpoint)
    table = dynamodb.Table(os.environ["DYNAMODB_TABLE"])

    for record in event.get("Records", []):
        body = json.loads(record["body"])
        table.put_item(Item={"id": record["messageId"], "body": json.dumps(body)})

    return {"statusCode": 200}
