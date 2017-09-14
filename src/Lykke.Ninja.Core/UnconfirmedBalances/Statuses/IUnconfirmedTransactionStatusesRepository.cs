using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Ninja.Core.UnconfirmedBalances.Statuses
{
    public interface IUnconfirmedTransactionStatus
    {
        string TxId { get; } 

        DateTime Created { get; }

        DateTime LastStatusChange { get; }

        bool Confirmed { get; }

        UnconfirmedTransactionProcessingStatus Status { get; }
    }

    public class UnconfirmedTransactionStatus : IUnconfirmedTransactionStatus
    {
        public string TxId { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastStatusChange { get; set; }
        public bool Confirmed { get; set; }
        public UnconfirmedTransactionProcessingStatus Status { get; set; }

        public static UnconfirmedTransactionStatus Create(string txId)
        {
            return new UnconfirmedTransactionStatus
            {
                Confirmed = false,
                Created = DateTime.Now,
                LastStatusChange = DateTime.Now,
                TxId = txId,
                Status = UnconfirmedTransactionProcessingStatus.Started
            };
        }
    }

    public enum UnconfirmedTransactionProcessingStatus
    {
        Queued = 0,
        Started = 1,
        Done = 2, 
        Fail = 3
    }

    public interface IUnconfirmedTransactionStatusesRepository
    {
        Task Insert(IEnumerable<IUnconfirmedTransactionStatus> items);
        Task SetProcessingStatus(IEnumerable<string> txIds, UnconfirmedTransactionProcessingStatus status);
        Task Confirm(IEnumerable<string> txIds);
        Task<IEnumerable<string>> GetUnconfirmedIds();
    }
}
