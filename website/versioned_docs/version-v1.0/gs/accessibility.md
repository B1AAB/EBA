---
title: Accessible Solutions
description: Making the Bitcoin Graph Accessible
sidebar_position: 2
slug: accessibility
---

# Accessible Solutions

The Bitcoin graph shared here consists of over `2.4` billion nodes 
and `39.72` billion time-stamped edges and covers over 16 years of real-world 
financial transactions recorded on the Bitcoin blockchain.
Working with extremely large graphs like this typically requires specialized infrastructure 
or hardware for creating, updating, analyzing, and even storing the data. 
For instance, Neo4j (a specialized graph database solution) recommends a machine with 
at least 1TB of RAM to import a graph of this scale. 
Furthermore, Neo4j's Graph Data Science library often requires projecting 
graph data into memory to run analytics algorithms; 
projecting even `0.1%` of this Bitcoin graph requires at least 96GB of RAM.


We strive for reproducibility and accessibility, and we recognize that such 
specialized hardware isn't available to the wider community. Therefore, 
we provide solutions designed to minimize hardware requirements as much as possible. 
To further enhance accessibility, we provide the output of each major step, 
allowing you to skip earlier stages of the pipeline and resume from 
your desired point using pre-processed data. 
We outline these options [on this page](/docs/bitcoin/overview).


These solutions are designed to leverage storage to minimize memory requirements. 
They support extreme parallelization on a single machine (vertical scaling) out-of-the-box.
Horizontal scaling (multiple machines, e.g., via Kubernetes) is currently out-of-scope.
