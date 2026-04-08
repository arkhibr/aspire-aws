def lambda_handler(event, context):
    result = dict(event)
    result["processed"] = True
    result["step"] = event.get("step", "unknown")
    return result
