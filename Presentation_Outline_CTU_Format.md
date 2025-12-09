# Alex The Wanderer - Project Presentation
## Table of Contents & Outline

**Czech Technical University in Prague (ČVUT)**  
Faculty of Information Technology / Faculty of Electrical Engineering  
Academic Year: 2024/2025

---

## PRESENTATION METADATA

**Project Title:** Alex The Wanderer: An AI-Powered Interactive RPG Experience  
**Project Type:** Unity 2D Game Development - One Month Sprint  
**Presentation Duration:** 20-25 minutes  
**Number of Slides:** 25 + Backup slides  
**Date:** December 2025  

**CTU Branding Resources:**
- Official CTU Logo (Blue version)
- CTU Presentation Template: https://www.cvut.cz/en/ctu-logo
- CTU Brand Style Guide colors:
  - Primary Blue: Pantone 2945 C / RGB(0, 101, 189) / #0065BD
  - Secondary colors as per CTU guidelines
- Font: Technika (CTU official font) or Arial/Helvetica as fallback

---

## TABLE OF CONTENTS

### I. INTRODUCTION (Slides 1-4)
**Slide 1: Title Slide**
- University logo (top right)
- Project title: "Alex The Wanderer"
- Subtitle: "An AI-Powered Interactive RPG Experience"
- Student/Team information
- Course/Module name
- Date
- Supervisor/Advisor name (if applicable)

**Slide 2: Table of Contents**
- Overview of presentation structure
- Main sections highlighted
- Estimated time per section

**Slide 3: Project Overview**
- Game genre and platform
- Development timeline
- Key innovation summary
- Target audience
- Project objectives

**Slide 4: Motivation & Goals**
- Problem statement: Traditional NPC interactions
- Solution: AI-powered voice conversations
- Project scope and deliverables
- Success criteria

---

### II. TECHNICAL ARCHITECTURE (Slides 5-8)
**Slide 5: System Architecture Overview**
- High-level architecture diagram
- Three-tier system:
  - Frontend: Unity 2D Game Engine
  - Backend: Python Flask REST API
  - AI Layer: Chatbot with voice recognition
- Technology stack overview

**Slide 6: Frontend - Unity Game Engine**
- Unity version and render pipeline (URP)
- C# scripting framework (60+ scripts)
- 2D animation and sprite systems
- Physics engine and pathfinding
- Scene management (5 main scenes)

**Slide 7: Backend - REST API System**
- Python Flask framework
- MySQL database architecture
- 28 REST API endpoints
- API categories overview
- Server port and configuration

**Slide 8: AI Integration Layer**
- Speech-to-text processing
- Natural language understanding (NLU)
- Context-aware response generation
- Text-to-speech (TTS) synthesis
- Voice interaction pipeline

---

### III. CORE GAME SYSTEMS (Slides 9-14)
**Slide 9: Game Systems Overview**
- Six core systems introduction:
  1. NPC AI System
  2. Quest Management
  3. Trading & Economy
  4. Inventory Management
  5. Player Progression
  6. Time Management

**Slide 10: NPC AI System - Living World Concept**
- Autonomous daily routines (24-hour cycle)
- Time-based behavior transitions
- Activity scheduling system
- Player request override mechanism
- Dialogue pause/resume functionality

**Slide 11: NPC AI - State Management Architecture**
- Dual-state system:
  - Activity States (What NPCs do)
  - Execution States (How they execute)
- State transition diagram
- Activity types: Sleep, Work, Social, Market Trading
- Benefits of modular design

**Slide 12: Voice Interaction System**
- Multi-modal input (voice + text)
- Interaction pipeline flowchart:
  1. Player voice input (microphone)
  2. Speech-to-text conversion
  3. AI processing (intent + context)
  4. Response generation
  5. TTS audio + text display
  6. Action execution
- Supported actions and commands

**Slide 13: Quest Management System**
- Quest lifecycle overview
- Database schema (3 tables):
  - Quests table
  - Quest objectives table
  - Quest progress table
- Visual indicators (!, ✓)
- Quest acceptance workflow
- Real-time progress tracking

**Slide 14: Trading & Economy System**
- Multi-currency economy (6 tiers):
  - Obal (Common)
  - Varos (Uncommon)
  - Sylv (Rare)
  - Feron (Epic)
  - Astryl (Legendary)
  - Aurum (Premium)
- Time-based market system (8 AM - 12 PM)
- Role-based NPC shops
- Transaction workflow

---

### IV. TECHNICAL IMPLEMENTATION (Slides 15-18)
**Slide 15: Database Architecture**
- MySQL database schema
- Key tables:
  - Players, Items, Inventory
  - Quests, Objectives, Progress
  - NPCs, Shop Inventory
  - Coins, Player Coins
- Entity-relationship diagram
- Data flow overview

**Slide 16: API Endpoint Architecture**
- 28 REST API endpoints breakdown:
  - Items API (1 endpoint)
  - Inventory API (3 endpoints)
  - Player API (3 endpoints)
  - Quest API (10 endpoints)
  - Coin API (5 endpoints)
  - Shop API (3 endpoints)
  - NPC API (2 endpoints)
- Request/response examples
- Error handling

**Slide 17: Pathfinding & Navigation**
- A* pathfinding algorithm
- Grid-based navigation system
- Obstacle avoidance (water, objects)
- Dynamic path replanning
- Map boundary clamping
- Performance optimization

**Slide 18: Event-Driven Architecture**
- Event bus system
- Event publishers:
  - Database events
  - Time events
  - Quest events
  - Inventory events
- Event subscribers:
  - NPCs, UI, Audio, Lighting
- Loose coupling benefits

---

### V. GAME FEATURES & MECHANICS (Slides 19-21)
**Slide 19: Inventory System**
- Dynamic inventory management
- Multiple bag types (different capacities)
- Item stacking and organization
- Drag-and-drop UI
- Database synchronization
- 60+ collectible items
- Coin inventory (separate UI)

**Slide 20: Player Progression System**
- Experience points (EXP) mechanics
- 6-tier EXP items:
  - Tier 1: Ember EXP
  - Tier 2: Spark EXP
  - Tier 3: Flame EXP
  - Tier 4: Blaze EXP
  - Tier 5: Inferno EXP
  - Tier 6: Supernova EXP
- Leveling curve
- Character stats (health, stamina)
- Visual feedback systems

**Slide 21: Time Management System**
- 24-hour in-game cycle
- Real-time to game-time conversion
- Day/night transitions
- Seasonal changes (30-day cycles)
- Time-dependent behaviors:
  - NPC schedules
  - Shop availability
  - Dynamic lighting
  - Audio ambience

---

### VI. DEVELOPMENT PROCESS (Slides 22-23)
**Slide 22: Development Statistics**
- Code metrics:
  - 60+ C# scripts
  - 28 REST API endpoints
  - 5 Unity scenes
  - 100+ prefabs
- Asset count:
  - 2D sprites and animations
  - UI components
  - Audio assets
  - Icon library
- Documentation: 10+ technical guides

**Slide 23: Technical Challenges & Solutions**
- Challenge 1: Speech recognition integration → Solution: Web API wrapper
- Challenge 2: Real-time database sync → Solution: Event-driven updates
- Challenge 3: Complex state management → Solution: Dual-state architecture
- Challenge 4: Time-based coordination → Solution: Centralized TimeManager
- Challenge 5: Multi-currency balancing → Solution: Database-driven pricing
- Challenge 6: Quest progress accuracy → Solution: Real-time tracking system

---

### VII. RESULTS & EVALUATION (Slides 24-25)
**Slide 24: Project Achievements**
- ✅ Functional RPG prototype delivered
- ✅ AI voice interaction integration
- ✅ Living world with autonomous NPCs
- ✅ Complete quest system
- ✅ Multi-currency economy
- ✅ 60+ items and progression system
- ✅ Professional documentation
- ✅ Full-stack demonstration

**Slide 25: Demo Highlights**
- Live demonstration scenarios:
  1. Voice conversation with NPC
  2. NPC daily routine observation
  3. Quest acceptance via dialogue
  4. Market trading (time-based)
  5. Currency transaction
  6. Inventory management
  7. Real-time quest progress
  8. Day/night cycle

---

### VIII. FUTURE WORK & CONCLUSION (Slides 26-28)
**Slide 26: Future Enhancement Roadmap**
- Phase 1: Enhanced AI
  - Memory system for NPCs
  - Relationship scores
  - Personality traits
- Phase 2: Expanded Gameplay
  - Weather reactions
  - Seasonal activities
  - Special events
- Phase 3: Advanced Features
  - Dynamic pricing
  - Quest chains
  - Multiplayer co-op

**Slide 27: Lessons Learned**
- Technical lessons:
  - Importance of modular design
  - Database-driven content benefits
  - State synchronization challenges
- Development lessons:
  - Documentation value
  - Time management in sprints
  - Integration complexity
- Design lessons:
  - Natural language interfaces
  - Player agency vs. NPC autonomy

**Slide 28: Conclusion**
- Project success summary
- Key contributions:
  - Modern AI integration in games
  - Living world design pattern
  - Full-stack game architecture
- Technical skills demonstrated
- Academic learning outcomes
- Future research directions

**Slide 29: Questions & Discussion**
- Q&A session
- Technical demonstrations available
- Contact information
- References and acknowledgments

---

### IX. BACKUP SLIDES (30-35)
**Slide 30: Detailed Code Architecture**
- Class diagram
- Component hierarchy
- Dependency graph

**Slide 31: Database Schema Visualization**
- Complete ERD (Entity-Relationship Diagram)
- Table relationships
- Indexes and constraints

**Slide 32: API Endpoint Reference Table**
- Complete endpoint list with examples
- Request/response formats
- Status codes

**Slide 33: NPC Schedule Timeline**
- 24-hour schedule visualization
- Activity breakdown by hour
- Multiple NPC role comparisons

**Slide 34: Currency System Deep Dive**
- Tier comparison table
- Item pricing examples
- Economic balancing methodology

**Slide 35: Performance Metrics**
- Frame rate analysis
- API response times
- Database query optimization
- Memory usage statistics

---

## PRESENTATION GUIDELINES

### Slide Design Standards (CTU Format)
1. **Title Slide Layout:**
   - CTU logo (top right corner, official blue version)
   - Faculty name below logo
   - Project title (centered, large bold font - Technika Bold or Arial Bold)
   - Subtitle (centered, smaller font)
   - Student/team info (centered, middle)
   - Course/supervisor info (centered, bottom)
   - Date (bottom right)

2. **Content Slide Layout:**
   - CTU logo miniature (top right corner)
   - Slide title (top left, CTU blue color: #0065BD)
   - Slide number (bottom right)
   - Maximum 6-7 bullet points per slide
   - Use CTU brand colors for accents
   - Leave sufficient white space

3. **Typography:**
   - Headings: Technika Bold / Arial Bold (24-28pt)
   - Body text: Technika Regular / Arial (18-20pt)
   - Captions: Technika / Arial (14-16pt)
   - Minimum font size: 14pt for readability

4. **Color Scheme:**
   - Primary: CTU Blue (#0065BD)
   - Secondary: Dark Gray (#333333) for text
   - Accents: Light Blue (#4A90E2), Orange (#F5A623) for highlights
   - Background: White or light gray (#F5F5F5)

5. **Visual Elements:**
   - Use high-quality diagrams and charts
   - Include screenshots of game features
   - Add flowcharts for technical processes
   - Use icons for bullet points (optional)
   - Maintain consistent visual style

### Content Guidelines
- **Time allocation:** ~1 minute per slide average
- **Introduction:** 3-4 minutes
- **Technical Architecture:** 4-5 minutes
- **Core Systems:** 6-7 minutes
- **Implementation:** 4-5 minutes
- **Results & Demo:** 3-4 minutes
- **Conclusion:** 2-3 minutes
- **Q&A:** 5-10 minutes

### Presentation Tips
1. Start with attention-grabbing demo clip (15 seconds)
2. Use storytelling: Problem → Solution → Implementation → Results
3. Include live demo if possible (2-3 minutes)
4. Prepare for technical questions on:
   - AI integration methodology
   - Database design decisions
   - State management approach
   - Performance optimization
5. Have backup slides ready for deep technical discussions
6. Practice timing to stay within 20-25 minutes
7. Prepare 3-4 key takeaway points

---

## APPENDIX

### A. References & Resources
- Unity Documentation: https://docs.unity3d.com/
- Python Flask API: https://flask.palletsprojects.com/
- MySQL Database: https://dev.mysql.com/doc/
- Speech Recognition APIs
- NPC AI Design Patterns
- Game Development Best Practices

### B. Acknowledgments
- CTU Faculty advisors
- Project supervisors
- Peer reviewers
- Open-source community contributions

### C. Contact Information
- Student/Team email
- Project repository (if public)
- LinkedIn/Portfolio links

### D. License & Usage
- Project license information
- Asset attribution
- Third-party library credits

---

## SLIDE COUNT SUMMARY

| Section | Slides | Duration |
|---------|--------|----------|
| Introduction | 4 | 3-4 min |
| Technical Architecture | 4 | 4-5 min |
| Core Game Systems | 6 | 6-7 min |
| Technical Implementation | 4 | 4-5 min |
| Game Features | 3 | 3-4 min |
| Development Process | 2 | 2-3 min |
| Results & Evaluation | 2 | 2-3 min |
| Future Work & Conclusion | 4 | 3-4 min |
| **Total Main Slides** | **29** | **20-25 min** |
| Backup Slides | 6 | As needed |
| **Grand Total** | **35** | **25-30 min** |

---

## PRESENTATION CHECKLIST

### Before the Presentation
- [ ] Download CTU official PowerPoint template from https://www.cvut.cz/en/ctu-logo
- [ ] Verify CTU logo usage (correct version, placement, size)
- [ ] Check all fonts are CTU-compliant (Technika or fallback)
- [ ] Test all embedded videos/animations
- [ ] Verify demo environment is working
- [ ] Prepare presenter notes for each slide
- [ ] Practice presentation timing (target: 20-25 min)
- [ ] Prepare answers to potential questions
- [ ] Test on presentation computer/projector
- [ ] Have backup USB drive with PDF version

### During the Presentation
- [ ] Introduce yourself and project context
- [ ] Maintain eye contact with audience
- [ ] Speak clearly and at moderate pace
- [ ] Explain technical terms for non-specialists
- [ ] Use pointer/laser for diagrams
- [ ] Monitor time allocation per section
- [ ] Engage audience with questions
- [ ] Demonstrate enthusiasm for the project

### After the Presentation
- [ ] Answer questions thoroughly
- [ ] Provide additional information if requested
- [ ] Share contact information
- [ ] Collect feedback for future improvements
- [ ] Document questions for project report

---

**Document Version:** 1.0  
**Last Updated:** December 2025  
**Template Compliance:** CTU Presentation Standards  
**Status:** Ready for slide creation

---

## QUICK START INSTRUCTIONS

1. **Download CTU Template:**
   - Visit: https://www.cvut.cz/en/ctu-logo
   - Download "Presentation Templates" ZIP file
   - Extract PowerPoint template (.pptx)

2. **Customize Template:**
   - Replace placeholder text with outline content
   - Add faculty/course information to title slide
   - Insert student/team details

3. **Add Content:**
   - Follow table of contents structure above
   - Use one slide per outline item
   - Add diagrams and screenshots from project

4. **Review & Polish:**
   - Check CTU branding compliance
   - Verify font consistency
   - Test animations and transitions
   - Proofread all content

5. **Practice:**
   - Rehearse with timer
   - Get feedback from peers
   - Refine based on feedback
   - Finalize presenter notes

---

**END OF OUTLINE**
