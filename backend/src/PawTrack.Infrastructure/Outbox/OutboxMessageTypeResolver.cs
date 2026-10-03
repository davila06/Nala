namespace PawTrack.Infrastructure.Outbox;

public static class OutboxMessageTypeResolver
{
    public static Type? Resolve(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType)) return null;

        var type = Type.GetType(messageType, throwOnError: false);
        if (type is not null) return type;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(messageType, throwOnError: false, ignoreCase: false);
            if (type is not null) return type;
        }

        return null;
    }
}
