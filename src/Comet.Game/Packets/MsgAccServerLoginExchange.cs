using System;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Comet.Game.Internal.Auth;
using Comet.Network.Packets.Internal;
using Comet.Shared;
using Comet.Shared.Models;

namespace Comet.Game.Packets
{
    public sealed class MsgAccServerLoginExchange : MsgAccServerLoginExchange<AccountServer>
    {
        public override async Task ProcessAsync(AccountServer client)
        {
            try
            {
                // Generate the access token
                var bytes = new byte[8];
                var rng = RandomNumberGenerator.Create();
                rng.GetBytes(bytes);
                var token = BitConverter.ToUInt64(bytes);

                var args = new TransferAuthArgs
                {
                    AccountID = AccountID,
                    AuthorityID = AuthorityID,
                    AuthorityName = AuthorityName,
                    IPAddress = IPAddress
                };
                // Store in the login cache with an absolute timeout
                var timeoutPolicy = new CacheItemPolicy {AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(60)};
                Kernel.Logins.Set(token.ToString(), args, timeoutPolicy);

#if DEBUG
                await Log.WriteLogAsync(LogLevel.Info,
                                        $"Received Login Information for {AccountID}. Expiration: {timeoutPolicy.AbsoluteExpiration.ToLocalTime()}");
#endif

                await client.SendAsync(new MsgAccServerLoginExchangeEx
                {
                    AccountIdentity = AccountID,
                    Result = MsgAccServerLoginExchangeEx<AccountServer>.ExchangeResult.Success,
                    Token = token
                });
            }
            catch
            {
                await client.SendAsync(new MsgAccServerLoginExchangeEx
                {
                    AccountIdentity = AccountID,
                    Result = MsgAccServerLoginExchangeEx<AccountServer>.ExchangeResult.KeyError
                });
            }
        }
    }
}