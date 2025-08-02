namespace BC2G.Blockchains.Bitcoin.Model;

public enum ChainToGraphModel
{
    /// <summary>
    /// Closely mirrors the UTxO mechanism, with high fidelity to the ledge model.
    /// This is Tx centric. 
    /// This is a UTxO-based view
    /// Mirrors blockchain as close as possible;
    /// for instance, it models a Tx as Script-to-Tx and Tx-to-Script,
    /// for redeeming a UTXO and creating one, respectively. 
    /// </summary>
    EventModel,

    /// <summary>
    /// This is the economic flow model, that models values flow.
    /// This is script-centric.
    /// This is an account-based view.
    /// Expands blockchain to more traditional trading model, 
    /// such as inputs and outputs of a Tx as Script-to-Script.
    /// </summary>
    StatefulModel
};
