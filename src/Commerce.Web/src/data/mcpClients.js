/**
 * MCP clients: id, name, and optional logo path for use in Home, HowToConnect, docs and slider.
 * Logo paths are under public/logos/ (png/jpeg). Fallback to letter if missing.
 */
export const MCP_CLIENTS = [
  { id: 'cursor', name: 'Cursor', logo: '/logos/cursor.jpeg', subtitle: 'IDE con MCP integrado', bgClass: 'bg-blue-100 dark:bg-blue-900/30' },
  { id: 'claude', name: 'Claude', logo: '/logos/claude.jpeg', subtitle: 'Claude Desktop, Claude Code', bgClass: 'bg-orange-100 dark:bg-orange-900/30' },
  { id: 'windsurf', name: 'Windsurf', logo: '/logos/windsurf.png', subtitle: 'IDE con soporte MCP', bgClass: 'bg-emerald-100 dark:bg-emerald-900/30' },
  { id: 'vscode', name: 'Copilot', logo: '/logos/copilot.jpeg', subtitle: 'VS Code / GitHub Copilot', bgClass: 'bg-sky-100 dark:bg-sky-900/30' },
  { id: 'chatgpt', name: 'ChatGPT', logo: '/logos/chatgpt.png', subtitle: 'Connectors', bgClass: 'bg-green-100 dark:bg-green-900/30' },
  { id: 'codex', name: 'Codex', logo: '/logos/codex.jpeg', subtitle: 'CLI MCP', bgClass: 'bg-gray-100 dark:bg-gray-700' },
  { id: 'other', name: 'Y más', logo: null, subtitle: 'Cualquier cliente MCP', bgClass: 'bg-gray-100 dark:bg-gray-700' }
]

/** Client ids used in HowToConnect details (order matches UI). */
export const HOW_TO_CONNECT_CLIENT_IDS = [
  'cursor',
  'claude-code',
  'vscode',
  'claude-desktop',
  'chatgpt',
  'windsurf',
  'codex',
  'other'
]

/** Display names for HowToConnect (some differ from MCP_CLIENTS). */
export const HOW_TO_CONNECT_NAMES = {
  'cursor': 'Cursor',
  'claude-code': 'Claude Code',
  'vscode': 'Copilot',
  'claude-desktop': 'Claude Desktop',
  'chatgpt': 'ChatGPT',
  'windsurf': 'Windsurf',
  'codex': 'Codex',
  'other': 'Otros clientes (JSON)'
}

/** Resolve client id to logo path (HowToConnect uses claude-code / claude-desktop). */
export function getClientLogo(clientId) {
  const normalized = clientId === 'claude-code' || clientId === 'claude-desktop' ? 'claude' : clientId
  const client = MCP_CLIENTS.find(c => c.id === normalized)
  return client?.logo ?? null
}

export function getClientById(clientId) {
  const normalized = clientId === 'claude-code' || clientId === 'claude-desktop' ? 'claude' : clientId
  return MCP_CLIENTS.find(c => c.id === normalized) ?? { id: 'other', name: 'Otros', logo: null, bgClass: 'bg-gray-100 dark:bg-gray-700' }
}
