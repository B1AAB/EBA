// EBA Bitcoin Graph Schema

// -----------------------------------------------------
// Schema for Block nodes
// -----------------------------------------------------

CREATE CONSTRAINT Block_Height_Unique IF NOT EXISTS FOR (n:Block) REQUIRE n.Height IS UNIQUE;


// -----------------------------------------------------
// Schema for Script nodes
// -----------------------------------------------------

CREATE CONSTRAINT Script_SHA256Hash_Unique IF NOT EXISTS FOR (n:Script) REQUIRE n.SHA256Hash IS UNIQUE;


// -----------------------------------------------------
// Schema for Tx nodes
// -----------------------------------------------------

CREATE CONSTRAINT Tx_Txid_Unique IF NOT EXISTS FOR (n:Tx) REQUIRE n.Txid IS UNIQUE;


// -----------------------------------------------------
// Schema for Tx-Credits-Script edges
// -----------------------------------------------------

CREATE INDEX utxo_spending_idx IF NOT EXISTS 
FOR ()-[r:Credits]-() 
ON (r.CreationHeight, r.SpentHeight);


// -----------------------------------------------------
// Schema for Block-Follows-Block edges
// -----------------------------------------------------

MATCH (target:Block), (source:Block) 
WHERE target.Height + 1 = source.Height 
MERGE (target)-[:Follows]->(source);

