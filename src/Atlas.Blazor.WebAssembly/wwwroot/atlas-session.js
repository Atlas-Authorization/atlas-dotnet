// Reads the script-readable Atlas session cookie. Used by Atlas.Blazor.WebAssembly.
export function readCookie(name) {
  const prefix = name + '=';
  for (const part of document.cookie.split(';')) {
    const trimmed = part.trim();
    if (trimmed.startsWith(prefix)) return trimmed.substring(prefix.length) || null;
  }
  return null;
}
