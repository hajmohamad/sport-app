using System.Collections.Concurrent;

namespace sport_app_backend.Services;

public class ActiveConversationService
{
    
    private readonly ConcurrentDictionary<int, ConcurrentHashSet<long>> _activeConversations = new();

    public void UserOpenedConversation(int userId, long conversationId)
    {
        _activeConversations
            .GetOrAdd(userId, _ => new ConcurrentHashSet<long>())
            .Add(conversationId);
    }

    public void UserClosedConversation(int userId, long conversationId)
    {
        if (_activeConversations.TryGetValue(userId, out var set))
            set.Remove(conversationId);
    }
    public void UserDisconnected(int userId)
    {
        _activeConversations.TryRemove(userId, out _);
    }

    public bool IsUserInConversation(int userId, long conversationId)
    {
        return _activeConversations.TryGetValue(userId, out var set)
               && set.Contains(conversationId);
    }
}
public class ConcurrentHashSet<T>
{
    private readonly ConcurrentDictionary<T, byte> _dict = new();
    public void Add(T item) => _dict.TryAdd(item, 0);
    public void Remove(T item) => _dict.TryRemove(item, out _);
    public bool Contains(T item) => _dict.ContainsKey(item);
}