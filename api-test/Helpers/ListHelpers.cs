using System.Collections.Generic;
using Bogus;

namespace SQE.ApiTest.Helpers
{
    public static class ListHelpers
    {
        private static readonly Faker _faker = new Faker("en");

        public static int RandomIdx<T>(List<T> list)
        {
            return list.Count > 1 ? _faker.Random.Number(0, list.Count - 1) : 0;
        }
    }
}