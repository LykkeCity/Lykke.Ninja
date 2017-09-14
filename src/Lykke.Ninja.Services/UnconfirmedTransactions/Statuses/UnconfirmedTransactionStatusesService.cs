using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;

namespace Lykke.Ninja.Services.UnconfirmedTransactions.Statuses
{
    public class SynchronizePlan : ISynchronizePlan
    {
        public IEnumerable<string> TxIdsToAdd { get; set; }
        public IEnumerable<string> TxIdsToRemove { get; set; }

        public static SynchronizePlan Create(IList<string> existedTxIds, IList<string> newTxIds)
        {
            var existedDic = existedTxIds.ToDictionary(p => p);
            var newDic = newTxIds.ToDictionary(p => p);

            var txIdsToAdd = newTxIds.Where(p => !existedDic.ContainsKey(p));
            var txIdsToRemove = existedTxIds.Where(p => !newDic.ContainsKey(p));

            return new SynchronizePlan
            {
                TxIdsToAdd = txIdsToAdd,
                TxIdsToRemove = txIdsToRemove
            };
        }
    }
    public class UnconfirmedTransactionStatusesService: IUnconfirmedTransactionStatusesService
    {
        private readonly IUnconfirmedTransactionStatusesRepository _repository;

        public UnconfirmedTransactionStatusesService(IUnconfirmedTransactionStatusesRepository repository)
        {
            _repository = repository;
        }

        public async Task<ISynchronizePlan> GetSynchronizePlan(IEnumerable<string> txIds)
        {
            var existed = await _repository.GetUnconfirmedIds();

            return  SynchronizePlan.Create(existed.Distinct().ToList(), txIds.Distinct().ToList());
        }

        public async Task Synchronize(ISynchronizePlan plan)
        {
            var insert = _repository.Insert(plan.TxIdsToAdd.Select(UnconfirmedTransactionStatus.Create));
            var update = _repository.Confirm(plan.TxIdsToRemove);

            await Task.WhenAll(insert, update);
        }
    }
}
