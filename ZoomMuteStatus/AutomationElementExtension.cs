using System.Windows.Automation;

namespace ZoomMuteStatus
{
    public static class AutomationElementExtension
    {
        public static string ToHumanReadable(this AutomationElement el)
        {
            return $"Name: {el.Current.Name}, ClassName: {el.Current.ClassName}, control: {el.Current.ControlType.ProgrammaticName}";
        }
    }
}