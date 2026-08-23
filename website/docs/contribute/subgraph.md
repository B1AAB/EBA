---
title: Database Subsetting
description: Subset the EBA Neo4j database and push the result to a Neo4j Aura instance
sidebar_position: 8
---


EBA's database spans the entire Bitcoin history and includes a full list of all blocks, transactions, and scripts.
The resulting database is a multi-billion node and edge database, with a file size of over 1.7&nbsp;TB.

Hosting a database of this size demands significant computational resources.

For many use-cases, the entire database is not needed — e.g., for testing purposes in CI/CD.
While for many such use-cases you may simply create a mock database with dummy data, 
there are situations where real data is preferred.
For instance, we may want to host a demo database that only includes 
transactions related to a specific use-case.
In such scenarios, it is better to subset only the needed information 
from the main database, so that only the intended nodes/edges are included, 
resulting in a much more focused and smaller database.

This page explains how to subset the original database to include 
(a) transactions related to 
_Individual X_ (see [this video for details](https://www.youtube.com/watch?v=327Q97uo4pw)), 
and (b) all transactions at a 2-hop distance from blocks 799,990 to 800,000, 
and then push the resulting subset to a Neo4j Aura instance.

## Prerequisites

APOC blocks file export by default for security. Add the following to 
[`neo4j.conf`](https://neo4j.com/docs/operations-manual/current/configuration/neo4j-conf/) 
on the **source** database and restart it 
(see [default file locations](https://neo4j.com/docs/operations-manual/current/configuration/file-locations/#neo4j-config) 
if you're unsure where `neo4j.conf` lives):

```
apoc.export.file.enabled=true
```

## Step 1: Set environment variables

Get your Aura connection details and set the following environment variables accordingly.

```powershell
$env:AURA_URI = "neo4j+s://xxxxxxxx.databases.neo4j.io"
$env:AURA_USER = "neo4j"
$env:AURA_PASSWORD = "<password>"
```

Set `NEO4J_DBMS_DIR` to the home directory of the **source** DBMS, the one containing the `bin/` and `import/` folders.

```powershell
$env:NEO4J_DBMS_DIR = "\path\to\neo4j\dbms"
```

Files exported by APOC are written to the source database's configured import directory 
(`server.directories.import`), not your shell's working directory.

## Step 2: Export the neighborhood of specific scripts

Run the following on the **source** database.
This exports the neighborhood of specific scripts. 
In this example, we use scripts belonging to 
[_Individual X_](https://www.youtube.com/watch?v=327Q97uo4pw).

You may run this query through any interface to the Neo4j database, e.g., the Query section in Neo4j Desktop.

```cypher
CALL apoc.export.cypher.query(
  "MATCH (s:Script)
   WHERE s.Address IN [
     '1BADznNF3W1gi47R65MQs754KB7zTaGuYZ',
     '1BBqjKsYuLEUE9Y5WzdbzCtYzCiQgHqtPN',
     '1HQ3Go3ggs8pFnXuHVHRytPCq5fGG8Hbhx'
   ]
   CALL apoc.path.spanningTree(s, {
     labelFilter: '/Block',
     relationshipFilter: '',
     maxLevel: 2
   }) YIELD path
   RETURN path",
  "subgraph_scripts.cypher",
  { format: "cypher-shell", cypherFormat: "updateAll", ifNotExists: true }
)
YIELD file, nodes, relationships, properties, time
RETURN file, nodes, relationships, properties, time;
```

Note the `nodes` and `relationships` counts returned — they are used to verify the import later:

| file                      | nodes | relationships | properties | time |
| ------------------------- | ----- | ------------- | ---------- | ---- |
| "subgraph_scripts.cypher" | 515   | 518           | 49453      | 556  |

## Step 3: Export the Block neighborhoods (batched)

Run against the **source** database. Blocks are processed in batches of 50 
to keep memory usage bounded. Each batch produces its own file, 
named by height range (e.g. `subgraph_blocks_799990_800000.cypher`).

```cypher
MATCH (b:Block)
WHERE b.Height >= 799990 AND b.Height <= 800000
WITH b ORDER BY b.Height
WITH collect(b) AS allBlocks
UNWIND range(0, size(allBlocks) - 1, 50) AS start
WITH allBlocks[start..start + 50] AS batch
CALL (batch) {
  WITH batch,
       batch[0].Height AS minH,
       batch[-1].Height AS maxH
  CALL apoc.path.subgraphAll(batch, {
    relationshipFilter: '',
    maxLevel: 2
  }) YIELD nodes, relationships
  CALL apoc.export.cypher.data(nodes, relationships,
    'subgraph_blocks_' + minH + '_' + maxH + '.cypher',
    { format: 'cypher-shell', cypherFormat: 'updateAll', ifNotExists: true }
  ) YIELD file
  RETURN file, minH, maxH
} IN TRANSACTIONS OF 1 ROWS
RETURN file, minH, maxH;
```

Adjust the `Height` range and batch size (`50`) to fit your use-case and available memory.

## Step 4: Export the schema (indexes + constraints)

Run against the **source** database. This exports all indexes and constraints into a single file:

```cypher
CALL apoc.export.cypher.schema(
  "subgraph_schema.cypher",
  { format: "cypher-shell", ifNotExists: true }
)
YIELD file, nodes, relationships, properties, time
RETURN file, nodes, relationships, properties, time;
```

Returns:

| file                     | nodes | relationships | properties | time |
| ------------------------ | ----- | ------------- | ---------- | ---- |
| "subgraph_schema.cypher" | 0     | 0             | 0          | 24   |

## Step 5: Push to Aura

:::caution Empty target recommended
The data exports use `MERGE` statements (`cypherFormat: 'updateAll'`), 
so re-importing the same files is safe and won't create duplicates. 
However, if the target instance already contains unrelated or stale data, 
it will remain mixed in with the imported subset — start from an empty instance for a clean result.
:::

### 5a. Clean the target (optional)

If the Aura instance isn't already empty, wipe it first:

```cypher
MATCH (n)
CALL (n) {
  DETACH DELETE n
} IN TRANSACTIONS OF 10000 ROWS;
```

Note that this removes nodes and relationships, but not indexes or constraints. 
Leftover schema is tolerated by the import (all schema statements use `IF NOT EXISTS`), 
but you can drop it manually via `SHOW CONSTRAINTS` / `SHOW INDEXES` and `DROP` 
if you want a fully fresh instance.

### 5b. Run the import

The schema file must be imported **first**, followed by the data files:


```powershell
cd $env:NEO4J_DBMS_DIR
cat .\import\subgraph_schema.cypher, .\import\subgraph_scripts.cypher, .\import\subgraph_blocks_*.cypher | .\bin\cypher-shell -a $env:AURA_URI -u $env:AURA_USER -p $env:AURA_PASSWORD
```



## Step 6: Verify

Run against the **Aura** database:

```cypher
MATCH (n) RETURN labels(n) AS label, count(*) ORDER BY count(*) DESC;
```

Compare the totals against the `nodes` and `relationships` 
counts returned by the export queries in Steps 2 and 3 to 
confirm everything landed correctly. Note that because the 
two exported neighborhoods may overlap, the imported totals 
can be slightly lower than the sum of the per-file export 
counts — overlapping nodes are merged, not duplicated.
