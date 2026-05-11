#!/usr/bin/env node
// Local static-file server + Cloudflare quick tunnel for Unity WebGL builds.
// Usage: node tools/serve-tunnel.mjs [buildPath] [port]
//   defaults: buildPath=./Build, port=8080

import { createServer } from 'http';
import { readFile } from 'fs/promises';
import { existsSync, readdirSync } from 'fs';
import { extname, join, resolve } from 'path';
import { spawn } from 'child_process';

const ROOT = resolve(process.argv[2] || './Build');
const PORT = parseInt(process.argv[3] || '8080', 10);

if (!existsSync(join(ROOT, 'index.html'))) {
  console.error(`ERROR: index.html not found in ${ROOT}`);
  console.error(`Build the Unity WebGL project to "${ROOT}" first (File > Build Settings > Build).`);
  process.exit(1);
}

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js':   'application/javascript',
  '.css':  'text/css',
  '.json': 'application/json',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream',
  '.png':  'image/png',
  '.jpg':  'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.svg':  'image/svg+xml',
  '.ico':  'image/x-icon',
  '.txt':  'text/plain; charset=utf-8',
};

// Unity WebGL の Brotli/Gzip 圧縮ビルド対応:
// build.wasm.br のような拡張子は、内側の拡張子(.wasm)で Content-Type を決め、
// ブラウザに展開させるため Content-Encoding ヘッダーを付ける。
function buildHeaders(filePath) {
  const ext = extname(filePath).toLowerCase();
  if (ext === '.br' || ext === '.gz') {
    const innerExt = extname(filePath.slice(0, -ext.length)).toLowerCase();
    return {
      'Content-Type': MIME[innerExt] || 'application/octet-stream',
      'Content-Encoding': ext === '.br' ? 'br' : 'gzip',
      'Cache-Control': 'no-store',
    };
  }
  return {
    'Content-Type': MIME[ext] || 'application/octet-stream',
    'Cache-Control': 'no-store',
  };
}

const server = createServer(async (req, res) => {
  try {
    let urlPath = decodeURIComponent(req.url.split('?')[0]);
    if (urlPath === '/' || urlPath === '') urlPath = '/index.html';
    const safe = urlPath.replace(/^[\/\\]+/, '');
    const filePath = resolve(ROOT, safe);
    if (!filePath.startsWith(ROOT)) { res.writeHead(403); res.end(); return; }
    const data = await readFile(filePath);
    res.writeHead(200, buildHeaders(filePath));
    res.end(data);
  } catch {
    res.writeHead(404, { 'Content-Type': 'text/plain' });
    res.end('Not found');
  }
});

function findCloudflared() {
  const onPath = (process.env.PATH || '').split(';').some(p => p && existsSync(join(p, 'cloudflared.exe')));
  if (onPath) return 'cloudflared';
  const wingetRoot = join(process.env.LOCALAPPDATA || '', 'Microsoft', 'WinGet', 'Packages');
  if (existsSync(wingetRoot)) {
    for (const d of readdirSync(wingetRoot)) {
      if (!d.startsWith('Cloudflare.cloudflared')) continue;
      const exe = join(wingetRoot, d, 'cloudflared.exe');
      if (existsSync(exe)) return exe;
    }
  }
  return null;
}

let cfProc = null;

server.listen(PORT, '127.0.0.1', () => {
  console.log(`[server] serving ${ROOT} on http://localhost:${PORT}`);

  const cf = findCloudflared();
  if (!cf) {
    console.error('[tunnel] cloudflared not found. Install with: winget install Cloudflare.cloudflared');
    process.exit(1);
  }

  console.log('[tunnel] starting Cloudflare quick tunnel...');
  cfProc = spawn(cf, ['tunnel', '--url', `http://localhost:${PORT}`], { stdio: ['ignore', 'pipe', 'pipe'] });

  let urlPrinted = false;
  const onData = chunk => {
    const s = chunk.toString();
    process.stdout.write(s);
    if (!urlPrinted) {
      const m = s.match(/https:\/\/[a-z0-9-]+\.trycloudflare\.com/i);
      if (m) {
        urlPrinted = true;
        console.log('\n========================================================');
        console.log(' PUBLIC URL: ' + m[0]);
        console.log(' (share this URL — it works from any PC on the internet)');
        console.log('========================================================\n');
      }
    }
  };
  cfProc.stdout.on('data', onData);
  cfProc.stderr.on('data', onData);
  cfProc.on('exit', code => {
    console.log(`[tunnel] cloudflared exited with code ${code}`);
    server.close(() => process.exit(code || 0));
  });
});

function shutdown() {
  console.log('\n[*] shutting down...');
  if (cfProc && !cfProc.killed) cfProc.kill();
  server.close(() => process.exit(0));
}
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
