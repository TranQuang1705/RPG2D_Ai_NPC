# NPC AI Game Design Document

## 1. NPC AI Overview

### System Architecture

The NPC AI system is built on a modular component-based architecture that enables NPCs to exhibit complex, autonomous behaviors while maintaining strong integration with player interaction systems. Each NPC is composed of multiple specialized components working in concert:

**[IMAGE SUGGESTION: System Architecture Diagram]**
*A component diagram showing the 4 core NPC components (Central Orchestrator, Behavior Engine, Quest Manager, Commerce System) with arrows showing data flow between them and connections to external systems (TimeManager, Database, Player, ChatbotAI)*

**Core Components:**
- **Central Orchestrator**: Manages all interactions, dialogue state, and component coordination
- **Autonomous Behavior Engine**: Controls daily activities, pathfinding, and environmental interaction
- **Quest Lifecycle Manager**: Handles quest availability, assignment, and completion
- **Commerce System**: Manages shop inventories, trading schedules, and market behaviors

### Design Philosophy

The NPC AI follows a **"Living World"** philosophy where NPCs are not static quest dispensers but autonomous entities with:

1. **Temporal Awareness**: NPCs operate on a 24-hour in-game time cycle, with behaviors changing based on time of day and seasonal context
2. **Contextual Intelligence**: NPCs understand their current activity, player relationships, and environmental conditions when responding to interactions
3. **Player-Centric Flexibility**: While NPCs follow autonomous routines, they can be interrupted and redirected by player requests, creating a responsive world
4. **Multi-Modal Interaction**: NPCs support voice input, text chat, and action-based responses, creating natural conversational experiences

### State Management

NPCs operate in a dual-state system:

**[IMAGE SUGGESTION: Dual-State System Diagram]**
*Two interconnected state machines: (1) Activity States circle showing transitions between Sleep→Morning→FlowerHunting→Lunch→Explore→Evening→Social→Night→Sleep, and (2) Execution States showing Idle↔MovingToTarget↔GatheringFlower↔Resting↔Socializing with transition conditions labeled*

**Activity States** (What the NPC is doing):
- Sleep, MorningRoutine, FlowerHunting, MarketTrading, LunchBreak
- ExploreVillage, EveningRoutine, SocialTime, NightRoutine

**Execution States** (How the NPC is executing their activity):
- Idle, MovingToTarget, GatheringFlower, ReturningHome, Resting, Socializing

This separation allows for flexible behavior composition - an NPC in "FlowerHunting" activity might be in "MovingToTarget" execution state while pathfinding to a flower.

---

## 2. Routine System Concept

### Time-Based Autonomy

The routine system creates the illusion of a living world where NPCs pursue their own goals independent of player observation. This is achieved through:

**Scheduled Activities**: Each NPC has a personal schedule defining what they do at different times of day. For example:
- 6:00-8:00: Morning routine (cleaning, breakfast at home)
- 8:00-12:00: Primary work activity (flower gathering for gatherers, market trading for merchants)
- 12:00-13:00: Lunch break at village center
- 13:00-17:00: Village exploration and socialization
- 17:00-20:00: Evening routine (cooking, home activities)
- 20:00-22:00: Social time with other NPCs
- 22:00-23:00: Night routine (preparation for sleep)
- 23:00-6:00: Sleep at home location

**TimeManager Integration**: The system integrates with a global TimeManager that controls:
- Day/night cycles (24-hour system compressed into real-time minutes)
- Seasonal transitions affecting NPC behaviors and dialogue
- Dynamic lighting synchronized with time progression
- Activity triggers based on current hour

### Player Request Override System

A key innovation is the **Player Request System** that allows players to temporarily override NPC routines:

**[IMAGE SUGGESTION: Player Request Flow Diagram]**
*Flowchart showing: Player Voice Input → Chatbot AI → Action Command → NPC Behavior Engine → Activity Override → Task Completion → Return to Routine. Include timeline showing "Autonomous Activity" interrupted by "Player Request Period" then returning to "Autonomous Activity"*

**Request Flow:**
1. Player asks NPC to perform an activity (e.g., "Can you gather some flowers?")
2. Chatbot interprets request and sends action command (e.g., GATHER_FLOWER)
3. Central orchestrator receives action and sets player request flag in behavior engine
4. Behavior engine prioritizes player request over scheduled routine
5. NPC performs requested activity until completion or timeout
6. NPC returns to autonomous routine after request completion

**Design Benefits:**
- Creates sense of player agency - NPCs respond to player needs
- Maintains NPC autonomy - they still follow routines when not directed
- Prevents exploitation - requests timeout after completion or 30 seconds
- Natural integration - requests work through dialogue, not UI buttons

### Activity Execution

Activities are implemented as coroutines that:
- Can be paused when player initiates dialogue
- Resume when dialogue ends
- Transition smoothly between different activities
- Handle interruptions and state changes gracefully

**Pause/Resume System:**
When player approaches for dialogue:
1. Current activity paused (movement stops, state saved)
2. NPC enters Idle state
3. Dialogue system takes control
4. On dialogue exit, NPC resumes previous activity

This creates realistic interaction where NPCs "pause their life" to talk to the player, then continue what they were doing.

---

## 3. Daily Schedule Design

**[IMAGE SUGGESTION: 24-Hour NPC Schedule Timeline]**
*A circular clock diagram showing different colored sections for each activity phase throughout the 24-hour day. Include small icons for each activity (bed for sleep, flower for gathering, market stall for trading, etc.). Show two examples side-by-side: Gatherer vs Trader schedules to highlight differences*

### Flower Gatherer Schedule Example

**Morning Phase (6:00-8:00) - MorningRoutine**
- Wake up at home location
- Perform cleaning activities near home (simulated work)
- Prepare for the day's work
- Movement: Small radius around homeLocation

**Work Phase (8:00-12:00) - FlowerHunting**
- Primary economic activity
- Search for flowers within detection radius (configurable, default 5f)
- Use pathfinding to navigate to flowers
- Gather flowers with animation and timer (3 seconds)
- Notify FlowerManager for respawn handling
- Movement: Village center radius (15f) with map boundary clamping

**Rest Phase (12:00-13:00) - LunchBreak**
- Return to village center
- Rest and recovery
- Social opportunity window
- Movement: Move to villageCenter

**Social Phase (13:00-17:00) - ExploreVillage**
- Random wandering within village radius
- Chance encounters with other NPCs
- Responsive to player interactions
- Movement: Random points within wanderRadius of villageCenter

**Evening Phase (17:00-20:00) - EveningRoutine**
- Return to village center area
- Cooking and evening activities (simulated)
- Preparation for night
- Movement: Village center area

**Night Social (20:00-22:00) - SocialTime**
- Seek out other NPCs within proximity (5f)
- Face-to-face interactions (simulated conversations)
- If no NPCs nearby, wander in village center
- Movement: Target other NPCs or wander

**Preparation (22:00-23:00) - NightRoutine**
- Move toward home location
- Preparation activities (simulated)
- Wind down
- Movement: Near homeLocation

**Sleep (23:00-6:00) - Sleep**
- Return to exact homeLocation
- Sleep animation/state
- Inactive period
- Movement: At homeLocation

### Trader/Merchant Schedule Example

**[IMAGE SUGGESTION: Market Trading Sequence]**
*A 4-panel comic-style sequence showing: (1) NPC waking at home, (2) NPC pathfinding to market location, (3) NPC at market stall with shop indicator visible, (4) NPC leaving market when time expires. Include timestamps and activity labels*

**Market Trading Phase (8:00-12:00) - MarketTrading**
Merchants have a specialized schedule focused on commerce:

1. **Navigation to Market**:
   - NPC pathfinds to marketStallLocation (auto-found via tags or hierarchy)
   - Uses A* pathfinding with obstacle avoidance
   - Physics locked during market hours to prevent wandering

2. **Market Setup**:
   - Arrives at StandPoint (tagged location near market building)
   - Spawns market stall visual (if marketStallPrefab defined)
   - Enables shop indicator (visual marker showing shop is open)
   - Loads shop inventory from database (by NPC ID and role)

3. **Trading Period**:
   - NPC stays at market location
   - Shop UI becomes accessible to players
   - Time-based availability enforced (only during market hours)
   - Idle animation, responsive to trade requests

4. **Market Close**:
   - Shop indicator hidden
   - NPC resumes normal routine (follows standard schedule outside market hours)
   - Physics re-enabled for movement

**Role-Based Inventory**:
- **Flower Merchant**: Daisy, Rose, Tulip (low-tier items, Obal currency)
- **Hunter**: Rabbit Pelt, Deer Meat (mid-tier, Obal/Varos)
- **Blacksmith**: Iron Sword, tools (high-tier, Sylv currency)
- **Alchemist**: Health Potions, ingredients (consumables)

### Schedule Flexibility

The system supports:
- **Dynamic activity duration**: Activities can have minimum/maximum durations
- **Conditional transitions**: Activities can end early based on conditions (e.g., no more flowers available)
- **Priority overrides**: Player requests or special events can temporarily override schedule
- **Seasonal variations**: Different schedules can be defined per season

---

## 4. Interaction Model (Voice → Text → AI Response)

**[IMAGE SUGGESTION: Complete Interaction Pipeline]**
*A comprehensive flowchart showing the entire interaction cycle: Player Microphone → Speech-to-Text → Chatbot Server (with branches for Intent Recognition, Context Analysis, Response Generation, TTS) → NPC Response (Audio + Text + Action) → Game Systems (Quest/Trade/Navigation). Use different colors for data types (voice=blue, text=green, actions=red)*

### Multi-Modal Input System

The interaction system supports both traditional text input and voice recognition, creating a natural conversational experience.

**Voice Input Flow:**

1. **Trigger**: Player enters NPC proximity (1.5f radius)
2. **Dialogue Activation**: 
   - NPC dialoguePanel becomes visible
   - NPC pauses current routine activity
   - NPC enters Idle state with animation
3. **Microphone Activation**:
   - SpeechRecognitionTest component begins recording (default 5 seconds)
   - Voice data captured from player microphone
   - Speech-to-text conversion (likely using Web Speech API or cloud service)
4. **Text Received**: Recognized text passed to NPC.Say() method

**Text Processing Flow:**

**[IMAGE SUGGESTION: Context Gathering System]**
*A diagram showing how context is built from multiple sources before sending to AI: Player Message (center) with 3 context bubbles feeding into it: (1) Quest Context (available quests, completed objectives), (2) NPC State Context (current activity, time, location), (3) Conversation History. All merge into a "Complete Context Payload" sent to Chatbot*

1. **Context Gathering**:
   - **Quest Context**: If player's message contains quest-related keywords ("need", "help", "quest", "task"), NPC checks:
     - Available quests from QuestManager for this NPC's ID
     - Completable quests (objectives finished, ready to turn in)
     - Pending quest dialogue state (player said "yes" to previous quest offer)
   - **NPC Activity Context**: Current activity name and game time (e.g., "Currently flower hunting. Time is 14.0")

2. **Chatbot Communication**:
   - Request sent to AI chatbot server at http://127.0.0.1:5000/chat
   - Payload includes:
     ```json
     {
       "text": "Player's message",
       "session_id": "unique_session_per_npc",
       "quest_context": "QUEST_AVAILABLE: Flower Collection\nDescription: ...",
       "npc_context": "Currently flower hunting. Time is 14.0"
     }
     ```

3. **AI Processing** (Server-Side):
   - Intent classification (greeting, quest_ask, trade_request, etc.)
   - Context understanding (quest status, NPC state, conversation history)
   - Response generation (natural language reply)
   - Action determination (GATHER_FLOWER, ASK_FOR_QUEST, TRADE, etc.)
   - TTS audio generation (text-to-speech MP3 file)

4. **Response Handling**:
   - Server returns JSON:
     ```json
     {
       "reply": "Of course! Let me gather some flowers for you.",
       "audio_url": "/tmp/tmp_abc123.mp3",
       "intent": "help_request",
       "action": "GATHER_FLOWER",
       "parameters": {}
     }
     ```

### Response Execution

**Audio & Text Display:**
- Audio response system handles playback
- Audio downloaded and played through audio output
- Text displayed in subtitle area with typing animation effect
- Typing speed synchronized with audio duration for natural timing
- Text overflow handling (auto-clear and continue)
- Emotion markers extracted (**happy**, **nervous**) for future avatar expressions

**Action Execution:**

**[IMAGE SUGGESTION: Action Routing System]**
*A branching diagram showing how chatbot actions are routed: Chatbot Response splits into two paths: (1) NPC-Specific Actions (QUEST_DIALOGUE, ACCEPT_QUEST, GATHER_FLOWER, TRADE) route to NPC Central Orchestrator, (2) Global Actions (NAVIGATE, START_COMBAT) route to Navigation System. Include icons for each action type*

Actions are categorized as NPC-specific or Global:

**NPC-Specific Actions** (handled by central orchestrator):
- `QUEST_DIALOGUE`: NPC explains quest details (no immediate accept)
- `ACCEPT_QUEST_CONFIRM`: Player confirmed "yes" after quest explanation → Accept quest
- `ASK_FOR_QUEST`: Direct quest request → Give quest immediately
- `COMPLETE_QUEST`: Turn in completed quest → Reward player
- `GATHER_FLOWER`: Request NPC to gather flowers → Set player request flag
- `OPEN_SHOP` / `TRADE`: Open trading interface → Trigger trade system
- `SHOW_QUEST_STATUS`: Open quest panel UI

**Global Actions** (handled by navigation system):
- `NAVIGATE`: Show navigation marker to location
- `START_COMBAT`: Initiate combat sequence
- `ANIM`: Trigger animation on NPC

### Dialogue State Management

**[IMAGE SUGGESTION: Multi-Turn Quest Conversation Flow]**
*A conversation tree diagram showing:
- Turn 1: Player asks "Need help?" → NPC explains quest → System stores pending quest
- Turn 2: Player says "Yes" → NPC thanks → System accepts quest
- Turn 3: Player says "No" → NPC acknowledges → System clears pending quest
Include visual indicators for system state changes (pending quest stored/cleared)*

**Quest Dialogue State:**
The system maintains a "pending quest" state to enable natural conversation:

1. Player: "Do you need any help?"
2. NPC: [Chatbot responds] "Actually, yes! I need 10 daisies..." (action: QUEST_DIALOGUE)
3. System: Stores pendingQuestId and pendingQuestContext
4. Player: "Sure!" or "Yes, I'll help"
5. NPC: [Chatbot detects quest context + affirmation] "Thank you!" (action: ACCEPT_QUEST_CONFIRM)
6. System: Accepts quest via QuestManager, clears pending state

**Benefits:**
- Natural multi-turn conversation about quests
- Player can ask for details before accepting
- Prevents accidental quest acceptance from ambiguous phrases
- Maintains conversational context across multiple exchanges

### Microphone Loop

After NPC finishes speaking:
1. Speech completion event triggered
2. 0.5 second delay (prevents audio overlap)
3. Listening system reactivated
4. Microphone reactivated for player's next message
5. Loop continues until player leaves proximity

**Exit Conditions:**
- Player moves beyond 1.8f radius (with hysteresis from 1.5f entry)
- Dialogue panel closes
- Pending quest state cleared
- NPC resumes previous activity

---

## 5. Quest Integration Concept

### Quest Lifecycle Management

The quest system integrates seamlessly with NPC dialogue and AI, creating dynamic quest discovery and completion experiences.

**[IMAGE SUGGESTION: Quest Visual Indicators]**
*Three side-by-side NPC sprites showing different indicator states:
1. NPC with yellow "!" above head (quest available)
2. NPC with green "!" above head (quest completable)
3. NPC with no indicator (no quests)
Include UI mockup of how indicators appear in-game with proper offset and scaling*

**Quest Discovery Indicators:**

Visual feedback system showing quest status at a glance:
- **Yellow Exclamation Mark**: NPC has available quest (quest_status = 'not_started')
- **Green Exclamation Mark**: Quest ready to turn in (all objectives complete)
- **No Indicator**: No quests available or in-progress

Indicators positioned above NPC (1.5f offset) and follow NPC movement.

**Quest Database Integration:**

**[IMAGE SUGGESTION: Quest Database Schema]**
*Entity-Relationship Diagram showing three connected tables:
1. Quests table (quest_id, quest_name, description, npc_id, difficulty, rewards, status)
2. Quest_Objectives table (objective_id, quest_id FK, type, target, quantity, description)
3. Quest_Progress table (quest_id FK, objective_id FK, current_count)
Show relationships with arrows and cardinality (1:N between Quests and Objectives)*

Quests stored in external database with structure:
```
quests table:
- quest_id (primary key)
- quest_name
- description
- npc_id (which NPC gives this quest)
- difficulty (Easy, Medium, Hard)
- reward_gold, reward_exp, reward_item_id
- status (not_started, in_progress, completed)

quest_objectives table:
- objective_id (primary key)
- quest_id (foreign key)
- objective_type (collect, kill, talk, explore)
- target_name (e.g., "Daisy Flower")
- quantity (e.g., 10)
- description

quest_progress table:
- quest_id, objective_id
- current_count
```

**Quest Assignment Flow:**

1. **Quest Check**: Quest system queries available quests for this NPC
2. **Filter**: Returns quests where:
   - NPC ID matches this NPC
   - Status = 'not_started'
   - Player hasn't completed this quest before
3. **Context Building**: When player asks about quests, system builds quest context string:
   ```
   QUEST_AVAILABLE: Flower Collection
   Description: The village elder needs flowers for the festival
   Difficulty: Easy
   Objectives:
   - collect: Gather 10x Daisy Flower
   - collect: Gather 5x Rose
   Rewards: 50 gold, 100 exp
   ```
4. **Chatbot Interpretation**: AI uses quest context to generate natural dialogue
5. **Acceptance**: On player confirmation (via ACCEPT_QUEST_CONFIRM action):
   - Quest acceptance triggered
   - Quest status updated to 'in_progress' in database
   - Quest progress initialized (all objectives set to 0)
   - Quest panel UI updated to show new active quest
   - Event broadcast: Quest accepted

**Quest Progress Tracking:**

**[IMAGE SUGGESTION: Quest Progress UI Mockup]**
*Quest panel UI showing:
- Quest title and description at top
- Objective list with progress bars:
  * "Collect Daisy Flowers: 7/10" (70% filled yellow bar)
  * "Collect Roses: 5/5" (100% filled green bar with checkmark)
- Rewards section showing gold/exp icons with amounts
- "Turn In" button (active when all complete)
Include before/after states showing progress update animation*

Real-time objective tracking:
1. **Trigger Detection**: Item pickups, enemy kills, NPC interactions trigger progress updates
2. **Progress Update**: Quest system updates objective progress
3. **Database Sync**: Progress saved to database immediately
4. **UI Update**: Quest panel reflects new progress (e.g., "Daisies: 7/10")
5. **Completion Check**: When all objectives reach target quantity:
   - Quest marked as "ready to turn in"
   - Green exclamation appears on quest-giver NPC
   - Event broadcast: Quest progress updated

**Quest Completion Flow:**

1. **Return to NPC**: Player sees green exclamation mark
2. **Dialogue Trigger**: Player talks to NPC
3. **Validation**: System checks:
   - Quest exists in active quests
   - All objectives completed (current count >= quantity for all)
   - Quest belongs to this NPC
4. **Completion Execution**:
   - Verify all objectives complete
   - Execute reward distribution
5. **Reward Distribution**:
   - Gold added to player's currency system
   - Experience points added to player level system
   - Items added to inventory system
   - Database updated: quest status = 'completed'
6. **Notification**: "Quest Complete: Flower Collection! +50 gold, +100 exp"
7. **Cleanup**: Green exclamation removed, quest removed from active list

### Quest-Dialogue Integration

The chatbot system enables natural quest conversation:

**Example Conversation Flow:**
```
Player: "Hi! Do you need anything?"
→ Chatbot detects quest intent + checks quest_context
→ NPC: "Actually, yes! The village festival is coming up, and I need flowers. 
        Could you gather 10 daisies and 5 roses for me?" 
        [action: QUEST_DIALOGUE]

Player: "What's the reward?"
→ Chatbot uses stored quest_context
→ NPC: "I can offer you 50 gold coins and some experience. The festival 
        means a lot to our village!" [action: QUEST_DIALOGUE]

Player: "Okay, I'll do it!"
→ Chatbot detects affirmation + has quest_context
→ NPC: "Thank you so much! I'll mark it in your quest log." 
        [action: ACCEPT_QUEST_CONFIRM]
→ System accepts quest, updates UI

[Player completes objectives...]

Player: "I have the flowers!"
→ System detects completable quest
→ NPC: "Wonderful! Here's your reward. The village thanks you!" 
        [action: COMPLETE_QUEST]
→ System distributes rewards
```

**Random Quest Assignment:**

For dynamic content, NPCs can give random quests from a pool:
- `canGiveRandomQuests = true`
- `availableQuestPool = [1, 3, 7, 12]` (quest IDs)
- When NPC has no predefined quests, picks random from pool
- Prevents duplicate quests (checks if player already has/completed)

---

## 6. Trade Concept

### Time-Based Commerce System

The trading system creates a living economy where shops operate on schedules, creating scarcity and planning opportunities.

**Market Schedule Enforcement:**

Traders follow strict time-based availability:
- `marketOpenHour = 8.0f` (8:00 AM)
- `marketCloseHour = 12.0f` (12:00 PM)
- `IsShopOpen()` returns true only when:
  - Current time within market hours
  - NPC is at market location (within marketProximity radius)
  - Both conditions must be met

**Design Intent:**
- Creates "market day" gameplay where players plan visits
- Encourages exploration (finding traders at their stalls)
- Prevents 24/7 convenience, adding realism
- Could extend to different markets on different days (future feature)

### Market Location System

**Auto-Discovery:**
Traders automatically find their workplace:

1. **Tag-Based Search**: Look for market building with "MarketStall" tag
2. **Hierarchy Search**: Search camp building for market structure
3. **Name Pattern Search**: Find any object containing "Market" in name
4. **StandPoint Detection**: Within market, find designated stand location
5. **Fallback**: Create stand position at market location if not found

**StandPoint Concept:**
- Specific position where NPC stands to trade
- Tagged child of Market building
- Local position forced to X=0 (center of stall)
- Prevents NPC from standing inside/behind market building
- Multiple StandPoints allow multiple traders per market

### Role-Based Shop Inventories

**Shop Item Structure:**
Each item in a shop contains:
- Item ID: Database reference
- Item Name: Display name
- Price: Cost in specified currency
- Currency Type: "Obal", "Varos", "Sylv", "Feron", "Astryl", "Aurum"
- Stock: Available quantity (or unlimited)
- Unlimited Flag: Whether stock is infinite
- Icon: Item image for display
- Description: Flavor text

**[IMAGE SUGGESTION: Currency Tier System]**
*A pyramid diagram showing currency hierarchy from bottom to top:
Bottom: Obal (copper coin icon) - Common goods
↑ Varos (silver coin) - Quality items
↑ Sylv (gold coin) - Weapons/Tools
↑ Feron (blue gem) - Special items
↑ Astryl (purple crystal) - Legendary
Top: Aurum (radiant gold) - Premium
Include example items at each tier with icons and prices*

**Currency Tiers:**
The game uses a multi-currency system representing different value tiers:
- **Obal**: Common currency (flowers, basic goods)
- **Varos**: Uncommon currency (quality items, services)
- **Sylv**: Rare currency (weapons, tools)
- **Feron**: Epic currency (special items)
- **Astryl**: Legendary currency (unique items)
- **Aurum**: Premium currency (exclusive items)

Each item specifies which currency it requires, creating economic depth.

**Role-Based Generation:**

**[IMAGE SUGGESTION: Merchant Role Comparison]**
*A 4-panel comparison showing different merchant types with their inventories:
1. Flower Merchant: Colorful flower icons (Daisy, Rose, Tulip) with low prices in Obal
2. Hunter: Animal products (Pelts, Meat) with medium prices in Obal/Varos
3. Blacksmith: Weapons and tools (Sword, Hammer) with high prices in Sylv
4. Alchemist: Potions and ingredients (bottles, herbs) with varied prices
Each panel shows the merchant NPC sprite, 3-4 sample items with icons/prices, and their shop location*

NPCs generate inventory based on their role:

1. **Flower Merchant** (role = "flower_merchant"):
   - Daisy Flower (5 Obal, stock: 20)
   - Rose (15 Obal, stock: 10)
   - Tulip (8 Obal, stock: 15)
   
2. **Hunter** (role = "hunter"):
   - Rabbit Pelt (20 Obal, stock: 8)
   - Deer Meat (30 Varos, stock: 5)
   
3. **Blacksmith** (role = "blacksmith"):
   - Iron Sword (100 Sylv, stock: 3)
   - Tool items (various prices)
   
4. **Alchemist** (role = "alchemist"):
   - Health Potion (25 Obal, stock: 10)
   - Ingredient items

**Database Integration:**
Shops can load from database:
- Query shop items table by NPC ID
- Fallback to role-based generation if database empty
- Allows dynamic inventory updates without code changes

### Trading Interaction Flow

**Opening the Shop:**

**[IMAGE SUGGESTION: Shop UI Mockup]**
*Full trade panel interface showing:
Left side: NPC's shop inventory (grid of items with icons, names, prices, stock count)
Right side: Player's inventory (grid of owned items)
Top: Currency display showing all 6 currency types with amounts
Bottom: Transaction area with quantity slider, total cost, "Buy" button
Include hover state showing item details (description, stats)
Add visual feedback for: insufficient funds (red), successful purchase (green flash)*

1. **Player Request**: Voice/text "I want to trade" or "Show me your wares"
2. **Chatbot Action**: Returns action: "OPEN_SHOP" or "TRADE"
3. **Availability Check**:
   - If not during market hours: "I'm not selling right now. Come back between 8:00-12:00."
   - If not at market location: "I need to be at my market stall to trade."
4. **UI Opening**: Trade panel displays:
   - NPC's shop inventory (left side)
   - Player's inventory (right side)
   - Currency display (player's coins)
   - Buy/Sell tabs

**Purchase Transaction:**

1. **Item Selection**: Player clicks item in shop inventory
2. **Quantity Input**: Slider or input field for quantity (max: available stock)
3. **Price Calculation**: Total cost = item price × quantity
4. **Currency Check**: 
   - If player doesn't have enough currency: "Not enough [currency type]"
5. **Transaction Execution**:
   - Deduct currency from player
   - Add item to player inventory
   - Reduce shop stock
   - Play purchase sound/animation
6. **Notification**: "Purchased 3x Daisy Flower for 15 Obal"

**Selling to NPCs (Future Feature):**
- Player can sell items from inventory to shop
- Sell price typically 50% of buy price
- Shop's role determines what they accept (blacksmith won't buy flowers)
- Stock increases when player sells

### Visual Indicators

**Shop Open Indicator:**
- Sprite displayed above NPC (indicatorOffset = 1.5f)
- Only visible when IsShopOpen() == true
- Can be custom sprite (shop icon) or colored marker
- Follows NPC position in Update()
- Provides visual feedback at a distance

**Market Stall Prefab:**
- Optional visual: instantiated when NPC arrives at market
- Adds immersion (table, goods display, canopy)
- Destroyed when NPC leaves or despawned when market closes
- Positioned at marketStallLocation

---

## Design Patterns & Technical Considerations

### Pathfinding System

**[IMAGE SUGGESTION: A* Pathfinding Visualization]**
*Top-down map view showing:
1. Grid overlay with walkable (green) and obstacle (red) tiles
2. NPC current position (blue circle)
3. Target position (yellow star)
4. Calculated path (purple line with waypoint dots)
5. Alternative blocked path (red X) showing dynamic replanning
Include legend explaining colors and a small inset showing the algorithm steps (open list, closed list, f-cost calculation)*

**A* Implementation:**
- Grid-based pathfinding using map tiles
- Obstacle avoidance (Water, Obstacle layers)
- Dynamic replanning when path blocked
- Continuous movement with physics (Rigidbody2D)
- Smooth turning and acceleration

**Movement Details:**
- Move speed = 3.5 units/second (configurable per NPC)
- Fixed update for physics consistency
- Clamping to map boundaries to prevent NPCs going off-map
- Stop distance = 0.15 units for waypoints, 0.45 units for final destination
- No timeout for long journeys (no artificial fails)

### State Synchronization

**Pause System:**
Dialogue pausing is critical for natural interaction:
- When player enters dialogue, activity paused
- Physics velocity zeroed
- Activity routines check pause flag
- Animation set to Idle
- On dialogue exit, activity resumed
- Routine continues from where it paused

**Physics Locking (for Traders):**
- When at market, physics disabled
- No physics forces applied (prevents movement)
- Movement routine stopped
- Prevents wandering/pushing during trades
- Re-enabled when leaving market

### Event System

**[IMAGE SUGGESTION: Event System Architecture]**
*A pub-sub diagram showing:
Center: Event Bus
Left side: Event Publishers (Database, TimeManager, QuestSystem, InventorySystem)
Right side: Event Subscribers (NPCs, UI, Audio, Lighting)
Arrows showing event flow with labels (QuestAccepted, DayStart, ItemPickup, etc.)
Include timeline showing cascading events: "6:00 AM" → DayStart event → Multiple NPCs wake up → Lighting changes → Audio switches to day ambient*

**Quest Events:**
- Quests Loaded: Database loaded, refresh NPC indicators
- Quest Accepted: Quest accepted, update UI
- Quest Completed: Quest completed, celebrate!
- Quest Progress Updated: Objective progress

**Time Events:**
- Day Start: Triggered at 6:00 AM
- Night Start: Triggered at 20:00 PM
- Hour Changed: Every hour transition
- Season Changed: Season transitions (every 30 days)

NPCs subscribe to these events to react (e.g., refresh quest status when quests loaded).

### Scalability Considerations

**Performance:**
- Routine-based activities (non-blocking)
- Distance-based level of detail for NPC AI (could disable distant NPCs)
- Lazy loading of shop inventories (only when needed)
- Database queries cached in quest system

**Extensibility:**
- Activity enum easily extended with new activities
- Action handlers easily add new chatbot commands
- Role-based systems allow new NPC types without code changes
- Database-driven content (quests, shop items) allows modding

---

## Future Enhancement Ideas

Based on the code architecture, potential expansions:

### Advanced AI
- **Memory System**: NPCs remember past player interactions
- **Relationship Scores**: Friend/enemy system affecting dialogue and prices
- **Personality Traits**: Different NPCs with unique speech patterns and behaviors
- **Group Activities**: Multiple NPCs cooperating on tasks

### Enhanced Routines
- **Weather Reactions**: NPCs seek shelter in rain, stay inside during storms
- **Seasonal Activities**: Different work in different seasons (hunting in winter, farming in spring)
- **Special Events**: Festival days with unique activities and dialogue
- **Fatigue System**: NPCs work slower when tired, affecting efficiency

### Trade Improvements
- **Dynamic Pricing**: Prices change based on supply/demand
- **Bartering**: Negotiate prices through dialogue
- **Special Orders**: NPCs request specific items for rewards
- **Black Market**: Illegal goods available at certain times/places

### Quest Complexity
- **Multi-Stage Quests**: Quests with multiple phases
- **Quest Chains**: Completing one quest unlocks next in series
- **Branching Quests**: Player choices affect quest outcomes
- **Timed Quests**: Limited time to complete certain tasks
- **Co-op Quests**: Multiple NPCs involved in same quest

---

## Conclusion

The NPC AI system represents a sophisticated blend of autonomous behavior, player interaction, and narrative integration. NPCs are not mere quest dispensers but living entities with schedules, needs, and personalities. The multi-modal interaction system (voice + text + AI) creates natural conversations, while the time-based systems and player request overrides balance autonomy with player agency.

The modular architecture ensures extensibility - new activities, dialogue actions, quest types, and trade behaviors can be added without restructuring core systems. Database integration allows for dynamic content updates, and the event-driven design enables loose coupling between systems.

This creates a living, breathing world where players feel they are interacting with real inhabitants rather than programmed robots, achieving the core goal of immersive RPG gameplay.
