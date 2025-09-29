---
title: Design decisions
description: Design decisions
sidebar_position: 0
slug: design-decisions
---


### Why Bitcoin Core?

[Bitcoin Core](https://bitcoin.org/en/bitcoin-core) 
is one of the most commonly used, maintained, and supported 
open-source clients for Bitcoin. Hence, it is an ideal option 
for communicating with the Bitcoin network and localizing 
all the blocks. 
However, one can implement a specialized method to read block 
information from the localized data, which could outperform 
Bitcoin Core's general-purpose methods. 
Despite this, we decided to use Bitcoin Core instead of 
developing a specialized parser because it would have 
added development complexity that is both beyond the 
scope of this work and highly error-prone, given the 
long history of BIPs implemented since the genesis block.
Therefore, we use Bitcoin Core's REST API to parse data 
from blocks. This enables us to remain focused on 
generating the graph from transaction data and rely 
on Bitcoin Core to resolve the technical details of 
reading transactions.


### Why Neo4j and TSV?

Our choice of data representation was driven by two key requirements: 
**portability** for easy community sharing and **scalability** to handle a massive graph. 
After experimenting with several formats, we selected a combination of batched TSV 
files and the Neo4j graph database to achieve the right balance.


- **Batched TSV Files for Portability**
  While a single, massive TSV file is impractical for this dataset, 
  TSVs remain highly portable. To solve the scaling issue, 
  we adopted a batching strategy: we store different node and edge types 
  in separate, homogeneous files and enforce a size limit on each. 
  This results in multiple, smaller batches of reasonably-sized 
  files that are easy to share and use.


- **Neo4j for Scalable Queries**
  Graph databases are purpose-built for the complex queries this dataset requires, 
  such as efficiently retrieving _n_-hop neighbors of a given node. 
  We chose Neo4j specifically because of its wide adoption, 
  and flexible licensing, offering both a free community edition 
  and scalable production-grade solutions. 
  It is ideal for a wide spectrum of applications, 
  which is why we provide Neo4j-based solutions.

- **Other Formats We Considered**
  - **[np.memmap](https://numpy.org/doc/stable/reference/generated/numpy.memmap.html):** 
    We tested this disk-based binary format, 
    which is designed to minimize RAM usage. 
    While promising, it was too slow for the random-access queries 
    required by our graph sampling algorithms, 
    a critical part of our workflow.

  - **Relational Databases (e.g., PostgreSQL):** 
    Relational databases were efficient for indexed lookups 
    (e.g., retrieving a script's details by its address); 
    however, their performance degraded significantly on graph traversal 
    queries, which are generally implemented using nested INNER JOINs. 
    This approach was not scalable for our primary use case: 
    sampling hundreds of thousands of multi-hop communities.
