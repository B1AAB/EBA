---
title: Setup Graph Database
description: Describes installing and connecting to a graph database.
sidebar_position: 4
slug: graphdb
---

:::tip Do I need to install and access a graph database?
**Yes**, if you want to 
[reproduce or update the data](/docs/bitcoin/etl/overview), or 
[sample custom communities](/docs/bitcoin/sampling/overview) 
from the graph.

**No**, if you only want to use the 
[communities](https://www.kaggle.com/datasets/vjalili/bitcoin-graph-sampled-communities), or 
the [sample models](https://github.com/B1AAB/GraphStudio) we provide.
::::


## Install Neo4j {#neo4j}

You can run the Neo4j Graph Database in several ways, 
such as a self-hosted production cluster or a fully managed, 
[cloud-based solution](https://neo4j.com/deployment-center).


For development and accessibility, 
all our solutions are designed to run on a standalone Neo4j installation, 
although a cloud-based deployment can be more performant.


*   Please follow [this documentation](https://neo4j.com/docs/operations-manual/current/installation) 
    to install a Neo4j database. 
    
*   Ensure you can connect to the installed Neo4j database.

After installation, you have two paths to load the graph:

### Path 1: Import from TSV Files

Choose this path if you have run the [ETL pipeline](/docs/bitcoin/etl/overview) and 
want to import the graph from the TSV files you generated.
Follow [this documentation](/docs/bitcoin/etl/import).


### Path 2: Restore from Database Dump

Choose this path if you want to use the 
[database dump](/releases/data-release/v1) 
we provided (this is the faster option).
Follow [this documentation](/docs/bitcoin/etl/restore).
