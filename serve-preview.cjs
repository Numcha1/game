const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, 'dist');
http.createServer((req, res) => {
  let pathname;
  try { pathname = decodeURIComponent(new URL(req.url, 'http://localhost').pathname); }
  catch { res.writeHead(400).end(); return; }
  const filename = path.resolve(root, '.' + (pathname === '/' ? '/index.html' : pathname));
  if (!filename.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
  fs.readFile(filename, (err, body) => {
    if (err) { res.writeHead(404).end('Not found'); return; }
    res.setHeader('Content-Type', ({'.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8', '.svg': 'image/svg+xml'})[path.extname(filename)] || 'application/octet-stream');
    res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');
    res.end(body);
  });
}).listen(4178, '127.0.0.1', () => console.log('Farm preview: http://127.0.0.1:4178'));
