using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.Utils;
using GameCommon;

namespace GameEntities
{
    public class AwesomeAircraftType : UnitType
    {
    }

    public class AwesomeAircraft : Unit
    {
        AwesomeAircraftType _type = null; public new AwesomeAircraftType Type { get { return _type; } }

        // Graphics
        public MeshObject mainMeshObject;
        Quat OriginalAileronLeftRot, OriginalAileronRightRot, OriginalElevatorRot;

        // Physic
        public Body mainBody;
        SpringWheel[] springs;

        // Filght Model
        public FlightModel flightModel;
        public ControlAxis axisAileron = new ControlAxis();
        public ControlAxis axisElevator = new ControlAxis();
        public ControlAxis axisRudder = new ControlAxis();
        public ControlThrust controlThrust = new ControlThrust();

        // Initial Parameters
        Vec3 initialPosition;
        Quat initialRotation;


        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);

            foreach (MapObjectAttachedObject attachedObject in AttachedObjects)
            {
                MapObjectAttachedMesh attachedMeshObject = attachedObject as MapObjectAttachedMesh;
                if (attachedMeshObject != null)
                {
                    mainMeshObject = attachedMeshObject.MeshObject;
                }
            }

            if (mainMeshObject.Skeleton != null)
            {
                foreach (Bone bone in mainMeshObject.Skeleton.Bones)
                {
                    bone.ManuallyControlled = true;
                }
            }

            mainBody = PhysicsModel.GetBody("MainBody");

            initialPosition = Position;
            initialRotation = Rotation;

            Reset(Quat.Identity);

            OriginalAileronLeftRot = mainMeshObject.Skeleton.GetBone("BoneAileronLeft").Rotation;
            OriginalAileronRightRot = mainMeshObject.Skeleton.GetBone("BoneAileronRight").Rotation;
            OriginalElevatorRot = mainMeshObject.Skeleton.GetBone("BoneElevator").Rotation;

            SkeletonInstance skel = mainMeshObject.Skeleton;
            springs = new SpringWheel[3];
            float length = 0.3f;
            float stiffness = 20f;
            float damping = 50f;
            springs[0] = new SpringWheel(skel.GetBone("BoneWheelFL"), this, length, stiffness, damping);
            springs[1] = new SpringWheel(skel.GetBone("BoneWheelFR"), this, length, stiffness, damping);
            springs[2] = new SpringWheel(skel.GetBone("BoneWheelB"), this, length, stiffness, damping);

        }

        protected override void OnSuspendPhysicsDuringMapLoading(bool suspend)
        {
            base.OnSuspendPhysicsDuringMapLoading(suspend);

            //After loading a map, the physics simulate 5 seconds, that bodies have fallen asleep.
            //During this time we will disable physics for this entity.
            if (PhysicsModel != null)
            {
                foreach (Body body in PhysicsModel.Bodies)
                    body.Static = suspend;
            }
        }

		protected override void OnTick()
        {
            base.OnTick();

            // Spring forces
            for (int i = 0; i < springs.Length; i++)
                springs[i].ApplyForce(mainBody, TickDelta);


            if (Intellect != null)
            {
                axisAileron.Tick(Intellect);
                axisElevator.Tick(Intellect);
                axisRudder.Tick(Intellect);
                controlThrust.Tick(Intellect);

                if (Intellect.IsControlKeyPressed(GameControlKeys.Weapon1))
                    Reset(Quat.Identity);

                flightModel.TickAndApplyForces(mainBody, TickDelta);
            }

            AdjustBones();

        }




        public virtual void Reset(Quat rotation)
        {
            SpawnPoint spawnPoint = SpawnPoint.GetDefaultSpawnPoint();
            if (spawnPoint == null) return;
            //Quat rot = Mat3.FromRotateByZ(MathFunctions.DegToRad(40)).ToQuat();
            mainBody.Position = initialPosition;
            Utils.LogPosInitial = mainBody.Position;
            mainBody.Rotation = initialRotation * rotation;
            mainBody.LinearVelocity = initialRotation * rotation * new Vec3(0, 0, 0);
            mainBody.AngularVelocity = Vec3.Zero;


            flightModel = new FlightModel(this, "Types/Units/AwesomeAircraft/glider.txt");
            mainBody.LinearDamping = flightModel.param.LinearDamping;
            mainBody.AngularDamping = flightModel.param.AngularDamping;
            axisAileron.Init(GameControlKeys.Left, GameControlKeys.Right, flightModel.handling.aileronInc, flightModel.handling.aileronInc, flightModel.handling.aileronDamping, flightModel.handling.aileronMax);
            axisElevator.Init(GameControlKeys.Forward, GameControlKeys.Backward, flightModel.handling.elevatorInc, flightModel.handling.elevatorInc, flightModel.handling.elevatorDamping, flightModel.handling.elevatorMax);
            axisRudder.Init(GameControlKeys.Jump, GameControlKeys.Reload, flightModel.handling.rudderInc, flightModel.handling.rudderInc, flightModel.handling.rudderDamping, flightModel.handling.rudderMax);
            controlThrust.Init(GameControlKeys.Fire2, GameControlKeys.Fire1, flightModel.handling.accelInc, flightModel.handling.accelInc, 0.5f);

        }

        public virtual void FlipBackUp()
        {
            mainBody.Rotation = Quat.Identity;
            mainBody.Position += new Vec3(0, 0, 2);
        }


        private void AdjustBones()
        {
            SkeletonInstance skel = mainMeshObject.Skeleton;
            Mat3 rot;

            // Aileron Right
            Mat3.FromRotateByY(+MathFunctions.DegToRad(axisAileron.Value * 10), out rot);
            skel.GetBone("BoneAileronRight").Rotation = OriginalAileronRightRot * rot.ToQuat();

            // Aileron Left
            Mat3.FromRotateByY(-MathFunctions.DegToRad(axisAileron.Value * 10), out rot);
            skel.GetBone("BoneAileronLeft").Rotation = OriginalAileronLeftRot * rot.ToQuat();

            // Elevator
            Mat3.FromRotateByY(MathFunctions.DegToRad(axisElevator.Value * 10), out rot);
            skel.GetBone("BoneElevator").Rotation = OriginalElevatorRot * rot.ToQuat();

            // Rudder
            Mat3.FromRotateByZ(-MathFunctions.DegToRad(axisRudder.Value * 10), out rot);
            skel.GetBone("BoneRudder").Rotation = OriginalElevatorRot * rot.ToQuat();


        }



    }
}
