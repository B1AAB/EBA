namespace BC2G.Blockchains.Bitcoin.Model;

public enum ChainToGraphModel
{
    /// <summary>
    /// Mirrors blockchain as close as possible;
    /// for instance, it models a Tx as Script-to-Tx and Tx-to-Script,
    /// for redeeming a UTXO and creating one, respectively. 
    /// </summary>
    Native,

    /// <summary>
    /// Expands blockchain to more traditional trading model, 
    /// such as inputs and outputs of a Tx as Script-to-Script.
    /// </summary>
    Expanded
};
