# Upstream Merge Conflict Resolution Guide

## Overview
This document provides guidance for resolving the 171 conflicts from the upstream merge (dec2d42a1d to 61d13ce40d).

## Summary
- **Commits**: ~210 upstream commits
- **Files Changed**: 531
- **Conflicts**: 171 .rej files
- **Auto-Applied**: 360 files

## How to Resolve Conflicts

Each .rej file contains "hunks" (sections of code) that couldn't be automatically applied. You need to:

1. Open the .rej file to see what upstream wanted to change
2. Open the corresponding source file
3. Manually apply the changes while preserving Goobstation-specific code
4. Delete the .rej file when done
5. Test the build

## Conflict Categories

### Priority 1: Core Systems (Must Fix First)
These are critical systems that likely break the build:

**Trigger System Refactor** - Major upstream change
- Content.Server/Explosion/EntitySystems/TriggerSystem.cs.rej
- Content.Shared/Explosion/EntitySystems/TriggerSystem.cs.rej
- Content.Shared/Trigger/* (multiple files)
- Content.Client/Explosion/TriggerSystem.cs.rej

**Chat & Communication Systems**
- Content.Server/Chat/Systems/ChatSystem.cs.rej
- Content.Server/Chat/Managers/ChatSanitizationManager.cs.rej
- Content.Server/Radio/EntitySystems/RadioDeviceSystem.cs.rej

**Body & Metabolism Systems**
- Content.Shared/Body/Systems/SharedBodySystem.Metabolism.cs.rej
- Content.Shared/Body/Systems/StomachSystem.cs.rej
- Content.Server/Nutrition/EntitySystems/HungerSystem.cs.rej

### Priority 2: Gameplay Systems
**NPC Systems**
- Content.Server/NPC/Systems/NPCSystem.cs.rej
- Content.Server/NPC/Systems/NPCCombatSystem.Ranged.cs.rej  
- Content.Server/NPC/Systems/NPCSteeringSystem.cs.rej

**Damage & Combat**
- Content.Server/Damage/Systems/DamageOnTriggerSystem.cs.rej
- Content.Server/Damage/Systems/DamageUserOnTriggerSystem.cs.rej
- Content.Shared/Damage/Systems/StaminaSystem.cs.rej

**Abilities & Powers**
- Content.Server/Abilities/Mime/MimePowersSystem.cs.rej
- Content.Server/Abilities/Psionics/Abilities/TelegnosisSystem.cs.rej

### Priority 3: UI & Client Systems
- Content.Client/Commands/DebugCommands.cs.rej
- Content.Client/Cooldown/CooldownGraphic.cs.rej
- Content.Client/Instruments/InstrumentSystem.cs.rej
- Content.Client/UserInterface/* (several files)

### Priority 4: Prototypes & Content
**Medicine** (8 rejects - significant changes)
- Resources/Prototypes/Reagents/medicine.yml.rej

**Wallmount Devices** (reorganization)
- Resources/Prototypes/Entities/Structures/Wallmounts/WallmountMachines/*.rej

**Other Prototypes**
- Resources/Prototypes/GameRules/roundstart.yml.rej
- Resources/Prototypes/tags.yml.rej

## Recommended Resolution Order

1. Start with **Trigger System** - this is a major refactor that affects many other systems
2. Fix **Chat & Body systems** - core gameplay
3. Resolve **NPC & Combat systems**
4. Handle **UI & Client conflicts**
5. Finally, update **Prototypes & YAML files**

## Testing Strategy

After resolving each priority group:
1. Run `dotnet build`
2. Fix any compile errors
3. Run integration tests
4. Test in-game if possible

## Common Conflict Patterns

**Pattern 1: Goobstation added new features**
- Keep Goobstation's additions
- Apply upstream refactors around them

**Pattern 2: Upstream refactored/renamed**  
- Follow upstream's new structure
- Port Goobstation changes to new structure

**Pattern 3: Both changed same code**
- Carefully merge both sets of changes
- Test thoroughly

## Files to Check Carefully

These files have multiple rejects and likely need significant work:
- Content.Server/Chat/Systems/ChatSystem.cs (likely many Goob changes)
- Content.Server/NPC/Systems/*.cs (NPC behavior differences)
- Content.Shared/Body/Systems/*.cs (metabolism/health system)
- Resources/Prototypes/Reagents/medicine.yml (8 rejects!)

## Next Steps

1. Read through this guide
2. Start with Priority 1 files
3. Use `git diff` to see what was auto-applied
4. Refer to upstream commits for context
5. Test frequently

## Getting Help

- Check upstream PR discussions for context on changes
- Look at commit messages: `git log dec2d42a1d..61d13ce40d -- <file>`
- Compare with vanilla SS14 if needed

Good luck! Take it one category at a time.
