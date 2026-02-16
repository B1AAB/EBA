---
title: Quick Start
description: Quick Start
sidebar_position: 1
slug: quickstart
---

Exploring the intersection of ML and Cryptocurrency 
is a fascinating and impactful journey. 
You can deep-dive into a specialized area or explore it end-to-end; 
either way, there is huge potential for novel data sources, 
model architectures, and impactful applications in decentralized finance (DeFi). 

EBA equips you with all the tools and data you need for this journey, 
and this quick start helps you decide the right starting point for your application.


### Path 1: Charting the Cosmos {#path1}

Choose this path to see the full, end-to-end journey. 
You will start with the raw data, 
learn how the graph pipeline is built, 
and finish by training a graph neural network (GNN) model.

* **What you will do:**
  * Learn about the raw data we collect from Bitcoin.
  * See why a graph database is essential for this dataset and how we can leverage it.
  * Understand how we sample communities from the graph database.
  * Train a _"hello-world"_ model to generate node embeddings for a 
    Bitcoin script based on its 3-hop neighborhood.
  * Run an unsupervised contrastive learning model, use the embeddings to cluster nodes, and evaluate the results.

* **Resources you need:**
  * Colab or a local Jupyter Notebook

* **Get started:** [Notebook Link](https://github.com/B1AAB/GraphStudio/blob/main/g101/g101.ipynb)



### Path 2: Visiting the Stars {#path2}

Choose this path to skip the pipeline setup and 
jump straight into model building and analysis.


* **What you will do:**
  * Use our pre-sampled communities and load them directly into a PyG `InMemoryDataset`
  * Train and evaluate a model to generate node embeddings for a 
    Bitcoin script (address) based on its 3-hop neighborhood.
  * Experiment with an unsupervised contrastive learning model to cluster 
    nodes based on their learned embeddings.
  * Compare your clusters with external annotations (like WalletExplorer) 
    to identify exchanges, mining pools, or gambling services.

* **Resources you need**:
  * Jupyter Notebook

* **Get started:** [Script classification quick-start](https://github.com/B1AAB/GraphStudio/tree/main/quickstart/script_classification)


### Charting Your Own Course {#your-own-course}


We're glad you're interested and 
hope you see how this resource can empower your work. 
You're working with a real-world dataset spanning over 16 years, 
so scale is an inseparable part of this journey. 
This sheer scale is what makes this dataset powerful, 
but it also demands significant computational resources. 
For instance, running a Bitcoin node needs `~800GB` of storage and a week to sync, 
and a graph database requires `~3TB` of storage.


We are committed to making this resource [widely accessible](./accessibility). 
To do this, we've taken two steps:

1. The entire ecosystem is fully modular. You can skip parts of the journey as we provide checkpoint data for each step.

2. We provide application-focused guides to help you find the correct starting point for your goals.


[Dive into our application-focused guides](/docs/bitcoin/etl/overview) 
to find the perfect path for your project.
