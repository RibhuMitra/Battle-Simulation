// Real-Time Dashboard Client Logic

// State Management
const soldiers = {};
const logFeed = [];
const eventFeed = [];

// Socket Connection
const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
const socketUrl = `${protocol}//${window.location.host}`;
const socket = new WebSocket(socketUrl);

// DOM Elements
const connStatus = document.getElementById('connection-status');
const pulseIndicator = document.getElementById('pulse-indicator');
const soldierList = document.getElementById('soldier-list');
const eventFeedEl = document.getElementById('event-feed');
const rawLogStream = document.getElementById('raw-log-stream');

const canvas = document.getElementById('radar-canvas');
const ctx = canvas.getContext('2d');

const countRomanEl = document.getElementById('count-roman');
const countCarthageEl = document.getElementById('count-carthage');
const strengthRoman = document.getElementById('strength-roman');
const strengthCarthage = document.getElementById('strength-carthage');

const stateCanvas = document.getElementById('states-chart');
const stateCtx = stateCanvas.getContext('2d');

// Radar variables
const battlefieldScale = 5.0; // scale factor to translate Unity coordinates into canvas space
let radarSweepAngle = 0;

// Initialize Canvas Sizing
function resizeCanvas() {
    const parent = canvas.parentElement;
    const size = Math.min(parent.clientWidth - 40, parent.clientHeight - 40, 500);
    canvas.width = size;
    canvas.height = size;
}
window.addEventListener('resize', resizeCanvas);
resizeCanvas();

// Socket Listeners
socket.onopen = () => {
    connStatus.textContent = 'CONNECTED';
    connStatus.className = 'server-status connected';
    pulseIndicator.className = 'pulse-icon connected';
    addEventLog('Connected to battlefield stream server.', 'system');
};

socket.onclose = () => {
    connStatus.textContent = 'DISCONNECTED';
    connStatus.className = 'server-status';
    pulseIndicator.className = 'pulse-icon';
    addEventLog('Lost connection to backend server.', 'death');
};

socket.onmessage = (event) => {
    const payload = JSON.parse(event.data);
    
    if (payload.type === 'system') {
        addEventLog(payload.message, 'system');
    } else if (payload.type === 'log') {
        processIncomingLog(payload.data);
    }
};

// Log Processing Engine
function processIncomingLog(data) {
    // 1. Log to bottom feed
    addRawLog(data);

    const name = data.name;
    if (!name) return;

    // 2. Handle Death Log
    if (data.isDead) {
        if (soldiers[name]) {
            addEventLog(`💀 ${name} has fallen in battle!`, 'death');
            delete soldiers[name];
        }
        updateUI();
        return;
    }

    // Initialize unit state entry if new
    if (!soldiers[name]) {
        soldiers[name] = {
            name: name,
            faction: data.faction || 'Neutral',
            state: 'Idle',
            x: 0,
            z: 0,
            tx: null,
            tz: null,
            hp: 100,
            lastSeen: Date.now()
        };
        addEventLog(`⚔️ ${name} (${soldiers[name].faction}) entered the battlefield.`, 'system');
    }

    // 3. Update variables
    const s = soldiers[name];
    s.lastSeen = Date.now();

    if (data.hp !== undefined) {
        s.hp = data.hp;
    }

    if (data.state !== undefined) {
        if (s.state !== data.state && data.state === 'Retreat') {
            addEventLog(`🏳️ ${name} health critical! Fleeing the fight!`, 'retreat');
        }
        s.state = data.state;
    }

    if (data.x !== undefined && data.z !== undefined) {
        s.x = data.x;
        s.z = data.z;
    }

    if (data.tx !== undefined && data.tz !== undefined) {
        s.tx = data.tx;
        s.tz = data.tz;
    }

    updateUI();
}

// UI Updating Engine
function updateUI() {
    renderSoldierList();
    renderStatsPanel();
}

// Render troop sidebar
function renderSoldierList() {
    const list = Object.values(soldiers);
    
    if (list.length === 0) {
        soldierList.innerHTML = '<div class="empty-state">Waiting for Unity simulation logs...</div>';
        return;
    }

    // Sort by name
    list.sort((a, b) => a.name.localeCompare(b.name));

    soldierList.innerHTML = list.map(s => {
        const hpClass = s.hp < 25 ? 'danger' : s.hp < 50 ? 'warning' : '';
        const factionClass = s.faction.toLowerCase();
        const stateClass = s.state.toLowerCase();
        
        return `
            <div class="soldier-card">
                <div class="card-header">
                    <span class="unit-name ${factionClass}">${s.name}</span>
                    <span class="unit-state ${stateClass}">${s.state}</span>
                </div>
                <div class="health-track">
                    <div class="health-bar ${hpClass}" style="width: ${s.hp}%"></div>
                </div>
                <div class="card-footer">
                    <span>HP: ${s.hp.toFixed(0)}</span>
                    <span>Pos: (${s.x.toFixed(1)}, ${s.z.toFixed(1)})</span>
                </div>
            </div>
        `;
    }).join('');
}

// Render Stats & Metrics
function renderStatsPanel() {
    const list = Object.values(soldiers);
    let romans = 0;
    let carthaginians = 0;

    list.forEach(s => {
        if (s.faction === 'Roman') romans++;
        else if (s.faction === 'Carthage') carthaginians++;
    });

    // Count updates
    countRomanEl.textContent = `Romans: ${romans}`;
    countCarthageEl.textContent = `Carthaginians: ${carthaginians}`;

    // Bar progress percentage
    const total = romans + carthaginians;
    if (total > 0) {
        strengthRoman.style.width = `${(romans / total) * 100}%`;
        strengthCarthage.style.width = `${(carthaginians / total) * 100}%`;
    } else {
        strengthRoman.style.width = '50%';
        strengthCarthage.style.width = '50%';
    }

    renderStateChart(list);
}

// Render Custom State Chart (Drawn using HTML Canvas API directly)
function renderStateChart(soldierList) {
    stateCtx.clearRect(0, 0, stateCanvas.width, stateCanvas.height);

    const states = { Idle: 0, Search: 0, Move: 0, Attack: 0, Retreat: 0 };
    soldierList.forEach(s => {
        if (states[s.state] !== undefined) states[s.state]++;
    });

    const categories = Object.keys(states);
    const maxVal = Math.max(...Object.values(states), 1);
    
    const colors = {
        Idle: '#475569',
        Search: '#3b82f6',
        Move: '#8b5cf6',
        Attack: '#f43f5e',
        Retreat: '#10b981'
    };

    const margin = 15;
    const chartHeight = stateCanvas.height - 40;
    const chartWidth = stateCanvas.width - 20;
    const barWidth = (chartWidth / categories.length) - 10;

    categories.forEach((cat, index) => {
        const val = states[cat];
        const pct = val / maxVal;
        const barHeight = chartHeight * pct;
        
        const x = margin + index * (barWidth + 10);
        const y = chartHeight - barHeight + 10;

        // Draw bar
        stateCtx.fillStyle = colors[cat];
        stateCtx.fillRect(x, y, barWidth, barHeight);

        // Value text
        stateCtx.fillStyle = '#e2e8f0';
        stateCtx.font = 'bold 10px Outfit';
        stateCtx.textAlign = 'center';
        stateCtx.fillText(val, x + barWidth / 2, y - 5);

        // Label text
        stateCtx.fillStyle = '#64748b';
        stateCtx.font = '9px Outfit';
        stateCtx.fillText(cat[0] + cat.slice(1, 3), x + barWidth / 2, chartHeight + 25);
    });
}

// Event Log Panel Handlers
function addEventLog(msg, type) {
    const time = new Date().toLocaleTimeString();
    const entry = document.createElement('div');
    entry.className = `feed-entry ${type}`;
    entry.textContent = `[${time}] ${msg}`;
    
    eventFeedEl.appendChild(entry);
    eventFeedEl.scrollTop = eventFeedEl.scrollHeight;

    // Limit log rows to prevent performance leak
    while (eventFeedEl.childNodes.length > 50) {
        eventFeedEl.removeChild(eventFeedEl.firstChild);
    }
}

// Raw log feed handlers
function addRawLog(data) {
    const entry = document.createElement('div');
    entry.textContent = `[${new Date().toLocaleTimeString()}] ${JSON.stringify(data)}`;
    
    rawLogStream.appendChild(entry);
    rawLogStream.scrollTop = rawLogStream.scrollHeight;

    while (rawLogStream.childNodes.length > 30) {
        rawLogStream.removeChild(rawLogStream.firstChild);
    }
}

// RADAR CANVAS RENDERING ENGINE
function drawRadar() {
    const width = canvas.width;
    const height = canvas.height;
    const centerX = width / 2;
    const centerY = height / 2;
    const maxRadius = width / 2 - 20;

    ctx.clearRect(0, 0, width, height);

    // 1. Draw Concentric radar scope circles
    ctx.strokeStyle = 'rgba(16, 185, 129, 0.08)';
    ctx.lineWidth = 1;
    for (let r = maxRadius / 4; r <= maxRadius; r += maxRadius / 4) {
        ctx.beginPath();
        ctx.arc(centerX, centerY, r, 0, Math.PI * 2);
        ctx.stroke();
    }

    // 2. Draw crosshairs
    ctx.beginPath();
    ctx.moveTo(centerX - maxRadius, centerY);
    ctx.lineTo(centerX + maxRadius, centerY);
    ctx.moveTo(centerX, centerY - maxRadius);
    ctx.lineTo(centerX, centerY + maxRadius);
    ctx.stroke();

    // 3. Draw Rotating radar scan lines (military radar aesthetic)
    radarSweepAngle = (radarSweepAngle + 0.005) % (Math.PI * 2);
    ctx.strokeStyle = 'rgba(16, 185, 129, 0.15)';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(centerX, centerY);
    ctx.lineTo(
        centerX + Math.cos(radarSweepAngle) * maxRadius,
        centerY + Math.sin(radarSweepAngle) * maxRadius
    );
    ctx.stroke();

    // 4. Render Active Soldiers
    Object.values(soldiers).forEach(s => {
        // Map Unity coordinates to radar canvas space
        // Centered around coordinate (0, 0)
        const renderX = centerX + (s.x * battlefieldScale);
        // Canvas uses downward Y coordinates, so we subtract Z to reverse direction
        const renderY = centerY - (s.z * battlefieldScale);

        const factionColor = s.faction === 'Roman' ? '#ef4444' : '#eab308';
        const factionGlow = s.faction === 'Roman' ? 'rgba(239, 68, 68, 0.4)' : 'rgba(234, 179, 8, 0.4)';

        // Draw path connecting lines to their target destinations if moving/retreating
        if (s.tx !== null && s.tz !== null && (s.state === 'Move' || s.state === 'Retreat')) {
            const renderTx = centerX + (s.tx * battlefieldScale);
            const renderTy = centerY - (s.tz * battlefieldScale);

            ctx.strokeStyle = s.state === 'Retreat' ? 'rgba(16, 185, 129, 0.25)' : 'rgba(139, 92, 246, 0.25)';
            ctx.lineWidth = 1.5;
            ctx.setLineDash([4, 4]);
            ctx.beginPath();
            ctx.moveTo(renderX, renderY);
            ctx.lineTo(renderTx, renderTy);
            ctx.stroke();
            ctx.setLineDash([]); // clear dash settings
        }

        // Draw soldier position dot
        ctx.fillStyle = factionColor;
        ctx.shadowColor = factionGlow;
        ctx.shadowBlur = 8;
        ctx.beginPath();
        ctx.arc(renderX, renderY, 6, 0, Math.PI * 2);
        ctx.fill();
        ctx.shadowBlur = 0; // reset shadows

        // Draw state indication outline ring
        ctx.strokeStyle = factionColor;
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.arc(renderX, renderY, 11, 0, Math.PI * 2);
        ctx.stroke();

        // Draw names next to the unit dot
        ctx.fillStyle = '#cbd5e1';
        ctx.font = 'bold 9px Outfit';
        ctx.textAlign = 'left';
        ctx.fillText(s.name, renderX + 15, renderY + 3);
    });

    requestAnimationFrame(drawRadar);
}

// Start Sweep render loops
drawRadar();
updateUI();
