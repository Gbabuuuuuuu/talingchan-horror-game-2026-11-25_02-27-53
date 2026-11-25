NIGHT MAP AND LEFT-HAND FLASHLIGHT

Open SmallTownSandbox and press Play, then click the Game view.
WASD / arrows: move. Mouse: look. Shift: run. Space: jump.
F: draw / holster the left-hand flashlight with an animation.
G: switch flashlight power on / off while drawn.
The flashlight remembers its power switch when put away, and never emits while holstered.
Camera is fixed to first person for this version. V is disabled.

10 working streetlights, 4 shadow-casting interior lights, blue moonlight,
night fog, illuminated fixtures, HDR bloom and ACES tone mapping.
A dedicated Sandbox night URP pipeline / Forward+ renderer is saved under Lighting.
The original render-pipeline assets are preserved.

Phone remains on the right hand; flashlight remains on the left.
E answers, Q declines / ends a call, T triggers a test incoming call.
Both items can be used at the same time.

Scripts/SandboxFlashlight.cs: draw, holster, power, beam, animation, input.
Scripts/SandboxNightLighting.cs: SetStreetLights(bool) and SetInteriorLights(bool).
Editor/SandboxNightSetup.cs: scene lighting setup and TuneLighting().
Inspector exposes beam range, intensity, animation timing and completion events.
