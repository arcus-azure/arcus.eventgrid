---
title: "Supported Azure Event Contracts"
layout: default
---

## Supported Azure Event Contracts

![](https://img.shields.io/badge/Available%20starting-v1.0-green)
![](https://img.shields.io/badge/Until%20inclusive-v2.4-green?link=https://github.com/arcus-azure/arcus.eventgrid/releases/tag/v2.4.0)

Currently we provide support for the following Azure events:
- **Azure Storage** ([docs](https://docs.microsoft.com/en-us/azure/event-grid/event-schema-blob-storage))
   - `Microsoft.Storage.BlobCreated`
- **Azure Event Hubs** ([docs](https://docs.microsoft.com/en-us/azure/event-grid/event-schema-event-hubs))
   - `Microsoft.EventHub.CaptureFileCreated`
- **Azure IoT Hub** ([docs](https://docs.microsoft.com/en-us/azure/event-grid/event-schema-iot-hub))
   - `Microsoft.Devices.DeviceCreated`
   - `Microsoft.Devices.DeviceDeleted`

[&larr; back](/arcus.eventgrid)