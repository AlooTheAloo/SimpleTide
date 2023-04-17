using Riptide;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddObjectToMessage : MonoBehaviour
{
    static Type[] supportedTypes = new Type[]
    {
        typeof(byte), typeof(sbyte), typeof(bool), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
        typeof(string)
    };

   
    public static Message handle(Message m, object o)
    {

        bool added = false; 

        var supportedDelegates = new Action[]
        {
            () => { m.Add((byte)o); }, () => { m.Add((sbyte)o); }, () => { m.Add((bool)o); },
            () => { m.Add((short)o); }, () => { m.Add((ushort)o); }, () => { m.Add((int)o); },
            () => { m.Add((uint)o); }, () => { m.Add((long)o); }, () => { m.Add((ulong)o); },
            () => { m.Add((float)o); }, () => { m.Add((double)o); }, () => { m.Add((string)o); }
        };

        for(int i = 0; i < supportedTypes.Length; i++)
        {
            if (o.GetType() == supportedTypes[i])
            {
                m.AddUShort((ushort) i);
                supportedDelegates[i].Invoke();
                added = true;
                break;
            }
        }

        if (!added) Debug.LogError("You are trying to synchronize a variable that isnt a primitive. SyncVar only works for primitive types.");

        return m;
    }




    public static retrieveRes retrieve(Message m)
    {
        object ret = null;

        Action[] supportedDelegates = new Action[]
        {
            () => { ret = m.GetByte(); }, 
            () => { ret = m.GetSByte(); }, 
            () => { ret = m.GetBool(); },
            () => { ret = m.GetShort();}, 
            () => { ret = m.GetUShort(); }, 
            () => { ret = m.GetInt(); },
            () => { ret = m.GetUInt();}, 
            () => { ret = m.GetLong(); }, 
            () => { ret = m.GetULong(); },
            () => { ret = m.GetFloat(); }, 
            () => { ret = m.GetDouble(); }, 
            () => { ret = m.GetString(); }
        };

        supportedDelegates[m.GetUShort()].Invoke();
        print("ret is " + ret); // prints 0
        
        
        return new retrieveRes(ret, m.GetString());
    }


}

public struct retrieveRes
{
    public object value { get; private set; }
    public string hash { get; private set; }

    public retrieveRes(object value, string hash)
    {
        this.value = value;
        this.hash = hash;
    }
}