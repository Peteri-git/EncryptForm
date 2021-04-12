using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EncryptForm.Server.Data;
using EncryptForm.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EncryptForm.Server.Hubs
{
    public class ChatHub : Hub
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private ApplicationDbContext dbContext;
        public ChatHub(ApplicationDbContext dbContext, SignInManager<ApplicationUser> _signInManager)
        {
            this.dbContext = dbContext;
            this._signInManager = _signInManager;
        }
        public async Task AddRoom(string name,byte[] key,byte[] IV, string choice)
        {
            await Clients.Caller.SendAsync("AddRoom", name, key, IV, choice);
        }
        public async Task JoinRoom(string name)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, name);
        }
        public async Task InviteToRoom(string username, string name, byte[] key, byte[] IV, string choice)
        {
            await Clients.All.SendAsync("AcceptInvite", username, name, key, IV, choice);
        }
        public async Task SendMessage(byte[] message, string roomName)
        {
            await Clients.Group(roomName).SendAsync("ReceiveMessage", message);
        }
        public string GetPublicKey(string username)
        {
            foreach (var user in dbContext.Users)
            {
                if (user.Email== username)
                {
                    return user.PublicKey;
                }
            }
            return "nothing found";
        }
        public async Task Login(string username, string password, string publicKey)
        {
            foreach (var user in dbContext.Users)
            {
                if (user.Email == username)
                {
                    var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
                    if (result.Succeeded)
                    {
                        foreach (var User in dbContext.Users)
                        {
                            if (User.Email == username)
                            {
                                User.PublicKey = publicKey;
                                //await dbContext.SaveChangesAsync();
                            }
                        }
                        await Clients.Caller.SendAsync("Login", "Success", username);
                        break;
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("Login", "Failed", username);
                        break;
                    }
                }
            }
            dbContext.SaveChanges();
        }
        public async override Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("JoinRoom");
        }
    }
}
