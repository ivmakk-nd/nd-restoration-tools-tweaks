# Copilot code-review instructions

This repo is a BepInEx 5 (Mono) Harmony mod for *Nomad Drive*, a co-op Unity game on Mirror. The plugin derives from `BaseUnityPlugin` and patches the game's `Assembly-CSharp.dll` at runtime. Review with these traps in mind; a general C# review misses most of them.

## Unity and Harmony traps

- **Unity null.** `UnityEngine.Object` overloads `==`, so a destroyed object compares equal to `null` only through `==` or its bool conversion. Flag `?.`, `??`, `is null`, and `is not null` on a Unity object (a `Component`, a `GameObject`, a `ScriptableObject`).
- **Guard game lookups.** Services, components, and singletons are often null during a scene load or in the main menu. Flag an unchecked dereference inside a patch.
- **A patch must not break the game.** Each patch body sits in a `try`/`catch` that logs the error once. Flag a patch body without one, and a log call inside a per-frame path with no once-only guard.
- **Reflection lookups can fail after a game update.** `AccessTools.Method`, `AccessTools.Field`, and `FieldRefAccess` by name return null or throw when the game renames a member. Flag a lookup whose result is used with no null check, and a lookup repeated on each call instead of cached in a static field.
- **Prefer a Postfix.** Flag a Prefix that returns `false` (skips the original) with no comment that says why.
- **State machines.** A patch of an `async` method or a coroutine runs only at its start; the work is in the `MoveNext` of the state machine. Flag a patch that expects to see the result of such a method.
- **Burst.** Flag a patch of an `Execute` method of a job with `[BurstCompile]`: it has no effect.

## Multiplayer (Mirror)

- **Woven methods.** The Mirror weaver moves the body of each `[Command]`, `[ClientRpc]`, and `[TargetRpc]` method into `UserCode_<Method>__<ParamTypes>`. Flag a patch on the original `Cmd`/`Rpc` method that expects to change what the receiver does.
- **SyncVars.** A `[SyncVar]` field syncs only when written through its `Network<field>` property, and only on the host. Flag a direct write of a SyncVar field, and a write on a client.
- **Side.** Flag server logic that runs with no `NetworkServer.active` or `isServer` check, and a change of shared state from a client.

## Performance

- Flag `FindObjectsByType`, `FindFirstObjectByType`, `GetComponentsInChildren`, `Camera.main`, LINQ, string building, or `Traverse` in an `Update`, `FixedUpdate`, or a patch of a per-frame or per-physics-step method.

## Structure and tests

- **Pure logic is separated and tested.** Logic that does not need the running game lives in its own file with no BepInEx, Unity, or game reference, unit-tested under `tests/`. Flag new pure logic in a patch class, and new pure logic with no test.
- **One patch class for each target method,** named `<Feature>On<Target>`.

## Release and config hygiene

- **Verbose ships off.** The `Verbose` config binds with default `false`. Diagnostic tracing goes on `LogDebug` behind it; `LogInfo` stays quiet apart from the load line.
- **The plugin GUID never changes.** It is the BepInEx identity and the config file name. Flag any edit to it.
- **The version is in two places that must agree:** `<Version>` in the csproj and the `BepInPlugin` attribute.
- **No committed build output.** Flag `bin/`, `obj/`, `dist/`, or a game DLL in the diff. Game `<Reference>` entries keep `<Private>false</Private>`.
- **No game source.** Flag a method body copied from the decompiled game code.
- **Changelog matches the change.** A player-visible change adds an `[Unreleased]` entry to `CHANGELOG.md` in player-facing wording. An internal-only refactor gets none.

## This mod

Restoration Tools Tweaks changes the brush size of the restoration tools (`PolishTool` and `GrinderTool`, both `RestorationTool`; `CleanTool`; `PaintSprayTool`) and the drain that goes with it.

- **Patch targets.** `HeldItem.IsCapturingScrollInput` (getter), `RestorationTool.Update` and `OnUnequip`, `CleanTool.Update` and `OnUnequip`, `PaintSprayTool.Awake` and `Update`, `ChargableDirectHeldItem.Update`, and the woven hit relays `UserCode_CmdRelayHitPoint__...` and `UserCode_CmdRelayHitLine__...` of `RestorationTool`, `CleanTool`, and `PaintSprayTool`. The private members are resolved once in `GameMembers.Resolve()` at `Awake`; a missing one turns the whole mod off with one warning (`UnpatchSelf`). Flag a private-member lookup outside `GameMembers`.
- **Multiplayer kinds.** The brush scale and the ring are local to the player who holds the tool. The drain runs only on the host (`isServer` checks in the drain prefixes, the relay postfixes run in `[Command]` bodies). The extra Spray Paint steps are synced by the game as a step byte, so they need the mod on every player. Flag drain code without a host check, and brush code that writes shared state.
- **Drain prefix and finalizer pairs.** A drain prefix scales the game's own rate field for one `Update` call and records the old value in `__state`; its finalizer restores the field, also when `Update` throws, and settles the work debt with what the game actually took. The mod never takes charge, condition, or paint itself, so the game's empty, dry, and out-of-paint paths stay intact. Flag a prefix that changes a rate without a matching restore, a finalizer that can throw, and any direct write of `_serverCharge`, `Condition`, or `PaintCapacity`.
- **The 2 m cap.** The host clamps a relayed brush radius to 2 m. Brush scale logic must respect it (`BrushScaleLogic.CapScale`).
- **Mask reads.** The work counters read the surface masks on the GPU only in `Work` drain mode or with `Verbose` on, only for panels touched in the last 2 s, and a counter idle for 2 s is destroyed. Flag a read that runs in `Area` or `Time` mode without `Verbose`, and a counter that is disabled instead of destroyed.
