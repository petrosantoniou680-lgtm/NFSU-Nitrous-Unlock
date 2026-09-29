NFS UNDERGROUND (2003) — INFINITE NITROUS + UNLOCK

Built specifically for the speed.exe inspected in:
C:\Program Files (x86)\EA Games\NFS Underground

USE
1. Close the game.
2. Open NFSU-Mod.exe. Both features are selected by default.
3. Click Apply selected features. Windows may ask for administrator access
   because the game is installed in Program Files.
4. Start the game normally. Fit a nitrous upgrade, then use the normal nitrous key.

Infinite nitrous stops consumption; it does not automatically fit a nitrous kit.
Parts are unlocked for selection, not automatically fitted or made free.
The game's native unlock-everything switch also unlocks cars and tracks.
The nitrous changes affect the shared vehicle routines, so they may also benefit
AI cars that use those routines. This is not a player-only patch.

UNDO / CHANGE FEATURES
Close the game and reopen NFSU-Mod.exe. Click Restore original, or change the
checkboxes and apply again. The original speed.exe is backed up beside it as:
speed.exe.before-nfsu-mod.bak
The backup is hash-verified and is never overwritten with a modified version.
Restoring the executable does not reverse purchases or other progress saved by
the game while the unlock option was enabled. The patcher does not edit saves.

COMPATIBILITY
Exact original size: 3,178,496 bytes
Original SHA-256:
8234B86F0BCABB9854CE89E7DF52534787D923080BA34791C79E6F5040920EA8

This executable has the entry point and file size associated with English 1.4,
but its unlock variable differs from the address in current Extra Options.
The patcher checks the full hash and refuses other builds or unexpected edits.
No ASI loader, Cheat Engine, internet connection, or background trainer is needed.
NfsuMod.cs is the complete patcher source. No game executable is bundled.

VALIDATION
Verified against the installed executable with disassembly and x86 emulation.
Tested nitrous activation, remaining supply, release, and long boost duration;
tested independent feature selection, backup, restore, and rejection of changes.
Not yet verified in a live race or garage. See Verification.json for results.

IMPLEMENTATION
Eight instruction changes prevent the nitrous timer and bottle allocation from
depleting supply. A startup stub sets this build's native unlock byte, then
returns to the unchanged original entry point. The stub preserves registers.
Installation uses a checked temporary file and an atomic executable replacement.

Research reference for the native unlock flag and common build identification:
https://github.com/ExOptsTeam/NFSUExOpts
The actual target addresses and instruction bytes were checked locally.

BUILD SOURCE
From a .NET Framework compiler command prompt in this folder:
csc /target:winexe /platform:x86 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:NFSU-Mod.exe NfsuMod.cs
