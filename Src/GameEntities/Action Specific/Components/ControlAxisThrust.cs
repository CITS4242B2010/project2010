using Engine;
using GameCommon;

namespace GameEntities
{

    public struct ControlThrust
    {
        private GameControlKeys keyDecel, keyAccel;
        private float increment, decrement, scale;
        float val;

        public float Value { get { return val * scale; } set { val = value / scale; } }

        public void Init(GameControlKeys keyDecel, GameControlKeys keyAccel, float increment, float decrement, float scale)
        {
            this.keyDecel = keyDecel;

            this.keyAccel = keyAccel;
            this.increment = increment;
            this.decrement = decrement;
            this.scale = scale;
            val = 0;
        }

        public void Tick(Intellect intellect)
        {
            if (intellect.IsControlKeyPressed(keyDecel))
                val -= increment;
            if (intellect.IsControlKeyPressed(keyAccel))
                val += increment;
            if (val < 0) val = 0;
            if (val > 1) val = 1;
        }
    };

    public class ControlAxis
    {
        private GameControlKeys keyMinus, keyPlus;
        private float increment, decrement, damping, scale, val;

        // Internally val goes from -1 to +1. But for outside it goes from -scale to +scale
        public float Value { get { return val * scale; } set { val = value / scale; } }

        public void Init(GameControlKeys keyMinus, GameControlKeys keyPlus, float increment, float decrement, float damping, float scale)
        {
            this.keyMinus = keyMinus;
            this.keyPlus = keyPlus;
            this.increment = increment;
            this.decrement = decrement;
            this.damping = damping;
            this.scale = scale;
            val = 0;
        }

        public void Tick(Intellect intellect)
        {
            bool damp = true;
            if (intellect.IsControlKeyPressed(keyMinus))
            {
                damp = false;
                if (val <= 0) val -= increment;
                if (val > 0) val -= decrement;
            }
            if (intellect.IsControlKeyPressed(keyPlus))
            {
                damp = false;
                if (val >= 0) val += increment;
                if (val < 0) val += decrement;
            }

            if (damp)
            {
                if (val < 0)
                {
                    val += damping;
                    if (val > 0) val = 0;
                }
                if (val > 0)
                {
                    val -= damping;
                    if (val < 0) val = 0;
                }
            }

            if (val < -1) val = -1;
            if (val > 1) val = 1;
        }

    }

}