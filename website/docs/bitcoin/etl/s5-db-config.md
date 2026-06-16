---
title: Neo4j Config
description: Step 6. Database Tuning
sidebar_label: Neo4j Configuration
sidebar_position: 6
slug: /bitcoin/etl/db-conf
---

After successfully 
[importing](./import) or 
[restoring](./restore) the database, 
you need to apply a few configurations before using it. 
This page explains those necessary steps.


## DBMS Configuration

Given the scale of the Bitcoin graph (billions of nodes and edges), 
the Neo4j Database Management System (DBMS) 
will likely run out of memory with the default configuration, 
potentially failing even during startup.


Therefore, you need to update and increase the default memory settings.

1.  Open `neo4j.conf`.
    You may follow [this documentation](https://neo4j.com/docs/operations-manual/current/configuration/neo4j-conf/) 
    on locating the file depending on the Neo4j deployment you are using.

2.  Make the changes listed below. 
    Note that the values provided are merely examples; 
    you should adjust them based on your system's available memory.

    ```bash
    server.memory.heap.initial_size=4G
    server.memory.heap.max_size=16G
    server.memory.pagecache.size=16G
    ```
