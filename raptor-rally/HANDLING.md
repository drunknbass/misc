# Barrier and truck contact handling

Glancing barrier impacts now preserve motion along the rail. During contact, lateral tire grip drops and acceleration into the wall is removed. Steering farther into the barrier is suppressed; a bounded heading correction guides an angled chassis along the wall. Steering away stays available. A square head-on hit stops instead of gaining artificial sideways speed.

Truck-to-truck contacts keep Unity rigidbody impulse exchange. Each vehicle has an arcade mass (F-150 1700, Bronco 1550, Ranger 1400 physics units), so the heavier truck pushes a lighter one more. Impact yaw inertia is six times the chassis default, giving offset bumper hits a modest rotation instead of a spin. Grip temporarily relaxes for 0.28 seconds after vehicle contact, allowing a shove to move the other truck before traction recovers. Collision restitution is zero, and each truck uses eight position/four velocity solver iterations. No repeated scripted collision kick or teleport is applied.

Rendering and visual geometry are unchanged. All existing barrier colliders and one-way off-course re-entry rules remain in place.

## Validation

The Play Mode contact suite passes 84 acceptance checks: angled wall contacts on both sides for all three trucks, steering away after every scrape, square wall hits, all nine truck pairings in rear/offset/side/head-on impacts, both actual segmented circuit rails, and three complete four-truck races. Offset rear impacts turn the target about 4–5 degrees in these scenarios. Full-throttle pushing continues to transfer momentum while the vehicles stay in contact. See COLLISIONS.md for measured distances, speeds and rotation.

The existing simulation suite also passes full races, checkpoints, throttle/nitro/reverse, pause, wheel pivot, follow camera, 24 inward barrier re-entry cases, nine full-nitro jumps, spatial-index equivalence, rail mesh validation and retry reset. These are automated acceptance checks; subjective driving feel still depends on player input.

Run with Unity 6000.6.3f1:

```sh
Unity -batchmode -projectPath /absolute/path/to/unity -executeMethod CollisionVerification.RunBatch -logFile /absolute/path/to/contact.log
Unity -batchmode -quit -projectPath /absolute/path/to/unity -executeMethod PrototypeBuilder.Verify -logFile /absolute/path/to/regression.log
```

Do not add `-quit` to the Play Mode command: the test runner enters Play Mode and exits with its own success/failure code. Play Mode is required because Unity does not deliver the standard contact callbacks during edit-mode simulation. The editor test runner is excluded from player builds.
