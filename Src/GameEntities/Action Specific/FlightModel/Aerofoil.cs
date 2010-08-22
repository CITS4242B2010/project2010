using System;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.Renderer;

namespace GameEntities
{

    class Aerofoil
    {
        public enum FlyingMode { FORWARD = 0, STALLED, REVERSE };
        public string name;
        private Quat originalNodeOrientation;

        public Vec3 position { get { return new Vec3(-position_y, position_x, position_z); } }
        private float[] x_offset { get { float[] res = new float[3]; res[0] = offset_forward; res[1] = offset_stalled; res[2] = offset_reverse; return res; } }
        private float get_area { get { return chord * span; } }

        public float health = 100;
        //Geometry
        public float position_x, position_y, position_z;
        public float offset_forward, offset_stalled, offset_reverse;
        public float chord, span;
        public float rotation;				// rotation around Z
        public float inc;						// angle of incidence (after rotation)
        //CL params
        public float CL_drop_scale;		// when we stall, CL drops by this much
        public float CL_rev_scale;			// scale parms by this for wing flying backwards
        public float CL_per_alpha;			// in units per deg
        public float CL_0;					// +ve for a wing aerofoil
        public float CL_max;					// so we stall at CL_max / CL_per_alpha
        public float CL_min;					// similarly
        // CD params
        public float CD_prof, CD_induced_frac, CD_max;
        // pitching moment
        public float CM_0;					// pitching moment for control = 0
        // control surface params
        public float CL_per_deg;			// change in CL (of whole graph) 
        public float CD_prof_per_deg;		// change in CD_prof
        public float CM_per_deg;			// pitching moment per control
        public float inc_per_deg;			// effective rotation of aerofoil
        public float control_per_chan_1;	// control per joystick channel
        public float control_per_chan_2;	// control per joystick channel
        public float control_per_chan_3;	// control per joystick channel
        public float control_per_chan_4;	// control per joystick channel
        public float control_per_chan_5;	// control per joystick channel
        public float control_per_chan_6;	// control per joystick channel
        public float control_per_chan_7;	// control per joystick channel
        public float control_per_chan_8;	// control per joystick channel


        //Constantes calculees a partir du reste dans Init()
        public float CL_0_r, CL_max_r, CL_min_r;
        public float CL_max_d, CL_min_d, CL_max_r_d, CL_min_r_d;
        //  public float alpha_CL_0;
        public float alpha_CL_max;
        public float alpha_CL_max_d;
        public float alpha_CL_min;
        public float alpha_CL_min_d;
        //  public float alpha_CL_0_r;
        public float alpha_CL_max_r;
        public float alpha_CL_max_r_d;
        public float alpha_CL_min_r;
        public float alpha_CL_min_r_d;

        public const int NUM_POINTS = 14;
        public float[] x = new float[NUM_POINTS];
        public float[] y = new float[NUM_POINTS];
        public FlyingMode[] flight = new FlyingMode[NUM_POINTS];


        // Debug variables to draw on the gui
        float debugControl;
        Vec3 debugWind;
        float debugLinearPosition, debugPitchPosition;
        Vec3 debugLift, debugDrag, debugPitchForce;


        public void Init()
        {
            CL_0_r = -CL_0 * CL_rev_scale;
            CL_max_r = -CL_min * CL_rev_scale;
            CL_min_r = -CL_max * CL_rev_scale;

            CL_max_d = CL_max * CL_drop_scale; // what CL drops down to
            CL_min_d = CL_min * CL_drop_scale;
            CL_max_r_d = CL_max_r * CL_drop_scale;
            CL_min_r_d = CL_min_r * CL_drop_scale;

            //   alpha_CL_0     = 0; // alpha at CL_0
            alpha_CL_max = (CL_max - CL_0) / CL_per_alpha;
            alpha_CL_max_d = alpha_CL_max + 10;
            alpha_CL_min = (CL_min - CL_0) / CL_per_alpha;
            alpha_CL_min_d = alpha_CL_min - 10;
            //   alpha_CL_0_r   = 180; // must be +ve
            alpha_CL_max_r = -180 + (CL_max_r - CL_0_r) / CL_per_alpha;
            alpha_CL_max_r_d = alpha_CL_max_r + 10;
            alpha_CL_min_r = 180 + (CL_min_r - CL_0_r) / CL_per_alpha;
            alpha_CL_min_r_d = alpha_CL_min_d - 10;

            x[0] = alpha_CL_min_r - 360;
            x[1] = -180;
            x[2] = alpha_CL_max_r;
            x[3] = alpha_CL_max_r_d;
            x[4] = -90;
            x[5] = alpha_CL_min_d;
            x[6] = alpha_CL_min;
            x[7] = 0;
            x[8] = alpha_CL_max;
            x[9] = alpha_CL_max_d;
            x[10] = 90;
            //		x[11]=alpha_CL_min_r_d;
            x[11] = 180 + alpha_CL_min_r_d;
            x[12] = alpha_CL_min_r;
            x[13] = 180;

            y[0] = CL_min_r;
            y[1] = CL_0_r;
            y[2] = CL_max_r;
            y[3] = CL_max_r_d;
            y[4] = 0;
            y[5] = CL_min_d;
            y[6] = CL_min;
            y[7] = CL_0;
            y[8] = CL_max;
            y[9] = CL_max_d;
            y[10] = 0;
            y[11] = CL_min_r_d;
            y[12] = CL_min_r;
            y[13] = CL_0_r;

            flight[0] = FlyingMode.STALLED;
            flight[1] = FlyingMode.REVERSE;
            flight[2] = FlyingMode.REVERSE;
            flight[3] = FlyingMode.STALLED;
            flight[4] = FlyingMode.STALLED;
            flight[5] = FlyingMode.FORWARD;
            flight[6] = FlyingMode.FORWARD;
            flight[7] = FlyingMode.FORWARD;
            flight[8] = FlyingMode.FORWARD;
            flight[9] = FlyingMode.STALLED;
            flight[10] = FlyingMode.STALLED;
            flight[11] = FlyingMode.REVERSE;
            flight[12] = FlyingMode.REVERSE;
            flight[13] = FlyingMode.REVERSE;
        }


        // This function easily wins when run under a profiler - gprof puts it
        // at around 17%. Almost all of this is this function - not any
        // (profiled) function it calls.
        public void get_lift_and_drag(FlightModel flightModel,
                                        Vec3 wind_rel_in, // in
                                        out Vec3 linear_lift, // lift force
                                        out Vec3 linear_drag, // drag force
                                        out Vec3 linear_position, // loc of force  
                                        out Vec3 pitch_force,
                                        out Vec3 pitch_position,
                                        float density,
                                        int num,
                                        float aileron, float elevator, float rudder)
        {
            float CL, CD, lift_force, drag_force;

            SwitchVec(ref wind_rel_in);

            debugWind = wind_rel_in;

            if (Utils.LogOn)
            {
                int abc = 123;
                abc++;
            }

            Quat Qalpha_beta = Mat3.FromRotateByZ(MathFunctions.DegToRad(rotation)).ToQuat() *
                               Mat3.FromRotateByX(MathFunctions.DegToRad(-inc)).ToQuat();
            Mat3 alpha_beta = Qalpha_beta.ToMat3();
            Mat3 alpha_beta_inv;
            Quat temp = Qalpha_beta;
            temp.Inverse();
            temp.ToMat3(out alpha_beta_inv);

            Vec3 wind_rel = alpha_beta * wind_rel_in;

            float alph = MathFunctions.RadToDeg((float)Math.Atan2(wind_rel.Y, -wind_rel.Z)); // angle of inclination
            float speed2 = wind_rel.Y * wind_rel.Y + wind_rel.Z * wind_rel.Z;
            float speed = MathFunctions.Sqrt(speed2);

            if (alph > 180)
                alph -= 360;
            else if (alph < -180)
                alph += 360;

            FlyingMode flying;
            float flying_float;
            // calculate and store the control input (might be used in the
            // display)
            float control_input =
                control_per_chan_1 * aileron +
                control_per_chan_2 * elevator +
                control_per_chan_3 * rudder;
            debugControl = control_input;

            CL = get_CL(alph, control_input, out flying, out flying_float);
            CD = get_CD(alph, control_input, flying, CL);

            float force_scale = get_area * 0.5f * speed2;
            lift_force = force_scale * CL; // main variation in density is air/water
            drag_force = density * force_scale * CD;

            float flying_offset;
            if (flying_float > 0)
            {
                flying_offset = x_offset[(int)FlyingMode.STALLED] +
                                     flying_float * (x_offset[(int)FlyingMode.FORWARD] -
                                     x_offset[(int)FlyingMode.STALLED]);
            }
            else
            {
                flying_offset = x_offset[(int)FlyingMode.STALLED] +
                                     -flying_float * (x_offset[(int)FlyingMode.REVERSE] -
                                     x_offset[(int)FlyingMode.STALLED]);
            }

            // we need cos(alph) etc. Rather than using trig, we can calculate the
            // value directly...
            //  const float cos_deg_alph = cos_deg(alph);
            //  const float sin_deg_alph = sin_deg(alph);
            bool doit = (Math.Abs(speed) > 0.01);
            float cos_deg_alph = doit ? -wind_rel.Z / speed : 0.0f;
            float sin_deg_alph = doit ? wind_rel.Y / speed : 0.0f; ;

            float force_up = lift_force * cos_deg_alph + drag_force * sin_deg_alph;
            float force_forward = lift_force * sin_deg_alph - drag_force * cos_deg_alph;

            linear_lift = new Vec3(0, lift_force, 0);
            linear_drag = new Vec3(0, 0, -drag_force);

            linear_position = position + new Vec3(0, flying_offset, 0);
            debugLinearPosition = flying_offset;

            // calculate the pitching moment - represented by a single 
            // force at the trailing edge
            float pitching_force = get_area * 0.5f * wind_rel.Z * Math.Abs(wind_rel.Z) *
                                          (CM_0 + CM_per_deg * control_input);
            pitch_force = new Vec3(0.0f, pitching_force, 0.0f);
            pitch_position = position;
            pitch_position.Y -= 0.5f * chord;
            debugPitchPosition = pitch_position.Y;

            // need to rotate the force back...
            linear_lift = alpha_beta_inv * linear_lift;
            linear_drag = alpha_beta_inv * linear_drag;
            pitch_force = alpha_beta_inv * pitch_force;

            linear_lift /= 500;
            linear_drag /= 500;
            pitch_force /= 500;
            linear_position /= 4;
            pitch_position /= 4;

            linear_lift *= flightModel.param.Mult_Lift;
            linear_drag *= flightModel.param.Mult_Drag;

            SwitchVec(ref linear_lift);
            SwitchVec(ref linear_drag);
            SwitchVec(ref pitch_force);

            debugLift = linear_lift;
            debugDrag = linear_drag;
            debugPitchForce = pitch_force;
        }

        void SwitchVec(ref Vec3 v)
        {
            float tmp = v.Z;
            v.Z = v.Y;
            v.Y = tmp;
            v.X = -v.X;
        }

        private float get_CL(float alpha, float control_input, out FlyingMode flying, out float flying_float)
        {
            float CL_offset = control_input * CL_per_deg;
            float inc_offset = control_input * inc_per_deg;

            // defaults...
            flying = FlyingMode.FORWARD;
            flying_float = 0.0f;

            alpha += inc_offset;

            // make sure input is in correct range
            if (alpha > 180)
                alpha -= 360;
            else if (alpha < -180)
                alpha += 360;

            if (alpha < x[1])
            {
                //Debug.Assert(false, "alpha out of range");
                alpha = 0.0f;
                //    return 0;
                //    abort();
            }

            float CL = -999;

            for (int i = 2; i < NUM_POINTS; ++i)
            {
                if (alpha <= x[i])
                {
                    CL = CL_offset + interp(x[i - 1], y[i - 1], x[i], y[i], alpha);
                    flying_float = 1.0f - interp(x[i - 1], (int)flight[i - 1], x[i], (int)flight[i], alpha);
                    flying = flight[i - 1];
                    break;
                }
            }

            //  flying_float = 1-flying_float;

            if (CL != -999)
            {
                return CL;
            }
            else
            {
                //			Debug.Assert( false, "alpha out of range" );
                return 0;
                //      abort();
            }
        }

        // alpha is angle of attack, including inc
        private float get_CD(float alpha, float control_input, FlyingMode flying, float CL)
        {
            float sin_deg_alpha = MathFunctions.Sin(MathFunctions.DegToRad(alpha));
            float CD_stall = ((flying == FlyingMode.STALLED) ? 1 : 0) * (CD_max * sin_deg_alpha * sin_deg_alpha);
            float CD_induced = CL * CL * CD_induced_frac; // 0.05f from crrcsim?
            return CD_prof + CD_prof_per_deg * (float)Math.Abs(control_input) + CD_induced + CD_stall;
        }


        private float interp(float x0, float y0, float x1, float y1, float x)
        {
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }

        public void CreateGeometry(MapObject parent, FlightModel flightModel)
        {
            MapObjectAttachedMesh obj = new MapObjectAttachedMesh();
            obj.MeshName = "Models\\DefaultBox\\DefaultBox.mesh";
            float mScale = .2f;
            obj.ScaleOffset = new Vec3(span * mScale, chord * mScale, .1f * mScale) * flightModel.scale;

            obj.RotationOffset =
                Mat3.FromRotateByY(MathFunctions.DegToRad(-rotation)).ToQuat() *
                Mat3.FromRotateByX(MathFunctions.DegToRad(+inc)).ToQuat();
            obj.PositionOffset = (position + Vec3.YAxis * flightModel.param.geometry_offset_for_wheels) * flightModel.scale;

            parent.Attach(obj);
        }


        void DrawVector(GuiRenderer g, Vec2 start, Vec2 end, ColorValue color)
        {
            Vec2 vAngle = end - start;
            vAngle.Normalize();
            float angle = MathFunctions.ATan(vAngle.Y, vAngle.X);

            //g.AddLine(start, end, color);

            float tipLength = 0.01f;
            Vec2 endArrow1 = end + new Vec2(MathFunctions.Cos(angle + MathFunctions.DegToRad(180 - 20)) * tipLength, MathFunctions.Sin(angle + MathFunctions.DegToRad(180 - 20)) * tipLength);
            Vec2 endArrow2 = end + new Vec2(MathFunctions.Cos(angle + MathFunctions.DegToRad(180 + 20)) * tipLength, MathFunctions.Sin(angle + MathFunctions.DegToRad(180 + 20)) * tipLength);

            g.AddLine(start, end, color);
            g.AddLine(end, endArrow1, color);
            g.AddLine(end, endArrow2, color);
        }

        public void DrawDebug(Rect r, GuiRenderer g)
        {
            float windScale = 0.1f;
            float forceScale = 0.2f;
            float positionScale = 5.0f;

            // Box around
            g.AddRectangle(r, new ColorValue(0.416f, 0.133f, 0.494f));

            // Aerofoil
            float controlAngle = MathFunctions.DegToRad(debugControl);
            g.AddLine(
                Utils.TR(r, new Vec2(0.5f - MathFunctions.Cos(controlAngle) * 0.25f, 0.5f - MathFunctions.Sin(controlAngle) * 0.25f)),
                Utils.TR(r, new Vec2(0.5f + MathFunctions.Cos(controlAngle) * 0.25f, 0.5f + MathFunctions.Sin(controlAngle) * 0.25f)),
                new ColorValue(0.416f, 0.553f, 0.576f)
            );

            // Wind
            Vec2 windStart = Utils.TR(r, new Vec2(0.5f - debugWind.Y * windScale, 0.5f - debugWind.Z * windScale));
            Vec2 windEnd = Utils.TR(r, new Vec2(0.5f, 0.5f));
            DrawVector(g, windStart, windEnd, new ColorValue(0.196f, 0.384f, 0.337f));

            // Lift
            Vec2 liftStart = Utils.TR(r, new Vec2(0.5f + debugLinearPosition * positionScale, 0.5f));
            Vec2 liftEnd = liftStart + new Vec2(0, -debugLift.Z * forceScale);
            DrawVector(g, liftStart, liftEnd, new ColorValue(0.333f, 0.973f, 0.969f));

            // Drag
            Vec2 dragStart = Utils.TR(r, new Vec2(0.5f + debugLinearPosition * positionScale, 0.5f + 0.01f));
            Vec2 dragEnd = dragStart + new Vec2(-debugDrag.Y * forceScale, 0);
            DrawVector(g, dragStart, dragEnd, new ColorValue(0.486f, 0.184f, 0.184f));

            // Pitch
            Vec2 pitchStart = Utils.TR(r, new Vec2(0.5f, 0.5f - debugPitchPosition * positionScale));
            Vec2 pitchEnd = pitchStart + new Vec2(0, -debugPitchForce.Z * forceScale);
            DrawVector(g, pitchStart, pitchEnd, new ColorValue(0.388f, 0.769f, 0.373f));

        }


    }

}