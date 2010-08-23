using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using Engine.MathEx;


public class Utils
{
    private static TextWriter LogFile;
    public static bool LogOn = false;
    public static Vec3 LogPosInitial = Vec3.Zero;
    public static void StartLog(String filename)
    {
        if (LogFile == null)
            LogFile = new StreamWriter(filename);
        LogOn = true;
    }
    public static void StopLog()
    {
        if (LogFile != null)
            LogFile.Close();
        LogOn = false;
    }
    public static void Write(String msg)
    {
        if (LogOn)
            LogFile.Write(msg);
    }
    public static void WriteLine(String msg)
    {
        if (LogOn)
            LogFile.WriteLine(msg);
    }
    public static String VectToString(Vec3 v)
    {
        return v.X.ToString() + "," + v.Y.ToString() + "," + v.Z.ToString();
    }

    static public Vec2 TR(Rect r, Vec2 pt)
    {
        Vec2 res = new Vec2(
            r.Left + (r.Right - r.Left) * pt.X,
            r.Top + (r.Bottom - r.Top) * pt.Y
        );
        return res;
    }


    static public string[] getCommandFromStr(string str)
    {
        char[] sep = { ' ', '\t' };
        string[] cmd = str.Split(sep);
        for (int i = 0; i < cmd.GetLength(0); i++) cmd[i] = cmd[i].Trim();
        return cmd;
    }


    static public string padstring(string s, int n)
    {
        if (s.Length < n)
        {
            for (int i = s.Length; i < n; i++) s += ' ';
            return s;
        }
        if (s.Length > n) return s.Substring(0, n);
        return s;
    }

    static private bool ReadEltFromLine_IFP(string line, out string[] elt)
    {
        elt = new string[2];
        elt[0] = elt[1] = "";
        int curelt = 0;
        bool prevspace = true;
        for (int i = 0; i < line.Length; i++)
        {
            if ((line[i] == ' ') || (line[i] == '\t'))
            {
                if (!prevspace)
                {
                    prevspace = true;
                    curelt++;
                }
            }
            else
            {
                prevspace = false;
                if (curelt < 2) elt[curelt] += line[i];
            }
        }
        for (int i = 0; i < 2; i++) elt[i] = elt[i].Trim();

        return curelt >= 1;
    }

    static private void CopyObject(object from, object to)
    {
        FieldInfo[] fields = from.GetType().GetFields();
        foreach (FieldInfo info in fields)
        {
            if (!info.IsLiteral)
                info.SetValue(to, info.GetValue(from));
        }
    }

    static public object LoadStructure(TextReader reader, object obj, string until, Hashtable listprev)
    {
        object result = obj;

        until = until.Trim();
        string line = reader.ReadLine();
        if (line != null) line = line.Trim();
        while ((line != null) && (line != until))
        {
            if (line == null) break;
            if (line == "") goto cont;
            if (line[0] == '#') goto cont;
            string[] elt;
            if (!ReadEltFromLine_IFP(line, out elt)) goto cont;
            if (elt[0] == "copy")
            {
                CopyObject(listprev[elt[1]], result);
                goto cont;
            }

            FieldInfo finfo = result.GetType().GetField(elt[0]);
            //			if (finfo!=null) {
            if (finfo.FieldType == typeof(float)) finfo.SetValue(result, float.Parse(elt[1]));
            if (finfo.FieldType == typeof(string)) finfo.SetValue(result, elt[1]);
            if (finfo.FieldType == typeof(bool)) finfo.SetValue(result, bool.Parse(elt[1]));
        //			}
        cont:
            line = reader.ReadLine();
            if (line != null) line = line.Trim();
        }
        return result;
    }


    /*    static public Quaternion CreateQuaternionFromZ(Vector3 vZ)
        {
            float seuil = .001f;
            if (vZ.Length < seuil) return Quaternion.Identity;
            vZ.Normalize();
            Vector3 vY = Vector3.UnitY;
            Vector3 vX = vY.Cross(vZ);
            if (vX.Length < seuil)
            {
                vY = Vector3.UnitX;
                vX = vY.Cross(vZ);
                Debug.Assert(vX.Length > seuil);
            }

            vX.Normalize();
            vY = vZ.Cross(vX);
            Quaternion res = Quaternion.Identity;
            res.FromAxes(vX, vY, vZ);
            return res;
        }*/


}
