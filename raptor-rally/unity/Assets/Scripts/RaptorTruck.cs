using UnityEngine;

namespace RaptorRally
{
    [System.Serializable]
    public struct TruckSpec
    {
        public string Name, Description;
        public float Length, Width, Acceleration, TopSpeed, Steering, Grip;
        public Color Color;
        public bool Enclosed;
        public TruckSpec(string name, string description, float length, float width, float acceleration, float topSpeed, float steering, float grip, Color color, bool enclosed = false)
        { Name = name; Description = description; Length = length; Width = width; Acceleration = acceleration; TopSpeed = topSpeed; Steering = steering; Grip = grip; Color = color; Enclosed = enclosed; }
        public static readonly TruckSpec[] Lineup = {
            new TruckSpec("F-150 RAPTOR", "Long wheelbase / strong boost", 4.8f, 2.4f, 14, 22, 122, 10f, new Color(.18f,.58f,.82f)),
            new TruckSpec("BRONCO RAPTOR", "Short wheelbase / quick rotation", 4.1f, 2.3f, 13, 20, 148, 11f, new Color(.96f,.49f,.17f), true),
            new TruckSpec("RANGER RAPTOR", "Light pickup / balanced grip", 4.4f, 2.15f, 15, 21, 135, 10.5f, new Color(.68f,.80f,.34f))
        };
    }

    public sealed class RaptorTruck : MonoBehaviour
    {
        public TruckSpec Spec;
        public Rigidbody Body;
        [System.NonSerialized] public Stadium Track;
        public string Driver;
        public bool IsPlayer, Autopilot, Grounded, Boosting;
        public float Nitro = 1, FinishTime = -1, Throttle, Steer;
        public bool Boost;
        public int CompletedLaps, NextGate;
        public bool CrossedStart;
        public float MaxAirHeight;
        float stuckTime, gateWait;
        public int RecoveryCount;
        Transform visual;
        readonly Transform[] wheels = new Transform[4];
        readonly Transform[] wheelPivots = new Transform[4];
        float wheelRoll;
        TrailRenderer[] trails;
        BoxCollider bodyCollider;
        bool[] passingBarriers;
        public bool OffCourse { get; private set; }
        public float Speed => Vector3.ProjectOnPlane(Body.linearVelocity, Vector3.up).magnitude;
        public bool Finished => FinishTime >= 0;
        public float Progress => CompletedLaps * Stadium.GateCount + (CrossedStart ? (NextGate == 0 ? Stadium.GateCount : NextGate) : 0)
            - Mathf.Clamp01(Vector3.Distance(transform.position, Track.Gate(NextGate)) / 14);

        public void Setup(Stadium track, int type, string driver, bool player, Color color)
        {
            Track = track; Spec = TruckSpec.Lineup[type]; Driver = driver; IsPlayer = player;
            gameObject.layer = 8;
            Body = gameObject.AddComponent<Rigidbody>(); Body.mass = 1400;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.centerOfMass = new Vector3(0, -.45f, 0);
            Body.angularDamping = 5;
            var shape = bodyCollider = gameObject.AddComponent<BoxCollider>(); shape.size = new Vector3(Spec.Width, 1.25f, Spec.Length);
            passingBarriers=new bool[track.Barriers.Count];
            shape.center = new Vector3(0, -.15f, 0);
            shape.sharedMaterial = new PhysicsMaterial("Arcade body") { dynamicFriction = 0, staticFriction = 0, bounciness = .05f, frictionCombine = PhysicsMaterialCombine.Minimum };
            var model=new RaptorModel(transform,type,color,player);
            visual=model.Body;
            System.Array.Copy(model.Wheels,wheels,4);
            System.Array.Copy(model.Pivots,wheelPivots,4);
            trails = new TrailRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                var trail = new GameObject("Tire marks").AddComponent<TrailRenderer>(); trail.transform.SetParent(transform, false);
                trail.transform.localPosition = new Vector3((i == 0 ? -1 : 1) * Spec.Width * .42f, -.76f, -Spec.Length * .3f);
                trail.time = 45; trail.minVertexDistance = .4f; trail.widthMultiplier = .18f;
                trail.sharedMaterial = track.Mat(new Color(.25f,.17f,.105f)); trail.emitting = false;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trails[i] = trail;
            }
        }

        public void Spawn(int grid)
        {
            Vector3 p = Track.Gate(0) - Track.Tangent(0) * (5 + grid / 2 * 6) + Track.Side(0) * (grid % 2 == 0 ? -2.2f : 2.2f);
            SetPose(p + Vector3.up * .92f, Track.Tangent(0));
        }
        public void Recover()
        {
            int index = CrossedStart ? Stadium.Wrap((NextGate - 1) * Stadium.Samples / Stadium.GateCount) : 0;
            Vector3 p = Track.Points[index] + (CrossedStart ? 1 : -5) * Track.Tangent(index) + Vector3.up * 1.05f;
            SetPose(p, Track.Tangent(index)); stuckTime = 0; gateWait = 0; RecoveryCount++;
        }
        void SetPose(Vector3 p, Vector3 forward)
        {
            Body.position = p; Body.rotation = Quaternion.LookRotation(forward);
            transform.SetPositionAndRotation(p, Body.rotation);
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            UpdateBarrierRecovery(true);
            if (trails != null) foreach (var trail in trails) trail.Clear();
        }
        public void DriveAI(float dt)
        {
            int near = Track.Nearest(Body.position);
            Vector3 goal = Track.Points[Stadium.Wrap(near + 5)];
            Vector3 local = Quaternion.Inverse(Body.rotation) * (goal - Body.position);
            Steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.9f, -1, 1);
            float bend = Mathf.Abs(Vector3.SignedAngle(Track.Tangent(near), Track.Tangent(near + 8), Vector3.up));
            float targetSpeed = Mathf.Lerp(14.5f, 8.5f, Mathf.Clamp01(bend / 42));
            Throttle = Speed < targetSpeed ? 1 : -.25f;
            Boost = false;
            stuckTime = Speed < 2 ? stuckTime + dt : 0;
            if (!Finished) gateWait += dt;
            if (stuckTime > 3 || gateWait > 14 || Body.position.y < -6) Recover();
        }
        public void Tick(float dt, bool active)
        {
            if (!active) { Boosting = false; foreach (var trail in trails) trail.emitting = false; return; }
            UpdateBarrierRecovery();
            if (!IsPlayer || Autopilot || Finished) DriveAI(dt);
            Grounded = HasGroundContact();
            Boosting = Boost && Throttle > 0 && Nitro > 0 && Grounded;
            if (Boosting) Nitro = Mathf.Max(0, Nitro - dt / 8);
            Vector3 velocity = Body.linearVelocity;
            if (velocity.y > 4.5f) { velocity.y = 4.5f; Body.linearVelocity = velocity; }
            Vector3 planar = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float forward = Vector3.Dot(planar, (Body.rotation * Vector3.forward));
            if (Grounded)
            {
                float throttle = Mathf.Clamp(Throttle, -1, 1);
                float force = throttle < 0 && forward > 1 ? 22 : Spec.Acceleration;
                Body.AddForce((Body.rotation * Vector3.forward) * (throttle * force * (Boosting ? 1.65f : 1)), ForceMode.Acceleration);
                Body.AddForce(-(Body.rotation * Vector3.right) * Vector3.Dot(planar, (Body.rotation * Vector3.right)) * Spec.Grip, ForceMode.Acceleration);
                Body.AddForce(-planar * .28f, ForceMode.Acceleration);
                float yaw = Steer * Spec.Steering * Mathf.Clamp01(Mathf.Abs(forward) / 4) * (forward < -.3f ? -1 : 1);
                Body.MoveRotation(Body.rotation * Quaternion.Euler(0, yaw * dt, 0));
                Body.AddForce(Vector3.down * 6, ForceMode.Acceleration);
            }
            else Body.AddForce(Vector3.down*4,ForceMode.Acceleration);
            float max = Boosting ? Spec.TopSpeed * 1.3f : Spec.TopSpeed;
            if (planar.magnitude > max) Body.linearVelocity = planar.normalized * max + Vector3.up * velocity.y;
            if (forward < -7) Body.linearVelocity = planar.normalized * 7 + Vector3.up * velocity.y;
            int near=Track.Nearest(Body.position);
            Vector3 slope=Track.Points[Stadium.Wrap(near+1)]-Track.Points[Stadium.Wrap(near-1)];
            float pitch=Grounded?-Mathf.Atan2(slope.y,new Vector2(slope.x,slope.z).magnitude)*Mathf.Rad2Deg:Mathf.Clamp(-velocity.y*1.5f,-10,10);
            visual.localRotation=Quaternion.Euler(pitch,0,-Steer*Mathf.Clamp(Speed,0,12)*.45f);
            visual.localPosition=Vector3.up*(Grounded?Mathf.Sin(Time.fixedTime*18)*Mathf.Min(Speed*.002f,.025f):.06f);
            wheelRoll+=forward*dt/0.575f*Mathf.Rad2Deg;
            for(int i=0;i<wheels.Length;i++)
            {
                wheelPivots[i].localRotation=Quaternion.Euler(0,i%2==1?Steer*23:0,0);
                wheels[i].localRotation=Quaternion.Euler(wheelRoll,0,0);
            }
            foreach (var trail in trails) trail.emitting = Grounded && Speed > 5 && Mathf.Abs(Steer) > .25f;
            MaxAirHeight = Mathf.Max(MaxAirHeight, Body.position.y);
            if (Body.position.y < -6) Recover();
        }
        public void CheckGate(float time)
        {
            if (Finished || Track.DistanceFromCourse(Body.position)>Stadium.HalfWidth) return;
            Vector3 delta = Body.position - Track.Gate(NextGate); delta.y = 0;
            // Sequential gates plus forward travel reject backwards laps and infield shortcuts.
            if (delta.sqrMagnitude > 7.2f * 7.2f || Vector3.Dot(Body.linearVelocity, Track.Tangent(NextGate * Stadium.Samples / Stadium.GateCount)) < .2f) return;
            if (NextGate == 0)
            {
                if (CrossedStart) CompletedLaps++;
                CrossedStart = true;
                if (CompletedLaps == 3) FinishTime = time;
            }
            NextGate = (NextGate + 1) % Stadium.GateCount; gateWait = 0;
        }
        bool HasGroundContact()
        {
            if(Physics.Raycast(Body.position+Vector3.up*.1f,Vector3.down,1.12f,1<<9)) return true;
            // Keep traction when the front or rear of the chassis climbs a bank while its center
            // is above lower dirt. A center-only ray incorrectly switches off drive at that point.
            for(int side=-1;side<=1;side+=2) for(int end=-1;end<=1;end+=2)
            {
                Vector3 point=Body.position+Body.rotation*new Vector3(side*Spec.Width*.4f,.1f,end*Spec.Length*.45f);
                if(Physics.Raycast(point,Vector3.down,1.12f,1<<9)) return true;
            }
            return false;
        }
        public void UpdateBarrierRecovery(bool reset=false)
        {
            OffCourse=Track.DistanceFromCourse(Body.position)>Stadium.HalfWidth;
            Vector3 right=Body.rotation*Vector3.right, forward=Body.rotation*Vector3.forward;
            for(int i=0;i<Track.Barriers.Count;i++)
            {
                var wall=Track.Barriers[i];
                Vector3 delta=Body.position-wall.Center; delta.y=0;
                float signed=Vector3.Dot(delta,wall.Outward);
                // Keep each collision pair open until the entire truck clears the wall.
                // An on-track truck cannot initiate passage outwards; an outside truck can return.
                float clearance=Mathf.Abs(Vector3.Dot(right,wall.Outward))*Spec.Width*.5f
                    +Mathf.Abs(Vector3.Dot(forward,wall.Outward))*Spec.Length*.5f+.45f;
                bool nearby=delta.sqrMagnitude<100;
                bool pass=nearby && (signed>0 || (!reset && passingBarriers[i] && signed>-clearance));
                if(pass!=passingBarriers[i])
                {
                    passingBarriers[i]=pass;
                    Physics.IgnoreCollision(bodyCollider,wall.Collider,pass);
                }
            }
        }
    }
}
