PHONE SYSTEM - SMALL TOWN SANDBOX

Press Play and click the Game view. A demo call arrives after 6 seconds.
T: trigger another incoming call when idle.
E: answer an incoming call.
Q: decline a ringing call, or hang up / cancel while picking up.
Esc: release the cursor, then click the on-screen buttons.
Ignoring the call for 20 seconds records a missed call.
V: first / third person. Both views include a phone animation.

Sequence: Ringing -> PickingUp -> RaisingToEar -> InCall -> PuttingAway -> Idle.
The incoming ringtone stops as soon as the player answers or declines.
InCall and onAnswered begin only after the phone reaches the ear.
No conversation or voice dialogue is included in this step.

Code: Assets/TalingchanSandbox/Scripts/SandboxPhone.cs
Component: SandboxPhone on Player - WASD Mouse Space.
Call BeginIncomingCall("Caller name") from future trigger / quest code.
Listen to onAnswered to begin dialogue, then call EndCall() when it finishes.
Additional events: onRinging, onDeclined, onMissed, onEnded.
Public methods reject invalid state transitions and overlapping incoming calls.
Timing and demo caller can be edited on the component in the Inspector.

Animation: lightweight two-bone right-arm pose, phone pickup, raise, ear hold,
and put-away, plus a separate first-person hand / phone model.
The rig, phone model, and materials are saved in the scene / project.
No third-party assets or extra packages were required.
