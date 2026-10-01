import readline from "node:readline";

function send(message) {
  process.stdout.write(`${JSON.stringify(message)}\n`);
}

export class McpServer {
  constructor({ tools, callTool, serverInfo, protocolVersions, log }) {
    this.tools = tools;
    this.callTool = callTool;
    this.serverInfo = serverInfo;
    this.protocolVersions = protocolVersions;
    this.log = log;
  }

  start() {
    const input = readline.createInterface({
      input: process.stdin,
      crlfDelay: Infinity,
    });
    input.on("line", async (line) => {
      if (!line.trim()) return;
      let request;
      try {
        request = JSON.parse(line);
      } catch {
        return;
      }

      if (!request.id && request.method?.startsWith("notifications/")) return;

      try {
        const result = await this.dispatch(
          request.method,
          request.params ?? {},
        );
        if (request.id !== undefined)
          send({ jsonrpc: "2.0", id: request.id, result });
      } catch (error) {
        this.log?.(`request failed: ${error.stack || error.message}`);
        if (request.id !== undefined) {
          send({
            jsonrpc: "2.0",
            id: request.id,
            error: { code: -32603, message: error.message || "Internal error" },
          });
        }
      }
    });
  }

  async dispatch(method, params) {
    switch (method) {
      case "initialize":
        return {
          protocolVersion: this.protocolVersions.default,
          capabilities: { tools: {} },
          serverInfo: this.serverInfo,
        };
      case "ping":
        return {};
      case "tools/list":
        return { tools: this.tools };
      case "tools/call": {
        const result = await this.callTool(params.name, params.arguments ?? {});
        return result;
      }
      default:
        throw new Error(`Unsupported MCP method: ${method}`);
    }
  }
}
