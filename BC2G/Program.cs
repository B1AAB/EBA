using BC2G;

try
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.Add("User-Agent", "BitcoinAgent");

    var orchestrator = new Orchestrator(".", client);
    await orchestrator.RunAsync();

    // Try these transactions, debug/test
    // 700000; //199233; //714460; //100; 3000; ; 
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}
