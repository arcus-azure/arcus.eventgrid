---
name: Request an event schema
about: Request support for a new event schema (version)

---

**Name of the requested event**
<!-- Please create an issuer per event -->
`Arcus.Demo.CarRegistered`

**Requested data version(s)**
Requested data version(s) for the event

**Sample Payload**
```json
[{
  "topic": "/car-lot",
  "subject": "/cars/",
  "eventType": "Arcus.Demo.CarRegistered",
  "eventTime": "2017-06-26T18:41:00.9584103Z",
  "id": "831e1650-001e-001b-66ab-eeb76e069631",
  "data": {
    "licensePlate": "1-TOM-1337"
  },
  "dataVersion": "1",
  "metadataVersion": "1"
}]
```

**Link to official schema**
https://docs.microsoft.com/en-us/azure/event-grid/event-schema-blob-storage
