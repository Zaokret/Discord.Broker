using DiscordBot.Contracts;
using DiscordBot.Entities;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Broker
{
  public class CoinService
  {
    public readonly IUserRepository _repository;
    public CoinService(IUserRepository repository)
    {
      _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    public async Task<float> AddCoin(ulong userId, Coin coin)
    {
      UserEntity entity = await _repository.GetUserByIdAsync(userId);
      if(entity != null)
      {
        Wallet wallet = new Wallet(entity.Funds);
        wallet.Deposit(coin.Value);
        await _repository.UpdateFundsAsync(userId, wallet.Funds);
        await _repository.SaveAsync();
        return wallet.Funds;
      }
      else
      {
        User user = new User(userId, new Wallet(coin.Value));
        await _repository.AddUserAsync(user);
        await _repository.SaveAsync();
        return user.Wallet.Funds;
      }
    }

    public async Task<float> RemoveCoin(ulong userId, Coin coin)
    {
      UserEntity entity = await _repository.GetUserByIdAsync(userId);
      if (entity != null)
      {
        Wallet wallet = new Wallet(entity.Funds);
        wallet.Widthdraw(coin.Value);
        await _repository.UpdateFundsAsync(userId, wallet.Funds);
        await _repository.SaveAsync();
        return wallet.Funds;
      }
      return 0;
    }
  }
}
