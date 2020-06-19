using System;
using System.Collections.Generic;
using System.Linq;

namespace Snowflake.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            SnowFlake snowFlake=new SnowFlake(1,1);
            List<long> list=new List<long>();
            for (int i = 1000 - 1; i >= 0; i--)
            {
                list.Add(snowFlake.NextId());
            }

            SnowFlake snowFlake1 = new SnowFlake(1, 2);
            for (int i = 1000 - 1; i >= 0; i--)
            {
                list.Add(snowFlake1.NextId());
              
            }

            SnowFlake snowFlake2 = new SnowFlake(1, 3);
            for (int i = 1000 - 1; i >= 0; i--)
            {
                list.Add(snowFlake2.NextId());

            }

            Console.WriteLine(list.DistinctBy(p => p).Count());
            Console.WriteLine("Hello World!");
        }

  

    }

    public static class Test
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }

}
