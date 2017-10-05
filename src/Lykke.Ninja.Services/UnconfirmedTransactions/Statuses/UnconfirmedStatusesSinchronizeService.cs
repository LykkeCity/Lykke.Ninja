using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;

namespace Lykke.Ninja.Services.UnconfirmedTransactions.Statuses
{
    public class StatusesSynchronizePlan : IStatusesSynchronizePlan
    {
        public IEnumerable<string> TxIdsToAdd { get; set; }
        public IEnumerable<string> TxIdsToRemove { get; set; }

        public static StatusesSynchronizePlan Create(IList<string> existedTxIds, IList<string> newTxIds)
        {
            var existedDic = existedTxIds.ToDictionary(p => p);
            var newDic = newTxIds.ToDictionary(p => p);

            var txIdsToAdd = newTxIds.Where(p => !existedDic.ContainsKey(p));
            var txIdsToRemove = existedTxIds.Where(p => !newDic.ContainsKey(p));

            return new StatusesSynchronizePlan
            {
                TxIdsToAdd = txIdsToAdd,
                TxIdsToRemove = txIdsToRemove
            };
        }
    }


    public class UnconfirmedStatusesSinchronizeService: IUnconfirmedStatusesSinchronizeService
    {
        private readonly IUnconfirmedStatusesRepository _repository;

        public UnconfirmedStatusesSinchronizeService(IUnconfirmedStatusesRepository repository)
        {
            _repository = repository;
        }

        public async Task<IStatusesSynchronizePlan> GetStatusesSynchronizePlan(IEnumerable<string> txIds)
        {
            var existed = await _repository.GetAllTxIds();

            return  StatusesSynchronizePlan.Create(existed.Distinct().ToList(), txIds.Distinct().ToList());
        }

        public async Task Synchronize(IStatusesSynchronizePlan plan)
        {
            var insert = _repository.Upsert(plan.TxIdsToAdd.Select(TransactionStatus.Create));
            var update = _repository.Remove(plan.TxIdsToRemove, RemoveProcessStatus.Waiting);

            await Task.WhenAll(insert, update);
        }
    }
}
