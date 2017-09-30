using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Ninja.Core.UnconfirmedBalances.Statuses
{
    public interface ITransactionStatus
    {
        string TxId { get; }

        DateTime Created { get; }

        DateTime LastStatusChange { get; }

        bool Removed { get; }

        InsertProcessStatus InsertProcessStatus { get; }
        RemoveProcessStatus RemoveProcessStatus { get; }
    }

    public class TransactionStatus : ITransactionStatus
    {
        public string TxId { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastStatusChange { get; set; }
        public bool Removed { get; set; }
        public InsertProcessStatus InsertProcessStatus { get; set; }
        public RemoveProcessStatus RemoveProcessStatus { get; set; }
        public static TransactionStatus Create(string txId)
        {
            return new TransactionStatus
            {
                Removed = false,
                Created = DateTime.Now,
                LastStatusChange = DateTime.Now,
                TxId = txId,
                InsertProcessStatus = InsertProcessStatus.Waiting,
                RemoveProcessStatus = RemoveProcessStatus.Unconfirmed
            };
        }
    }

    public enum InsertProcessStatus
    {
        Waiting = 0,
        Processed = 1,
        Failed = 2
    }

    public enum RemoveProcessStatus
    {
        Waiting = 0,
        Processed = 1,
        Failed = 2,
        Unconfirmed = 3
    }

    public interface IUnconfirmedStatusesRepository
    {
        Task Upsert(IEnumerable<ITransactionStatus> items);
        Task SetInsertStatus(IEnumerable<string> txIds, InsertProcessStatus status);
        Task SetRemovedProcessingStatus(IEnumerable<string> txIds, RemoveProcessStatus status);
        Task Remove(IEnumerable<string> txIds, RemoveProcessStatus status);
        Task<IEnumerable<string>> GetAllTxIds();
        Task<IEnumerable<string>> GetNotRemovedTxIds(params InsertProcessStatus[] status);
        Task<IEnumerable<string>> GetRemovedTxIds(params RemoveProcessStatus[] status);
    }
}
