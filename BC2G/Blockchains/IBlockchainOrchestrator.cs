namespace BC2G.Blockchains;

public interface IBlockchainOrchestrator
{
    public Task TraverseAsync(Options options, CancellationToken cT);
}
