using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace Avalonia.Controls.PanAndZoom;

public struct Interval : IComparable<Interval>
{
    public Interval(double _from, double _to)
    {
        From = _from;
        To = _to;
    }
    public readonly double From;
    public readonly double To;
    public override bool Equals(object obj)
    {
        if (obj is Interval)
        {
            var other = (Interval)obj;
            return From == other.From && To == other.To;
        }
        return false;
    }
    public double Length => To - From;
    public override int GetHashCode()
        => From.GetHashCode() ^ To.GetHashCode();
    public override string ToString() => $"[{From:f2};{To:f2}]";

    public int CompareTo(Interval other) => From.CompareTo(other.From);
    public bool In(Interval _out)
        => _out.From <= From && To <= _out.To;
    public Interval? Intersect(Interval other)
        => other.To < From || To < other.From ? (Interval?) null
        : new Interval(Math.Max(From, other.From), Math.Min(To, other.To));
}

public static class IntervalHelper
{
    public static bool In(this double pt, Interval bounds)
        => bounds.From <= pt && pt <= bounds.To;
    public static Interval? Intersect(this Interval i1, Interval i2)
    {
        double p1 = Math.Max(i1.From, i2.From);
        double p2 = Math.Min(i1.To, i2.To);
        if (p1 > p2)
            return null;
        return new Interval(p1, p2);
    }
    public static Interval? SubTract(this Interval i1, Interval i2)
        => i1.From < i2.From? new Interval(i1.From, Math.Min(i1.To, i2.From))
        : i1.To > i2.To? new Interval(Math.Max(i1.From, i2.To), i1.To)
        : null;
        
    // Index of interval to insert
    public static int IndexFor(this List<Interval> intervals, Interval _target, out Interval? at)
    {
        int mid, first = 0, last = intervals.Count - 1;

        if (last == -1)
        {
            at = null;
            return 0;
        }

        //for a sorted array with descending values
        while (first <= last)
        {
            mid = (first + last) / 2;
            if (_target.To < intervals[mid].From)
            {
                if (mid == 0)
                {
                    at = null;
                    return 0;
                }
                else if (intervals[mid - 1].To < _target.From)
                {
                    at = null;
                    return mid;
                }
                else last = mid - 1;
            }
            if (_target.From > intervals[mid].To)
            {
                if (mid == last)
                {
                    at = null;
                    return mid + 1;
                }
                else if (_target.To < intervals[mid + 1].From)
                {
                    at = null;
                    return mid + 1;
                }
                first = mid + 1;
            }
            else /* interval intersection */
            {
                at = intervals[mid];
                if (_target.In(intervals[mid]))
                {
                    return -1;
                }
                while (mid > 0 && intervals[mid].In(_target)) mid--;
                return mid;
            }
        }
        throw new InvalidProgramException();
    }

    public static Interval XProj(this Rect rect)
        => new Interval(rect.Left, rect.Right);

    public static Interval YProj(this Rect rect)
        => new Interval(rect.Top, rect.Bottom);

    static void InsertInterval(this List<Interval> intervals, Interval _target, int i)
    {
        int j = i;
        if (intervals.Count == 0)
        {
            intervals.Add(_target);
            return;
        }
        double from;
        if (intervals.Count > i)
        {
            from = Math.Min(intervals[i].From, _target.From);
        }
        else from = _target.From;
        double to = _target.To;
        if (j < intervals.Count)
        {
            while (j < intervals.Count && intervals[j].From <= _target.To)
            {
                to = Math.Max(intervals[j].To, to);
                j++;
            }
            for (int k = j-1; k >= i; k--)
            {
                intervals.RemoveAt(k);
            }
        }
        intervals.Insert(i, new Interval(from, to));
    }

    public static void Insert(this List<Interval> intervals, Interval _target)
    {
        int i = intervals.IndexFor(_target, out Interval? at);
        if (i >= 0)
            intervals.InsertInterval(_target, i);
    }

    public static double? NearestFree(this List<Interval> intervals, Interval bounds, double at, double delta)
    {
        var idx = intervals.IndexFor(new Interval(at, at + delta), out Interval? found);
        if (found.HasValue)
        {
            if (found.Value.To - at < at - found.Value.From)
            {
                double up = found.Value.To + delta;
                if (up.In(bounds))
                    return up;
            }
            double down = found.Value.From - delta;
            if (down.In(bounds))
                return down;
            return null;
        }
        return at;
    }
}

