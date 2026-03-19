using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace ProyectoMVCNetCore.Hubs
{
    public class PresenceHub : Hub
    {
        // Almacena la cantidad de conexiones por cada usuario (por id)
        private static readonly ConcurrentDictionary<int, int> OnlineUsers = new();

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    bool isNew = false;
                    OnlineUsers.AddOrUpdate(userId, 
                        // add value factory
                        id => {
                            isNew = true;
                            return 1;
                        }, 
                        // update value factory
                        (id, count) => count + 1);

                    if (isNew)
                    {
                        await Clients.All.SendAsync("UserConnected", userId);
                    }
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    bool removed = false;
                    OnlineUsers.AddOrUpdate(userId,
                        // add value shouldn't be called, but Provide 0
                        0,
                        (id, count) =>
                        {
                            if (count <= 1)
                            {
                                removed = true;
                                return 0;
                            }
                            return count - 1;
                        });

                    if (removed)
                    {
                        OnlineUsers.TryRemove(userId, out _);
                        await Clients.All.SendAsync("UserDisconnected", userId);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public Task<IEnumerable<int>> GetOnlineUsers()
        {
            return Task.FromResult<IEnumerable<int>>(OnlineUsers.Keys);
        }
    }
}
