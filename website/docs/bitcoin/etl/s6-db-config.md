---
title: Neo4j Config & Schema Init
description: Step 6. Database Tuning and Constraint Definitions
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

## Schema Application

To improve query performance 
(e.g., during neighborhood sampling), 
we define database constraints, such as uniqueness constraints, 
which index commonly used properties.


EBA generates a `schema.cypher` file containing the queries required 
to create these constraints and indexes. 
EBA creates this file in the same directory where the Bitcoin graph was serialized. 


The Cypher queries in the `schema.cypher` file are as follows:

```javascript
// EBA Bitcoin Graph Schema

// Uniqueness constraint for Script.Address property.
CREATE CONSTRAINT Script_Address_Unique 
IF NOT EXISTS 
FOR (v:Script) REQUIRE v.Address IS UNIQUE;

// Uniqueness constraint for Tx.Txid property.
CREATE CONSTRAINT Tx_Txid_Unique 
IF NOT EXISTS 
FOR (v:Tx) REQUIRE v.Txid IS UNIQUE;

// Uniqueness constraint for Block.Height property.
CREATE CONSTRAINT Block_Height_Unique 
IF NOT EXISTS 
FOR (v:Block) REQUIRE v.Height IS UNIQUE;
```


Take the following steps to apply these constraints to your database:

1.  Start the Neo4j database service.

2.  Execute the schema.cypher file using cypher-shell:

    ```Shell
    cypher-shell -u neo4j -p password -f schema.cypher
    ```

    Please refer to 
    [this documentation](https://neo4j.com/docs/operations-manual/current/cypher-shell/) 
    for details on locating and running `cypher-shell` depending on your Neo4j deployment.
