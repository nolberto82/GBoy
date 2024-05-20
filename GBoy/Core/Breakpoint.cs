
using System.Net;

namespace GBoy.Core;
public class Breakpoint
{
    public int Addr { get; private set; }
    public int Condition { get; private set; }
    public int Type { get; set; }
    public bool Enabled { get; set; }

    public List<Breakpoint> BPs { get; private set; } = new();

    public Breakpoint() { }
    public Breakpoint(int addr, int condition, int type, bool enabled)
    {
        Addr = (ushort)addr;
        Condition = condition;
        Type = type;
        Enabled = enabled;
    }

    public void InsertRemove(dynamic a, int type)
    {
        ushort addr = 0;
        if (a is string && a != "")
            addr = Convert.ToUInt16(a, 16);
        else
            addr = (ushort)a;

        bool v = BPs.Any(b => b.Addr == addr);
        if (v)
        {
            int index = BPs.FindIndex(b => b.Addr == addr);
            if (index > -1)
                BPs.RemoveAt(index);
        }
        else
            BPs.Add(new Breakpoint(addr, -1, type, true));


    }

    public bool Execution(int pcaddr)
    {
        foreach (var bp in BPs)
        {
            if (bp.Enabled && bp.Type == BPType.Exec)
            {
                if (bp.Addr == pcaddr)
                    return true;
            }
        }
        return false;
    }

    public bool Access(int accessaddr, int memval)
    {
        if (accessaddr == Addr && Condition == memval)
            return true;
        else if (Condition == -1)
            return accessaddr == Addr;
        return false;
    }

    public bool ProcessWatchpoints(int a, byte v, int type)
    {
        foreach (var bp in BPs)
        {
            if ((bp.Type & BPType.Read) == type)
            {
                if (bp.Enabled && a == bp.Addr && bp.Access(a, v))
                    return true;
            }
            if ((bp.Type & BPType.Write) == type)
            {
                if (bp.Enabled && a == bp.Addr && bp.Access(a, v))
                    return true;
            }
        }
        return false;
    }
}

public static class BPType
{
    public const int Read = 1;
    public const int Write = 2;
    public const int Exec = 4;
};
