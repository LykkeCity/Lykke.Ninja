using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Lykke.Ninja.Repositories.Mongo
{
    public static class MongoHelpers
    {
        public static async Task<bool> IsCollectionExists(this IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collectionCursor = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collectionCursor.AnyAsync();
        }
    }
}
