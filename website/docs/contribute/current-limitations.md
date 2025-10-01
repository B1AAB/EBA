---
title: Current limitations
description: current limitations
sidebar_position: 1
---


- **Transactions with more than 25 source and target scripts are skipped.**
  This is mainly due to how source and taget transactions are connected 
  to form a graph, which in when there are many source and target 
  scripts per transaction, it leads to considerable number of relationships,
  Which cause issues with Neo4j (e.g., min required query heap size).
  Such transactions are temporarily ignored. 

  One example is transaction the following transaction at block `134863`.

  ```
  1c19389b0461f0901d8eace260764691926a5636c74bd8a3cc68db08dbbeb80a
  ```

  This transaction has `99` input scripts and `999` output scripts,
  which results in creating `86,756` script-to-script relationships, 
  `100` tx-to-tx relationships, `1098` script nodes, and `103` tx nodes.