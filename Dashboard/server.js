const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const path = require('path');

const app = express();
app.use(express.json());

// Serve static files from the 'public' directory
app.use(express.static(path.join(__dirname, 'public')));

const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

// WebSocket connection handler
wss.on('connection', (ws) => {
    console.log('Web client connected to dashboard');
    ws.send(JSON.stringify({ type: 'system', message: 'Connected to Real-Time Battle Simulation Stream' }));
});

// Broadcast helper
function broadcast(data) {
    wss.clients.forEach((client) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(JSON.stringify(data));
        }
    });
}

// REST endpoint for Unity Log Forwarder
app.post('/api/logs', (req, res) => {
    const logData = req.body;
    
    // Broadcast incoming log to all connected WebSocket web clients
    broadcast({ type: 'log', data: logData });
    
    res.status(200).send({ status: 'ok' });
});

const PORT = process.env.PORT || 5000;
server.listen(PORT, () => {
    console.log(`==================================================`);
    console.log(`  Battle Simulation Dashboard Running!`);
    console.log(`  Open http://localhost:${PORT} in your web browser`);
    console.log(`==================================================`);
});
