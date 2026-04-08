import json
import os

import boto3


def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    s3 = boto3.client("s3", endpoint_url=endpoint)
    bucket = os.environ["S3_BUCKET"]

    for record in event.get("Records", []):
        outer = json.loads(record["body"])
        message = outer.get("Message", record["body"])
        key = f"results/{record['messageId']}.json"
        s3.put_object(Bucket=bucket, Key=key, Body=message)

    return {"statusCode": 200}
