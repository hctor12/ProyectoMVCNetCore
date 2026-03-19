using Microsoft.AspNetCore.SignalR;

namespace ProyectoMVCNetCore.Hubs
{
    public class IncidenciasHub : Hub
    {
        public async Task JoinTicketGroup(int idIncidencia)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, idIncidencia.ToString());
        }

        public async Task LeaveTicketGroup(int idIncidencia)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, idIncidencia.ToString());
        }

        public async Task NotifyTyping(int idIncidencia, string userName)
        {
            await Clients.GroupExcept(idIncidencia.ToString(), Context.ConnectionId).SendAsync("UserTyping", userName);
        }

        public async Task NotifyStoppedTyping(int idIncidencia, string userName)
        {
            await Clients.GroupExcept(idIncidencia.ToString(), Context.ConnectionId).SendAsync("UserStoppedTyping", userName);
        }
    }
}
