# ⚔️ Intelligent Infantry Battle Simulation

Welcome to the **Intelligent Infantry Battle Simulation** project! This repository contains a high-fidelity simulation of autonomous combat agents designed to participate in massive historic battle scenarios. 

Rather than relying on simple scripting, each unit operates under an advanced **4-layer AI architecture** to perceive its environment, remember target tracking data, assess combat threats, and execute coordinated behaviors.

---

## 🧠 System Architecture

The soldier intelligence model is split into four distinct layers:

```
  Perception  (SoldierPerception)
      ↓
    Memory    (SoldierMemory)
      ↓
    Brain     (SoldierBrain)
      ↓
    Action    (SoldierAI / Combat)
```

1. **Perception**: Identifies surrounding units within a vision radius, tracking list of visible allies and enemies.
2. **Memory**: Remembers last seen targets and their positions. Memory automatically decays (default: `5s`) after losing line-of-sight.
3. **Brain**: The core decision-making finite state machine. Evaluates health levels, outnumbered ratios, target threats, and dictates actions.
4. **Action**: The physical execution modules that handle C# movements, attack ranges, cooldowns, and damage calculations.

---

## 📂 Core Scripts Directory

All scripts are located in [Assets/Scripts](file:///d:/unity_projects/Simulation/Assets/Scripts):

*   [Team.cs](file:///d:/unity_projects/Simulation/Assets/Scripts/Team.cs): Designates faction alignment (e.g., `Roman` vs. `Carthage`).
*   [Health.cs](file:///d:/unity_projects/Simulation/Assets/Scripts/Health.cs): Handles damage calculation, health metrics, and unit death. Includes a toggleable debug logging system.
*   [Combat.cs](file:///d:/unity_projects/Simulation/Assets/Scripts/Combat.cs): Performs damage application to target components when within attack range and cooldowns.
*   [SoldierPerception.cs](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierPerception.cs): Scans the local coordinate space using sphere overlaps to index nearby units.
*   [SoldierMemory.cs](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierMemory.cs): Retains target transforms and coordinates with timestamped forgetting parameters.
*   [SoldierState.cs](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierState.cs): Enumeration defining unit states (`Idle`, `Search`, `Move`, `Attack`, `Retreat`).
*   [SoldierBrain.cs](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierBrain.cs): The state machine that binds the components together and makes tactical decisions.

---

## 🛠️ Tactical Features

### 1. Threat Assessment (Target Prioritization)
The [SoldierBrain](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierBrain.cs#L3) does not just target the closest enemy. It scores all visible enemies using the following formula:
$$\text{Score} = \frac{0.7}{\text{Distance}} + 0.3 \times (1.0 - \text{EnemyHealthPercentage})$$
This ensures units prioritize closer targets but will dynamically choose to finish off heavily weakened enemies nearby.

### 2. Tactical Self-Preservation & Ally Awareness
*   **Base Retreat**: When health falls below 20%, the unit enters `Retreat` state, clears its combat target, and flees in the opposite direction of the nearest enemy.
*   **Outnumbered Retreat**: If a unit is outnumbered (detected enemies > allies + 1), it exercises tactical caution and will flee if health falls below 40%.

---

## ⚙️ Installation & Unity Setup

Follow these steps to set up the system in your Unity Project:

### Step 1: Assign Component Scripts
Attach the following scripts to your Roman and Carthage infantry prefab GameObjects:
1.  [Team](file:///d:/unity_projects/Simulation/Assets/Scripts/Team.cs#L3)
2.  [Health](file:///d:/unity_projects/Simulation/Assets/Scripts/Health.cs#L3)
3.  [Combat](file:///d:/unity_projects/Simulation/Assets/Scripts/Combat.cs#L3)
4.  [SoldierPerception](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierPerception.cs#L3)
5.  [SoldierAI](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierAI.cs#L3)
6.  [SoldierMemory](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierMemory.cs#L3)
7.  [SoldierBrain](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierBrain.cs#L3)

### Step 2: Configure Factions
On the [Team](file:///d:/unity_projects/Simulation/Assets/Scripts/Team.cs#L3) script component, assign the faction:
*   Set Roman soldiers to `Roman`.
*   Set Carthage soldiers to `Carthage`.

### Step 3: Run the Simulation
Press **Play** in Unity. The [SoldierBrain](file:///d:/unity_projects/Simulation/Assets/Scripts/SoldierBrain.cs#L3) will automatically link all components, begin scanning, choose targets, and fight autonomously.

### Step 4: Adjust Debug Logs (Optional)
If you want to reduce logging output in large battles, check or uncheck `enableCombatLogging` on the [Health](file:///d:/unity_projects/Simulation/Assets/Scripts/Health.cs#L3) and [Combat](file:///d:/unity_projects/Simulation/Assets/Scripts/Combat.cs#L3) scripts in the Inspector.
