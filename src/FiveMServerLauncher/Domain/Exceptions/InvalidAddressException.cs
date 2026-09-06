namespace FiveMServerLauncher.Domain.Exceptions;

public class InvalidAddressException(string address) : Exception($"The server address '{address}' is invalid."){}
