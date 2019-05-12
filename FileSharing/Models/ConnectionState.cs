namespace FileSharing.Models
{
    public static class ConnectionState
    {
        public const uint StateSending = 1;
        public const uint StateRecieving = 2;
        public const uint StateRecieve = 0;
        public const uint StateDeny = 9;
    }
}
