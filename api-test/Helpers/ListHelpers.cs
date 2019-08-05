using System.Collections.Generic;
using Bogus;

namespace SQE.ApiTest.Helpers
{
    public static class ListHelpers
    {
        private static readonly Faker _faker = new Faker("en");

        /// <summary>
        /// This randomly selects an entity from a list and return it.
        /// </summary>
        /// <param name="list">List from which one entity will be randomly selected</param>
        /// <typeparam name="T">The type of the entities in the list</typeparam>
        /// <returns>Returns an entity of type T.</returns>
        public static int RandomIdx<T>(List<T> list)
        {
            return list.Count > 1 ? _faker.Random.Number(0, list.Count - 1) : 0;
        }
    }
}