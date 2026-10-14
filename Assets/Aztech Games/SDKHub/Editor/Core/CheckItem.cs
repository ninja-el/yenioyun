namespace SDKHub.Editor
{
    public enum CheckLevel { Ok, Warning, Error }

    public readonly struct CheckItem
    {
        public readonly CheckLevel Level;
        public readonly string Message;

        public CheckItem(CheckLevel level, string message)
        {
            Level = level;
            Message = message;
        }
    }
}
