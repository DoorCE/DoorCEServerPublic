namespace DoorCEServer.Common.Exceptions;

public class SystemHaltException(string message) : Exception(message);