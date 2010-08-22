using System;
using System.IO;
using System.Collections;
using Engine;
using Engine.FileSystem;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.EntitySystem;
using Engine.UISystem;
using Engine.Renderer;
using GameCommon;

namespace GameEntities
{

    public class FlightModel
    {

        public class AircraftSSSGeneralParam
        {
            public string msg1, msg2;
            public float mass;
            public float I_x, I_y, I_z;
            public float geometry_offset_for_wheels;
            public float CLMult = 1.0f;
            //Force Mult
            public float Mult_Lift = 1.0f;
            public float Mult_Drag = 1.0f;
            public float CapForce = 100.0f;
            //Damping
            public float LinearDamping = 0.2f;
            public float AngularDamping = 0.05f;
            public void Adapt()
            {
                float ix = I_x, iy = I_y, iz = I_z;
                I_x = iy;
                I_y = iz;
                I_z = ix;
                msg1 = msg1.Replace('_', ' ');
                msg2 = msg2.Replace('_', ' ');
            }
        }

        public class AircraftHandling
        {
            public float thrustInc = 1;
            //Aileron Controls (Banking)
            public float aileronMax = 1.0f;
            public float aileronInc = .05f;
            public float aileronDamping = .03f;
            //Elevator Controls (Pitch)
            public float elevatorMax = 1f;
            public float elevatorInc = .05f;
            public float elevatorDamping = .03f;
            //Rudder Controls (Yaw)
            public float rudderMax = 1f;
            public float rudderInc = 1f;
            public float rudderDamping = 0f;
            //Accel
            public float accelInc = .005f;
            public float accelMax = 10.0f;
            public float speedMax = 15.0f;
        }


        private AwesomeAircraft awesomeAircraft;
        public float scale = 1.0f;

        private const float minimalContactSpeed = 1f;

        private string script;
        public AircraftSSSGeneralParam param = new AircraftSSSGeneralParam();
        public AircraftHandling handling = new AircraftHandling();
        private Aerofoil[] aerofoils;


        public FlightModel(AwesomeAircraft awesomeAircraft, string script)
        {
            this.awesomeAircraft = awesomeAircraft;
            VirtualFileStream s = VirtualFile.Open(script);
            long len = s.Length;
            byte[] buf = new byte[len];
            s.Read(buf, 0, (int)len);
            string str = "";
            for (long i = 0; i < len; i++)
                str += (char)buf[i];
            StringReader f = new StringReader(str);
            Load(f);
            s.Close();
        }


        protected void Load(TextReader f)
        {
            param = (AircraftSSSGeneralParam)Utils.LoadStructure(f, param, "end general", null);
            param.Adapt();

            Hashtable listAerofoil = new Hashtable();
            //Hashtable listWheel = new Hashtable();
            //Hashtable listEngines = new Hashtable();

            string line = f.ReadLine();
            if (line != null) line = line.Trim();
            while (line != null)
            {
                if (line == "begin handling")
                    handling = (AircraftHandling)Utils.LoadStructure(f, handling, "end handling", null);
                if (line == "begin aerofoil")
                {
                    Aerofoil aerofoil = new Aerofoil();
                    aerofoil = (Aerofoil)Utils.LoadStructure(f, aerofoil, "end aerofoil", listAerofoil);
                    aerofoil.CL_per_alpha *= param.CLMult;
                    aerofoil.CL_0 *= param.CLMult;
                    aerofoil.CL_max *= param.CLMult;
                    aerofoil.CL_min *= param.CLMult;
                    listAerofoil[aerofoil.name] = aerofoil;
                }
                line = f.ReadLine();
                if (line != null) line = line.Trim();
            }

            aerofoils = new Aerofoil[listAerofoil.Count];
            //wheels = new Wheel[listWheel.Count];
            //engines = new AircraftSSSEngine[listEngines.Count];
            listAerofoil.Values.CopyTo(aerofoils, 0);
            //listWheel.Values.CopyTo(wheels, 0);
            //listEngines.Values.CopyTo(engines, 0);

            foreach (Aerofoil aerofoil in aerofoils) aerofoil.Init();
        }


        public void CreateGeometry(MapObject parent)
        {
            foreach (Aerofoil aerofoil in aerofoils)
            {
                aerofoil.CreateGeometry(parent, this);
            }
        }

        public void DrawDebug(Rect r, GuiRenderer renderer)
        {
            Rect[] rs = new Rect[5];
            rs[0] = new Rect(Utils.TR(r, new Vec2(0.05f, 0.375f)), Utils.TR(r, new Vec2(0.30f, 0.625f)));
            rs[4] = new Rect(Utils.TR(r, new Vec2(0.05f, 0.05f)), Utils.TR(r, new Vec2(0.30f, 0.30f)));
            rs[2] = new Rect(Utils.TR(r, new Vec2(0.05f, 0.70f)), Utils.TR(r, new Vec2(0.30f, 0.95f)));
            rs[3] = new Rect(Utils.TR(r, new Vec2(0.375f, 0.375f)), Utils.TR(r, new Vec2(0.625f, 0.625f)));
            rs[1] = new Rect(Utils.TR(r, new Vec2(0.70f, 0.375f)), Utils.TR(r, new Vec2(0.95f, 0.625f)));

            for (int i = 0; i < aerofoils.Length; i++)
                aerofoils[i].DrawDebug(rs[i], renderer);

            float speed = awesomeAircraft.mainBody.LinearVelocity.Length();
            renderer.AddText("speed:" + speed.ToString(), r.LeftTop);
        }


        public static float totalTime = 0;

        public void TickAndApplyForces(Body body, float TickDelta)
        {
            //mainBody.AddForce(ForceType.LocalAtLocalPos, TickDelta, new Vec3(0, 0, intellect.controlThrust.Value), new Vec3(0, 0, 0));

            //PhysicsWorld.Instance.CreateFixedJoint(body,)

            /*body.LinearVelocity = new Vec3(0, 10, -4);
            body.AngularVelocity = Vec3.Zero;
            body.Rotation = Quat.Identity;*/

            Vec3 speed = body.LinearVelocity;
            Vec3 omega = body.AngularVelocity;

            // Apply Thrust
            //EngineConsole.Instance.Print("speed = " + speed.ToString());
            EngineConsole.Instance.Print("thrustControl = " + awesomeAircraft.controlThrust.Value.ToString());
            float airspeed_scale = 1.0f - speed.Length() / handling.speedMax;
            float thrustPower = awesomeAircraft.controlThrust.Value * handling.accelMax * airspeed_scale;
            body.AddForce(ForceType.LocalAtLocalPos, TickDelta, thrustPower * Vec3.YAxis, new Vec3(0, 5, 0));

            if (Utils.LogOn)
                totalTime += 0.02f;
            //Utils.Write(totalTime.ToString() + "," + thrustPower.ToString() + ",,");
            //Utils.Write(Utils.VectToString(body.Position-Utils.LogPosInitial) + ",," + body.Rotation.ToString() + ",,");
            //Utils.Write(Utils.VectToString(speed) + ",," + Utils.VectToString(omega) + ",,");

            int i = 0;
            foreach (Aerofoil aerofoil in aerofoils)
            {
                Vec3 speedAtAerofoil = speed + Vec3.Cross(omega, body.Rotation * aerofoil.position);

                float density = 5;
                Vec3 liftL, dragL, forceappL, pitch_forceL, pitch_forceappL;
                Mat3 M3Inv = body.Rotation.ToMat3();
                M3Inv.Inverse();
                aerofoil.get_lift_and_drag(
                    this,
                    -M3Inv * speedAtAerofoil,
                    out liftL, out dragL, out forceappL, out pitch_forceL, out pitch_forceappL,
                    density,
                    i,
                    awesomeAircraft.axisAileron.Value,
                    awesomeAircraft.axisElevator.Value,
                    awesomeAircraft.axisRudder.Value
                );
                float seuil = 100;

                if (liftL.Length() > param.CapForce) liftL *= param.CapForce / liftL.Length();
                if (dragL.Length() > param.CapForce) dragL *= param.CapForce / dragL.Length();
                if (pitch_forceL.Length() > param.CapForce) pitch_forceL *= param.CapForce / pitch_forceL.Length();

                body.AddForce(ForceType.LocalAtLocalPos, TickDelta, liftL, forceappL);
                body.AddForce(ForceType.LocalAtLocalPos, TickDelta, dragL, forceappL);
                body.AddForce(ForceType.LocalAtLocalPos, TickDelta, pitch_forceL, pitch_forceappL);


                /*                Utils.Write(
                                    Utils.VectToString(forceappL) + ",," +
                                    Utils.VectToString(liftL) + ",," +
                                    Utils.VectToString(dragL) + ",," +
                                    Utils.VectToString(pitch_forceappL) + ",," +
                                    Utils.VectToString(pitch_forceL) + ",,"
                                );*/
                Utils.Write(liftL.Length().ToString() + ',' + dragL.Length().ToString() + ',' + pitch_forceL.Length().ToString() + ",,");


                i++;
            }

            Utils.WriteLine("");

        }


    }

}