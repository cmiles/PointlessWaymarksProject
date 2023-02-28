namespace PointlessWaymarks.CommonTools;

public static class DynamicTypeTools
{
    public static bool PropertyExists(dynamic obj, string name)
    {
        //From https://stackoverflow.com/questions/9956648/how-do-i-check-if-a-property-exists-on-a-dynamic-anonymous-type-in-c
        if (obj == null) return false;
        if (obj is IDictionary<string, object> dict) return dict.ContainsKey(name);
        return obj.GetType().GetProperty(name) != null;
    }
}