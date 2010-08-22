using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using GameCommon;

namespace GameEntities
{
    public class SpringWheel
    {
        Bone bone;
        float size, stiffness, damping;
        float lastPos = -1.0f;
        Vec3 originalBonePosition;
        Quat originalBoneRotation;
        Unit unit;
        MapObjectAttachedMesh mesh;

        public SpringWheel(Bone bone, Unit unit, float size, float stiffness, float damping)
        {
            this.bone = bone;
            this.size = size;
            this.stiffness = stiffness;
            this.damping = damping;
            this.unit = unit;
            originalBonePosition = bone.Position;
            originalBoneRotation = bone.Rotation;

            mesh = new MapObjectAttachedMesh();
            mesh.MeshName = "Types/Units/AwesomeAircraft/AwesomeAircraftWheel.mesh";
            mesh.ScaleOffset = new Vec3(1, 1, 1);
            mesh.RotationOffset = Quat.Identity;
            mesh.PositionOffset = bone.GetDerivedPosition();
            unit.Attach(mesh);

        }

        public void ApplyForce(Body body, float TickDelta)
        {
            Vec3 originalBoneDerivedPosition = bone.Parent.Position + bone.Parent.Rotation * originalBonePosition;
            Quat originalBoneDerivedRotation = bone.Parent.Rotation * originalBoneRotation;
            Vec3 pos = body.Position + body.Rotation * originalBoneDerivedPosition;
            Vec3 dir = body.Rotation * originalBoneDerivedRotation * new Vec3(size, 0, 0);
            Ray ray = new Ray(pos, dir);
            RayCastResult res = PhysicsWorld.Instance.RayCast(ray, (int)ContactGroup.CastOnlyContact);

            if (res.Distance != 0)
            {
                // Stiffness
                float forceStiffness = 0, forceDamping = 0;
                forceStiffness = (size - res.Distance) * stiffness;

                // Damping
                if (lastPos != -1.0f)
                {
                    float speed = res.Distance - lastPos;
                    forceDamping = -speed * damping;
                }
                lastPos = res.Distance;

                //EngineConsole.Instance.Print("force(" + bone.Name + ")=" + forceStiffness.ToString() + ", " + forceDamping.ToString());
                body.AddForce(ForceType.LocalAtLocalPos, TickDelta, new Vec3(0, 0, forceStiffness + forceDamping), originalBoneDerivedPosition);

                // Lateral Friction
                Vec3 speedAtWheel = body.LinearVelocity + Vec3.Cross(body.AngularVelocity, body.Rotation * bone.GetDerivedPosition());
                float lateralForce = -Vec3.Dot(speedAtWheel, body.Rotation * Vec3.XAxis) * 0.3f;
                body.AddForce(ForceType.LocalAtLocalPos, TickDelta, new Vec3(lateralForce, 0, 0), bone.GetDerivedPosition());

                // Longitudinal Friction
                float longitudinalForce = -Vec3.Dot(speedAtWheel, body.Rotation * Vec3.YAxis) * 0.001f;
                body.AddForce(ForceType.LocalAtLocalPos, TickDelta, new Vec3(0, longitudinalForce, 0), bone.GetDerivedPosition());


                bone.Position = originalBonePosition - new Vec3(0, 0, res.Distance);
                float wheelRadius = 0.0439f;
                mesh.PositionOffset = originalBoneDerivedPosition - new Vec3(0, 0, res.Distance - wheelRadius);
            }
        }
    }

}
