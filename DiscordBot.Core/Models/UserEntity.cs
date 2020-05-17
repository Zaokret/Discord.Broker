using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Entities
{
  public class UserEntity
  {
    public UserEntity() { }
    public UserEntity(ulong userId, float funds)
    {
      UserId = userId;
      Funds = funds;
    }
    public ulong UserId { get; set; }
    public float Funds { get; set; }
  }
}
