﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Ninja.Core.UnconfirmedBalances.Statuses
{
    public interface ITransactionStatus
    {
        string TxId { get; }

        DateTime Created { get; }

        DateTime Changed { get; }

        bool Removed { get; }

        InsertProcessStatus InsertProcessStatus { get; }
        RemoveProcessStatus RemoveProcessStatus { get; }
    }

    public class TransactionStatus : ITransactionStatus
    {
        public string TxId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Changed { get; set; }
        public bool Removed { get; set; }
        public InsertProcessStatus InsertProcessStatus { get; set; }
        public RemoveProcessStatus RemoveProcessStatus { get; set; }
        public static TransactionStatus Create(string txId)
        {
            return new TransactionStatus
            {
                Removed = false,
                Created = DateTime.Now,
                Changed = DateTime.Now,
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
        Task Upsert(IEnumerable<ITransactionStatus> items, CancellationToken cancellationToken);
        Task SetInsertStatus(IEnumerable<string> txIds, InsertProcessStatus status, CancellationToken cancellationToken);
        Task SetRemovedProcessingStatus(IEnumerable<string> txIds, RemoveProcessStatus status, CancellationToken cancellationToken);
        Task Remove(IEnumerable<string> txIds, RemoveProcessStatus status, CancellationToken cancellationToken);
        Task<IEnumerable<string>> GetAllTxIds();
        Task<long> GetAllTxCount();
        Task<IEnumerable<string>> GetNotRemovedTxIds(params InsertProcessStatus[] status);
        Task<long> GetNotRemovedTxCount(params InsertProcessStatus[] status);
        Task<IEnumerable<string>> GetRemovedTxIds(params RemoveProcessStatus[] status);
        Task UpdateExpiration(IEnumerable<string> txIds, CancellationToken cancellationToken);
    }
}
