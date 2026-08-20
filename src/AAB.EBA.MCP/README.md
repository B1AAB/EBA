# AAB.EBA.MCP

The MCP server speaks both `stdio/stdout` and `Streamable HTTP`. 

* `stdio/stdout` is the default, and it is mostly used for clients that launch the server themselves
(Claude Desktop or `npx @modelcontextprotocol/inspector dotnet AAB.EBA.MCP.dll`)

* `Streamable HTTP` is used when interfacing with a hosted server 
(e.g., Visual Studio launched server and connecting to it from inspector).
In order to launch the MCP server with `Streamable HTTP`, pass `--http` argument. 


## Prerequisites

1. Set Neo4j connection env vars (defaults to `bolt://localhost:7687` / `neo4j` / `password` if unset):

```bash
export NEO4J_URI=bolt://localhost:7687
export NEO4J_USER=neo4j
export NEO4J_PASSWORD=your_password
```

2. Start EBA's database on Neo4j. 


## Option 1: Run standalone for manual testing without Visual Studio debugging

```bash
npx @modelcontextprotocol/inspector dotnet src/AAB.EBA.MCP/bin/Debug/net10.0/AAB.EBA.MCP.dll
```

## Option 2: Run with option of debugging in Visual Studio

1. In Visual Studio, set the startup profile to `AAB.EBA.MCP`,
2. Open `AAB.EBA.MCP` Debug configuration and ensure `--http` is passed as command line argument.
3. Press F5.
2. Run:
   ```bash
   npx @modelcontextprotocol/inspector
   ```
3. If this is your first time: 
  3.1. Click `Add Servers` then `Add Manually`
  3.2. Set a server ID to `EBA-MCP-Local-Dev`
  3.3. Set Transport to `Streamable HTTP`
  3.4. Set URL to `http://localhost:5000`
  3.5. Click `Add`
4. Connect to the server by clicking on the toggle next to `Disconnected` on `EBA-MCP-Local-Dev` card.

With this setup, you can call a tool, and if any related breakpoints you've set in the code, will be hit in Visual studio. 

## Option 3: Connect to Claude Desktop

1. Open Claude settings, click on `Developer` tab on the left panel, then click `Open Config`.
2. It opens path and selects the `claude_desktop_config.json` file, open the file.
3. Add the following key in the root of the json object.

  ```json
  "mcpServers": {
    "EBA-MCP-Local-Dev": {
      "command": "dotnet",
      "args": [
        "<ABS PATH>/src/AAB.EBA.MCP/bin/Debug/net10.0/AAB.EBA.MCP.dll"
      ]
    }
  },
  ```

## Option 4: Connect to Claude Code

```bash
claude mcp add EBA-MCP-Local-Dev -- dotnet src/AAB.EBA.MCP/bin/Debug/net10.0/AAB.EBA.MCP.dll
```

Verify:

```bash
claude mcp list
```

Remove:

```bash
claude mcp remove EBA-MCP-Local-Dev
```
