```shell
# in 1st terminal, start the mcp service:
dotnet .\src\AAB.EBA.MCP\bin\Debug\net10.0\AAB.EBA.MCP.dll

# in 2nd terminal, run inspector:
npx @modelcontextprotocol/inspector
```

After the inspector webpage opens: 

- Add server entry
    - Transport: Streamable HTTP
    - URL: http://localhost:5000


# AAB.EBA.MCP


## Prerequisites

Set Neo4j connection env vars (defaults to `bolt://localhost:7687` / `neo4j` / `password` if unset):

```bash
export NEO4J_URI=bolt://localhost:7687
export NEO4J_USER=neo4j
export NEO4J_PASSWORD=your_password
```

## Build

```bash
dotnet build src/AAB.EBA.MCP/AAB.EBA.MCP.csproj
```

## Run standalone (for manual testing)

```bash
npx @modelcontextprotocol/inspector dotnet src/AAB.EBA.MCP/bin/Debug/net10.0/AAB.EBA.MCP.dll
```

## Connect to Claude Code

```bash
claude mcp add eba-mcp -- dotnet D:\EBA_fork\src\AAB.EBA.MCP\bin\Debug\net10.0\AAB.EBA.MCP.dll
```

Verify:

```bash
claude mcp list
```

Remove:

```bash
claude mcp remove eba-mcp
```

## Connect to Claude Desktop

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "eba-mcp": {
      "command": "dotnet",
      "args": [
        "ABS_PATH_TO\\AAB.EBA.MCP.dll"
      ]
    }
  }
}
```
