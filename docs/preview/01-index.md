---
title: "Arcus Event Grid"
layout: default
slug: /
sidebar_label: Welcome
---

# Introduction

Arcus EventGrid allows you to work more easily with Azure EventGrid during event publishing and parsing. Instead of worrying about the event type (EventGrid events, CloudEvent events, custom events), Arcus EventGrid allows you to interact with events in a simple manner by parsing the events to a canonical format. Same goes with publishing events where Arcus EventGrid lets you publish in a secure and transient fashion events on EventGrid.

EventGrid is often used in integration tests, so we made sure that retrieving events is easy, transient, and customizable.

# Installation

```shell
PM > Install-Package Arcus.EventGrid.All
```

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.eventgrid/blob/master/LICENSE)*
