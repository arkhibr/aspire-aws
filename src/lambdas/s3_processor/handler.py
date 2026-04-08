import os

import boto3


def lambda_handler(event, context):
    endpoint = os.environ.get("AWS_ENDPOINT_URL")
    dynamodb = boto3.resource("dynamodb", endpoint_url=endpoint)
    table = dynamodb.Table(os.environ["DYNAMODB_TABLE"])

    for record in event.get("Records", []):
        bucket = record["s3"]["bucket"]["name"]
        key = record["s3"]["object"]["key"]
        table.put_item(Item={"key": key, "bucket": bucket, "status": "processed"})

    return {"statusCode": 200}
